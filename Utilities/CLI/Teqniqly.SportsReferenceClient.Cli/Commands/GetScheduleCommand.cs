using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;
using Teqniqly.SportsReferenceClient.Common;

namespace Teqniqly.SportsReferenceClient.Cli.Commands
{
    /// <summary>
    /// Downloads a baseball season schedule page and writes it verbatim to a file.
    /// </summary>
    internal sealed class GetScheduleCommand : AsyncCommand<GetScheduleCommand.Settings>
    {
        private const int FirstSeason = 1871;
        private const int FailureExitCode = 1;
        private const int CanceledExitCode = 2;

        private readonly IScheduleClient _scheduleClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetScheduleCommand"/> class.
        /// </summary>
        /// <param name="scheduleClient">The schedule client used to fetch the page.</param>
        /// <exception cref="ArgumentNullException"><paramref name="scheduleClient"/> is null.</exception>
        public GetScheduleCommand(IScheduleClient scheduleClient)
        {
            ArgumentNullException.ThrowIfNull(scheduleClient);
            _scheduleClient = scheduleClient;
        }

        /// <summary>
        /// Options for the <c>schedule get</c> command.
        /// </summary>
        internal sealed class Settings : CommandSettings
        {
            /// <summary>Gets the season year to download.</summary>
            [CommandOption("--year")]
            [Description("Season year (1871..current UTC year).")]
            public required int Year { get; init; }

            /// <summary>Gets the output path for the raw <c>.shtml</c> file.</summary>
            [CommandOption("--file")]
            [Description("Output path for the raw .shtml file.")]
            public required string File { get; init; }

            /// <inheritdoc />
            public override ValidationResult Validate()
            {
                if (string.IsNullOrWhiteSpace(File))
                {
                    return ValidationResult.Error("--file is required.");
                }

                var currentYear = DateTime.UtcNow.Year;

                if (Year < FirstSeason || Year > currentYear)
                {
                    return ValidationResult.Error(
                        $"--year must be between {FirstSeason} and {currentYear}."
                    );
                }

                return ValidationResult.Success();
            }
        }

        /// <inheritdoc />
        protected override async Task<int> ExecuteAsync(
            CommandContext context,
            Settings settings,
            CancellationToken cancellationToken
        )
        {
            ArgumentNullException.ThrowIfNull(settings);
            return await DownloadAsync(settings, cancellationToken);
        }

        /// <summary>
        /// Fetches the schedule for <paramref name="settings"/> and writes it to the target file.
        /// </summary>
        /// <param name="settings">The validated command options.</param>
        /// <param name="cancellationToken">A token to cancel the download.</param>
        /// <returns>The process exit code: 0 on success, non-zero on failure or cancellation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is null.</exception>
        internal async Task<int> DownloadAsync(
            Settings settings,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(settings);

            // Write to a temp file in the target directory and atomically move it into place on
            // success, so a mid-copy failure or cancellation never leaves a partial file behind.
            string? tempFile = null;

            try
            {
                var stopwatch = Stopwatch.StartNew();

                await using var stream = await _scheduleClient.GetScheduleAsync(
                    settings.Year,
                    cancellationToken
                );

                var fullPath = Path.GetFullPath(settings.File);
                var directory = Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                tempFile = $"{fullPath}.download-{Guid.NewGuid():N}.tmp";

                long bytesWritten;

                await using (
                    var file = new FileStream(
                        tempFile,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None
                    )
                )
                {
                    await stream.CopyToAsync(file, cancellationToken);
                    bytesWritten = file.Length;
                }

                File.Move(tempFile, fullPath, overwrite: true);
                tempFile = null;

                stopwatch.Stop();

                AnsiConsole.MarkupLineInterpolated(
                    CultureInfo.CurrentCulture,
                    $"[green]Saved {bytesWritten:N0} bytes to[/] {settings.File} [green]in[/] {stopwatch.Elapsed.TotalSeconds:N2}s"
                );

                return 0;
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]Canceled.[/]");
                return CanceledExitCode;
            }
            catch (HttpRequestException ex)
            {
                AnsiConsole.MarkupLineInterpolated(
                    CultureInfo.CurrentCulture,
                    $"[red]Download failed:[/] {ex.Message}"
                );
                return FailureExitCode;
            }
            catch (Exception ex)
                when (ex
                        is IOException
                            or UnauthorizedAccessException
                            or NotSupportedException
                            or ArgumentException
                )
            {
                AnsiConsole.MarkupLineInterpolated(
                    CultureInfo.CurrentCulture,
                    $"[red]Could not write file:[/] {ex.Message}"
                );
                return FailureExitCode;
            }
            finally
            {
                if (tempFile is not null)
                {
                    TryDeleteTempFile(tempFile);
                }
            }
        }

        private static void TryDeleteTempFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best-effort cleanup; a leftover temp file is preferable to masking the
                // original failure with a delete error.
            }
        }
    }
}

using System.Text;
using NSubstitute;
using Spectre.Console;
using Teqniqly.SportsReferenceClient.Cli.Commands;
using Teqniqly.SportsReferenceClient.Common;

namespace Teqniqly.SportsReferenceClient.Cli.Tests
{
    public sealed class GetScheduleCommandTests : IDisposable
    {
        private readonly List<IDisposable> _disposables = [];
        private readonly List<string> _tempFiles = [];

        private MemoryStream Stream(string content)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            _disposables.Add(stream);
            return stream;
        }

        private string TempFile()
        {
            var path = Path.Combine(Path.GetTempPath(), $"sportsref-test-{Guid.NewGuid():N}.shtml");
            _tempFiles.Add(path);
            return path;
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }

            foreach (var file in _tempFiles.Where(File.Exists))
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void Constructor_NullScheduleClient_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GetScheduleCommand(null!));
        }

        [Fact]
        public async Task DownloadAsync_Success_WritesStreamToFileAndReturnsZero()
        {
            const string expected = "<!DOCTYPE html><html>2026 schedule</html>";
            var client = Substitute.For<IScheduleClient>();
            client
                .GetScheduleAsync(2026, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Stream>(Stream(expected)));
            var command = new GetScheduleCommand(client);
            var file = TempFile();
            var settings = new GetScheduleCommand.Settings { Year = 2026, File = file };

            var exitCode = await command.DownloadAsync(settings, CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.Equal(expected, await File.ReadAllTextAsync(file, CancellationToken.None));
        }

        [Fact]
        public async Task DownloadAsync_CreatesMissingTargetDirectory()
        {
            var client = Substitute.For<IScheduleClient>();
            client
                .GetScheduleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Stream>(Stream("<html/>")));
            var command = new GetScheduleCommand(client);
            var directory = Path.Combine(Path.GetTempPath(), $"sportsref-test-{Guid.NewGuid():N}");
            var file = Path.Combine(directory, "schedule.shtml");
            _tempFiles.Add(file);
            var settings = new GetScheduleCommand.Settings { Year = 2026, File = file };

            var exitCode = await command.DownloadAsync(settings, CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(file));
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public async Task DownloadAsync_HttpRequestException_ReturnsOneAndWritesNoFile()
        {
            var client = Substitute.For<IScheduleClient>();
            client
                .GetScheduleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns<Stream>(_ => throw new HttpRequestException("boom"));
            var command = new GetScheduleCommand(client);
            var file = TempFile();
            var settings = new GetScheduleCommand.Settings { Year = 2026, File = file };

            var exitCode = await command.DownloadAsync(settings, CancellationToken.None);

            Assert.Equal(1, exitCode);
            Assert.False(File.Exists(file));
        }

        [Fact]
        public async Task DownloadAsync_MidStreamFailure_ReturnsOneAndLeavesNoFile()
        {
            var brokenStream = new ThrowingStream(bytesBeforeThrow: 8);
            _disposables.Add(brokenStream);
            var client = Substitute.For<IScheduleClient>();
            client
                .GetScheduleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Stream>(brokenStream));
            var command = new GetScheduleCommand(client);
            var file = TempFile();
            var settings = new GetScheduleCommand.Settings { Year = 2026, File = file };

            var exitCode = await command.DownloadAsync(settings, CancellationToken.None);

            Assert.Equal(1, exitCode);
            Assert.False(File.Exists(file));
        }

        [Fact]
        public async Task DownloadAsync_YearOutOfRange_ReturnsOneWithYearMessage()
        {
            var client = Substitute.For<IScheduleClient>();
            client
                .GetScheduleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                // Mirror the ArgumentOutOfRangeException ScheduleClient throws for a bad year.
#pragma warning disable S3928 // paramName mimics the real "year" argument, not a local parameter.
                .Returns<Stream>(_ => throw new ArgumentOutOfRangeException("year"));
#pragma warning restore S3928
            var command = new GetScheduleCommand(client);
            var settings = new GetScheduleCommand.Settings { Year = 3000, File = TempFile() };

            var writer = new StringWriter();
            _disposables.Add(writer);
            var original = AnsiConsole.Console;
            AnsiConsole.Console = AnsiConsole.Create(
                new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) }
            );

            int exitCode;
            try
            {
                exitCode = await command.DownloadAsync(settings, CancellationToken.None);
            }
            finally
            {
                AnsiConsole.Console = original;
            }

            Assert.Equal(1, exitCode);
            Assert.Contains("Invalid year", writer.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task DownloadAsync_InvalidPath_ReturnsOne()
        {
            var client = Substitute.For<IScheduleClient>();
            client
                .GetScheduleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Stream>(Stream("<html/>")));
            var command = new GetScheduleCommand(client);
            var settings = new GetScheduleCommand.Settings
            {
                Year = 2026,
                File = "bad\0path.shtml",
            };

            var exitCode = await command.DownloadAsync(settings, CancellationToken.None);

            Assert.Equal(1, exitCode);
        }

        [Fact]
        public async Task DownloadAsync_Canceled_ReturnsTwo()
        {
            var client = Substitute.For<IScheduleClient>();
            client
                .GetScheduleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns<Stream>(_ => throw new OperationCanceledException());
            var command = new GetScheduleCommand(client);
            var settings = new GetScheduleCommand.Settings { Year = 2026, File = TempFile() };

            var exitCode = await command.DownloadAsync(settings, CancellationToken.None);

            Assert.Equal(2, exitCode);
        }

        [Fact]
        public async Task DownloadAsync_NullSettings_Throws()
        {
            var command = new GetScheduleCommand(Substitute.For<IScheduleClient>());

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                command.DownloadAsync(null!, CancellationToken.None)
            );
        }

        [Fact]
        public void Validate_WhitespaceFile_Fails()
        {
            var settings = new GetScheduleCommand.Settings { Year = 2026, File = "  " };

            Assert.False(settings.Validate().Successful);
        }

        [Fact]
        public void Validate_MissingYear_FailsWithRequiredMessage()
        {
            var settings = new GetScheduleCommand.Settings { File = "schedule.shtml" };

            var result = settings.Validate();

            Assert.False(result.Successful);
            Assert.Equal("--year is required.", result.Message);
        }

        [Theory]
        [InlineData(1870)]
        [InlineData(9999)]
        public void Validate_YearOutOfRange_Fails(int year)
        {
            var settings = new GetScheduleCommand.Settings { Year = year, File = "schedule.shtml" };

            Assert.False(settings.Validate().Successful);
        }

        [Fact]
        public void Validate_ValidYearAndFile_Succeeds()
        {
            var settings = new GetScheduleCommand.Settings
            {
                Year = DateTime.UtcNow.Year,
                File = "schedule.shtml",
            };

            Assert.True(settings.Validate().Successful);
        }

        private sealed class ThrowingStream : Stream
        {
            private int _remaining;

            public ThrowingStream(int bytesBeforeThrow)
            {
                _remaining = bytesBeforeThrow;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_remaining <= 0)
                {
                    throw new IOException("stream broke mid-copy");
                }

                var n = Math.Min(count, _remaining);
                Array.Clear(buffer, offset, n);
                _remaining -= n;
                return n;
            }

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();
        }
    }
}

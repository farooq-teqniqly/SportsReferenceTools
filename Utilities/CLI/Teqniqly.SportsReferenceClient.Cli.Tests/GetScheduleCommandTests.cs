using System.Text;
using NSubstitute;
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
    }
}

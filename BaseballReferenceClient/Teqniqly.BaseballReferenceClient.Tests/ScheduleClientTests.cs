using System.Net;
using System.Text;
using NSubstitute;
using Teqniqly.SportsReferenceClient.Common.Tests;

namespace Teqniqly.BaseballReferenceClient.Tests
{
    public sealed class ScheduleClientTests : IDisposable
    {
        private static readonly Uri BaseAddress = new("https://example.test/");

        private readonly List<IDisposable> _disposables = [];

        private (ScheduleClient client, TestHttpMessageHandler handler) CreateClient()
        {
            var handler = Substitute.For<TestHttpMessageHandler>();
            var httpClient = new HttpClient(handler) { BaseAddress = BaseAddress };
            _disposables.Add(httpClient); // also disposes the handler
            return (new ScheduleClient(httpClient), handler);
        }

        private HttpResponseMessage Response(HttpStatusCode status, string? body = null)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body ?? string.Empty, Encoding.UTF8),
            };
            _disposables.Add(response);
            return response;
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }

        [Fact]
        public void Constructor_NullHttpClient_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ScheduleClient(null!));
        }

        [Theory]
        [InlineData(1870)] // one before the first MLB season (1871)
        [InlineData(int.MinValue)]
        public async Task GetScheduleAsync_YearBeforeFirstSeason_Throws(int year)
        {
            var (client, _) = CreateClient();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                client.GetScheduleAsync(year, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetScheduleAsync_YearAfterCurrent_Throws()
        {
            var (client, _) = CreateClient();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                client.GetScheduleAsync(DateTime.UtcNow.Year + 1, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetScheduleAsync_BoundaryYears_DoNotThrow()
        {
            var (client, handler) = CreateClient();
            handler
                .MockSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Response(HttpStatusCode.OK));

            Assert.NotNull(await client.GetScheduleAsync(1871, CancellationToken.None));
            Assert.NotNull(
                await client.GetScheduleAsync(DateTime.UtcNow.Year, CancellationToken.None)
            );
        }

        [Theory]
        [InlineData(2024)]
        [InlineData(1998)]
        public async Task GetScheduleAsync_SendsGetToExpectedUri(int year)
        {
            var (client, handler) = CreateClient();
            HttpRequestMessage? captured = null;

            handler
                .MockSendAsync(
                    Arg.Do<HttpRequestMessage>(r => captured = r),
                    Arg.Any<CancellationToken>()
                )
                .Returns(Response(HttpStatusCode.OK));

            await client.GetScheduleAsync(year, CancellationToken.None);

            Assert.NotNull(captured);
            Assert.Equal(HttpMethod.Get, captured!.Method);
            Assert.Equal(new Uri(BaseAddress, $"{year}-schedule.shtml"), captured.RequestUri);
        }

        [Fact]
        public async Task GetScheduleAsync_SuccessResponse_ReturnsStreamContent()
        {
            const string expected = "<html>schedule</html>";
            var (client, handler) = CreateClient();

            handler
                .MockSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Response(HttpStatusCode.OK, expected));

            await using var stream = await client.GetScheduleAsync(2024, CancellationToken.None);
            using var reader = new StreamReader(stream);
            var actual = await reader.ReadToEndAsync(CancellationToken.None);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task GetScheduleAsync_NonSuccess_ThrowsHttpRequestException()
        {
            var (client, handler) = CreateClient();

            handler
                .MockSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Response(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<HttpRequestException>(() =>
                client.GetScheduleAsync(2024, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetScheduleAsync_HonorsCancellationToken()
        {
            var (client, handler) = CreateClient();

            // The handler observes the token it receives; if ScheduleClient forwards the caller's
            // (cancelled) token, ThrowIfCancellationRequested fires. If it dropped the token, the
            // handler would see an uncancelled one and return OK instead.
            handler
                .MockSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    ci.Arg<CancellationToken>().ThrowIfCancellationRequested();
                    return Response(HttpStatusCode.OK);
                });

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                client.GetScheduleAsync(2024, cts.Token)
            );
        }
    }
}

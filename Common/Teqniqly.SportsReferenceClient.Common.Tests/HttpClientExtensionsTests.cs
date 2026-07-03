using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Teqniqly.SportsReferenceClient.Common.Tests
{
    public sealed class HttpClientExtensionsTests : IDisposable
    {
        private const string BaseAddressKey = "BaseAddresses:BaseballReference:ScheduleClient";
        private const string BaseAddressValue = "https://example.test/";

        private readonly List<IDisposable> _disposables = [];

        private static IConfiguration Configuration(string? baseAddress)
        {
            var values = new Dictionary<string, string?>();
            if (baseAddress is not null)
            {
                values[BaseAddressKey] = baseAddress;
            }

            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private (HttpClient client, TestHttpMessageHandler handler) CreateClient()
        {
            var handler = Substitute.For<TestHttpMessageHandler>();
            var httpClient = new HttpClient(handler);
            _disposables.Add(httpClient); // also disposes the handler
            return (httpClient, handler);
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
        public void Configure_NullClient_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                HttpClientExtensions.Configure(null!, Configuration(BaseAddressValue))
            );
        }

        [Fact]
        public void Configure_NullConfiguration_Throws()
        {
            var (client, _) = CreateClient();

            Assert.Throws<ArgumentNullException>(() => client.Configure(null!));
        }

        [Fact]
        public void Configure_MissingBaseAddress_Throws()
        {
            var (client, _) = CreateClient();

            Assert.Throws<InvalidOperationException>(() => client.Configure(Configuration(null)));
        }

        [Fact]
        public void Configure_AppliesBaseAddressAndHeaders()
        {
            var (client, _) = CreateClient();

            client.Configure(Configuration(BaseAddressValue));

            Assert.Equal(new Uri(BaseAddressValue), client.BaseAddress);

            var headers = client.DefaultRequestHeaders;
            var accept = string.Join(",", headers.GetValues("Accept"));

            Assert.Contains("text/html", accept, StringComparison.Ordinal);
            Assert.Contains("image/webp", accept, StringComparison.Ordinal);

            var acceptEncoding = string.Join(",", headers.GetValues("Accept-Encoding"));

            Assert.Contains("gzip", acceptEncoding, StringComparison.Ordinal);
            Assert.Contains("deflate", acceptEncoding, StringComparison.Ordinal);

            Assert.Contains(
                "en-US",
                string.Join(",", headers.GetValues("Accept-Language")),
                StringComparison.Ordinal
            );

            Assert.Contains(
                "Chrome/120.0.0.0",
                string.Join(" ", headers.GetValues("User-Agent")),
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void Configure_ReturnsSameClient()
        {
            var (client, _) = CreateClient();

            Assert.Same(client, client.Configure(Configuration(BaseAddressValue)));
        }

        [Fact]
        public async Task GetPageAsync_NullClient_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                HttpClientExtensions.GetPageAsync(
                    null!,
                    new Uri(BaseAddressValue),
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task GetPageAsync_NullUri_Throws()
        {
            var (client, _) = CreateClient();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                client.GetPageAsync(null!, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetPageAsync_SendsGetToUri()
        {
            var (client, handler) = CreateClient();
            var uri = new Uri("https://example.test/2024-schedule.shtml");
            HttpRequestMessage? captured = null;

            handler
                .MockSendAsync(
                    Arg.Do<HttpRequestMessage>(r => captured = r),
                    Arg.Any<CancellationToken>()
                )
                .Returns(Response(HttpStatusCode.OK));

            await client.GetPageAsync(uri, CancellationToken.None);

            Assert.NotNull(captured);
            Assert.Equal(HttpMethod.Get, captured!.Method);
            Assert.Equal(uri, captured.RequestUri);
        }

        [Fact]
        public async Task GetPageAsync_SuccessResponse_ReturnsStreamContent()
        {
            const string expected = "<html>page</html>";
            var (client, handler) = CreateClient();

            handler
                .MockSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Response(HttpStatusCode.OK, expected));

            await using var stream = await client.GetPageAsync(
                new Uri(BaseAddressValue),
                CancellationToken.None
            );
            using var reader = new StreamReader(stream);
            var actual = await reader.ReadToEndAsync(CancellationToken.None);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task GetPageAsync_NonSuccess_ThrowsHttpRequestException()
        {
            var (client, handler) = CreateClient();

            handler
                .MockSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Response(HttpStatusCode.NotFound));

            await Assert.ThrowsAsync<HttpRequestException>(() =>
                client.GetPageAsync(new Uri(BaseAddressValue), CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetPageAsync_HonorsCancellationToken()
        {
            var (client, handler) = CreateClient();

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
                client.GetPageAsync(new Uri(BaseAddressValue), cts.Token)
            );
        }
    }
}

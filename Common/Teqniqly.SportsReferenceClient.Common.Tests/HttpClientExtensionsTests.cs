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

        private static IConfiguration Configuration(
            string? baseAddress,
            string key = BaseAddressKey
        )
        {
            var values = new Dictionary<string, string?>();
            if (baseAddress is not null)
            {
                values[key] = baseAddress;
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
                HttpClientExtensions.Configure(
                    null!,
                    Configuration(BaseAddressValue),
                    BaseAddressKey
                )
            );
        }

        [Fact]
        public void Configure_NullConfiguration_Throws()
        {
            var (client, _) = CreateClient();

            Assert.Throws<ArgumentNullException>(() => client.Configure(null!, BaseAddressKey));
        }

        [Fact]
        public void Configure_MissingBaseAddress_Throws()
        {
            var (client, _) = CreateClient();

            Assert.Throws<InvalidOperationException>(() =>
                client.Configure(Configuration(null), BaseAddressKey)
            );
        }

        [Fact]
        public void Configure_AppliesBaseAddressAndHeaders()
        {
            var (client, _) = CreateClient();

            client.Configure(Configuration(BaseAddressValue), BaseAddressKey);

            Assert.Equal(new Uri(BaseAddressValue), client.BaseAddress);

            var headers = client.DefaultRequestHeaders;
            var accept = string.Join(",", headers.GetValues("Accept"));

            Assert.Contains("text/html", accept, StringComparison.Ordinal);
            Assert.Contains("image/webp", accept, StringComparison.Ordinal);

            // Accept-Encoding is intentionally not set here (handled by AutomaticDecompression).
            Assert.False(headers.Contains("Accept-Encoding"));

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
        public void Configure_CalledTwice_DoesNotDuplicateHeaders()
        {
            var (client, _) = CreateClient();

            client.Configure(Configuration(BaseAddressValue), BaseAddressKey);
            var headers = client.DefaultRequestHeaders;
            var acceptCount = headers.GetValues("Accept").Count();
            var languageCount = headers.GetValues("Accept-Language").Count();
            var userAgentCount = headers.GetValues("User-Agent").Count();

            client.Configure(Configuration(BaseAddressValue), BaseAddressKey);

            Assert.Equal(acceptCount, headers.GetValues("Accept").Count());
            Assert.Equal(languageCount, headers.GetValues("Accept-Language").Count());
            Assert.Equal(userAgentCount, headers.GetValues("User-Agent").Count());
        }

        [Fact]
        public void Configure_ReturnsSameClient()
        {
            var (client, _) = CreateClient();

            Assert.Same(client, client.Configure(Configuration(BaseAddressValue), BaseAddressKey));
        }

        [Fact]
        public void Configure_BaseAddressWithoutTrailingSlash_IsNormalized()
        {
            // A base without a trailing slash makes the relative "{year}-schedule.shtml" replace
            // the last path segment, so it must be normalized to end with "/".
            var (client, _) = CreateClient();

            client.Configure(Configuration("https://example.test/leagues/majors"), BaseAddressKey);

            Assert.Equal(new Uri("https://example.test/leagues/majors/"), client.BaseAddress);
        }

        [Fact]
        public void Configure_InvalidBaseAddress_Throws()
        {
            var (client, _) = CreateClient();

            Assert.Throws<InvalidOperationException>(() =>
                client.Configure(Configuration("not a valid uri"), BaseAddressKey)
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Configure_EmptyBaseAddress_ThrowsMissing(string value)
        {
            var (client, _) = CreateClient();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                client.Configure(Configuration(value), BaseAddressKey)
            );

            Assert.Contains("missing", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Configure_UsesProvidedKey()
        {
            const string customKey = "BaseAddresses:Custom:ScheduleClient";
            var (client, _) = CreateClient();

            client.Configure(Configuration(BaseAddressValue, customKey), customKey);

            Assert.Equal(new Uri(BaseAddressValue), client.BaseAddress);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Configure_NullOrWhitespaceKey_Throws(string? key)
        {
            var (client, _) = CreateClient();

            Assert.ThrowsAny<ArgumentException>(() =>
                client.Configure(Configuration(BaseAddressValue), key!)
            );
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

            await using var stream = await client.GetPageAsync(uri, CancellationToken.None);

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

        private HttpResponseMessage StreamResponse(HttpStatusCode status, Stream content)
        {
            var response = new HttpResponseMessage(status) { Content = new StreamContent(content) };
            _disposables.Add(response);
            return response;
        }

        [Fact]
        public async Task GetPageAsync_DisposingReturnedStream_DisposesResponse()
        {
            var (client, handler) = CreateClient();
            var body = new TrackingStream("<html>page</html>");
            handler
                .MockSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(StreamResponse(HttpStatusCode.OK, body));

            var stream = await client.GetPageAsync(
                new Uri(BaseAddressValue),
                CancellationToken.None
            );
            Assert.False(body.Disposed);

            await stream.DisposeAsync();

            Assert.True(body.Disposed);
        }

        [Fact]
        public async Task GetPageAsync_NonSuccess_DisposesResponse()
        {
            var (client, handler) = CreateClient();
            var body = new TrackingStream("not found");
            handler
                .MockSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(StreamResponse(HttpStatusCode.NotFound, body));

            await Assert.ThrowsAsync<HttpRequestException>(() =>
                client.GetPageAsync(new Uri(BaseAddressValue), CancellationToken.None)
            );

            Assert.True(body.Disposed);
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

        [Fact]
        public async Task GetPageAsync_ReadStreamThrows_DisposesResponse()
        {
            var (client, handler) = CreateClient();
            var content = new ThrowingContent();
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            _disposables.Add(response);
            handler
                .MockSendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(response);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.GetPageAsync(new Uri(BaseAddressValue), CancellationToken.None)
            );

            Assert.True(content.Disposed);
        }

        [Fact]
        public void Dispose_InnerThrows_StillDisposesResponse()
        {
            var body = new TrackingStream("x");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(body),
            };
            var stream = new ResponseOwningStream(response, new ThrowOnDisposeStream());

            Assert.ThrowsAny<Exception>(stream.Dispose);

            Assert.True(body.Disposed);
        }

        [Fact]
        public async Task DisposeAsync_InnerThrows_StillDisposesResponse()
        {
            var body = new TrackingStream("x");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(body),
            };
            var stream = new ResponseOwningStream(response, new ThrowOnDisposeStream());

            await Assert.ThrowsAnyAsync<Exception>(async () => await stream.DisposeAsync());

            Assert.True(body.Disposed);
        }

        [Fact]
        public async Task DisposeAsync_DisposesInnerExactlyOnce()
        {
            // Content is separate from inner so the count reflects only inner's own disposal.
            var inner = new DisposeCountingStream();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty),
            };
            _disposables.Add(response);
            var stream = new ResponseOwningStream(response, inner);

            await stream.DisposeAsync();

            Assert.Equal(1, inner.DisposeCount);
        }

        // A stream that counts how many times it is disposed, to prove disposal is not re-entered.
        private sealed class DisposeCountingStream : MemoryStream
        {
            public int DisposeCount { get; private set; }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    DisposeCount++;
                }

                base.Dispose(disposing);
            }
        }

        // Content whose read stream cannot be created, to exercise the ReadAsStreamAsync failure
        // path in GetPageAsync.
        private sealed class ThrowingContent : HttpContent
        {
            public bool Disposed { get; private set; }

            protected override Task SerializeToStreamAsync(
                Stream stream,
                TransportContext? context
            ) => throw new InvalidOperationException("boom");

            protected override Task<Stream> CreateContentReadStreamAsync(
                CancellationToken cancellationToken
            ) => throw new InvalidOperationException("boom");

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Disposed = true;
                }

                base.Dispose(disposing);
            }
        }

        // A stream that throws on disposal, to prove ResponseOwningStream still disposes the
        // response when the inner stream's disposal fails.
        private sealed class ThrowOnDisposeStream : MemoryStream
        {
            protected override void Dispose(bool disposing) =>
                throw new IOException("dispose boom");
        }
    }
}

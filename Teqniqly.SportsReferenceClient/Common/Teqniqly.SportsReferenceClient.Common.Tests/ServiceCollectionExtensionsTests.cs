using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Teqniqly.SportsReferenceClient.Common.Tests
{
    public sealed class ServiceCollectionExtensionsTests
    {
        private const string BaseAddressKey = "BaseAddresses:Test:ScheduleClient";
        private const string BaseAddressValue = "https://example.test/";
        private const string ClientName = nameof(ITestClient);

        private interface ITestClient;

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S1144:Unused private types or members should be removed",
            Justification = "Constructed by the typed HttpClient factory via reflection."
        )]
        private sealed class TestClient : ITestClient
        {
            public TestClient(HttpClient httpClient)
            {
                HttpClient = httpClient;
            }

            public HttpClient HttpClient { get; }
        }

        private static IConfiguration Configuration(string? baseAddress)
        {
            var values = new Dictionary<string, string?>();
            if (baseAddress is not null)
            {
                values[BaseAddressKey] = baseAddress;
            }

            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static ServiceProvider BuildProvider(string? baseAddress)
        {
            return new ServiceCollection()
                .AddSportsReferenceHttpClient<ITestClient, TestClient>(
                    Configuration(baseAddress),
                    BaseAddressKey
                )
                .BuildServiceProvider();
        }

        [Fact]
        public void AddSportsReferenceHttpClient_NullServices_Throws()
        {
            IServiceCollection services = null!;

            Assert.Throws<ArgumentNullException>(() =>
                services.AddSportsReferenceHttpClient<ITestClient, TestClient>(
                    Configuration(BaseAddressValue),
                    BaseAddressKey
                )
            );
        }

        [Fact]
        public void AddSportsReferenceHttpClient_NullConfiguration_Throws()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() =>
                services.AddSportsReferenceHttpClient<ITestClient, TestClient>(
                    null!,
                    BaseAddressKey
                )
            );
        }

        [Fact]
        public void AddSportsReferenceHttpClient_MissingBaseAddress_ThrowsOnResolve()
        {
            // The configure delegate runs lazily, when the typed client is instantiated -- not
            // at registration time. So the missing-key failure only surfaces on resolve.
            using var provider = BuildProvider(baseAddress: null);
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            Assert.Throws<InvalidOperationException>(() => factory.CreateClient(ClientName));
        }

        [Fact]
        public void AddSportsReferenceHttpClient_ValidConfig_AppliesBaseAddress()
        {
            using var provider = BuildProvider(BaseAddressValue);
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            using var client = factory.CreateClient(ClientName);

            Assert.Equal(new Uri(BaseAddressValue), client.BaseAddress);
        }

        [Fact]
        public void AddSportsReferenceHttpClient_ValidConfig_ResolvesTypedClient()
        {
            using var provider = BuildProvider(BaseAddressValue);

            var client = provider.GetRequiredService<ITestClient>();

            Assert.IsType<TestClient>(client);
        }

        [Fact]
        public void CreatePrimaryHandler_EnablesGzipDeflateAndBrotliDecompression()
        {
            using var handler = ServiceCollectionExtensions.CreatePrimaryHandler();

            var decompression = Assert.IsType<SocketsHttpHandler>(handler).AutomaticDecompression;

            Assert.True(decompression.HasFlag(System.Net.DecompressionMethods.GZip));
            Assert.True(decompression.HasFlag(System.Net.DecompressionMethods.Deflate));
            Assert.True(decompression.HasFlag(System.Net.DecompressionMethods.Brotli));
        }
    }
}

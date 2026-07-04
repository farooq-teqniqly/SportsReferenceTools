using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Teqniqly.SportsReferenceClient.Common;
using BaseballDi = Teqniqly.BaseballReferenceClient.ServiceCollectionExtensions;

namespace Teqniqly.BaseballReferenceClient.Tests
{
    public sealed class ServiceCollectionExtensionsTests
    {
        private const string BaseAddressKey = BaseballDi.ScheduleBaseAddressKey;
        private const string ClientName = nameof(IScheduleClient);
        private const string BaseAddressValue = "https://example.test/";

        private static IConfiguration BuildConfiguration(string? baseAddress)
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
                .AddBaseballReferenceClient(BuildConfiguration(baseAddress))
                .BuildServiceProvider();
        }

        [Fact]
        public void AddBaseballReferenceClient_NullServices_Throws()
        {
            IServiceCollection services = null!;

            Assert.Throws<ArgumentNullException>(() =>
                services.AddBaseballReferenceClient(BuildConfiguration(BaseAddressValue))
            );
        }

        [Fact]
        public void AddBaseballReferenceClient_NullConfiguration_Throws()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() => services.AddBaseballReferenceClient(null!));
        }

        [Fact]
        public void AddBaseballReferenceClient_MissingBaseAddress_ThrowsOnResolve()
        {
            // The configure delegate runs lazily, when the typed client is instantiated -- not
            // at registration time. So the missing-key failure only surfaces on resolve.
            using var provider = BuildProvider(baseAddress: null);
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            Assert.Throws<InvalidOperationException>(() => factory.CreateClient(ClientName));
        }

        [Fact]
        public void AddBaseballReferenceClient_ValidConfig_AppliesBaseAddressAndHeaders()
        {
            using var provider = BuildProvider(BaseAddressValue);
            var factory = provider.GetRequiredService<IHttpClientFactory>();

            using var client = factory.CreateClient(ClientName);

            Assert.Equal(new Uri(BaseAddressValue), client.BaseAddress);

            // HttpHeaders reformats typed headers (e.g. ";q=" -> "; q="), so assert the header is
            // present and carries a distinctive token rather than pinning the exact rendering.
            var headers = client.DefaultRequestHeaders;
            var accept = string.Join(",", headers.GetValues(HttpHeaderNames.Accept));

            Assert.Contains("text/html", accept, StringComparison.Ordinal);
            Assert.Contains("image/webp", accept, StringComparison.Ordinal);

            // Accept-Encoding is intentionally not set here (handled by AutomaticDecompression).
            Assert.False(headers.Contains(HttpHeaderNames.AcceptEncoding));

            Assert.Contains(
                "en-US",
                string.Join(",", headers.GetValues(HttpHeaderNames.AcceptLanguage)),
                StringComparison.Ordinal
            );

            Assert.Contains(
                "Chrome/120.0.0.0",
                string.Join(" ", headers.GetValues(HttpHeaderNames.UserAgent)),
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void AddBaseballReferenceClient_ValidConfig_ResolvesScheduleClient()
        {
            using var provider = BuildProvider(BaseAddressValue);

            var client = provider.GetRequiredService<IScheduleClient>();

            Assert.IsType<ScheduleClient>(client);
        }
    }
}

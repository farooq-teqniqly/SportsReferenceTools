using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Teqniqly.BaseballReferenceClient
{
    /// <summary>
    /// Dependency-injection registration for the Baseball Reference schedule client.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IScheduleClient"/> as a typed <see cref="HttpClient"/>, using the
        /// base address at configuration key
        /// <c>BaseAddresses:BaseballReference:ScheduleClient</c>.
        /// </summary>
        /// <param name="services">The service collection to add the client to.</param>
        /// <param name="configuration">The configuration supplying the client's base address.</param>
        /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The base-address configuration key is missing; thrown when the typed client is first
        /// resolved, not at registration time.
        /// </exception>
        public static IServiceCollection AddScheduleClient(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddHttpClient<IScheduleClient, ScheduleClient>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration["BaseAddresses:BaseballReference:ScheduleClient"]
                        ?? throw new InvalidOperationException(
                            "BaseAddresses:BaseballReference:ScheduleClient configuration is missing."
                        )
                );

                client.DefaultRequestHeaders.Add(
                    "Accept",
                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8"
                );

                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

                client.DefaultRequestHeaders.Add(
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
                );
            });

            return services;
        }
    }
}

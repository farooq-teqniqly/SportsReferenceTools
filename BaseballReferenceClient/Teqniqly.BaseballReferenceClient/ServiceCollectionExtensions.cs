using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Teqniqly.SportsReferenceClient.Common;

namespace Teqniqly.BaseballReferenceClient
{
    /// <summary>
    /// Dependency-injection registration for the Baseball Reference schedule client.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        private const string BaseAddressKey = "BaseAddresses:BaseballReference:ScheduleClient";

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
            return services.AddSportsReferenceHttpClient<IScheduleClient, ScheduleClient>(
                configuration,
                BaseAddressKey
            );
        }
    }
}

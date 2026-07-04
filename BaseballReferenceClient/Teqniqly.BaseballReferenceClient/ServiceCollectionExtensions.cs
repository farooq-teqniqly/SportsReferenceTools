using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Teqniqly.SportsReferenceClient.Common;

namespace Teqniqly.BaseballReferenceClient
{
    /// <summary>
    /// Dependency-injection registration for the Baseball Reference clients.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        private const string ScheduleBaseAddressKey =
            "BaseAddresses:BaseballReference:ScheduleClient";

        /// <summary>
        /// Registers all Baseball Reference clients as typed <see cref="HttpClient"/> instances.
        /// Currently the schedule client (<see cref="IScheduleClient"/>); further clients (e.g.
        /// players) are added here as they are introduced.
        /// </summary>
        /// <param name="services">The service collection to add the clients to.</param>
        /// <param name="configuration">The configuration supplying each client's base address.</param>
        /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="configuration"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// A client's base-address configuration key is missing or holds an invalid URI; thrown
        /// when that typed client is first resolved, not at registration time.
        /// </exception>
        public static IServiceCollection AddBaseballReferenceClient(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSportsReferenceHttpClient<IScheduleClient, ScheduleClient>(
                configuration,
                ScheduleBaseAddressKey
            );

            return services;
        }
    }
}

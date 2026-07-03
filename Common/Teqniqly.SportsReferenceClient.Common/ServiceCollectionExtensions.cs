using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Teqniqly.SportsReferenceClient.Common
{
    /// <summary>
    /// Dependency-injection registration shared by all sports-reference clients.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <typeparamref name="TClient"/> as a typed <see cref="HttpClient"/> whose base
        /// address and default headers are applied by <see cref="HttpClientExtensions.Configure"/>.
        /// </summary>
        /// <typeparam name="TClient">The client abstraction to register.</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation of <typeparamref name="TClient"/>.</typeparam>
        /// <param name="services">The service collection to add the client to.</param>
        /// <param name="configuration">The configuration supplying the client's base address.</param>
        /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The base-address configuration key is missing; thrown when the typed client is first
        /// resolved, not at registration time.
        /// </exception>
        public static IServiceCollection AddSportsReferenceHttpClient<TClient, TImplementation>(
            this IServiceCollection services,
            IConfiguration configuration
        )
            where TClient : class
            where TImplementation : class, TClient
        {
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddHttpClient<TClient, TImplementation>(client =>
            {
                client.Configure(configuration);
            });

            return services;
        }
    }
}

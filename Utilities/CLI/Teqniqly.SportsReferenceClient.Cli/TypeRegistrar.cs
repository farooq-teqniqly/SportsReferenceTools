using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Teqniqly.SportsReferenceClient.Cli
{
    /// <summary>
    /// Bridges Spectre.Console.Cli's <see cref="ITypeRegistrar"/> to a
    /// <see cref="IServiceCollection"/> so commands are constructed by Microsoft DI.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class TypeRegistrar : ITypeRegistrar
    {
        private readonly IServiceCollection _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
        /// </summary>
        /// <param name="services">The backing service collection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
        public TypeRegistrar(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            _services = services;
        }

        /// <inheritdoc />
        public ITypeResolver Build()
        {
            return new TypeResolver(_services.BuildServiceProvider());
        }

        /// <inheritdoc />
        public void Register(Type service, Type implementation)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(implementation);
            _services.AddSingleton(service, implementation);
        }

        /// <inheritdoc />
        public void RegisterInstance(Type service, object implementation)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(implementation);
            _services.AddSingleton(service, implementation);
        }

        /// <inheritdoc />
        public void RegisterLazy(Type service, Func<object> factory)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(factory);
            _services.AddSingleton(service, _ => factory());
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace Teqniqly.SportsReferenceClient.Cli
{
    /// <summary>
    /// Resolves command and dependency instances from the built
    /// <see cref="IServiceProvider"/> for Spectre.Console.Cli.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class TypeResolver : ITypeResolver, IDisposable
    {
        private readonly IServiceProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeResolver"/> class.
        /// </summary>
        /// <param name="provider">The service provider to resolve from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> is null.</exception>
        public TypeResolver(IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            _provider = provider;
        }

        /// <inheritdoc />
        public object? Resolve(Type? type)
        {
            return type is null ? null : _provider.GetService(type);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

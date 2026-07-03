using Microsoft.Extensions.Configuration;

namespace Teqniqly.SportsReferenceClient.Common
{
    /// <summary>
    /// <see cref="HttpClient"/> helpers for talking to sports-reference sites.
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Sets the client's base address from the configuration value at
        /// <paramref name="baseAddressKey"/> and adds the default browser-like request headers
        /// (Accept, Accept-Language, Accept-Encoding, User-Agent).
        /// </summary>
        /// <param name="client">The client to configure.</param>
        /// <param name="configuration">The configuration supplying the base address.</param>
        /// <param name="baseAddressKey">The configuration key holding the base address.</param>
        /// <returns>The same <paramref name="client"/> instance, for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="client"/> or <paramref name="configuration"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="baseAddressKey"/> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">
        /// No configuration value exists at <paramref name="baseAddressKey"/>.
        /// </exception>
        public static HttpClient Configure(
            this HttpClient client,
            IConfiguration configuration,
            string baseAddressKey
        )
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(baseAddressKey);

            client.BaseAddress = new Uri(
                configuration[baseAddressKey]
                    ?? throw new InvalidOperationException(
                        $"Configuration key '{baseAddressKey}' is missing."
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

            return client;
        }

        /// <summary>
        /// Issues a GET to <paramref name="uri"/> and returns the response body as a stream.
        /// </summary>
        /// <param name="client">The client to send the request with.</param>
        /// <param name="uri">The page URI to fetch.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>
        /// A stream over the response content. The caller owns the returned stream and must
        /// dispose it.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="client"/> or <paramref name="uri"/> is null.
        /// </exception>
        /// <exception cref="HttpRequestException">The response status is not a success code.</exception>
        public static async Task<Stream> GetPageAsync(
            this HttpClient client,
            Uri uri,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(uri);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Not disposed here: the returned stream is backed by the response content, so the
            // response must outlive this method. The caller owns the returned stream.
            var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var stream = await response
                .Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            return stream;
        }
    }
}

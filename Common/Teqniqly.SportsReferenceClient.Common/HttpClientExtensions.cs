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
        /// (Accept, Accept-Language, User-Agent). Accept-Encoding is left to the handler's
        /// automatic decompression.
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
        /// No configuration value exists at <paramref name="baseAddressKey"/>, or that value is
        /// not a valid absolute URI.
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

            var baseAddress =
                configuration[baseAddressKey]
                ?? throw new InvalidOperationException(
                    $"Configuration key '{baseAddressKey}' is missing."
                );

            // A trailing slash is required so a relative request URI resolves under the base path
            // rather than replacing its last segment.
            if (!baseAddress.EndsWith('/'))
            {
                baseAddress += "/";
            }

            if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException(
                    $"Configuration value at '{baseAddressKey}' is not a valid absolute URI: '{baseAddress}'."
                );
            }

            client.BaseAddress = baseUri;

            client.DefaultRequestHeaders.Add(
                "Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8"
            );

            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

            // Accept-Encoding is negotiated by the primary handler's AutomaticDecompression so the
            // response is transparently decompressed; adding it here would fight that.
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
        /// A stream over the response content, read from the network on demand (the body is not
        /// buffered into memory). The caller owns the returned stream and must dispose it;
        /// disposing it also disposes the underlying <see cref="HttpResponseMessage"/>.
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

            // ResponseHeadersRead so the body streams from the socket instead of being buffered.
            var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            Stream stream;

            try
            {
                response.EnsureSuccessStatusCode();
                stream = await response
                    .Content.ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                response.Dispose();
                throw;
            }

            // Ownership transfers to the caller: disposing the returned stream disposes the response.
            return new ResponseOwningStream(response, stream);
        }
    }
}

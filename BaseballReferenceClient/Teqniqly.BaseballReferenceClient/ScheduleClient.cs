namespace Teqniqly.BaseballReferenceClient
{
    /// <summary>
    /// Default <see cref="IScheduleClient"/> that fetches schedule pages over a typed
    /// <see cref="HttpClient"/>.
    /// </summary>
    internal sealed class ScheduleClient : IScheduleClient
    {
        private const int FirstSeason = 1871;

        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleClient"/> class.
        /// </summary>
        /// <param name="httpClient">The configured HTTP client, with its base address set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is null.</exception>
        public ScheduleClient(HttpClient httpClient)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        /// <exception cref="HttpRequestException">The response status is not a success code.</exception>
        public async Task<Stream> GetScheduleAsync(
            int year,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(year, FirstSeason);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(year, DateTime.UtcNow.Year);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{year}-schedule.shtml");

            // Not disposed here: the returned stream is backed by the response content, so the
            // response must outlive this method. The caller owns the returned stream.
            var response = await _httpClient
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

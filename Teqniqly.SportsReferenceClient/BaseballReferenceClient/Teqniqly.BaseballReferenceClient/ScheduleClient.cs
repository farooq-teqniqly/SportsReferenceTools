using Teqniqly.SportsReferenceClient.Common;

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

        /// <inheritdoc cref="IScheduleClient.GetScheduleAsync" />
        /// <remarks>
        /// The valid <paramref name="year"/> range is 1871 (the first MLB season) through the
        /// current UTC year, inclusive. The upper bound is UTC-based, so a caller east of UTC on
        /// the local New Year may need to wait until the season page exists.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is before 1871 or after the current UTC year.
        /// </exception>
        /// <exception cref="HttpRequestException">The response status is not a success code.</exception>
        public async Task<Stream> GetScheduleAsync(
            int year,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(year, FirstSeason);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(year, DateTime.UtcNow.Year);

            return await _httpClient
                .GetPageAsync(
                    new Uri($"{year}-schedule.shtml", UriKind.Relative),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
    }
}

namespace Teqniqly.SportsReferenceClient.Common
{
    /// <summary>
    /// Retrieves season schedule data from Baseball Reference.
    /// </summary>
    public interface IScheduleClient
    {
        /// <summary>
        /// Fetches the schedule page for the given season.
        /// </summary>
        /// <param name="year">
        /// The season year; must be between 1871 (the first MLB season) and the current year,
        /// inclusive.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>
        /// A stream over the schedule page content. The caller owns the returned stream and must
        /// dispose it.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is before 1871 or after the current year.
        /// </exception>
        Task<Stream> GetScheduleAsync(int year, CancellationToken cancellationToken = default);
    }
}

namespace Teqniqly.SportsReferenceClient.Common
{
    /// <summary>
    /// Retrieves season schedule data from a sports-reference site.
    /// </summary>
    public interface IScheduleClient
    {
        /// <summary>
        /// Fetches the schedule page for the given season.
        /// </summary>
        /// <param name="year">
        /// The season year. The valid range is defined by the implementing client.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>
        /// A stream over the schedule page content. The caller owns the returned stream and must
        /// dispose it.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is outside the range supported by the implementing client.
        /// </exception>
        Task<Stream> GetScheduleAsync(int year, CancellationToken cancellationToken = default);
    }
}

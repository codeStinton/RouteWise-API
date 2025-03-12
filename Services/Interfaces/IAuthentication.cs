namespace RouteWise.Services.Interfaces
{
    public interface IAuthentication
    {
        /// <summary>
        /// Retrieves a new access token or refreshes the existing one if needed.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the access token string.
        /// </returns>
        Task<string> GetOrRefreshAccessToken(CancellationToken cancellationToken = default);
    }
}

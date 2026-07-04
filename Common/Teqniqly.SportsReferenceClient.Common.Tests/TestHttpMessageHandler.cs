namespace Teqniqly.SportsReferenceClient.Common.Tests
{
    // HttpMessageHandler.SendAsync is protected, so NSubstitute cannot intercept it directly.
    // This abstract wrapper exposes a substitutable method the protected override delegates to.
    // Named MockSendAsync to avoid hiding the inherited HttpMessageHandler.Send.
    internal abstract class TestHttpMessageHandler : HttpMessageHandler
    {
        public abstract Task<HttpResponseMessage> MockSendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        );

        // sealed so the NSubstitute proxy cannot override SendAsync itself (which would return a
        // null Task and defeat the delegation); only MockSendAsync is substituted.
        protected sealed override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => MockSendAsync(request, cancellationToken);
    }
}

using System.Net;

namespace RouteWise.Tests.ServiceTests.Utilities
{
    public sealed class CountingHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, string> _payloadFactory;

        /// <summary>
        /// Gets the number of times the handler has been invoked.
        /// </summary>
        public int CallCount { get; private set; }

        /// <summary>
        /// Gets the HTTP request received by the handler.
        /// </summary>
        public HttpRequestMessage? LastRequest { get; private set; }

        /// <summary>
        /// Creates a new CountingHandler that returns the specified fixed JSON payload.
        /// </summary>
        public CountingHandler(string fixedPayload) : this(_ => fixedPayload) { }

        /// <summary>
        /// Creates a new CountingHandler that uses a delegate to generate the JSON response content.
        /// </summary>
        public CountingHandler(Func<HttpRequestMessage, string> payloadFactory) => _payloadFactory = payloadFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payloadFactory(request), System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}

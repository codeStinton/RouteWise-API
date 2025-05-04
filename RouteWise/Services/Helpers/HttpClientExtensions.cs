using System.Text;
using System.Text.Json;
using RouteWise.Exceptions;

namespace RouteWise.Services.Helpers
{
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Helper method for GET requests.
        /// </summary>
        public static Task<T> GetAsync<T>(
            this HttpClient httpClient,
            string url,
            string token,
            JsonSerializerOptions options,
            CancellationToken cancellationToken = default)
        {
            return httpClient.SendRequestAsync<T>(HttpMethod.Get, url, token, null, options, cancellationToken);
        }

        /// <summary>
        /// Helper method for POST requests.
        /// </summary>
        public static Task<T> PostAsync<T>(
            this HttpClient httpClient,
            string url,
            string token,
            string body,
            JsonSerializerOptions options,
            CancellationToken cancellationToken = default)
        {
            return httpClient.SendRequestAsync<T>(HttpMethod.Post, url, token, body, options, cancellationToken);
        }

        private static async Task<T> SendRequestAsync<T>(
            this HttpClient httpClient,
            HttpMethod method,
            string url,
            string token,
            string? body,
            JsonSerializerOptions options,
            CancellationToken cancellationToken = default)
        {
            // Amedeus API calls require an access token provided
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            // If the request is POST protocol
            if (method == HttpMethod.Post && !string.IsNullOrWhiteSpace(body))
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new FlightSearchException($"API error: {response.StatusCode}: {responseContent}");
            }

            var result = JsonSerializer.Deserialize<T>(responseContent, options)
                         ?? throw new FlightSearchException("Deserialization error: response content is null or invalid.");

            return result;
        }
    }
}

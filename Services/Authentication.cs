using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RouteWise.Models.Amadeus;
using RouteWise.Services.Endpoints;
using RouteWise.Services.Interfaces;

namespace RouteWise.Services
{
    public class Authentication : IAuthentication
    {
        private readonly HttpClient _httpClient;
        private readonly AmadeusSettings _settings;

        public Authentication(HttpClient httpClient, IOptions<AmadeusSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, AmadeusEndpoints.AuthenticationEndpoint);
            var content = new StringContent(
                $"grant_type=client_credentials&client_id={_settings.ClientId}&client_secret={_settings.ClientSecret}",
                Encoding.UTF8, "application/x-www-form-urlencoded"
            );
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement.GetProperty("access_token").GetString()!;
        }
    }
}
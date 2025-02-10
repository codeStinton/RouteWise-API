using System.Text.Json.Serialization;

namespace RouteWise.Models.Amadeus.V1
{
    public class FlightDestinationResponse
    {
        [JsonPropertyName("data")]
        public List<DestinationData> Data { get; set; } = new();
    }

    public class DestinationData
    {
        [JsonPropertyName("destination")]
        public string Destination { get; set; } = string.Empty;
    }
}
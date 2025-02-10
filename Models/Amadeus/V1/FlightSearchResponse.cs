using System.Text.Json.Serialization;

namespace RouteWise.Models.Amadeus.V1
{
    public class FlightSearchResponse
    {
        [JsonPropertyName("data")]
        public List<FlightDestination> Data { get; set; } = new();

        [JsonPropertyName("dictionaries")]
        public Dictionaries Dictionaries { get; set; } = new();

        [JsonPropertyName("meta")]
        public Meta Meta { get; set; } = new();
    }

    public class FlightDestination
    {
        [JsonPropertyName("origin")]
        public string Origin { get; set; } = string.Empty;

        [JsonPropertyName("destination")]
        public string Destination { get; set; } = string.Empty;

        [JsonPropertyName("departureDate")]
        public string DepartureDate { get; set; } = string.Empty;

        [JsonPropertyName("returnDate")]
        public string ReturnDate { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public Price Price { get; set; } = new();

        [JsonPropertyName("links")]
        public FlightLinks Links { get; set; } = new();
    }

    public class Price
    {
        [JsonPropertyName("total")]
        public string Total { get; set; } = string.Empty;
    }

    public class FlightLinks
    {
        [JsonPropertyName("flightDates")]
        public string FlightDates { get; set; } = string.Empty;

        [JsonPropertyName("flightOffers")]
        public string FlightOffers { get; set; } = string.Empty;
    }

    public class Dictionaries
    {
        [JsonPropertyName("currencies")]
        public Dictionary<string, string> Currencies { get; set; } = new();

        [JsonPropertyName("locations")]
        public Dictionary<string, LocationInfo> Locations { get; set; } = new();
    }

    public class LocationInfo
    {
        [JsonPropertyName("subType")]
        public string SubType { get; set; } = string.Empty;

        [JsonPropertyName("detailedName")]
        public string DetailedName { get; set; } = string.Empty;
    }

    public class Meta
    {
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("links")]
        public MetaLinks Links { get; set; } = new();

        [JsonPropertyName("defaults")]
        public MetaDefaults Defaults { get; set; } = new();
    }

    public class MetaLinks
    {
        [JsonPropertyName("self")]
        public string Self { get; set; } = string.Empty;
    }

    public class MetaDefaults
    {
        [JsonPropertyName("departureDate")]
        public string DepartureDate { get; set; } = string.Empty;

        [JsonPropertyName("oneWay")]
        public bool OneWay { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; } = string.Empty;

        [JsonPropertyName("nonStop")]
        public bool NonStop { get; set; }

        [JsonPropertyName("viewBy")]
        public string ViewBy { get; set; } = string.Empty;
    }
}

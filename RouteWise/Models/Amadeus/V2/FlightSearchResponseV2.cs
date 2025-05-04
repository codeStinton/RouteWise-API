using System.Text.Json.Serialization;

namespace RouteWise.Models.Amadeus.V2
{
    /// <summary>
    /// Gets or sets the list of Flight Search response data
    /// </summary>
    public class FlightSearchResponseV2
    {
        [JsonPropertyName("meta")]
        public Meta Meta { get; set; } = new();

        [JsonPropertyName("data")]
        public List<FlightOffer> Data { get; set; } = new();
    }

    public class Meta
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("links")]
        public MetaLinks Links { get; set; } = new();
    }

    public class MetaLinks
    {
        [JsonPropertyName("self")]
        public string Self { get; set; } = string.Empty;
    }

    public class FlightOffer
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("instantTicketingRequired")]
        public bool InstantTicketingRequired { get; set; }

        [JsonPropertyName("nonHomogeneous")]
        public bool NonHomogeneous { get; set; }

        [JsonPropertyName("oneWay")]
        public bool OneWay { get; set; }

        [JsonPropertyName("isUpsellOffer")]
        public bool IsUpsellOffer { get; set; }

        [JsonPropertyName("lastTicketingDate")]
        public string LastTicketingDate { get; set; } = string.Empty;

        [JsonPropertyName("lastTicketingDateTime")]
        public string LastTicketingDateTime { get; set; } = string.Empty;

        [JsonPropertyName("numberOfBookableSeats")]
        public int NumberOfBookableSeats { get; set; }

        [JsonPropertyName("itineraries")]
        public List<Itinerary> Itineraries { get; set; } = new();

        [JsonPropertyName("price")]
        public OfferPrice Price { get; set; } = new();

        [JsonPropertyName("pricingOptions")]
        public PricingOptions PricingOptions { get; set; } = new();

        [JsonPropertyName("validatingAirlineCodes")]
        public List<string> ValidatingAirlineCodes { get; set; } = new();

        [JsonPropertyName("travelerPricings")]
        public List<TravelerPricing> TravelerPricings { get; set; } = new();
    }

    public class Itinerary
    {
        [JsonPropertyName("duration")]
        public string Duration { get; set; } = string.Empty;

        [JsonPropertyName("segments")]
        public List<Segment> Segments { get; set; } = new();
    }

    public class Segment
    {
        [JsonPropertyName("departure")]
        public FlightEndpoint Departure { get; set; } = new();

        [JsonPropertyName("arrival")]
        public FlightEndpoint Arrival { get; set; } = new();

        [JsonPropertyName("carrierCode")]
        public string CarrierCode { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("aircraft")]
        public Aircraft Aircraft { get; set; } = new();

        [JsonPropertyName("operating")]
        public Operating Operating { get; set; } = new();

        [JsonPropertyName("duration")]
        public string Duration { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("numberOfStops")]
        public int NumberOfStops { get; set; }

        [JsonPropertyName("blacklistedInEU")]
        public bool BlacklistedInEU { get; set; }
    }

    public class FlightEndpoint
    {
        [JsonPropertyName("iataCode")]
        public string IataCode { get; set; } = string.Empty;

        [JsonPropertyName("terminal")]
        public string? Terminal { get; set; }

        [JsonPropertyName("at")]
        public string At { get; set; } = string.Empty;
    }

    public class Aircraft
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
    }

    public class Operating
    {
        [JsonPropertyName("carrierCode")]
        public string CarrierCode { get; set; } = string.Empty;
    }

    public class OfferPrice
    {
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("total")]
        public string Total { get; set; } = string.Empty;

        [JsonPropertyName("base")]
        public string Base { get; set; } = string.Empty;

        [JsonPropertyName("fees")]
        public List<Fee> Fees { get; set; } = new();

        [JsonPropertyName("grandTotal")]
        public string GrandTotal { get; set; } = string.Empty;

        [JsonPropertyName("additionalServices")]
        public List<AdditionalService>? AdditionalServices { get; set; }
    }

    public class Fee
    {
        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class AdditionalService
    {
        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class PricingOptions
    {
        [JsonPropertyName("fareType")]
        public List<string> FareType { get; set; } = new();

        [JsonPropertyName("includedCheckedBagsOnly")]
        public bool IncludedCheckedBagsOnly { get; set; }
    }

    public class TravelerPricing
    {
        [JsonPropertyName("travelerId")]
        public string TravelerId { get; set; } = string.Empty;

        [JsonPropertyName("fareOption")]
        public string FareOption { get; set; } = string.Empty;

        [JsonPropertyName("travelerType")]
        public string TravelerType { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public TravelerPrice Price { get; set; } = new();

        [JsonPropertyName("fareDetailsBySegment")]
        public List<FareDetailsBySegment> FareDetailsBySegment { get; set; } = new();
    }

    public class TravelerPrice
    {
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("total")]
        public string Total { get; set; } = string.Empty;

        [JsonPropertyName("base")]
        public string Base { get; set; } = string.Empty;
    }

    public class FareDetailsBySegment
    {
        [JsonPropertyName("segmentId")]
        public string SegmentId { get; set; } = string.Empty;

        [JsonPropertyName("cabin")]
        public string Cabin { get; set; } = string.Empty;

        [JsonPropertyName("fareBasis")]
        public string FareBasis { get; set; } = string.Empty;

        [JsonPropertyName("brandedFare")]
        public string BrandedFare { get; set; } = string.Empty;

        [JsonPropertyName("brandedFareLabel")]
        public string BrandedFareLabel { get; set; } = string.Empty;

        [JsonPropertyName("class")]
        public string Class { get; set; } = string.Empty;

        [JsonPropertyName("includedCheckedBags")]
        public IncludedCheckedBags IncludedCheckedBags { get; set; } = new();

        [JsonPropertyName("amenities")]
        public List<Amenity>? Amenities { get; set; }
    }

    public class IncludedCheckedBags
    {
        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; }
    }

    public class Amenity
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("isChargeable")]
        public bool? IsChargeable { get; set; }

        [JsonPropertyName("amenityType")]
        public string AmenityType { get; set; } = string.Empty;

        [JsonPropertyName("amenityProvider")]
        public AmenityProvider AmenityProvider { get; set; } = new();
    }

    public class AmenityProvider
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}

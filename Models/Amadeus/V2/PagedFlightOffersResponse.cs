namespace RouteWise.Models.Amadeus.V2
{
    public class PagedFlightOffersResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<SimpleFlightOffer> Flights { get; set; } = new();
    }
}

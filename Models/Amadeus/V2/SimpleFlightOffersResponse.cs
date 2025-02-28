namespace RouteWise.Models.Amadeus.V2
{
    public class SimpleFlightOffersResponse
    {
        public List<SimpleFlightOffer> Flights { get; set; } = new();
    }

    public class SimpleFlightOffer
    {
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string DepartureDate { get; set; } = string.Empty;
        public string ReturnDate { get; set; } = string.Empty;
        public List<SimpleLayover> Layovers { get; set; } = new();
    }

    public class SimpleLayover
    {
        public string Airport { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string ArrivalTimeOfPreviousFlight { get; set; } = string.Empty;
        public string DepartureTimeOfNextFlight { get; set; } = string.Empty;
    }
}

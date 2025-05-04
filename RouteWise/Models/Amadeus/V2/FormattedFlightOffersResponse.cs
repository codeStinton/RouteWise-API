namespace RouteWise.Models.Amadeus.V2
{
    public class FormattedFlightOffersResponse
    {
        /// <summary>
        /// Gets or sets the list of formatted Flight Search data
        /// </summary>
        public List<FormattedFlightOffer> Flights { get; set; } = new();


        /// <summary>
        /// Converts a raw flight search response into a formatted flight offers response.
        /// </summary>
        /// <param name="rawResponse">The raw flight search response containing detailed flight offer information.</param>
        /// <returns>
        /// A <see cref="FormattedFlightOffersResponse"/> object that represents the simplified flight offers with optional layover information.
        /// </returns>
        public static FormattedFlightOffersResponse ConvertToSimpleOffersWithLayovers(FlightSearchResponseV2 rawResponse)
        {
            var simpleResponse = new FormattedFlightOffersResponse();

            foreach (var offer in rawResponse.Data)
            {
                if (offer.Itineraries.Count == 0) continue;

                var firstItinerary = offer.Itineraries.First();
                var outboundFirstSegment = firstItinerary.Segments.FirstOrDefault();
                var outboundLastSegment = firstItinerary.Segments.LastOrDefault();
                if (outboundFirstSegment is null || outboundLastSegment is null) continue;

                var lastItinerary = offer.Itineraries.Last();
                var inboundLastSegment = (offer.Itineraries.Count > 1)
                    ? lastItinerary.Segments.LastOrDefault()
                    : null;

                var simpleOffer = new FormattedFlightOffer
                {
                    Origin = outboundFirstSegment.Departure.IataCode,
                    Destination = outboundLastSegment.Arrival.IataCode,
                    Price = offer.Price.GrandTotal,
                    DepartureDate = ParseDateOnly(outboundFirstSegment.Departure.At),
                    ReturnDate = inboundLastSegment != null
                        ? ParseDateOnly(inboundLastSegment.Arrival.At)
                        : string.Empty
                };

                foreach (var itinerary in offer.Itineraries)
                {
                    for (int i = 0; i < itinerary.Segments.Count - 1; i++)
                    {
                        var current = itinerary.Segments[i];
                        var next = itinerary.Segments[i + 1];

                        int layoverMins = CalculateLayoverDuration(current.Arrival.At, next.Departure.At);

                        simpleOffer.Layovers.Add(new FormattedLayover
                        {
                            Airport = current.Arrival.IataCode,
                            DurationMinutes = layoverMins,
                            ArrivalTimeOfPreviousFlight = current.Arrival.At,
                            DepartureTimeOfNextFlight = next.Departure.At
                        });
                    }
                }

                simpleResponse.Flights.Add(simpleOffer);
            }

            return simpleResponse;
        }

        private static int CalculateLayoverDuration(string arrivalTime, string nextDepartureTime)
        {
            DateTime arrival = DateTime.Parse(arrivalTime);
            DateTime nextDeparture = DateTime.Parse(nextDepartureTime);
            return (int)(nextDeparture - arrival).TotalMinutes;
        }

        private static string ParseDateOnly(string dateTime)
        {
            if (DateTime.TryParse(dateTime, out var parsed))
            {
                return parsed.ToString("yyyy-MM-dd");
            }
            return dateTime;
        }
    }

    public class FormattedFlightOffer
    {
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string DepartureDate { get; set; } = string.Empty;
        public string ReturnDate { get; set; } = string.Empty;
        public List<FormattedLayover> Layovers { get; set; } = new();
    }

    public class FormattedLayover
    {
        public string Airport { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string ArrivalTimeOfPreviousFlight { get; set; } = string.Empty;
        public string DepartureTimeOfNextFlight { get; set; } = string.Empty;
    }
}

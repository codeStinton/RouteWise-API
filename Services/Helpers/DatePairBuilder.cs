using RouteWise.DTOs.V2;

namespace RouteWise.Services.Helpers
{
    /// <summary>
    /// Provides helper methods for constructing date pairs for flight search requests.
    /// </summary>
    public static class DatePairBuilder
    {
        /// <summary>
        /// Determines whether the specified flight search request contains values for Year, Month, DepartureDayOfWeek, and ReturnDayOfWeek.
        /// </summary>
        public static bool HasYearMonthAndDays(FlightSearchRequestV2 request) =>
            request.Year.HasValue && request.Month.HasValue && request.DepartureDayOfWeek.HasValue && request.ReturnDayOfWeek.HasValue;

        /// <summary>
        /// Determines whether the specified flight search request contains values for Year and Month.
        /// </summary>
        public static bool HasYearAndMonth(FlightSearchRequestV2 request) => 
            request.Year.HasValue && request.Month.HasValue;

        /// <summary>
        /// Determines whether the specified flight search request contains a value for DurationDays.
        /// </summary>
        public static bool HasOnlyDurationDays(FlightSearchRequestV2 request) => 
            request.DurationDays.HasValue;

        /// <summary>
        /// Determines whether the specified flight search request contains non-empty departure and return date strings.
        /// </summary>
        public static bool HasDateValues(FlightSearchRequestV2 request) =>
            !string.IsNullOrWhiteSpace(request.DepartureDate) && !string.IsNullOrWhiteSpace(request.ReturnDate);


        /// <summary>
        /// Builds date pairs for a flight search request when the request specifies Year, Month, and specific days of week for departure and return.
        /// </summary>
        /// <param name="request">The flight search request.</param>
        /// <returns>
        /// A list of tuples representing date pairs, where each tuple contains a departure date and a corresponding return date.
        /// </returns>
        public static List<(string Departure, string? Return)> BuildDatePairsForYearMonthAndDays(FlightSearchRequestV2 request)
        {
            var pairs = new List<(string, string?)>();

            // Find the start and end date of the requested month
            DateTime startDate = new(request.Year!.Value, request.Month!.Value, 1);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
            {
                if (d.DayOfWeek != request.DepartureDayOfWeek!.Value)
                    continue;

                DateTime returnCandidate = GetNextDayOfWeek(d, request.ReturnDayOfWeek!.Value);
                if (returnCandidate <= endDate)
                {
                    pairs.Add((d.ToString("yyyy-MM-dd"), returnCandidate.ToString("yyyy-MM-dd")));
                }
            }

            return pairs;
        }

        /// <summary>
        /// Builds date pairs for a flight search request when the request specifies only Year and Month.
        /// </summary>
        /// <param name="request">The flight search request.</param>
        /// <returns>
        /// A list of tuples representing date pairs, where each tuple contains a departure date and a <c>null</c> return date.
        /// </returns>
        public static List<(string Departure, string? Return)> BuildDatePairsForYearAndMonth(FlightSearchRequestV2 request)
        {
            var pairs = new List<(string, string?)>();
            DateTime firstDay = new(request.Year!.Value, request.Month!.Value, 1);
            DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

            for (DateTime d = firstDay; d <= lastDay; d = d.AddDays(1))
            {
                pairs.Add((d.ToString("yyyy-MM-dd"), null));
            }

            return pairs;
        }

        /// <summary>
        /// Builds date pairs based on a specified duration in days.
        /// </summary>
        /// <param name="durationDays">The number of days between the departure and return dates.</param>
        /// <returns>
        /// A list of tuples representing date pairs, where each tuple contains a departure date and a return date calculated by adding the duration to the departure date.
        /// </returns>
        public static List<(string Departure, string? Return)> BuildDatePairsForDuration(int durationDays)
        {
            var pairs = new List<(string, string?)>();
            DateTime now = DateTime.UtcNow.Date;
            DateTime end = now.AddMonths(1).AddDays(-1);

            for (var d = now; d < end; d = d.AddDays(1))
            {
                pairs.Add((d.ToString("yyyy-MM-dd"), d.AddDays(durationDays).ToString("yyyy-MM-dd")));
            }

            return pairs;
        }

        /// <summary>
        /// Builds a default date pair with a departure date set 7 days from now and a return date 7 days after the departure.
        /// </summary>
        /// <returns>
        /// A list containing a single tuple representing the default departure and return dates.
        /// </returns>
        public static List<(string Departure, string? Return)> BuildDefaultDatePairs()
        {
            DateTime defaultDep = DateTime.UtcNow.AddDays(7);
            DateTime defaultRet = defaultDep.AddDays(7);
            return new List<(string, string?)> { (defaultDep.ToString("yyyy-MM-dd"), defaultRet.ToString("yyyy-MM-dd")) };
        }

        private static DateTime GetNextDayOfWeek(DateTime start, DayOfWeek targetDay)
        {
            int daysToAdd = ((int)targetDay - (int)start.DayOfWeek + 7) % 7;
            daysToAdd = daysToAdd == 0 ? 7 : daysToAdd;
            return start.AddDays(daysToAdd);
        }
    }
}
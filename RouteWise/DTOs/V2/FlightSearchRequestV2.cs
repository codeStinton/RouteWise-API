using RouteWise.Controllers.Defaults;
using System.ComponentModel.DataAnnotations;

namespace RouteWise.DTOs.V2
{
    /// <summary>
    /// Flight search request parameters for V2 endpoint.
    /// </summary>
    public class FlightSearchRequestV2
    {
        /// <summary>
        /// Required origin IATA code.
        /// </summary>
        public string Origin { get; set; } = default!;

        /// <summary>
        /// Destination IATA code.
        /// </summary>
        public string? Destination { get; set; }

        /// <summary>
        /// Search year.
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Search month.
        /// </summary>
        public int? Month { get; set; }

        /// <summary>
        /// Desired departure day of week.
        /// </summary>
        public DayOfWeek? DepartureDayOfWeek { get; set; }

        /// <summary>
        /// Desired return day of week.
        /// </summary>
        public DayOfWeek? ReturnDayOfWeek { get; set; }

        /// <summary>
        /// Specific departure date in YYYY-MM-DD format.
        /// </summary>
        public string? DepartureDate { get; set; }

        /// <summary>
        /// Specific return date in YYYY-MM-DD format.
        /// </summary>
        public string? ReturnDate { get; set; }

        /// <summary>
        /// Trip duration in days.
        /// </summary>
        public int? DurationDays { get; set; }

        /// <summary>
        /// Mminimum layover duration (in hours).
        /// </summary>
        public int? MinLayoverDuration { get; set; }

        /// <summary>
        /// Number of layovers allowed.
        /// </summary>
        public int? Layovers { get; set; }

        /// <summary>
        /// Maximum price.
        /// </summary>
        public int? MaxPrice { get; set; }

        /// <summary>
        /// Number of adults traveling (default 1).
        /// </summary>
        public int Adults { get; set; } = 1;

        /// <summary>
        /// Maximum number of results to be returned by the API (default from FlightSearchDefaults).
        /// </summary>
        [Range(1, 50)]
        public int Max { get; set; } = FlightSearchDefaults.ApiResultLimit;

        /// <summary>
        /// Total result limit (default from FlightSearchDefaults).
        /// </summary>
        /// 
        [Range(1, 50)]
        public int ResultLimit { get; set; } = FlightSearchDefaults.TotalResultLimit;

        public override string ToString()
        {
            var parts = new[]
            {
                $"Origin={Origin}",
                $"Destination={Destination ?? "null"}",
                $"Year={Year?.ToString() ?? "null"}",
                $"Month={Month?.ToString() ?? "null"}",
                $"DepartureDayOfWeek={DepartureDayOfWeek?.ToString() ?? "null"}",
                $"ReturnDayOfWeek={ReturnDayOfWeek?.ToString() ?? "null"}",
                $"DepartureDate={DepartureDate ?? "null"}",
                $"ReturnDate={ReturnDate ?? "null"}",
                $"DurationDays={DurationDays?.ToString() ?? "null"}",
                $"MinLayoverDuration={MinLayoverDuration?.ToString() ?? "null"}",
                $"Layovers={Layovers?.ToString() ?? "null"}",
                $"MaxPrice={MaxPrice?.ToString() ?? "null"}",
                $"Adults={Adults}",
                $"Max={Max}",
                $"ResultLimit={ResultLimit}"
            };

            return string.Join("_", parts);
        }

    }
}

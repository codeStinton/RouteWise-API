namespace RouteWise.DTOs.V1
{
    /// <summary>
    /// Flight search request parameters for V1 endpoint.
    /// </summary>
    public class FlightSearchRequestV1
    {
        /// <summary>
        /// Maximum price to filter.
        /// </summary>
        public int? MaxPrice { get; set; }

        /// <summary>
        /// True if only one-way flights, false if only round-trip flights, null if both.
        /// </summary>
        public bool? OneWay { get; set; }

        /// <summary>
        /// Departure date in YYYY-MM-DD format.
        /// </summary>
        public string? DepartureDate { get; set; }

        /// <summary>
        /// Duration in days (only used if one-way is true).
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// True if non-stop flights only, null otherwise.
        /// </summary>
        public bool? NonStop { get; set; }

        public override string ToString()
        {
            var parts = new[]
            {
                $"MaxPrice={MaxPrice?.ToString() ?? "null"}",
                $"OneWay={OneWay?.ToString() ?? "null"}",
                $"DepartureDate={DepartureDate ?? "null"}",
                $"Duration={Duration?.ToString() ?? "null"}",
                $"NonStop={NonStop?.ToString() ?? "null"}"
            };

            return string.Join("_", parts);
        }
    }
}

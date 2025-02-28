using Microsoft.AspNetCore.Mvc;
using RouteWise.Controllers.Defaults;
using RouteWise.Models.Amadeus.V1;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Services.Interfaces;

namespace RouteWise.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightSearchController : ControllerBase
    {
        private readonly IAmadeusService _amadeusService;

        public FlightSearchController(IAmadeusService amadeusService)
        {
            _amadeusService = amadeusService;
        }

        [HttpGet("search/{origin}")]
        public async Task<ActionResult<FlightSearchResponse>> GetFlights(
            string origin,
            [FromQuery] int? maxPrice = null,
            [FromQuery] bool? oneWay = null,
            [FromQuery] string? departureDate = null,
            [FromQuery] int? duration = null,
            [FromQuery] bool? nonStop = null)
        {
            var result = await _amadeusService.FlightSearch(origin, maxPrice, oneWay, departureDate, duration, nonStop);
            return Ok(result);
        }

        [HttpGet("explore")]
        public async Task<ActionResult<PagedFlightOffersResponse>> ExploreFlights(
            [FromQuery] string origin,
            [FromQuery] string? destination = null,

            // Day-of-week logic
            [FromQuery] int? year = null,
            [FromQuery] int? month = null,
            [FromQuery] DayOfWeek? departureDayOfWeek = null,
            [FromQuery] DayOfWeek? returnDayOfWeek = null,

            // Explicit date range logic
            [FromQuery] string? departureDate = null,
            [FromQuery] string? returnDate = null,

            // Separate approach for searching by just "duration" (e.g. 7 days)
            [FromQuery] int? durationDays = null,

            // Shared filter params
            [FromQuery] int? minLayoverDuration = null,
            [FromQuery] int? layovers = null,
            [FromQuery] int? maxPrice = null,
            [FromQuery] int adults = 1,
            [FromQuery] int max = 50,
            [FromQuery] int resultLimit = 10,

            // Pagination
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            SimpleFlightOffersResponse finalResults;

            // CASE A: day-of-week logic for a given year/month
            if (year.HasValue && month.HasValue
                && departureDayOfWeek.HasValue
                && returnDayOfWeek.HasValue)
            {
                finalResults = await _amadeusService.FlightSearchByDayOfWeekForMonthOptimized(
                    origin,
                    destination,
                    year.Value,
                    month.Value,
                    departureDayOfWeek.Value,
                    returnDayOfWeek.Value,
                    adults,
                    max,
                    minLayoverDuration,
                    layovers,
                    maxPrice,
                    resultLimit
                );
            }
            // CASE B: if user wants an entire month’s flights (no day-of-week specified)
            else if (year.HasValue && month.HasValue)
            {
                // This calls FlightSearchByMonth, which checks each day of the month
                finalResults = await _amadeusService.FlightSearchByMonth(
                    origin,
                    destination,
                    year.Value,
                    month.Value,
                    adults,
                    max,
                    minLayoverDuration,
                    layovers,
                    maxPrice,
                    resultLimit
                );
            }
            // CASE C: if user wants a certain duration (e.g. 7 days), 
            //         you handle it with a separate method
            else if (durationDays.HasValue)
            {
                finalResults = await _amadeusService.FlightSearchByDuration(
                    origin,
                    destination,
                    durationDays.Value,
                    adults,
                    max,
                    minLayoverDuration,
                    layovers,
                    maxPrice,
                    resultLimit
                );
            }
            // CASE D: if the user gave explicit departure & return dates
            else if (!string.IsNullOrWhiteSpace(departureDate)
                     && !string.IsNullOrWhiteSpace(returnDate))
            {
                finalResults = await _amadeusService.FlightSearchV2(
                    origin,
                    destination,
                    minLayoverDuration,
                    adults,
                    max,
                    layovers,
                    maxPrice,
                    resultLimit,
                    departureDate,
                    returnDate
                );
            }
            // CASE E: fallback if none of the above conditions matched
            else
            {
                // e.g. your "default" Explore logic or 
                // a simpler call to FlightSearchV2 with no explicit dates
                finalResults = await _amadeusService.FlightSearchV2(
                    origin,
                    destination,
                    minLayoverDuration,
                    adults,
                    max,
                    layovers,
                    maxPrice,
                    resultLimit
                );
            }

            // Pagination: clamp, then page
            var limitedFlights = finalResults.Flights.Take(resultLimit).ToList();
            var pagedFlights = limitedFlights
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new PagedFlightOffersResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = limitedFlights.Count,
                Flights = pagedFlights
            });
        }

        [HttpPost("multi-city")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<FlightSearchResponseV2>> MultiCitySearch([FromBody] MultiCitySearchRequest request)
        {
            var result = await _amadeusService.MultiCityFlightSearch(request);
            return Ok(result);
        }
    }
}
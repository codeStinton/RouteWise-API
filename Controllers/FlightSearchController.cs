using Microsoft.AspNetCore.Mvc;
using RouteWise.Models.Amadeus.V1;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Services.Interfaces;

namespace RouteWise.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightSearchController : ControllerBase
    {
        private readonly IV1FlightSearchService _amadeusService;

        public FlightSearchController(IV1FlightSearchService amadeusService)
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
        public async Task<ActionResult<List<SimpleFlightOffer>>> GetFlightsV2(
            [FromQuery] string origin,
            [FromQuery] string? destination = null,
            [FromQuery] int? year = null,
            [FromQuery] int? month = null,
            [FromQuery] DayOfWeek? departureDayOfWeek = null,
            [FromQuery] DayOfWeek? returnDayOfWeek = null,
            [FromQuery] string? departureDate = null,
            [FromQuery] string? returnDate = null,
            [FromQuery] int? durationDays = null,
            [FromQuery] int? minLayoverDuration = null,
            [FromQuery] int? layovers = null,
            [FromQuery] int? maxPrice = null,
            [FromQuery] int adults = 1,
            [FromQuery] int max = 50,
            [FromQuery] int resultLimit = 10
        )
        {
            var result = await _amadeusService.FlightSearchUnified(
                origin,
                destination,
                year,
                month,
                departureDayOfWeek,
                returnDayOfWeek,
                durationDays,
                departureDate,
                returnDate,
                minLayoverDuration,
                layovers,
                maxPrice,
                adults,
                max,
                resultLimit
            );

            return Ok(result.Flights);
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
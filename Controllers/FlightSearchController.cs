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

        [HttpGet("{origin}")]
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
        public async Task<ActionResult<FlightSearchResponseV2>> ExploreFlights(
            [FromQuery] string origin,
            [FromQuery] int duration = FlightSearchDefaults.DurationDays,
            [FromQuery] int minLayoverDuration = FlightSearchDefaults.MinimumLayoverMinutes)
        {
            var result = await _amadeusService.FlightSearchWithLayovers(origin, duration, minLayoverDuration);
            return Ok(result);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using RouteWise.DTOs.V1;
using RouteWise.DTOs.V2;
using RouteWise.Models.Amadeus.V1;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Services.Interfaces;

namespace RouteWise.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightSearchController : ControllerBase
    {
        private readonly IFlightSearchServiceV1 _amedeusSearchV1;
        private readonly IFlightSearchServiceV2 _amedeusSearchV2;
        private readonly IMultiCityServiceV2 _amedeusMultiCityV2;

        public FlightSearchController(IFlightSearchServiceV1 amedeusSearchV1, IFlightSearchServiceV2 amedeusSearchV2, IMultiCityServiceV2 amedeusMultiCityV2)
        {
            _amedeusSearchV1 = amedeusSearchV1;
            _amedeusSearchV2 = amedeusSearchV2;
            _amedeusMultiCityV2 = amedeusMultiCityV2;
        }

        /// <summary>
        /// Searches for flights (Amedeus API V1 version)
        /// </summary>
        /// <param name="origin">The 3-letter IATA origin code.</param>
        /// <param name="request">Additional search parameters.</param>
        /// <returns>A list of flights in <see cref="FlightSearchResponseV1"/> format.</returns>
        [HttpGet("search/{origin}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FlightSearchResponseV1))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<FlightSearchResponseV1>> GetFlights(string origin, [FromQuery] FlightSearchRequestV1 request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                return BadRequest("Origin is required.");
            }

            var result = await _amedeusSearchV1.FlightSearch(origin, request, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Searches for flights (Amedeus API V2 version)
        /// </summary>
        /// <param name="request">Flight search request parameters.</param>
        /// <returns>A list of flights in <see cref="FormattedFlightOffer"/> format.</returns>
        [HttpGet("explore")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<FormattedFlightOffer>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<FormattedFlightOffer>>> GetFlightsV2([FromQuery] FlightSearchRequestV2 request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Origin))
            {
                return BadRequest("Origin is required.");
            }

            var result = await _amedeusSearchV2.FlightSearch(request, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Searches for flights across multiple cities (multi-city search).
        /// </summary>
        /// <param name="request">Multi-city flight search request details.</param>
        /// <returns>A list of flights in <see cref="FlightSearchResponseV2"/> format.</returns>
        [HttpPost("multi-city")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FlightSearchResponseV2))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<FlightSearchResponseV2>> MultiCitySearch([FromBody] MultiCitySearchRequestV2 request, CancellationToken cancellationToken)
        {
            if (request is null || request.OriginDestinations.Count == 0)
            {
                return BadRequest("Invalid multi-city request: at least one origin-destination is required");
            }

            var result = await _amedeusMultiCityV2.MultiCityFlightSearch(request, cancellationToken);
            return Ok(result);
        }
    }
}
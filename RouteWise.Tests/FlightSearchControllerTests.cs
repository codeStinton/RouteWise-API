using Microsoft.AspNetCore.Mvc;
using Moq;
using RouteWise.Controllers;
using RouteWise.DTOs.V1;
using RouteWise.DTOs.V2;
using RouteWise.Models.Amadeus.V1;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Services.Interfaces;

namespace RouteWise.Tests
{
    [TestClass]
    public class FlightSearchControllerTests
    {
        private Mock<IFlightSearchServiceV1> _fightSearchServiceV1Mock;
        private Mock<IFlightSearchServiceV2> _fightSearchServiceV2Mock;
        private Mock<IMultiCityServiceV2> _multiCityServiceV2Mock;
        private FlightSearchController _flightSearchControllerMock;

        [TestInitialize]
        public void Setup()
        {
            _fightSearchServiceV1Mock = new Mock<IFlightSearchServiceV1>();
            _fightSearchServiceV2Mock = new Mock<IFlightSearchServiceV2>();
            _multiCityServiceV2Mock = new Mock<IMultiCityServiceV2>();

            _flightSearchControllerMock = new FlightSearchController(
                _fightSearchServiceV1Mock.Object,
                _fightSearchServiceV2Mock.Object,
                _multiCityServiceV2Mock.Object
            );
        }

        [TestMethod]
        public async Task GetFlights_EmptyOrigin_ReturnsBadRequest()
        {
            var result = await _flightSearchControllerMock.GetFlights(string.Empty, new FlightSearchRequestV1(), CancellationToken.None);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));

            _fightSearchServiceV1Mock.Verify(
                s => s.FlightSearch(It.IsAny<string>(), It.IsAny<FlightSearchRequestV1>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task GetFlights_ValidOrigin_ReturnsOk()
        {
            var origin = "BOS";
            var expected = new FlightSearchResponseV1();

            _fightSearchServiceV1Mock
                .Setup(s => s.FlightSearch(origin, It.IsAny<FlightSearchRequestV1>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _flightSearchControllerMock.GetFlights(origin, new FlightSearchRequestV1(), CancellationToken.None);

            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(expected, okResult.Value);

            _fightSearchServiceV1Mock.Verify(
                s => s.FlightSearch(origin, It.IsAny<FlightSearchRequestV1>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task GetFlightsV2_MissingOrigin_ReturnsBadRequest()
        {
            var request = new FlightSearchRequestV2 { Origin = "" };

            var result = await _flightSearchControllerMock.GetFlightsV2(request, CancellationToken.None);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));

            _fightSearchServiceV2Mock.Verify(
                s => s.FlightSearch(It.IsAny<FlightSearchRequestV2>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task GetFlightsV2_WithOrigin_ReturnsOk()
        {
            var request = new FlightSearchRequestV2 { Origin = "LON" };
            var expected = new List<FormattedFlightOffer>();

            _fightSearchServiceV2Mock
                .Setup(s => s.FlightSearch(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _flightSearchControllerMock.GetFlightsV2(request, CancellationToken.None);

            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(expected, okResult.Value);

            _fightSearchServiceV2Mock.Verify(
                s => s.FlightSearch(request, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task MultiCitySearch_NullRequest_ReturnsBadRequest()
        {
            var result = await _flightSearchControllerMock.MultiCitySearch(null, CancellationToken.None);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));

            _multiCityServiceV2Mock.Verify(
                s => s.MultiCityFlightSearch(It.IsAny<MultiCitySearchRequestV2>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task MultiCitySearch_EmptyOriginDestinations_ReturnsBadRequest()
        {
            // Arrange
            var request = new MultiCitySearchRequestV2 { OriginDestinations = [] };

            var result = await _flightSearchControllerMock.MultiCitySearch(request, CancellationToken.None);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));

            _multiCityServiceV2Mock.Verify(
                s => s.MultiCityFlightSearch(It.IsAny<MultiCitySearchRequestV2>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task MultiCitySearch_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new MultiCitySearchRequestV2
            {
                OriginDestinations = new List<OriginDestinationDto>
                {
                    new OriginDestinationDto
                    {
                        DestinationLocationCode = "PAR",
                        OriginLocationCode = "LDN"
                    }
                }
            };

            var expected = new FlightSearchResponseV2();

            _multiCityServiceV2Mock
                .Setup(s => s.MultiCityFlightSearch(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _flightSearchControllerMock.MultiCitySearch(request, CancellationToken.None);

            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(expected, okResult.Value);

            _multiCityServiceV2Mock.Verify(
                s => s.MultiCityFlightSearch(request, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

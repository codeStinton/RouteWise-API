using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using RouteWise.DTOs.V2;
using RouteWise.Exceptions;
using RouteWise.Models.Amadeus;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Services;
using RouteWise.Services.Interfaces;
using RouteWise.Tests.ServiceTests.Utilities;
using System.Text.Json;

namespace RouteWise.Tests.ServiceTests
{
    [TestClass]
    public class FlightSearchServiceV2Tests
    {
        private IMemoryCache _cache;
        private Mock<IAuthentication> _authMock;
        private JsonSerializerOptions _jsonOptions;
        private AmadeusSettings _settings;
        private FlightSearchRequestV2 _requestV2;

        [TestInitialize]
        public void Init()
        {
            _authMock = new Mock<IAuthentication>();
            _jsonOptions = new JsonSerializerOptions();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _settings = new AmadeusSettings { CacheDurationInMinutes = 30 };

            _authMock.Setup(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("token");

            _requestV2 = new FlightSearchRequestV2
            {
                Origin = "BOS",
                Destination = "MAD",
                DepartureDate = "2025-12-01",
                ReturnDate = "2025-12-10",
                Adults = 1,
                ResultLimit = 5,
                Max = 10
            };
        }

        [TestMethod]
        public async Task V2_FirstCall_ReturnsFlightsAndCaches()
        {
            var dummyOffer = new FlightOffer
            {
                Itineraries =
            [
                new Itinerary
                {
                    Segments =
                    [
                        new Segment
                        {
                            Departure = new FlightEndpoint { IataCode = "BOS", At = "2025-12-01T10:00:00" },
                            Arrival   = new FlightEndpoint { IataCode = "MAD", At = "2025-12-01T14:00:00" }
                        }
                    ]
                }
            ],
                Price = new OfferPrice { Total = "250.00" }
            };

            var response = new FlightSearchResponseV2 { Data = [dummyOffer] };

            var (service, handler) = ServiceTestHelper.CreateService(response, _jsonOptions,
                client => 
                new FlightSearchServiceV2(_cache, client, _authMock.Object,Options.Create(_jsonOptions),Options.Create(_settings)));

            var result = await service.FlightSearch(_requestV2, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var flightResult = result.First();
            Assert.AreEqual("BOS", flightResult.Origin);
            Assert.AreEqual("MAD", flightResult.Destination);
            Assert.AreEqual("2025-12-01", flightResult.DepartureDate);

            Assert.AreEqual(1, handler.CallCount);
            _authMock.Verify(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>()), Times.Once);
        }


        [TestMethod]
        public async Task V2_SecondCall_UsesCache_NoAdditionalApiCall()
        {
            var offer = new FlightOffer
            {
                Itineraries =
            [
                new Itinerary
                {
                    Segments =
                    [
                        new Segment
                        {
                            Departure = new FlightEndpoint { IataCode = "BOS", At = "2025-12-01T09:00:00" },
                            Arrival   = new FlightEndpoint { IataCode = "MAD", At = "2025-12-01T13:00:00" }
                        }
                    ]
                }
            ],
                Price = new OfferPrice { Total = "250.00" }
            };

            var (service, handler) = ServiceTestHelper.CreateService(new FlightSearchResponseV2 { Data = [offer] }, _jsonOptions,
                client => 
                new FlightSearchServiceV2(_cache, client, _authMock.Object, Options.Create(_jsonOptions), Options.Create(_settings)));

            var first = await service.FlightSearch(_requestV2, CancellationToken.None);
            var second = await service.FlightSearch(_requestV2, CancellationToken.None);

            Assert.AreEqual(1, handler.CallCount);
            Assert.AreEqual(first.Count, second.Count);
            _authMock.Verify(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>()), Times.Once);
        }


        [TestMethod]
        public async Task V2_NoOffers_ThrowsFlightSearchException()
        {
            var (service, _) = ServiceTestHelper.CreateService(new FlightSearchResponseV2 { Data = [] }, _jsonOptions,
                client => 
                new FlightSearchServiceV2(_cache, client, _authMock.Object, Options.Create(_jsonOptions), Options.Create(_settings)));

            await Assert.ThrowsExceptionAsync<FlightSearchException>(() => service.FlightSearch(_requestV2, CancellationToken.None));
        }
    }
}

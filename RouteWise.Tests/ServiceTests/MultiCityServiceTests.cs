using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using RouteWise.DTOs.V2;
using RouteWise.Models.Amadeus;
using RouteWise.Models.Amadeus.V2;
using RouteWise.Services;
using RouteWise.Services.Interfaces;
using RouteWise.Tests.ServiceTests.Utilities;
using System.Text.Json;

namespace RouteWise.Tests.ServiceTests
{
    [TestClass]
    public class MultiCityService
    {
        private IMemoryCache _cache;
        private Mock<IAuthentication> _authMock;
        private JsonSerializerOptions _jsonOptions;
        private AmadeusSettings _settings;

        private CountingHandler _handlerMulti;
        private MultiCityServiceV2 _multiCityService;
        private MultiCitySearchRequestV2 _requestMulti;

        [TestInitialize]
        public void Init()
        {
            _authMock = new Mock<IAuthentication>();
            _authMock.Setup(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("token");

            _jsonOptions = new JsonSerializerOptions();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _settings = new AmadeusSettings { CacheDurationInMinutes = 30 };

            var responseMulti = new FlightSearchResponseV2 { Data = [new FlightOffer()] };

            (_multiCityService, _handlerMulti) = ServiceTestHelper.CreateService(responseMulti, _jsonOptions, 
                client =>
                new MultiCityServiceV2(_cache, client, _authMock.Object, Options.Create(_jsonOptions), Options.Create(_settings)));

            _requestMulti = new MultiCitySearchRequestV2
            {
                OriginDestinations = [ new OriginDestinationDto
            {
                OriginLocationCode = "BOS",
                DestinationLocationCode = "MAD",
                DepartureDate = "2025-12-01"
            }],
                Travelers = [new TravelerDto { TravelerType = TravelerType.ADULT, FareOptions = ["STANDARD"] }],
                Sources = ["GDS"],
                SearchCriteria = new SearchCriteriaDto { MaxFlightOffers = 10 }
            };
        }

        [TestMethod]
        public async Task MultiCity_FirstCall_FetchesAndCaches()
        {
            var result = await _multiCityService.MultiCityFlightSearch(_requestMulti, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, _handlerMulti.CallCount);
            _authMock.Verify(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task MultiCity_SecondCall_UsesCache()
        {
            await _multiCityService.MultiCityFlightSearch(_requestMulti, CancellationToken.None);
            await _multiCityService.MultiCityFlightSearch(_requestMulti, CancellationToken.None);

            Assert.AreEqual(1, _handlerMulti.CallCount);
            _authMock.Verify(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>()), Times.Once);
        }


        [TestMethod]
        public async Task MultiCity_AuthenticationFailure_ThrowsException()
        {
            var badAuth = new Mock<IAuthentication>();
            badAuth.Setup(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Auth failed"));

            var client = new HttpClient(_handlerMulti) { BaseAddress = new Uri(ServiceTestHelper.TestBaseAddress) };
            var service = new MultiCityServiceV2(_cache, client, badAuth.Object, Options.Create(_jsonOptions), Options.Create(_settings));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.MultiCityFlightSearch(_requestMulti, CancellationToken.None));
        }
    }
}

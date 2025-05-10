using Microsoft.Extensions.Caching.Memory;
using Moq;
using RouteWise.DTOs.V1;
using RouteWise.Models.Amadeus;
using RouteWise.Services.Interfaces;
using RouteWise.Services;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RouteWise.Models.Amadeus.V1;
using RouteWise.Tests.ServiceTests.Utilities;

namespace RouteWise.Tests.ServiceTests
{
    [TestClass]
    public class FlightSearchServiceV1Tests
    {
        private IMemoryCache _cache;
        private Mock<IAuthentication> _authMock;
        private JsonSerializerOptions _jsonOptions;
        private AmadeusSettings _settings;

        private CountingHandler _handlerV1;
        private FlightSearchServiceV1 _serviceV1;
        private FlightSearchRequestV1 _requestV1;

        [TestInitialize]
        public void Init()
        {
            _authMock = new Mock<IAuthentication>();
            _jsonOptions = new JsonSerializerOptions();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _settings = new AmadeusSettings { CacheDurationInMinutes = 30 };

            _authMock.Setup(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("token");

            (_serviceV1, _handlerV1) = ServiceTestHelper.CreateService(new FlightSearchResponseV1(), _jsonOptions,
                client =>
                new FlightSearchServiceV1(_cache, client, _authMock.Object, Options.Create(_jsonOptions), Options.Create(_settings))); 
            
            _requestV1 = new FlightSearchRequestV1();
        }

        [TestMethod]
        public async Task FlightSearch_ReturnsDestinationsAndCaches()
        {
            var responseObj = new FlightSearchResponseV1 
            { 
                Data = [ new FlightDestination { Destination = "MAD", Price = new Price { Total = "250" } } ] 
            };

            var (service, handler) = ServiceTestHelper.CreateService(responseObj, _jsonOptions,
                client =>
                new FlightSearchServiceV1(_cache, client, _authMock.Object, Options.Create(_jsonOptions), Options.Create(_settings)));

            var request = new FlightSearchRequestV1 { MaxPrice = 500 };

            var result = await service.FlightSearch("BOS", request, CancellationToken.None);

            Assert.IsNotNull(result);

            var flightResult = result.Data.First();
            Assert.AreEqual("MAD", flightResult.Destination);
            Assert.AreEqual("250", flightResult.Price.Total);

            Assert.AreEqual(1, handler.CallCount);
            _authMock.Verify(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task FlightSearch_DuplicateCall_UsesCache()
        {
            var responseObj = new FlightSearchResponseV1
            {
                Data = [new FlightDestination { Destination = "MAD", Price = new Price { Total = "250" } }]
            };

            var (service, handler) = ServiceTestHelper.CreateService(responseObj, _jsonOptions,
                client => 
                new FlightSearchServiceV1(_cache, client, _authMock.Object, Options.Create(_jsonOptions), Options.Create(_settings)));

            var request = new FlightSearchRequestV1 { MaxPrice = 500 };

            await service.FlightSearch("BOS", request, CancellationToken.None);
            await service.FlightSearch("BOS", request, CancellationToken.None);

            Assert.AreEqual(1, handler.CallCount);
            _authMock.Verify(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task FlightSearch_CacheKeyDiffers_ForDifferentOrigins()
        {
            await _serviceV1.FlightSearch("BOS", _requestV1, CancellationToken.None);
            await _serviceV1.FlightSearch("LON", _requestV1, CancellationToken.None);
            Assert.AreEqual(2, _handlerV1.CallCount);
        }

        [TestMethod]
        public async Task FlightSearch_BuildsCorrectQueryString()
        {
            var customRequest = new FlightSearchRequestV1
            {
                MaxPrice = 500,
                OneWay = true,
                DepartureDate = "2025-12-01",
                Duration = 2,
                NonStop = true
            };

            await _serviceV1.FlightSearch("BOS", customRequest, CancellationToken.None);

            var query = _handlerV1.LastRequest!.RequestUri!.Query;
            StringAssert.Contains(query, "origin=BOS");
            StringAssert.Contains(query, "maxPrice=500");
            StringAssert.Contains(query, "oneWay=true");
            StringAssert.Contains(query, "duration=2");
            StringAssert.Contains(query, "nonStop=true");
            StringAssert.Contains(query, "departureDate=2025-12-01");
        }

        [TestMethod]
        public async Task FlightSearch_AuthenticationFailure_ThrowsException()
        {
            var failAuth = new Mock<IAuthentication>();
            failAuth.Setup(a => a.GetOrRefreshAccessToken(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Auth failed"));

            var client = new HttpClient(_handlerV1) { BaseAddress = new Uri(ServiceTestHelper.TestBaseAddress) };
            var service = new FlightSearchServiceV1(_cache, client, failAuth.Object, Options.Create(_jsonOptions), Options.Create(_settings));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.FlightSearch("BOS", _requestV1, CancellationToken.None));
        }
    }
}

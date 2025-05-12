using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RouteWise.Tests.ServiceTests.Utilities
{
    public static class ServiceTestHelper
    {
        /// <summary>
        /// The base address used for all test HttpClients.
        /// </summary>
        public const string TestBaseAddress = "https://api.example.com/";

        /// <summary>
        /// Creates a service instance using a raw JSON response payload.
        /// </summary>
        public static (TService service, CountingHandler handler) CreateService<TService>(
            string jsonPayload,
            Func<HttpClient, TService> factory)
            where TService : class
        {
            var handler = new CountingHandler(jsonPayload);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(TestBaseAddress)
            };
            return (factory(client), handler);
        }


        /// <summary>
        /// Creates a service instance by serializing a response object to JSON.
        /// </summary>
        public static (TService service, CountingHandler handler) CreateService<TService>(
            object responseObject,
            JsonSerializerOptions jsonOptions,
            Func<HttpClient, TService> factory)
            where TService : class
        {
            string json = JsonSerializer.Serialize(responseObject, jsonOptions);
            return CreateService(json, factory);
        }
    }
}

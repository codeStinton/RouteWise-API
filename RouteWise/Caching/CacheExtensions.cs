using Microsoft.Extensions.Caching.Memory;
using RouteWise.Caching;
using System.Text;

namespace RouteWise.Caching
{
    public static class CacheExtensions
    {
        /// <summary>
        /// Generates a cache key using a prefix and any number of key parts.
        /// </summary>
        public static string GenerateCacheKey(string prefix, params object?[] keyParts)
        {
            // Filter out null values and join remaining parts with an underscore.
            var key = new StringBuilder(prefix);
            foreach (var part in keyParts)
            {
                key.Append('_').Append(part?.ToString() ?? "null");
            }
            return key.ToString();
        }

        /// <summary>
        /// Retrieves an item from the cache or creates and caches it using the provided factory.
        /// </summary>
        public static async Task<T> GetOrCreateAsync<T>(
            this IMemoryCache cache,
            string key,
            TimeSpan absoluteExpirationRelativeToNow,
            Func<Task<T>> factory)
        {
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
                return await factory();
            });
        }

        /// <summary>
        /// Retrieves an item from the cache or creates and caches it using the provided factory.
        /// Allows more control over cache durations
        /// </summary>
        public static async Task<T> GetOrCreateWithEntryAsync<T>(
            this IMemoryCache cache,
            string key,
            Func<ICacheEntry, Task<T>> factory)
        {
            return await cache.GetOrCreateAsync(key, factory);
        }
    }
}

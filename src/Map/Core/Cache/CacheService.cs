using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Map.Core.Cache
{
    public class CacheService : ICacheService
    {
        private static readonly ConcurrentDictionary<string, bool> CacheKeys = new();
        private readonly IDistributedCache _distributedCache;

        public CacheService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellation = default) where T : class
        {
            string? cachedValue = await _distributedCache.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedValue) || cachedValue == "[]")
            {
                return null;
            }

            T? value = JsonConvert.DeserializeObject<T>(cachedValue);
            return value;
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            CacheKeys.TryRemove(key, out _);
        }

        public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
        {
            foreach (var key in CacheKeys.Keys.Where(k => k.StartsWith(prefixKey)).ToList())
            {
                await RemoveAsync(key, cancellationToken);
            }
        }

        public async Task SetAsync<T>(string key, T value, CancellationToken cancellation = default)
        {
            string cacheValue = JsonConvert.SerializeObject(value);
            await _distributedCache.SetStringAsync(key, cacheValue, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(60),
                SlidingExpiration = TimeSpan.FromDays(5)
            }, cancellation);

            CacheKeys.TryAdd(key, false);
        }
    }
}

using Microsoft.Extensions.Caching.Memory;

namespace UniCorn.Core
{
    public class InMemoryCache : ICache
    {
        IMemoryCache memoryCache;

        public InMemoryCache()
        {
            memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        public void AddOrUpdate(ICacheEntry cacheEntry)
        {
            memoryCache.Set(cacheEntry.Key, cacheEntry);
        }

        public void Remove(object key)
        {
            memoryCache.Remove(key);
        }

        public bool TryGetValue(object key, out ICacheEntry value)
        {
            return memoryCache.TryGetValue(key, out value);
        }
    }
}
using System;
using System.Threading.Tasks;

namespace UniCorn.Core
{
    public static class CacheExtensions
    {
        private static readonly object syncLock = new object();

        public static object Get(this ICache cache, object key)
        {
            return cache.Get<object>(key);
        }

        public static TItem Get<TItem>(this ICache cache, object key)
        {
            ICacheEntry result;
            return cache.TryGetValue(key, out result) ? (TItem)result.Value : default(TItem);
        }

        public static TItem GetOrAdd<TItem>(this ICache cache, object key, Func<ICacheEntry, TItem> factory)
        {
            lock (syncLock)
            {
                TItem obj = cache.Get<TItem>(key);

                if (obj == null && factory != null)
                {
                    ICacheEntry cacheEntry = new CacheEntry { Key = key };
                    obj = factory(cacheEntry);
                    cacheEntry.Value = obj;
                    cache.AddOrUpdate(cacheEntry);
                }

                return obj;
            }
        }

        public static Task<TItem> GetOrAddAsync<TItem>(this ICache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
        {
            lock (syncLock)
            {
                TItem obj = cache.Get<TItem>(key);

                if (obj == null && factory != null)
                {
                    ICacheEntry cacheEntry = new CacheEntry { Key = key };
                    Task<TItem> t = factory(cacheEntry);
                    t.Wait();
                    obj = t.Result;
                    cacheEntry.Value = obj;
                    cache.AddOrUpdate(cacheEntry);
                }

                return Task.FromResult(obj);
            }
        }

        public static void AddOrUpdate<TItem>(this ICache cache, object key, TItem value)
        {
            ICacheEntry cacheEntry = new CacheEntry { Key = key, Value = value };
            cache.AddOrUpdate(cacheEntry);
        }

        public static void AddOrUpdate<TItem>(this ICache cache, object key, TItem value, DateTimeOffset absoluteExpiration)
        {
            ICacheEntry cacheEntry = new CacheEntry { Key = key, Value = value, AbsoluteExpiration = absoluteExpiration };
            cache.AddOrUpdate(cacheEntry);
        }

        public static bool TryGetValue<TItem>(this ICache cache, object key, out TItem value)
        {
            ICacheEntry cacheEntry;
            if (cache.TryGetValue(key, out cacheEntry))
            {
                value = (TItem)cacheEntry.Value;
                return true;
            }
            value = default(TItem);
            return false;
        }
    }
}
namespace UniCorn.Core
{
    public interface ICache
    {
        void AddOrUpdate(ICacheEntry cacheEntry);
        void Remove(object key);
        bool TryGetValue(object key, out ICacheEntry value);
    }
}
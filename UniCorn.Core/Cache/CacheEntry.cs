using System;

namespace UniCorn.Core
{
    public class CacheEntry : ICacheEntry
    {
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public object Key { get; set; }
        public object Value { get; set; }
        public void Dispose()
        {
        }
    }
}
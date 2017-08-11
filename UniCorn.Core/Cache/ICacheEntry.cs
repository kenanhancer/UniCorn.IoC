using System;

namespace UniCorn.Core
{
    public interface ICacheEntry : IDisposable
    {
        //Gets or sets an absolute expiration date for the cache entry.
        DateTimeOffset? AbsoluteExpiration { get; set; }
        //Gets or sets how long a cache entry can be inactive (e.g. not accessed) before
        //it will be removed. This will not extend the entry lifetime beyond the absolute
        //expiration (if set).
        TimeSpan? SlidingExpiration { get; set; }
        object Key { get; }
        object Value { get; set; }
    }
}
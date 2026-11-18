using EventBooking.Application.Abstractions;
using EventBooking.Application.Dtos;
using EventBooking.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EventBooking.Infrastructure.Caching;

/// <summary>
/// Caches paged event listings. Invalidates via version bump when seat availability changes.
/// </summary>
public sealed class EventListCache(IMemoryCache memoryCache, ILogger<EventListCache> logger) : IEventListCache
{
    public const string VersionKey = "events:list:version";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public bool TryGet(EventListQuery query, out PagedResult<EventResponse>? result)
    {
        var key = BuildKey(query);
        if (memoryCache.TryGetValue(key, out PagedResult<EventResponse>? cached) && cached is not null)
        {
            logger.LogDebug("Event list cache hit for {CacheKey}", key);
            result = cached;
            return true;
        }

        logger.LogDebug("Event list cache miss for {CacheKey}", key);
        result = null;
        return false;
    }

    public void Set(EventListQuery query, PagedResult<EventResponse> result)
    {
        var key = BuildKey(query);
        memoryCache.Set(key, result, CacheDuration);
        logger.LogDebug("Cached event list for {CacheKey}", key);
    }

    public void Invalidate()
    {
        var nextVersion = memoryCache.GetOrCreate(VersionKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return 0L;
        }) + 1;

        memoryCache.Set(VersionKey, nextVersion);
        logger.LogInformation("Invalidated event list cache. New version {Version}", nextVersion);
    }

    private string BuildKey(EventListQuery query)
    {
        var version = memoryCache.GetOrCreate(VersionKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return 0L;
        });

        return CacheKeyBuilder.EventListKey(
            version,
            query.Page,
            query.PageSize,
            query.Name,
            query.From,
            query.To);
    }
}

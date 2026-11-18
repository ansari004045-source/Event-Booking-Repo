namespace EventBooking.Helpers;

public static class CacheKeyBuilder
{
    public static string EventListKey(
        long version,
        int page,
        int pageSize,
        string? name,
        DateTimeOffset? from,
        DateTimeOffset? to) =>
        $"events:list:v{version}:p{page}:s{pageSize}:n{name ?? "*"}:" +
        $"f{from?.UtcDateTime.Ticks.ToString() ?? "*"}:t{to?.UtcDateTime.Ticks.ToString() ?? "*"}";
}

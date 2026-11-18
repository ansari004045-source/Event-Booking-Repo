namespace EventBooking.Helpers;

public static class DateTimeUtc
{
    public static DateTime ToUtcDateTime(DateTimeOffset value) => value.UtcDateTime;

    public static DateTimeOffset FromUtcDateTime(DateTime utc) =>
        new(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}

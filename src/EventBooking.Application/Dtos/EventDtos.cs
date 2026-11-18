using EventBooking.Helpers;

namespace EventBooking.Application.Dtos;

public record CreateEventRequest(
    string Name,
    string? Description,
    DateTimeOffset StartsAt,
    int SeatLimit);

public record EventResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset StartsAt,
    int SeatLimit,
    int SeatsBooked,
    int SeatsAvailable);

public record EventListQuery(
    int Page = 1,
    int PageSize = 10,
    string? Name = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PagingHelper.TotalPages(TotalCount, PageSize);
}

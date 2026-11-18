using EventBooking.Application.Dtos;

namespace EventBooking.Application.Abstractions;

public interface IEventListCache
{
    bool TryGet(EventListQuery query, out PagedResult<EventResponse>? result);
    void Set(EventListQuery query, PagedResult<EventResponse> result);
    void Invalidate();
}

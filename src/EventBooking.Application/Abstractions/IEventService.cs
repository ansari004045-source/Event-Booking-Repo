using EventBooking.Application.Dtos;

namespace EventBooking.Application.Abstractions;

public interface IEventService
{
    Task<EventResponse> CreateAsync(CreateEventRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<EventResponse>> ListAsync(EventListQuery query, CancellationToken cancellationToken = default);
    Task<EventResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

using EventBooking.Application.Dtos;
using EventBooking.Domain.Entities;

namespace EventBooking.Application.Abstractions;

public interface IEventRepository
{
    Task AddAsync(Event entity, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(Guid id, bool tracking = true, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<EventResponse> Items, int TotalCount)> ListAsync(
        EventListQuery query,
        CancellationToken cancellationToken = default);
}

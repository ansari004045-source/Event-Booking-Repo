using EventBooking.Application.Abstractions;
using EventBooking.Application.Dtos;
using EventBooking.Application.Mapping;
using EventBooking.Domain.Entities;
using EventBooking.Helpers;
using Microsoft.Extensions.Logging;

namespace EventBooking.Application.Services;

public sealed class EventService(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IEventListCache listCache,
    ILogger<EventService> logger) : IEventService
{
    public async Task<EventResponse> CreateAsync(CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Event
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            StartsAt = DateTimeUtc.ToUtcDateTime(request.StartsAt),
            SeatLimit = request.SeatLimit,
            SeatsBooked = 0,
            RowVersion = 0
        };

        await eventRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        listCache.Invalidate();
        logger.LogInformation("Created event {EventId} with seat limit {SeatLimit}", entity.Id, entity.SeatLimit);

        return entity.ToResponse();
    }

    public async Task<PagedResult<EventResponse>> ListAsync(EventListQuery query, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = PagingHelper.Normalize(query.Page, query.PageSize);
        var normalized = query with { Page = page, PageSize = pageSize };

        if (listCache.TryGet(normalized, out var cached) && cached is not null)
        {
            return cached;
        }

        var (items, total) = await eventRepository.ListAsync(normalized, cancellationToken);
        var result = new PagedResult<EventResponse>(items, normalized.Page, normalized.PageSize, total);
        listCache.Set(normalized, result);
        return result;
    }

    public async Task<EventResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await eventRepository.GetByIdAsync(id, tracking: false, cancellationToken);
        return entity?.ToResponse();
    }
}

using EventBooking.Application.Abstractions;
using EventBooking.Application.Dtos;
using EventBooking.Domain.Entities;
using EventBooking.Helpers;
using EventBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventBooking.Infrastructure.Repositories;

public sealed class EventRepository(AppDbContext db) : IEventRepository
{
    public async Task AddAsync(Event entity, CancellationToken cancellationToken = default) =>
        await db.Events.AddAsync(entity, cancellationToken);

    public async Task<Event?> GetByIdAsync(Guid id, bool tracking = true, CancellationToken cancellationToken = default)
    {
        var query = tracking ? db.Events.AsQueryable() : db.Events.AsNoTracking();
        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<EventResponse> Items, int TotalCount)> ListAsync(
        EventListQuery query,
        CancellationToken cancellationToken = default)
    {
        var events = db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLowerInvariant();
            events = events.Where(e => e.Name.ToLower().Contains(name));
        }

        if (query.From is not null)
        {
            var fromUtc = DateTimeUtc.ToUtcDateTime(query.From.Value);
            events = events.Where(e => e.StartsAt >= fromUtc);
        }

        if (query.To is not null)
        {
            var toUtc = DateTimeUtc.ToUtcDateTime(query.To.Value);
            events = events.Where(e => e.StartsAt <= toUtc);
        }

        var total = await events.CountAsync(cancellationToken);
        var items = await events
            .OrderBy(e => e.StartsAt)
            .ThenBy(e => e.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new EventResponse(
                e.Id,
                e.Name,
                e.Description,
                new DateTimeOffset(e.StartsAt, TimeSpan.Zero),
                e.SeatLimit,
                e.SeatsBooked,
                e.SeatLimit - e.SeatsBooked))
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

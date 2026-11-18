using EventBooking.Application.Abstractions;
using EventBooking.Domain.Entities;
using EventBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventBooking.Infrastructure.Repositories;

public sealed class BookingRepository(AppDbContext db) : IBookingRepository
{
    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default) =>
        await db.Bookings.AddAsync(booking, cancellationToken);

    public async Task<Booking?> GetByIdWithEventAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Bookings
            .Include(b => b.Event)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<Booking?> GetByIdAsync(Guid id, bool tracking = false, CancellationToken cancellationToken = default)
    {
        var query = tracking ? db.Bookings.AsQueryable() : db.Bookings.AsNoTracking();
        return await query.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public void Detach(Booking booking) =>
        db.Entry(booking).State = EntityState.Detached;
}

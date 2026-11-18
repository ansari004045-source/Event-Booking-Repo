using EventBooking.Domain.Entities;

namespace EventBooking.Application.Abstractions;

public interface IBookingRepository
{
    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<Booking?> GetByIdWithEventAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Booking?> GetByIdAsync(Guid id, bool tracking = false, CancellationToken cancellationToken = default);
    void Detach(Booking booking);
}

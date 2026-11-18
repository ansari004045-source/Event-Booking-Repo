using EventBooking.Application.Dtos;

namespace EventBooking.Application.Abstractions;

public interface IBookingService
{
    Task<BookingResponse> BookAsync(Guid eventId, BookSeatsRequest request, CancellationToken cancellationToken = default);
    Task<BookingResponse> CancelAsync(Guid bookingId, CancellationToken cancellationToken = default);
}

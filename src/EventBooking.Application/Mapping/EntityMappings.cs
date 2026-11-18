using EventBooking.Application.Dtos;
using EventBooking.Domain.Entities;
using EventBooking.Helpers;

namespace EventBooking.Application.Mapping;

public static class EntityMappings
{
    public static EventResponse ToResponse(this Event entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Description,
            DateTimeUtc.FromUtcDateTime(entity.StartsAt),
            entity.SeatLimit,
            entity.SeatsBooked,
            entity.SeatsAvailable);

    public static BookingResponse ToResponse(this Booking booking) =>
        new(
            booking.Id,
            booking.EventId,
            booking.AttendeeName,
            booking.AttendeeEmail,
            booking.Seats,
            booking.BookedAt,
            booking.IsCancelled,
            booking.CancelledAt);
}

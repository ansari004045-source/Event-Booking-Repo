namespace EventBooking.Application.Dtos;

public record BookSeatsRequest(
    string AttendeeName,
    string AttendeeEmail,
    int Seats);

public record BookingResponse(
    Guid Id,
    Guid EventId,
    string AttendeeName,
    string AttendeeEmail,
    int Seats,
    DateTimeOffset BookedAt,
    bool IsCancelled,
    DateTimeOffset? CancelledAt);

namespace EventBooking.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public int Seats { get; set; }
    public DateTimeOffset BookedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsCancelled { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

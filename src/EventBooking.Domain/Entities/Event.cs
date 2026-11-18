namespace EventBooking.Domain.Entities;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>UTC start time.</summary>
    public DateTime StartsAt { get; set; }

    public int SeatLimit { get; set; }
    public int SeatsBooked { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Incremented on every successful seat change.
    /// </summary>
    public int RowVersion { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public int SeatsAvailable => SeatLimit - SeatsBooked;
}

using EventBooking.Application.Dtos;
using EventBooking.Application.Exceptions;
using EventBooking.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventBooking.Tests;

public class BookingServiceTests
{
    // Booking reserves seats, and cancelling returns those seats and clears the cached event list.
    [Fact]
    public async Task BookAsync_ReservesSeats_AndCancelReleasesThem()
    {
        await using var fixture = new SqlServerTestFixture();
        await using var db = fixture.CreateContext();
        var cache = fixture.CreateCache();
        var (events, bookings, _, _) = fixture.CreateServices(db, cache);

        var evt = await events.CreateAsync(new CreateEventRequest(
            "Demo Night", null, DateTimeOffset.UtcNow.AddDays(7), 10));

        var booking = await bookings.BookAsync(evt.Id, new BookSeatsRequest("Ada Lovelace", "ada@example.com", 3));
        booking.Seats.Should().Be(3);

        await using (var verifyDb = fixture.CreateContext())
        {
            var updated = await verifyDb.Events.SingleAsync(e => e.Id == evt.Id);
            updated.SeatsBooked.Should().Be(3);
            updated.SeatsAvailable.Should().Be(7);
        }

        cache.Set(new EventListQuery(1, 10), new PagedResult<EventResponse>([], 1, 10, 0));
        await bookings.CancelAsync(booking.Id);
        cache.TryGet(new EventListQuery(1, 10), out _).Should().BeFalse();

        await using (var verifyDb = fixture.CreateContext())
        {
            var updated = await verifyDb.Events.SingleAsync(e => e.Id == evt.Id);
            updated.SeatsBooked.Should().Be(0);

            var cancelled = await verifyDb.Bookings.SingleAsync(b => b.Id == booking.Id);
            cancelled.IsCancelled.Should().BeTrue();
            cancelled.CancelledAt.Should().NotBeNull();
        }
    }

    // A request for more seats than the event has left must be rejected as a conflict.
    [Fact]
    public async Task BookAsync_ThrowsConflict_WhenNotEnoughSeats()
    {
        await using var fixture = new SqlServerTestFixture();
        await using var db = fixture.CreateContext();
        var (events, bookings, _, _) = fixture.CreateServices(db);

        var evt = await events.CreateAsync(new CreateEventRequest(
            "Small Room", null, DateTimeOffset.UtcNow.AddDays(3), 2));

        var act = async () => await bookings.BookAsync(
            evt.Id, new BookSeatsRequest("Grace Hopper", "grace@example.com", 3));

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Not enough seats*");
    }

    // Simultaneous bookings for the last seats must succeed only up to the seat limit, so the event is not overbooked.
    [Fact]
    public async Task ConcurrentBookings_ForLastSeats_DoNotOverbook()
    {
        await using var fixture = new SqlServerTestFixture();

        Guid eventId;
        await using (var seedDb = fixture.CreateContext())
        {
            var evt = new Event
            {
                Name = "Last Seat Rush",
                StartsAt = DateTime.UtcNow.AddDays(2),
                SeatLimit = 10,
                SeatsBooked = 0,
                RowVersion = 0
            };
            seedDb.Events.Add(evt);
            await seedDb.SaveChangesAsync();
            eventId = evt.Id;
        }

        const int parallelRequests = 20;
        var tasks = Enumerable.Range(0, parallelRequests).Select(async i =>
        {
            await using var db = fixture.CreateContext();
            var (_, bookingService, _, _) = fixture.CreateServices(db);
            try
            {
                var booking = await bookingService.BookAsync(
                    eventId,
                    new BookSeatsRequest($"Guest {i}", $"guest{i}@example.com", 1));
                return (Succeeded: true, BookingId: (Guid?)booking.Id, Error: (string?)null);
            }
            catch (ConflictException ex)
            {
                return (Succeeded: false, BookingId: null, Error: ex.Message);
            }
        });

        var results = await Task.WhenAll(tasks);

        results.Count(r => r.Succeeded).Should().Be(10, "exactly the seat limit should succeed");
        results.Count(r => !r.Succeeded).Should().Be(10, "remaining requests should be rejected");

        await using var verifyDb = fixture.CreateContext();
        var finalEvent = await verifyDb.Events.SingleAsync(e => e.Id == eventId);
        finalEvent.SeatsBooked.Should().Be(10);
        finalEvent.SeatsAvailable.Should().Be(0);

        var activeBookings = await verifyDb.Bookings.CountAsync(b => b.EventId == eventId && !b.IsCancelled);
        activeBookings.Should().Be(10);
    }
}

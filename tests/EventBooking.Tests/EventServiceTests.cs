using EventBooking.Application.Dtos;
using FluentAssertions;

namespace EventBooking.Tests;

public class EventServiceTests
{
    // Creating an event stores a full seat inventory and drops a cached event list so the new event is not hidden.
    [Fact]
    public async Task CreateAsync_PersistsEvent_AndInvalidatesCache()
    {
        await using var fixture = new SqlServerTestFixture();
        var cache = fixture.CreateCache();
        cache.Set(new EventListQuery(1, 10), new PagedResult<EventResponse>([], 1, 10, 0));
        cache.TryGet(new EventListQuery(1, 10), out _).Should().BeTrue();

        await using var db = fixture.CreateContext();
        var (events, _, _, _) = fixture.CreateServices(db, cache);

        var created = await events.CreateAsync(new CreateEventRequest(
            "Tech Summit",
            "Annual conference",
            DateTimeOffset.UtcNow.AddDays(14),
            100));

        created.Name.Should().Be("Tech Summit");
        created.SeatLimit.Should().Be(100);
        created.SeatsBooked.Should().Be(0);
        created.SeatsAvailable.Should().Be(100);
        cache.TryGet(new EventListQuery(1, 10), out _).Should().BeFalse();
    }

    // Name filtering must return only matching events and report that filtered total.
    [Fact]
    public async Task ListAsync_SupportsPagingAndNameFilter()
    {
        await using var fixture = new SqlServerTestFixture();
        await using var db = fixture.CreateContext();
        var (events, _, _, _) = fixture.CreateServices(db);

        await events.CreateAsync(new CreateEventRequest("Alpha Meetup", null, DateTimeOffset.UtcNow.AddDays(1), 10));
        await events.CreateAsync(new CreateEventRequest("Beta Workshop", null, DateTimeOffset.UtcNow.AddDays(2), 20));
        await events.CreateAsync(new CreateEventRequest("Alpha Conference", null, DateTimeOffset.UtcNow.AddDays(3), 30));

        var page = await events.ListAsync(new EventListQuery(1, 10, Name: "alpha"));

        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items.Select(i => i.Name).Should().OnlyContain(n => n.Contains("Alpha", StringComparison.OrdinalIgnoreCase));
    }

    // Repeating the same list query should reuse the cached page instead of building a new result.
    [Fact]
    public async Task ListAsync_ReturnsCachedResult_OnSecondCall()
    {
        await using var fixture = new SqlServerTestFixture();
        var cache = fixture.CreateCache();
        await using var db = fixture.CreateContext();
        var (events, _, _, _) = fixture.CreateServices(db, cache);

        await events.CreateAsync(new CreateEventRequest("Cached Event", null, DateTimeOffset.UtcNow.AddDays(5), 50));

        var first = await events.ListAsync(new EventListQuery(1, 10));
        var second = await events.ListAsync(new EventListQuery(1, 10));

        first.TotalCount.Should().Be(1);
        second.Should().BeSameAs(first);
    }
}

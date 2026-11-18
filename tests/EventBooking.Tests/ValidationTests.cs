using EventBooking.Application.Dtos;
using EventBooking.Application.Validation;
using FluentAssertions;

namespace EventBooking.Tests;

public class ValidationTests
{
    // An event with no name, an oversized description, a past start, or no seats must be rejected before it is saved.
    [Fact]
    public async Task CreateEventRequest_RejectsPastStartAndZeroSeats()
    {
        var validator = new CreateEventRequestValidator();
        var result = await validator.ValidateAsync(new CreateEventRequest(
            "",
            new string('x', 2001),
            DateTimeOffset.UtcNow.AddMinutes(-1),
            0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequest.Name));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequest.Description));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequest.StartsAt));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequest.SeatLimit));
    }

    // A booking needs a real attendee name, a valid email, and at least one seat.
    [Fact]
    public async Task BookSeatsRequest_RequiresEmailAndPositiveSeats()
    {
        var validator = new BookSeatsRequestValidator();
        var result = await validator.ValidateAsync(new BookSeatsRequest(" ", "not-an-email", 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BookSeatsRequest.AttendeeName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BookSeatsRequest.AttendeeEmail));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(BookSeatsRequest.Seats));
    }

    // A named future event with a positive seat limit is a valid create request.
    [Fact]
    public async Task CreateEventRequest_AcceptsValidPayload()
    {
        var validator = new CreateEventRequestValidator();
        var result = await validator.ValidateAsync(new CreateEventRequest(
            "Valid Event",
            "Optional description",
            DateTimeOffset.UtcNow.AddHours(2),
            25));

        result.IsValid.Should().BeTrue();
    }
}

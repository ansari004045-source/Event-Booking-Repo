using EventBooking.Application.Abstractions;
using EventBooking.Application.Dtos;
using EventBooking.Application.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace EventBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(
    IBookingRepository bookingRepository,
    IBookingService bookingService) : ControllerBase
{
    /// <summary>Get a booking by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(id, tracking: false, cancellationToken);
        return booking is null ? NotFound() : Ok(booking.ToResponse());
    }

    /// <summary>Cancel a booking and release seats.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var cancelled = await bookingService.CancelAsync(id, cancellationToken);
        return Ok(cancelled);
    }
}

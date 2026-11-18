using EventBooking.Application.Abstractions;
using EventBooking.Application.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EventBooking.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(
    IEventService eventService,
    IBookingService bookingService,
    IValidator<CreateEventRequest> createEventValidator,
    IValidator<BookSeatsRequest> bookSeatsValidator) : ControllerBase
{
    /// <summary>Create an event with a seat limit.</summary>
    [HttpPost]
    //[ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        await createEventValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await eventService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>List events with paging and optional name/date filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? name = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await eventService.ListAsync(
            new EventListQuery(page, pageSize, name, from, to),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Get a single event by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var evt = await eventService.GetByIdAsync(id, cancellationToken);
        return evt is null ? NotFound() : Ok(evt);
    }

    /// <summary>Book seats for an event.</summary>
    [HttpPost("{id:guid}/bookings")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Book(
        Guid id,
        [FromBody] BookSeatsRequest request,
        CancellationToken cancellationToken)
    {
        await bookSeatsValidator.ValidateAndThrowAsync(request, cancellationToken);
        var booking = await bookingService.BookAsync(id, request, cancellationToken);
        return CreatedAtAction(
            nameof(BookingsController.GetById),
            "Bookings",
            new { id = booking.Id },
            booking);
    }
}

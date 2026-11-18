using EventBooking.Application.Abstractions;
using EventBooking.Application.Dtos;
using EventBooking.Application.Exceptions;
using EventBooking.Application.Mapping;
using EventBooking.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EventBooking.Application.Services;

public sealed class BookingService(
    IEventRepository eventRepository,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    IEventListCache listCache,
    ILogger<BookingService> logger) : IBookingService
{
    private const int MaxConcurrencyRetries = 8;

    public async Task<BookingResponse> BookAsync(
        Guid eventId,
        BookSeatsRequest request,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var evt = await eventRepository.GetByIdAsync(eventId, tracking: true, cancellationToken)
                ?? throw new NotFoundException($"Event '{eventId}' was not found.");

            if (evt.SeatsAvailable < request.Seats)
            {
                throw new ConflictException(
                    $"Not enough seats available. Requested {request.Seats}, available {evt.SeatsAvailable}.");
            }

            evt.SeatsBooked += request.Seats;
            evt.RowVersion += 1;

            var booking = new Booking
            {
                EventId = evt.Id,
                AttendeeName = request.AttendeeName.Trim(),
                AttendeeEmail = request.AttendeeEmail.Trim().ToLowerInvariant(),
                Seats = request.Seats
            };

            await bookingRepository.AddAsync(booking, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                listCache.Invalidate();
                logger.LogInformation(
                    "Booked {Seats} seat(s) for event {EventId} as booking {BookingId} (attempt {Attempt})",
                    booking.Seats, eventId, booking.Id, attempt);
                return booking.ToResponse();
            }
            catch (ConcurrencyConflictException ex)
            {
                logger.LogWarning(
                    ex,
                    "Concurrency conflict booking event {EventId} on attempt {Attempt}/{Max}",
                    eventId, attempt, MaxConcurrencyRetries);

                await unitOfWork.ReloadTrackedAsync(cancellationToken);
                bookingRepository.Detach(booking);

                if (attempt == MaxConcurrencyRetries)
                {
                    throw new ConflictException(
                        "Could not complete booking due to concurrent updates. Please retry.");
                }
            }
        }

        throw new ConflictException("Could not complete booking due to concurrent updates. Please retry.");
    }

    public async Task<BookingResponse> CancelAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var booking = await bookingRepository.GetByIdWithEventAsync(bookingId, cancellationToken)
                ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");

            if (booking.IsCancelled)
            {
                throw new ConflictException($"Booking '{bookingId}' is already cancelled.");
            }

            booking.IsCancelled = true;
            booking.CancelledAt = DateTimeOffset.UtcNow;
            booking.Event.SeatsBooked -= booking.Seats;
            booking.Event.RowVersion += 1;

            if (booking.Event.SeatsBooked < 0)
            {
                throw new BadRequestException("Seat accounting became inconsistent while cancelling.");
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                listCache.Invalidate();
                logger.LogInformation(
                    "Cancelled booking {BookingId} for event {EventId}, released {Seats} seat(s)",
                    booking.Id, booking.EventId, booking.Seats);
                return booking.ToResponse();
            }
            catch (ConcurrencyConflictException ex)
            {
                logger.LogWarning(
                    ex,
                    "Concurrency conflict cancelling booking {BookingId} on attempt {Attempt}/{Max}",
                    bookingId, attempt, MaxConcurrencyRetries);

                await unitOfWork.ReloadTrackedAsync(cancellationToken);

                if (attempt == MaxConcurrencyRetries)
                {
                    throw new ConflictException(
                        "Could not cancel booking due to concurrent updates. Please retry.");
                }
            }
        }

        throw new ConflictException("Could not cancel booking due to concurrent updates. Please retry.");
    }
}

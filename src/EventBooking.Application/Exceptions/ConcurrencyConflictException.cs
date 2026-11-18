namespace EventBooking.Application.Exceptions;

/// <summary>
/// Raised when an optimistic concurrency token check fails at the database.
/// </summary>
public sealed class ConcurrencyConflictException(string message, Exception? inner = null)
    : Exception(message, inner);

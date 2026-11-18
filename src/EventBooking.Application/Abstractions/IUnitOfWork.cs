namespace EventBooking.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>
    /// Persists changes. Throws <see cref="Exceptions.ConcurrencyConflictException"/> on optimistic concurrency failure.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Reloads all tracked entities from the database after a concurrency conflict.</summary>
    Task ReloadTrackedAsync(CancellationToken cancellationToken = default);
}

using EventBooking.Application.Abstractions;
using EventBooking.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EventBooking.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "A concurrency conflict occurred while saving changes.", ex);
        }
    }

    public async Task ReloadTrackedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            if (entry.State is EntityState.Detached)
            {
                continue;
            }

            if (entry.State is EntityState.Added)
            {
                entry.State = EntityState.Detached;
                continue;
            }

            await entry.ReloadAsync(cancellationToken);
        }
    }
}

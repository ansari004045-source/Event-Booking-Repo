using EventBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventBooking.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var eventEntity = modelBuilder.Entity<Event>();
        eventEntity.ToTable("Events");
        eventEntity.HasKey(e => e.Id);
        eventEntity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        eventEntity.Property(e => e.Description).HasMaxLength(2000);
        eventEntity.Property(e => e.StartsAt).IsRequired();
        eventEntity.Property(e => e.SeatLimit).IsRequired();
        eventEntity.Property(e => e.SeatsBooked).IsRequired();
        eventEntity.Property(e => e.RowVersion).IsConcurrencyToken();
        eventEntity.HasIndex(e => e.StartsAt);
        eventEntity.HasIndex(e => e.Name);
        eventEntity.Ignore(e => e.SeatsAvailable);

        var bookingEntity = modelBuilder.Entity<Booking>();
        bookingEntity.ToTable("Bookings");
        bookingEntity.HasKey(b => b.Id);
        bookingEntity.Property(b => b.AttendeeName).HasMaxLength(200).IsRequired();
        bookingEntity.Property(b => b.AttendeeEmail).HasMaxLength(320).IsRequired();
        bookingEntity.HasOne(b => b.Event)
            .WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        bookingEntity.HasIndex(b => b.EventId);
    }
}

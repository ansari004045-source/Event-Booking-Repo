using EventBooking.Application.Abstractions;
using EventBooking.Application.Services;
using EventBooking.Infrastructure.Caching;
using EventBooking.Infrastructure.Persistence;
using EventBooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventBooking.Tests;

/// <summary>
/// Creates an isolated SQL Server database per test class/instance for concurrent-safe tests.
/// Override with TEST_SQLSERVER_CONNECTION (without Database=) or ConnectionStrings__Default.
/// </summary>
internal sealed class SqlServerTestFixture : IAsyncDisposable, IDisposable
{
    private readonly string _databaseName;
    private readonly string _masterConnectionString;
    private readonly string _connectionString;

    public SqlServerTestFixture()
    {
        _databaseName = $"EventBooking_Tests_{Guid.NewGuid():N}";
        var baseConnection = Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=(localdb)\\mssqllocaldb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true";

        _masterConnectionString = EnsureDatabaseKeyword(baseConnection, "master");
        _connectionString = EnsureDatabaseKeyword(baseConnection, _databaseName);

        using var db = CreateContext();
        db.Database.Migrate();
    }

    public string ConnectionString => _connectionString;

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    public IEventListCache CreateCache() =>
        new EventListCache(new MemoryCache(new MemoryCacheOptions()), NullLogger<EventListCache>.Instance);

    public (EventService Events, BookingService Bookings, IEventListCache Cache, AppDbContext Db) CreateServices(
        AppDbContext? db = null,
        IEventListCache? cache = null)
    {
        db ??= CreateContext();
        cache ??= CreateCache();
        var events = new EventRepository(db);
        var bookings = new BookingRepository(db);
        var uow = new UnitOfWork(db);
        var eventService = new EventService(events, uow, cache, NullLogger<EventService>.Instance);
        var bookingService = new BookingService(events, bookings, uow, cache, NullLogger<BookingService>.Instance);
        return (eventService, bookingService, cache, db);
    }

    public void Dispose() => DropDatabase();

    public ValueTask DisposeAsync()
    {
        DropDatabase();
        return ValueTask.CompletedTask;
    }

    private void DropDatabase()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_masterConnectionString)
                .Options;
            using var db = new AppDbContext(options);
            // Database name is Guid-generated (alphanumeric only).
            if (_databaseName.Any(c => !(char.IsLetterOrDigit(c) || c == '_')))
            {
                throw new InvalidOperationException("Unexpected test database name.");
            }

#pragma warning disable EF1002
            db.Database.ExecuteSqlRaw(
                $"""
                 IF DB_ID(N'{_databaseName}') IS NOT NULL
                 BEGIN
                     ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                     DROP DATABASE [{_databaseName}];
                 END
                 """);
#pragma warning restore EF1002
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static string EnsureDatabaseKeyword(string connectionString, string database)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = database
        };
        return builder.ConnectionString;
    }
}

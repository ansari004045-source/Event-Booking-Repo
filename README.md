# Event Booking API (Demo)

Multilayer **.NET 8** Web API for creating events, booking/cancelling seats, and listing events with paging/filtering.

- **SQL Server** persistence (LocalDB or Docker)
- **EF Core code-first** with migrations
- Optimistic concurrency prevents overbooking under parallel load
- `IMemoryCache` listing cache with invalidation on create/book/cancel
- FluentValidation, exception middleware, logging, unit tests

## Solution layout

```
EventBooking/
├── docker-compose.yml                 # SQL Server 2022 for local demo
├── DEMO-NOTES.md                      # Design / concurrency / caching notes
├── src/
│   ├── EventBooking.Api/              # Controllers, middleware, host
│   ├── EventBooking.Application/      # Services, DTOs, validators, abstractions
│   ├── EventBooking.Domain/           # Entities
│   ├── EventBooking.Helpers/          # Paging, cache keys, date helpers
│   └── EventBooking.Infrastructure/   # EF Core, repositories, cache, migrations
└── tests/
    └── EventBooking.Tests/            # Unit + concurrent last-seat tests
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server via one of:
  - **Docker** (recommended cross-platform) — see below
  - **LocalDB** (Windows) — see connection string example

Optional EF tools:

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```

## SQL Server — Docker (Linux / macOS / Windows)

```bash
docker compose up -d
```

Default connection (already in `appsettings.json`):

```
Server=localhost,1433;Database=EventBooking;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False
```

Override with env:

```bash
export ConnectionStrings__Default="Server=localhost,1433;Database=EventBooking;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False"
```

## SQL Server — LocalDB (Windows)

Copy or merge `src/EventBooking.Api/appsettings.LocalDB.example.json`:

```
Server=(localdb)\mssqllocaldb;Database=EventBooking;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Or set `ConnectionStrings__Default` to that value.

## Migrations

Apply the initial migration (also runs automatically on API startup via `Database.Migrate()`):

```bash
dotnet ef database update \
  --project src/EventBooking.Infrastructure \
  --startup-project src/EventBooking.Api
```

Create a new migration after model changes:

```bash
dotnet ef migrations add <Name> \
  --project src/EventBooking.Infrastructure \
  --startup-project src/EventBooking.Api \
  --output-dir Persistence/Migrations
```

## Run the API

```bash
# Ensure SQL Server is reachable, then:
dotnet run --project src/EventBooking.Api
```

Swagger (Development): `http://localhost:5054/swagger` (see `launchSettings.json`).

### Example requests

```bash
# Create
curl -s -X POST http://localhost:5054/api/events \
  -H "Content-Type: application/json" \
  -d '{"name":"Tech Summit","description":"Annual","startsAt":"2026-12-01T09:00:00Z","seatLimit":100}'

# List
curl -s "http://localhost:5054/api/events?page=1&pageSize=10&name=tech"

# Book
curl -s -X POST http://localhost:5054/api/events/{eventId}/bookings \
  -H "Content-Type: application/json" \
  -d '{"attendeeName":"Ada Lovelace","attendeeEmail":"ada@example.com","seats":2}'

# Cancel
curl -s -X DELETE http://localhost:5054/api/bookings/{bookingId}
```

## Run tests

SQL Server must be available (Docker default credentials work out of the box).

```bash
dotnet test EventBooking.sln -c Release
```

Optional override:

```bash
export TEST_SQLSERVER_CONNECTION="Server=localhost,1433;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False"
dotnet test EventBooking.sln -c Release
```

Includes `ConcurrentBookings_ForLastSeats_DoNotOverbook` (20 parallel bookings vs 10 seats).

## Design notes

See [`DEMO-NOTES.md`](./DEMO-NOTES.md) (also at `/cursor/stores/self/docs/event-booking-demo-notes.md`).

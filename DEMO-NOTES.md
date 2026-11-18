# Event Booking API — Demo Notes

Standalone multilayer **.NET 8** Web API demo: event creation, seat booking/cancellation, paged listing, concurrency-safe seats, and listing cache invalidation — backed by **SQL Server** and **EF Core migrations**.

**Solution path:** `/agent/EventBooking`

---

## 1. Design overview

| Layer | Project | Responsibility |
|--------|---------|----------------|
| API | `EventBooking.Api` | Controllers, exception middleware, host, Swagger |
| Application | `EventBooking.Application` | Services, DTOs, FluentValidation, repository/cache abstractions |
| Domain | `EventBooking.Domain` | `Event`, `Booking` entities |
| Helpers | `EventBooking.Helpers` | Paging, cache key builder, UTC helpers |
| Infrastructure | `EventBooking.Infrastructure` | EF Core + SQL Server, repositories, unit of work, memory cache, migrations |
| Tests | `EventBooking.Tests` | Validation, services, concurrent last-seat booking |

### Persistence choice

**EF Core + SQL Server** with code-first migrations. Connection string from `ConnectionStrings:Default` (appsettings or `ConnectionStrings__Default` env). Local demo options: **Docker SQL Server** (`docker-compose.yml`) or **LocalDB** on Windows (`appsettings.LocalDB.example.json`).

### Seat safety

Optimistic concurrency token `Event.RowVersion` + service-level retry. Listing cache: versioned `IMemoryCache` keys invalidated on create/book/cancel.

### HTTP surface

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/api/events` | Create event with seat limit |
| `GET` | `/api/events` | List with `page`, `pageSize`, `name`, `from`, `to` |
| `GET` | `/api/events/{id}` | Get one event |
| `POST` | `/api/events/{id}/bookings` | Book seats |
| `GET` | `/api/bookings/{id}` | Get booking |
| `DELETE` | `/api/bookings/{id}` | Cancel booking |

---

## 2. Code flow

### Create event

1. Controller validates `CreateEventRequest`.
2. `EventService` builds domain entity → `IEventRepository.AddAsync` → `IUnitOfWork.SaveChangesAsync`.
3. Listing cache invalidated.
4. `201 Created`.

### Book seats

1. Validate `BookSeatsRequest`.
2. `BookingService` loads event via repository, checks availability.
3. Increments `SeatsBooked` and `RowVersion`, adds `Booking`.
4. `SaveChanges` — EF includes `RowVersion` in `UPDATE … WHERE`.
5. Success → invalidate cache.
6. `ConcurrencyConflictException` (from infrastructure mapping of `DbUpdateConcurrencyException`) → reload tracked entities, detach unsaved booking, **retry** (up to 8). Exhausted seats → `409 Conflict`.

### Cancel booking

Same concurrency pattern: mark cancelled, decrement seats, bump `RowVersion`, invalidate cache.

### List events

1. Normalize paging in the service (via `PagingHelper`).
2. Versioned cache key (`CacheKeyBuilder`).
3. Miss → `EventRepository.ListAsync` (SQL Server), cache ~30s.

---

## 3. How overbooking is prevented

Two concurrent “last seat” requests can both read “1 available.” Only one `UPDATE` with the expected `RowVersion` succeeds; the other gets a concurrency failure, reloads, and either books remaining seats or returns **409**.

Test: `ConcurrentBookings_ForLastSeats_DoNotOverbook` — **20** parallel 1-seat bookings against a **10**-seat event → exactly **10** successes, **10** conflicts, `SeatsBooked == 10`.

---

## 4. Caching strategy

- **What:** Paged/filtered event listings.
- **Where:** Process-local `IMemoryCache` (`EventListCache` in Infrastructure).
- **Key:** Version + page + filters (`CacheKeyBuilder`).
- **TTL:** 30 seconds safety net.
- **Invalidation:** Create/book/cancel bumps version so old keys stop resolving.

---

## 5. Alternatives considered

### A. Overbooking

| Option | Pros | Cons | Demo choice |
|--------|------|------|-------------|
| **Optimistic concurrency + retry** | Portable; clear EF story; testable | Retries under heavy contention | **Chosen** |
| Atomic conditional SQL | One round-trip | Less layered EF demo | Strong production option |
| Serializable / row locks | Strong isolation | Heavier; SQL Server specific tuning | Better at large scale |
| In-process lock | Simple | Not multi-instance safe | Rejected |

### B. Persistence & cache

| Option | Pros | Cons | Demo choice |
|--------|------|------|-------------|
| **SQL Server + migrations** | Realistic enterprise stack; LocalDB/Docker | Needs a server | **Chosen** |
| SQLite | Zero install | Weaker concurrency story | Previous iteration; replaced per requirements |
| EF InMemory | Fast | No real concurrency | Rejected for seat-safety proof |
| Redis listing cache | Multi-node | Extra dependency | Unnecessary for single-process demo |

### C. Layering

Separate class libraries (Domain / Application / Infrastructure / Helpers / Api) keep controllers thin and make repository + service boundaries obvious for a reporting-manager walkthrough. A single project would run the same, but multilayer matches the requested architecture.

---

## 6. Cross-cutting

- **Validation:** FluentValidation in Application.
- **Exceptions:** API middleware maps validation → 400, `NotFound` → 404, `Conflict` → 409.
- **Logging:** Console `ILogger` on services and middleware.
- **Migrations:** `Persistence/Migrations`; applied with `dotnet ef database update` and on startup via `Database.Migrate()`.

---

## 7. How to run (quick)

```bash
cd /agent/EventBooking
docker compose up -d
dotnet ef database update --project src/EventBooking.Infrastructure --startup-project src/EventBooking.Api
dotnet run --project src/EventBooking.Api
dotnet test EventBooking.sln -c Release
```

Full instructions: `/agent/EventBooking/README.md`.

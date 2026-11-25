# Testing strategy

| Suite | Project | Tools | Needs Docker |
|---|---|---|---|
| Unit | `tests/Library.UnitTests` | xUnit v3, NSubstitute, Bogus, `FakeTimeProvider` | No |
| Architecture | `tests/Library.ArchitectureTests` | NetArchTest | No |
| Integration | `tests/Library.IntegrationTests` | `WebApplicationFactory`, Testcontainers (SQL Server with full-text, RabbitMQ), SignalR client | Yes |
| Load | `tests/load` | k6 | Yes (to run the API) |

Run everything except the load test:

```bash
dotnet test
```

Run a single suite:

```bash
dotnet test --project tests/Library.UnitTests
```

## Unit tests (216)

- **Domain**: printed and e-book loan periods, overdue and expiry rules, rounding of days overdue, renewals, reading progress, reservation states and hold expiry, fine amounts and settlement, review ratings, single-use tokens, e-book licenses, email confirmation.
- **Services**: borrowing inside a book lock, free capacity with ready holds, a ready reservation taking the held copy, standing (overdue loans, unpaid fines, loan limit), late returns issuing fines, returns promoting the next member, renewals blocked by a waiting list, reservation rules, fines, reviews and rating refresh, password reset and email confirmation links, ISBN import, e-book upload and Gutenberg import, maintenance (expirations, holds, reminders, notices), wishlist, catalog cache usage.
- **Common**: ISBN check digits, CSV escaping (including spreadsheet formula injection), e-book type detection by magic bytes, reviewer display names.
- **Infrastructure**: signed links, full-text condition building, Open Library mapping with a stub HTTP handler, HybridCache invalidation by tag, JWT, PBKDF2, routing keys for every event, outbox retry bookkeeping.

The clock is controlled with `FakeTimeProvider`, so tests move time forward instead of waiting.

## Architecture tests (12)

- Domain depends on no other layer and on no framework.
- Application depends on neither Infrastructure nor the API, nor on EF Core, ASP.NET Core, Dapper, RabbitMQ or SendGrid.
- Infrastructure does not depend on the API.
- Controllers inherit `ControllerBase`, end with `Controller` and never touch the database directly.
- Application services are sealed and implement a matching interface; repository contracts are interfaces implemented in Infrastructure.

## Integration tests (59)

The API runs in memory with `WebApplicationFactory`, connected to **real SQL Server and RabbitMQ containers** started by Testcontainers. The SQL Server image is built from [`docker/sqlserver/Dockerfile`](../docker/sqlserver/Dockerfile) with the full-text feature, so the full-text search is tested for real. Open Library and Project Gutenberg are replaced by fakes, so the suite never depends on the internet.

- **Authentication and authorization**: 401, 403, token rotation and reuse detection, logout, and email confirmation and password reset with the links captured from the emails delivered through RabbitMQ.
- **Circulation**: availability from copies, two members racing for the last copy (one 201, one 422), the waiting list with positions, a late return with a shifted clock issuing a fine, the fine blocking new loans until it is paid, the hold for the first member, unclaimed holds passing to the next member, renewals and the loan limit.
- **E-books**: multipart upload, licenses, signed download with range requests, tampered and revoked links, reading progress, early return promoting the waiting list, expiry by the maintenance job, Gutenberg import, rejected fake files.
- **Catalog**: full-text prefix search, filters and sorting, genres, suggestions, ISBN import with duplicate detection, copies, reviews and the average rating, wishlist and "also read".
- **Real time and admin**: SignalR notification after borrowing, unread count, anonymous hub connections rejected, audit trail with the actor, dashboard, CSV export, reading statistics, API version header.
- **SQL, messaging and hosting**: pagination, the Dapper ranking, the stored procedure (March 2031: 2 loans, 1 overdue, 50%, 10 days late), the outbox processed exactly once, rate limiting, CORS and Swagger configuration.

Tests that run the maintenance job with a shifted clock share an xUnit collection, so they never run at the same time.

## Coverage

```bash
dotnet test --coverage --coverage-output-format cobertura
dotnet tool restore
dotnet reportgenerator "-reports:TestResults/*.cobertura.xml" "-targetdir:coverage-report" "-reporttypes:HtmlInline;TextSummary" "-classfilters:-Library.Infrastructure.Persistence.Migrations.*"
```

| Metric | Result |
|---|---|
| Lines | 88.6% |
| Branches | 75.3% |

Generated EF Core migrations are excluded. CI uploads the HTML report as an artifact and writes the summary to the workflow page.

## Load test (k6)

The script in [`tests/load/library-load-test.js`](../tests/load/library-load-test.js) seeds 50 books and runs two scenarios for about one minute:

- `catalog`: up to 30 virtual users paging through and searching the catalog.
- `members`: 5 virtual users registering, signing in, calling `/api/v1/auth/me` and refreshing the token.

Thresholds: less than 1% failed requests, catalog p95 under 300 ms, member flow p95 under 1 s.

The member scenario signs in many times from one IP, so raise the authentication rate limit for the test run:

```bash
AUTH_RATE_LIMIT=100000 docker compose up -d --build
docker run --rm -i -e BASE_URL=http://host.docker.internal:5080 grafana/k6 run - < tests/load/library-load-test.js
```

Latest local result (Docker Desktop on a developer laptop):

| Metric | Result |
|---|---|
| Requests | 3,469 |
| Failed requests | 0.11% (one SQL client timeout on a login; the other three failures were the same virtual user calling `me` and `refresh` without a token) |
| Catalog p95 | 11 ms (HybridCache) |
| Member flow p95 | 199 ms |

The member flow is slower on purpose: PBKDF2 with 100,000 iterations makes password checks expensive to resist brute force.

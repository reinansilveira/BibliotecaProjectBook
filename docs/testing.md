# Testing strategy

| Suite | Project | Tools | Needs Docker |
|---|---|---|---|
| Unit | `tests/Library.UnitTests` | xUnit v3, NSubstitute, Bogus, `FakeTimeProvider` | No |
| Architecture | `tests/Library.ArchitectureTests` | NetArchTest | No |
| Integration | `tests/Library.IntegrationTests` | `WebApplicationFactory`, Testcontainers (SQL Server, RabbitMQ) | Yes |
| Load | `tests/load` | k6 | Yes (to run the API) |

Run everything except the load test:

```bash
dotnet test
```

Run a single suite:

```bash
dotnet test --project tests/Library.UnitTests
```

## Unit tests (81)

- **Domain**: loan period and overdue rules, returning twice, refresh token expiry and revocation, member defaults.
- **Services**: every loan rule (blacklist, one active loan per member, book already on loan, not found cases), blacklist refresh without duplicates, soft deletes, email normalization and duplicates.
- **Events**: each use case publishes the expected integration event, and nothing is published when a rule fails.
- **Authentication**: login, refresh token rotation, reuse detection revoking every session, expiration, logout.
- **Notifications**: each event produces the right email.
- **Infrastructure**: PBKDF2 hashing, JWT claims and signature, routing keys, event serialization, outbox retry bookkeeping.

The clock is controlled with `FakeTimeProvider`, so tests can move time forward instead of waiting.

## Architecture tests (12)

- Domain depends on no other layer and on no framework.
- Application depends on neither Infrastructure nor the API, nor on EF Core, ASP.NET Core, Dapper, RabbitMQ or SendGrid.
- Infrastructure does not depend on the API.
- Controllers inherit `ControllerBase`, end with `Controller` and never touch the database directly.
- Application services are sealed and implement a matching interface; repository contracts are interfaces and their implementations live in Infrastructure.

## Integration tests (33)

The API runs in memory with `WebApplicationFactory`, connected to **real SQL Server and RabbitMQ containers** started by Testcontainers. The containers are shared by all tests through an assembly fixture and removed at the end.

- **Authentication and authorization**: 401 without a token or with an invalid token, 403 when a member calls an admin endpoint or reads another member's data, token refresh, reuse detection and logout.
- **Loan flow**: borrowing, the "book already on loan" rule, returns, history, validation errors (400), duplicates (409), not found (404).
- **Blacklist**: a client with the clock moved 10 days forward runs the overdue check, the member is blocked, and is released after the return.
- **SQL**: pagination and search, the Dapper ranking, and the stored procedure with a controlled scenario (March 2031: 2 loans, 1 overdue, 50%, 10 days late).
- **Messaging**: registering a member delivers the welcome email through RabbitMQ; borrowing a book delivers the confirmation; the outbox message is marked as processed and consumed exactly once.
- **Rate limiting**: a host with a limit of 3 returns `429` with `Retry-After` on the fourth sign-in attempt, while other endpoints stay unlimited.
- **Hosting configuration**: the configured front end origin passes the CORS preflight and other origins do not; Swagger stays hidden outside development unless `Swagger:Enabled` is set.

## Coverage

```bash
dotnet test --coverage --coverage-output-format cobertura
dotnet tool restore
dotnet reportgenerator "-reports:TestResults/*.cobertura.xml" "-targetdir:coverage-report" "-reporttypes:HtmlInline;TextSummary" "-classfilters:-Library.Infrastructure.Persistence.Migrations.*"
```

| Metric | Result |
|---|---|
| Lines | 87.8% |
| Branches | 75.6% |
| Methods | 92.7% |

Generated EF Core migrations are excluded. CI uploads the HTML report as an artifact and writes the summary to the workflow page.

## Load test (k6)

The script in [`tests/load/library-load-test.js`](../tests/load/library-load-test.js) seeds 50 books and runs two scenarios for about one minute:

- `catalog`: up to 30 virtual users paging through and searching the catalog.
- `members`: 5 virtual users registering, signing in, calling `/api/auth/me` and refreshing the token.

Thresholds: less than 1% failed requests, catalog p95 under 300 ms, member flow p95 under 1 s.

The member scenario signs in many times from one IP, so raise the authentication rate limit for the test run:

```bash
AUTH_RATE_LIMIT=100000 docker compose up -d --build
docker run --rm -i -e BASE_URL=http://host.docker.internal:5080 grafana/k6 run - < tests/load/library-load-test.js
```

Latest local result (Docker Desktop on a developer laptop):

| Metric | Result |
|---|---|
| Requests | 3,435 |
| Failed requests | 0.00% |
| Catalog p95 | 29 ms |
| Member flow p95 | 205 ms |

The member flow is slower on purpose: PBKDF2 with 100,000 iterations makes password checks expensive to resist brute force.

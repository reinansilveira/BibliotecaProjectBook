# Architecture Decision Records

Short records of the main technical decisions and why they were made.

| # | Decision |
|---|---|
| [0001](0001-layered-architecture.md) | Layered architecture with feature folders |
| [0002](0002-transactional-outbox.md) | Transactional outbox for RabbitMQ messaging |
| [0003](0003-jwt-with-rotating-refresh-tokens.md) | Short-lived JWT with rotating refresh tokens |
| [0004](0004-dapper-for-reports.md) | EF Core for writes, Dapper and a stored procedure for reports |
| [0005](0005-tests-with-real-dependencies.md) | Integration tests with real SQL Server and RabbitMQ |
| [0006](0006-rate-limiting-authentication.md) | Rate limiting on authentication endpoints |
| [0007](0007-free-tier-deployment.md) | Free hosting on MonsterASP.NET and GitHub Pages |
| [0008](0008-application-locks-for-circulation.md) | Per-book application locks for circulation |
| [0009](0009-ebooks-with-signed-links.md) | E-books stored in the database and served through signed links |
| [0010](0010-notifications-after-commit.md) | In-app notifications pushed after the commit |
| [0011](0011-audit-with-an-interceptor.md) | Audit trail written by an EF Core interceptor |
| [0012](0012-full-text-search-and-hybrid-cache.md) | Full-text search with a fallback, and HybridCache for the catalog |
| [0013](0013-opentelemetry-and-api-versioning.md) | OpenTelemetry for observability and URL versioning for the API |

# 0005. Integration tests with real SQL Server and RabbitMQ

## Context

In-memory database providers and broker fakes behave differently from the real services: they ignore SQL features (stored procedures, table hints, filtered indexes) and messaging semantics (acks, dead-lettering).

## Decision

Integration tests start SQL Server and RabbitMQ containers with Testcontainers and run the API in memory with `WebApplicationFactory`. Only the email sender is replaced, so emails can be asserted without sending them. Time-dependent scenarios use a shifted `TimeProvider` instead of waiting.

## Consequences

- The tests exercise migrations, the stored procedure, the outbox locking query and the RabbitMQ topology exactly as in production.
- Docker is required to run them, locally and in CI (available on GitHub-hosted runners).
- The suite takes around 40 seconds, mostly for starting the containers.

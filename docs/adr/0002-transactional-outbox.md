# 0002. Transactional outbox for RabbitMQ messaging

## Context

Notifications (welcome, loan confirmation, return receipt, overdue warning) should not slow down or break the HTTP request. Publishing to RabbitMQ directly from a service is unsafe: the database can commit while the publish fails (lost event) or the publish can succeed while the database rolls back (phantom event).

## Decision

Write integration events to an `OutboxMessages` table in the same transaction as the business change. A background `OutboxProcessor` publishes pending messages to a topic exchange with publisher confirms, locking rows with `UPDLOCK, READPAST` so multiple instances can run safely. The consumer records handled message ids in `ProcessedMessages` to be idempotent, and failed messages go to a dead-letter queue.

The broker client is `RabbitMQ.Client` directly, keeping the moving parts visible instead of hidden behind a framework.

## Consequences

- No event is lost or published for a rolled-back change.
- Delivery is at-least-once, so consumers must be idempotent (they are).
- Events are delayed by up to the polling interval (5 seconds by default).

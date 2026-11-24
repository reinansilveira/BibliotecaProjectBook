# 0010. In-app notifications pushed after the commit

## Context

Members should see "your reserved book is available" immediately, and the notification must not exist if the operation that caused it rolls back. Emails already go through the transactional outbox, but the outbox polls every few seconds and RabbitMQ is turned off on the free host.

## Decision

Services add a `Notification` entity to the same unit of work as the business change. An EF Core interceptor that also observes transactions collects the added notifications and pushes them to the member through a SignalR hub after `SaveChanges` succeeds, or after the surrounding transaction commits. A rollback discards them. The inbox endpoints read the same table, so a member who was offline still sees everything.

## Consequences

- The notification and the change are atomic, and pushing is best effort: if SignalR fails, the inbox still has the message.
- Real-time delivery works without RabbitMQ, which matters on the free host.
- With several API instances, SignalR would need a backplane (Redis or Azure SignalR Service).

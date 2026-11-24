# 0008. Per-book application locks for circulation

## Context

Copies, e-book licenses and holds are shared by every member. Two requests for the last copy, or a return racing with a reservation, can both read "one free" and both act on it. Optimistic concurrency on a counter does not fit, because availability is computed from several tables (copies, loans and reservations), not stored in one row.

## Decision

Every use case that changes the capacity of a book runs inside a transaction (with the EF Core execution strategy, so transient retries stay safe) and starts with `sp_getapplock` on the resource `book:{id}` with transaction ownership. The lock is released on commit or rollback. As a second barrier, a filtered unique index on `Loans(CopyId) WHERE ReturnedAt IS NULL` makes it impossible to lend a copy twice, and a unique violation is translated to `409 Conflict`.

## Consequences

- Requests for different books never block each other; requests for the same book are serialized for a few milliseconds.
- The rule lives in one place (`ReservationQueue.LockFor`) and is visible in the use cases, instead of being hidden in isolation levels.
- It is SQL Server specific; another database would need its own advisory lock (for example `pg_advisory_xact_lock`).
- An integration test races two members for the last copy and expects exactly one `201`.

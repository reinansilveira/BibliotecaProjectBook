# 0011. Audit trail written by an EF Core interceptor

## Context

Admins need to know who created a book, who returned a loan or who waived a fine. Writing audit code in every service would be repetitive and easy to forget.

## Decision

An EF Core `SaveChangesInterceptor` inspects the change tracker before every save and writes an `AuditEntry` for audited entities in the same transaction: actor from the JWT (or `system` for background jobs), an action named after the change (`Loan.Returned`, `Fine.Paid`, `Book.Deleted`), the entity id and a JSON document with the old and new values. Sensitive fields are masked and noisy fields ignored.

## Consequences

- New features are audited automatically, and the audit can never disagree with the data.
- Bulk updates done with `ExecuteUpdate` bypass the change tracker and are not audited; they are only used for derived data (rating averages, marking notifications as read).

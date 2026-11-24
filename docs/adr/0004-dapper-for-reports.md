# 0004. EF Core for writes, Dapper and a stored procedure for reports

## Context

Most operations are simple reads and writes of aggregates, which EF Core handles well. Reports aggregate many rows and benefit from SQL Server features such as window functions.

## Decision

- EF Core and migrations for the domain model and all writes.
- Dapper with hand-written SQL (CTE + `DENSE_RANK`) for the most borrowed books ranking.
- A stored procedure, versioned in the migrations, for the monthly overdue report.
- Indexes designed for these queries: filtered indexes for active rows and covering indexes (`INCLUDE`) for the reports.

## Consequences

- Reports are explicit, readable SQL that can be tuned with the execution plan.
- Two data access styles to maintain; both are covered by integration tests against a real SQL Server.

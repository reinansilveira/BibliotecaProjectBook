# 0012. Full-text search with a fallback, and HybridCache for the catalog

## Context

`LIKE '%term%'` cannot use indexes and does not match word prefixes in descriptions. The catalog is also the most requested part of the API and changes rarely compared with how often it is read.

## Decision

- A migration creates a SQL Server full-text index on `Title`, `Author` and `Description` when the feature is installed. The repository builds a safe prefix condition from the words typed and uses `CONTAINS`; it checks once whether the index exists and falls back to `LIKE` when it does not, which is the case on most shared hosts. The Docker image and the integration tests use SQL Server with the full-text feature.
- Catalog reads go through HybridCache with a `catalog` tag. Every use case that changes availability, ratings or book data removes the tag after committing.

## Consequences

- Fast, relevant search where the feature exists, and the same API contract everywhere else.
- Full-text indexes are populated asynchronously, so a new book becomes searchable after a short delay.
- HybridCache works in memory today; adding Redis as the second level only needs configuration.

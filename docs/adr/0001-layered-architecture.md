# 0001. Layered architecture with feature folders

## Context

The original project kept everything in one Web API project. Services returned MVC types, talked directly to the `DbContext`, and could not be unit tested.

## Decision

Split the solution into Domain, Application, Infrastructure and API projects. Dependencies point inwards: the Domain has no dependencies and the Application only knows interfaces. Inside the Application layer, code is grouped by feature (`Books`, `Loans`, `Auth`...) instead of by technical type.

Repositories and a unit of work are used so services can be tested with in-memory fakes and mocks.

## Consequences

- Business rules are tested without a database.
- More files and interfaces than a single-project API.
- The rules are enforced automatically by the architecture tests, so they cannot erode silently.

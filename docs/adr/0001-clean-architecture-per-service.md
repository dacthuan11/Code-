# ADR 0001 — Clean Architecture per microservice

**Status:** Accepted  
**Date:** 2026-10-01

## Context

Each microservice needs a consistent internal structure that separates business logic from infrastructure concerns.

## Decision

Every service follows four layers: `Domain → Application → Infrastructure → API`.

- **Domain:** Entities, value objects, repository interfaces — no external dependencies.
- **Application:** CQRS handlers (MediatR), DTOs, service interfaces.
- **Infrastructure:** EF Core DbContext, repository implementations, external clients.
- **API:** Controllers, middleware, DI composition root.

## Consequences

- Business logic is testable without spinning up a database.
- Adding a new persistence provider only touches Infrastructure.
- More projects per service than a vertical-slice approach, but each layer has a clear job.

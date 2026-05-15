# Code architecture

SearchProject is intentionally kept as one exam repository, but the code is structured as a walking skeleton for independently deployable services.

## Service boundaries

- `SearchApi` owns search use cases, search ranking, cache coordination and search-index persistence access.
- `SearchLoadBalancer` owns backend selection, retry/failover direction and load-balancer metrics.
- `SearchWebApp` owns the user-facing UI.
- `ConsoleSearch` is a small CLI/test client.
- `indexer` owns index-building and persistence writes.
- `Shared` contains DTOs/contracts that cross service boundaries. It should not contain service-specific business logic.

In a full microservice setup these could live in separate repositories, with an infrastructure repository containing Compose/Kubernetes manifests and startup scripts. For this exam project they stay in one repo to make the architecture easier to inspect and demonstrate.

## SearchApi layering

`SearchApi` now follows a lightweight enterprise-ready layout:

- `Domain/`
  - Pure search business concepts and interfaces.
  - `SearchLogic` contains ranking logic and is testable without HTTP, Redis or PostgreSQL.
  - `ISearchIndexRepository` describes the repository contract for reading the search index.
- `Application/`
  - Use-case orchestration.
  - `SearchService` validates search input shape, coordinates cache, calls domain logic and records metrics.
- `Infrastructure/Persistence/`
  - Concrete repository implementations for SQLite/PostgreSQL.
  - `SearchIndexRepositoryFactory` chooses the concrete persistence adapter for the current request/demo configuration.
- `Program.cs`
  - Composition root: dependency registration, middleware, OpenAPI and HTTP endpoints.

This is not a heavy DDD implementation, but it demonstrates the same direction: domain logic is separated from infrastructure, infrastructure depends on domain contracts, and tests target business rules rather than Docker or databases.

## Patterns used

- **Repository Pattern:** `ISearchIndexRepository` abstracts search-index data access from the domain search algorithm.
- **Strategy Pattern:** `IBackendScheduler` lets the load balancer switch between round-robin and random backend selection.
- **Application Service:** `SearchService` coordinates one application use case without putting HTTP code into the domain.
- **DTO/Contract sharing:** `Shared/Model` contains request/response contracts used across deployable services.
- **Observability by design:** services expose health, OpenAPI in development, Prometheus metrics and structured logs.

## Test strategy

`SearchProject.Tests` currently focuses on fast unit tests:

- search ranking and missing-term behavior in `SearchLogic`
- load-balancer scheduler behavior
- scheduler factory fallback behavior

These tests are intentionally independent of Docker, PostgreSQL, Redis and HTTP so they can run quickly in local development and CI.

## Exam framing

The code is best described as a **modular monolith repository containing independently deployable services**. It supports the microservice architecture story without requiring one repository per service for the exam baseline.

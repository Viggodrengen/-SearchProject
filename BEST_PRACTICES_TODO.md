# Best practices TODO – code quality and test pass

## Checklist

- [x] Work from `main` and verify baseline build.
- [x] Read architecture/project docs for exam scope: microservice-style deployability, x/y/z scaling, caching, observability, failover.
- [x] Add focused xUnit tests for pure business logic and load-balancer behavior.
- [x] Keep tests close to business rules, not Docker/infrastructure.
- [ ] Run full build and test suite after changes.
- [ ] If Docker is running, smoke-test HTTP endpoints through API/load balancer.

## Architecture/code review notes

- Services are separate deployable projects (`SearchApi`, `SearchLoadBalancer`, `SearchWebApp`, `indexer`) and match the walking-skeleton microservice story.
- Shared DTOs live in `Shared/Model`, which is acceptable for a small exam baseline. Avoid putting service-specific logic in `Shared`.
- Search ranking is isolated in `SearchApi/Search/SearchLogic`, which makes it testable without HTTP, Redis or PostgreSQL.
- Load-balancer scheduling is isolated behind `IBackendScheduler`, which makes strategy behavior testable.
- Swagger/OpenAPI is already enabled through `AddOpenApi()` and `MapOpenApi()` in Development.

## Follow-up candidates

- Consider moving Minimal API endpoint mapping into extension methods if `Program.cs` grows.
- Consider a repository abstraction name (`ISearchIndexRepository`) if explaining Repository Pattern explicitly at exam.
- Add Docker healthcheck/smoke tests once Docker is available locally.

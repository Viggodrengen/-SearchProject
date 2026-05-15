# Best practices TODO – code quality and test pass

## Checklist

- [x] Work from `main` and verify baseline build.
- [x] Read architecture/project docs for exam scope: microservice-style deployability, x/y/z scaling, caching, observability, failover.
- [x] Add focused xUnit tests for pure search business logic.
- [x] Keep tests close to business rules, not Docker/infrastructure.
- [x] Run full build and test suite after changes.
- [ ] If Docker is running, smoke-test HTTP endpoints through Nginx/API.

## Architecture/code review notes

- Services are separate deployable projects (`SearchApi`, `SearchWebApp`, `indexer`) with Nginx as infrastructure-level reverse proxy/load balancer.
- Shared DTOs live in `Shared/Model`, which is acceptable for a small exam baseline. Avoid putting service-specific logic in `Shared`.
- `SearchApi` follows the same simple style as the AuthService reference: `Interfaces`, `Repository`, `Services` and `Program.cs`.
- Search ranking is isolated in `SearchApi/Services/SearchLogic`, which makes it testable without HTTP, Redis or PostgreSQL.
- `SearchApi/Interfaces/IDatabase.cs` is the simple repository abstraction; `SearchApi/Repository/*` contains the concrete SQLite/PostgreSQL data access.
- Swagger/OpenAPI is already enabled through `AddOpenApi()` and `MapOpenApi()` in Development.

## Follow-up candidates

- Consider moving Minimal API endpoint mapping into controllers only if we want to mirror AuthService even more closely.
- Add Docker healthcheck/smoke tests once Docker is available locally.

Version 2: 17-02-2026

This codebase is a PoC search engine with 4 projects:

1) indexer (console app)
2) SearchApi (ASP.NET Core API with all search logic)
3) ConsoleSearch (console client that talks to SearchApi)
4) SearchWebApp (Blazor web app that talks to SearchApi)

Shared is a class library with common models and path configuration used across projects.

Architecture after scaling:

- indexer writes reverse index data to SQLite or Postgres.
- SearchApi reads from SQLite or Postgres and exposes `/api/search`.
- ConsoleSearch and SearchWebApp are clients only (no local search logic).
- Indexing now preserves original word casing. Case-sensitive search relies on this.

Run order:

1. Run indexer if you need to (re)build index data.
   Important: if you indexed data before 17-02-2026, run indexer again so word casing is preserved.
2. Run API:
   `dotnet run --project SearchApi`
3. Run console client:
   `dotnet run --project ConsoleSearch`
4. Run web app:
   `dotnet run --project SearchWebApp`

API defaults:

- Base URL: `http://localhost:5017`
- Search endpoint: `POST /api/search`
- Health endpoint: `GET /api/health`

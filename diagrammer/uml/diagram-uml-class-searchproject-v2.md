# UML Class Diagram - SearchProject v2 (To-Be)

Fil: `diagram-uml-class-searchproject-v2.drawio`

## Formaal
Diagrammet viser den planlagte klassearkitektur for v2 med fokus paa:
- caching som separat arkitekturkomponent,
- z-akse routing paa request-niveau,
- observability hooks,
- samt koblingen til eksisterende load balancer scheduler-struktur.

## Hvad er planlagt (ikke implementeret endnu)
- `CachedSearchService`
- `IQueryCache` og `RedisQueryCache`
- `CacheKeyBuilder`
- `IZAxisRouter` og `TenantShardRouter`
- `IMetricsRecorder`
- ekstra felter: `TenantKey` paa `SearchRequest` og `CacheHit` paa `SearchResult`

## Hvad er allerede eksisterende i kodebasen
- `SearchService`, `SearchLogic`, `SearchConfig`
- `IDatabase`, `DatabaseSqlite`, `DatabasePostgres`, `DatabaseFactory`
- `IBackendScheduler`, `RoundRobinBackendScheduler`, `RandomBackendScheduler`, `BackendSchedulerFactory`, `LoadBalancerOptions`, `LoadBalancerStatsStore`
- `SearchRequest`, `SearchResult`, `DocumentHit`, `BEDocument`

## Arkitekturpointes til eksamen
- Caching ligger i service-laget (ikke i controller), saa cache-politik er forretningsnaer og testbar.
- Z-akse routing abstraheres bag interface (`IZAxisRouter`) for at undgaa hardcoding af shard-logik i `SearchLogic`.
- Dataadgang holder eksisterende `IDatabase`-kontrakt, saa migration kan ske gradvist.

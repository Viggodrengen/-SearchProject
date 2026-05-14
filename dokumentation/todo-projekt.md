# To-Do Projekt (kode) - SearchProject v2

Opdateret: 2026-05-05
Scope: Denne fil handler kun om implementering, kode, tests og driftsetup.
Eksamenspraesentation ligger i `todo-eksamen.md`.

## 1. Projektmaal i kode

- Implementere Redis caching i soegeflow.
- Maale baseline vs cache (latency/performance).
- Dokumentere failover-adfaerd under belastning.
- Udarbejde konkret z-akse anvisning med teknisk retning.

## 2. Kodeopgaver (prioriteret)

### A. Redis cache i SearchApi
- [x] Tilfoej Redis service i `docker-compose.yml`.
- [x] Tilfoej cache-konfiguration i `SearchApi` (TTL, key-strategi).
- [x] Implementer query caching omkring soegerespons.
- [x] Definer invalidation-strategi (minimum: TTL-baseret).
- [x] Log cache hit/miss for observability.

### B. Performance og latency maaling
- [x] Definer baseline-scenarie uden cache.
- [x] Definer samme scenarie med cache.
- [x] Maal mindst: p50/p95/p99 + throughput.
- [x] Gem resultater i `dokumentation/eksamen-testrapport.md`.

### C. Failover test
- [x] Script: start load -> stop `searchapi1` -> verifer fortsat svar via LB.
- [x] Registrer fejlrate og recovery-tid.
- [x] Gem resultater i testrapport.

### C2. Indexer i Docker Compose
- [x] Goer `indexer` non-interaktiv via environment variables.
- [x] Tilfoej `indexer` Dockerfile.
- [x] Tilfoej repo-lokalt testdatasæt til Compose.
- [x] Tilfoej `indexer` som one-shot service i `docker-compose.yml`.
- [x] Lad `SearchApi` vente paa gennemfoert indexer-job.
- [x] Flush Redis cache efter reindeksering.
- [x] Verificer at Postgres-data kommer fra indexer og ikke kun seed SQL.

### C3. Prometheus/OpenTelemetry observability
- [x] Tilfoej OpenTelemetry metrics til `SearchApi`.
- [x] Tilfoej OpenTelemetry metrics til `SearchLoadBalancer`.
- [x] Eksponer `/metrics` endpoints.
- [x] Tilfoej Prometheus service i `docker-compose.yml`.
- [x] Tilfoej Prometheus scrape config.
- [x] Tilfoej Prometheus datasource i Grafana.
- [x] Tilfoej Grafana metrics-dashboard.
- [x] Verificer Prometheus targets og custom metrics.

### D. Z-akse (teknisk retning)
- [ ] Beskriv mindst 2 mulige shard-strategier.
- [ ] Vaelg 1 anbefalet strategi med tradeoffs.
- [ ] Beskriv routing-princip i soegning (pseudo-flow er nok).

### E. Kubernetes/Minikube retning (ikke fuld migration)
- [ ] Lav teknisk roadmap fra Docker Compose til K8s objekter.
- [ ] Definer hvilke services der boer vaere stateless/stateful.
- [ ] Noter minimum setup for persistence + replicas.

## 3. Filer der skal oprettes/opdateres

- [x] `docker-compose.yml` (Redis service + wiring)
- [x] `SearchApi/*` (cache integration)
- [x] `dokumentation/eksamen-testrapport.md`
- [ ] `dokumentation/eksamen-z-akse-anvisning.md`
- [ ] `dokumentation/eksamen-runbook.md`

## 4. Definition of done (projekt)

- [x] Redis cache virker i koerende miljoe.
- [x] Der er dokumenterede maalinger foer/efter cache.
- [x] Failover er dokumenteret med konkret testkoersel.
- [ ] Z-akse anvisning er konkret nok til at kunne implementeres bagefter.
- [ ] Hele setup kan startes reproducerbart via runbook.

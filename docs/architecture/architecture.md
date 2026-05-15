# Architecture (eksamensfokus)

**Status:** 2026-04-07  
**Kilde for scope:** `docs/exam/projektdefinition_eksamen_searchproject_v0_2.md`

Dette dokument er den aktive arkitekturretning frem mod eksamen.

## 1) Nuværende baseline (allerede implementeret)

Løsningen består af:
- `SearchWebApp` (UI)
- `ConsoleSearch` (CLI)
- `Nginx` (reverse proxy/load balancing mellem API-instanser)
- `SearchApi` (søgelogik)
- `Indexer` (indeksering)
- `Postgres` (data)
- `Loki + Grafana` (observability)

Arkitekturprincipper der allerede er i spil:
- **Modulær/mikroservice-inspireret opdeling**
- **X-akse skalering** (flere `SearchApi` instanser bag Nginx/reverse proxy)
- **Observability** (struktureret logging + dashboards)

## 2) Eksamensmål (det vi bygger videre med)

### A. Redis som tydelig caching-komponent
- Cache-aside i `SearchApi` for `/api/search`
- Cache key baseret på query + options
- TTL-strategi + cache hit/miss logging

### B. Kvalificerede performance/latency tests
- Reproducerbare tests (fx k6)
- Sammenligning af baseline vs cache-enabled
- Målinger: p50/p95/p99, throughput, fejlrate

### C. Failover-tests
- Belastningstest mens en `SearchApi` instans stoppes
- Verifikation af fortsat service via øvrige instanser
- Recovery-tid og fejlrate dokumenteres

### D. Z-akse database-anvisning
- Konkrete forslag til partitionering/sharding
- Forslag til read-replica strategi
- Trinvis migrationsplan (ingen big-bang)

## 3) Drift og miljø

- **Primær demo:** Docker Compose (stabil, hurtig at demonstrere)
- **Arkitekturanvisning:** Kubernetes/Minikube deployment-design i UML deployment diagram

## 4) Diagrammer (eksamensversion)

- Samlet draw.io-fil: `diagrammer/SearchProject diagrams - UML + C4.drawio`
- Primært: C4 Containerdiagram og Kubernetes produktionsdiagram
- Backup/bilag: UML class/object/deployment og C4 context/component

## 5) Arbejdsstyring

- Plan og delopgaver: `TODO.md`
- Eksamensprojektdefinition: `docs/exam/projektdefinition_eksamen_searchproject_v0_2.md`

## 6) Afgrænsning

Vi implementerer **ikke** fuld platform-migration eller komplet sharding i produktion.
Fokus er at levere et stærkt walking skeleton med tydelige arkitekturvalg,
målinger og dokumenterede trade-offs.

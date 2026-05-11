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
- [ ] Tilfoej Redis service i `docker-compose.yml`.
- [ ] Tilfoej cache-konfiguration i `SearchApi` (TTL, key-strategi).
- [ ] Implementer query caching omkring soegerespons.
- [ ] Definer invalidation-strategi (minimum: TTL-baseret).
- [ ] Log cache hit/miss for observability.

### B. Performance og latency maaling
- [ ] Definer baseline-scenarie uden cache.
- [ ] Definer samme scenarie med cache.
- [ ] Maal mindst: p50/p95/p99 + throughput.
- [ ] Gem resultater i `dokumentation/eksamen-testrapport.md`.

### C. Failover test
- [ ] Script: start load -> stop `searchapi1` -> verifer fortsat svar via LB.
- [ ] Registrer fejlrate og recovery-tid.
- [ ] Gem resultater i testrapport.

### D. Z-akse (teknisk retning)
- [ ] Beskriv mindst 2 mulige shard-strategier.
- [ ] Vaelg 1 anbefalet strategi med tradeoffs.
- [ ] Beskriv routing-princip i soegning (pseudo-flow er nok).

### E. Kubernetes/Minikube retning (ikke fuld migration)
- [ ] Lav teknisk roadmap fra Docker Compose til K8s objekter.
- [ ] Definer hvilke services der boer vaere stateless/stateful.
- [ ] Noter minimum setup for persistence + replicas.

## 3. Filer der skal oprettes/opdateres

- [ ] `docker-compose.yml` (Redis service + wiring)
- [ ] `SearchApi/*` (cache integration)
- [ ] `dokumentation/eksamen-testrapport.md`
- [ ] `dokumentation/eksamen-z-akse-anvisning.md`
- [ ] `dokumentation/eksamen-runbook.md`

## 4. Definition of done (projekt)

- [ ] Redis cache virker i koerende miljoe.
- [ ] Der er dokumenterede maalinger foer/efter cache.
- [ ] Failover er dokumenteret med konkret testkoersel.
- [ ] Z-akse anvisning er konkret nok til at kunne implementeres bagefter.
- [ ] Hele setup kan startes reproducerbart via runbook.

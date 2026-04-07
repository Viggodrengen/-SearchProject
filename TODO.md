# TODO – SearchProject eksamensforløb

Dette dokument er vores arbejdsplan frem mod eksamen.
Formål: holde scope skarpt og gøre det nemt at oprette GitHub issues.

## Scope (aftalt)
1. Redis caching som tydelig arkitekturkomponent i `SearchApi`
2. Kvalificerede performance/latency tests
3. Failover tests (under load)
4. Konkret z-akse anvisning for database
5. Opdaterede diagrammer (C4 container, UML class, UML deployment)

## Milepæle

### M1 – Stabil baseline
- [ ] Få standardiseret config (env vars) i alle services
- [ ] Fikse `Shared/Paths.cs` default-værdier (ingen maskinspecifikke stier)
- [ ] Verificere `docker compose up --build` virker clean
- [ ] Definere "Definition of Done" for eksamensdemo

### M2 – Redis caching i SearchApi
- [ ] Tilføj Redis service i `docker-compose.yml`
- [ ] Implementér cache-aside i search flow
- [ ] Cache key design (query + case + max + db)
- [ ] TTL-strategi + invalidation-strategi (minimum dokumenteret)
- [ ] Logging af cache hit/miss
- [ ] Smoke-test: samme query før/efter cache

### M3 – Performance / latency tests
- [ ] Vælg værktøj (k6 anbefalet)
- [ ] Lav baseline-test uden cache (p50/p95/p99, RPS, error rate)
- [ ] Lav test med cache aktiveret
- [ ] Lav test med 1 vs 2 API-instanser
- [ ] Saml resultater i kort testrapport

### M4 – Failover tests
- [ ] Test-scenarie: stop `search-api-1` under belastning
- [ ] Verificer svar fortsætter via resterende instanser
- [ ] Mål fejlrate og recovery-tid
- [ ] Dokumentér observeret adfærd i Grafana/Loki

### M5 – Z-akse databaseplan
- [ ] Beskriv shard key strategi (fx tenant/domæne/tid)
- [ ] Beskriv routingansvar (LB/API/router)
- [ ] Beskriv read-replica strategi
- [ ] Beskriv migration i trin (ingen big-bang)
- [ ] Lav trade-off tabel (kompleksitet vs skalerbarhed)

### M6 – Eksamensmateriale
- [ ] Finalisér projektdefinition (1 side)
- [ ] Opdatér C4 container diagram
- [ ] Opdatér UML class diagram
- [ ] Opdatér UML deployment diagram (Compose + Minikube-view)
- [ ] Forbered 8-10 min demo-script

## Forslag til issues (copy/paste)

1. **[Infra] Add Redis to docker-compose and app config**  
   _AC:_ Redis kører i compose, SearchApi kan forbinde via env var.

2. **[SearchApi] Implement cache-aside for /api/search**  
   _AC:_ Gentagne requests giver cache hit og reduceret latency.

3. **[Observability] Add cache hit/miss logging and dashboard panel**  
   _AC:_ Grafana viser cache-relaterede metrics/log-kurver.

4. **[Perf] Create k6 baseline test suite**  
   _AC:_ Script + output med p50/p95/p99, RPS og error rate.

5. **[Perf] Compare baseline vs cache-enabled**  
   _AC:_ Kort rapport med før/efter tal og konklusion.

6. **[Resilience] Failover test during load**  
   _AC:_ Dokumenteret test hvor 1 API-instans fejler og systemet fortsætter.

7. **[Architecture] Z-axis database scaling proposal**  
   _AC:_ Dokument med shard/replica strategi, migrationstrin og risici.

8. **[Docs] Finalize exam diagrams (C4 + UML class + UML deployment)**  
   _AC:_ Diagrammer matcher implementeret løsning og er brugbare i fremlæggelse.

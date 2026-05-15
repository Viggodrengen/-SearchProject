# TODO – SearchProject eksamensforløb

Dette dokument samler både den tekniske arbejdsplan og eksamensfokus efter feedback fra Henrik.
Formål: holde scope skarpt, gøre GitHub issues nemme at oprette og sikre at diagrammer + kode understøtter præsentationen.

## Kort konklusion efter feedback

Vi skal ikke forsøge at gennemgå alle diagrammer til eksamen. Diagrammerne er støtte, men præsentationen bør primært fokusere på:

1. **C4 Containerdiagrammet** – forklarer systemets containere, ansvar og skalering.
2. **Kubernetes production diagrammet** – forklarer ønsket produktionsdrift.
3. **Koden** – viser at walking skeleton’et understøtter arkitekturvalgene.

De øvrige UML-diagrammer kan ligge i draw.io-filen som baggrund/dokumentation, men bør ikke være centrale i præsentationen medmindre der bliver spurgt ind til dem.

## Scope (aftalt)

1. Redis caching som tydelig arkitekturkomponent i `SearchApi`
2. Kvalificerede performance/latency tests
3. Failover tests under load
4. Konkret z-akse anvisning for database
5. Opdaterede diagrammer: C4 container, Kubernetes production og relevante UML-bilag
6. Kodegennemgang af walking skeleton’et

## Hvad skal vi være skarpe på?

### 1. C4 Containerdiagram

Formål: vise den samlede arkitektur på container-niveau.

Skal kunne forklare:

- Hvad hver container har ansvar for:
  - `SearchWebApp` – UI
  - `ConsoleSearch` – CLI/testklient
  - `SearchLoadBalancer` – fordeler requests og giver failover-retning
  - `SearchApi` – stateless søgelogik
  - `Indexer` – opbygger reverse index
  - `PostgreSQL` – persistent data/index
  - `Redis` – cache af gentagne/hyppige søgninger
  - `Loki/Grafana` – logs og driftsoverblik
- Hvad der skalerer:
  - primært `SearchApi` via flere replikaer bag load balancer
  - senere kan `Indexer`/pipeline tænkes som y-skalering
- Hvorfor det er relevant:
  - bedre throughput ved flere søgninger
  - mindre kobling mellem UI, load balancing, søgelogik og data
  - tydelig drift/monitorering
  - cache reducerer latency ved gentagne søgninger

Arkitekturprincipper vi kobler på:

- **X-akse skalering:** flere `SearchApi`-instanser.
- **Y-akse opdeling:** funktionel opdeling mellem WebApp, API, Indexer, database, cache og observability.
- **Caching:** Redis som arkitektonisk komponent til hot queries.
- **Observability:** Loki/Grafana til logs og drift.
- **Failover-retning:** load balancer kan prøve anden API-instans ved fejl.

### 2. Kubernetes production diagram

Formål: vise hvordan løsningen tænkes deployet i produktion.

Henriks feedback:

- Control Plane er ikke nødvendig at vise. Den er implicit i Kubernetes.
- Diagrammet skal hellere vise workloads, services, pods, database, storage og drift.

Bør forklare:

- Ingress som ekstern indgang.
- Services som stabile endpoints foran pods.
- `SearchWebApp`, `SearchLoadBalancer`, `SearchApi` som Deployments/Pods.
- `SearchApi` med flere pods for x-akse skalering.
- PostgreSQL som stateful del med persistent storage.
- Redis som cache-workload.
- Loki/Grafana som observability namespace.
- Secrets/Vault til credentials/connection strings.

TODO:

- [ ] Tilret Kubernetes-diagrammet og fjern/de-emphasize Control Plane.
- [ ] Gør Kubernetes-diagrammet mere direkte koblet til C4 Containerdiagrammet.
- [ ] Tilføj/tilret sticky note, så den forklarer: Ingress → Services → Pods → data/cache/observability.

### 3. Kodegennemgang

Koden skal bruges til at vise, at arkitekturen ikke kun er tegnet.

Vi bør kunne pege på:

- `SearchWebApp` kalder load balancer/API.
- `SearchLoadBalancer` fordeler mellem API backends.
- `SearchApi` indeholder søgelogik, cache og databaseadgang.
- `Shared/Model` indeholder request/result DTO’er.
- Docker Compose viser API-instanser, PostgreSQL, Redis, Loki/Grafana og Prometheus/Grafana-metrics.

TODO:

- [ ] Find 3-5 kodefiler, vi vil kunne åbne og forklare hurtigt.
- [ ] Lav kort demo-script: start system, lav søgning, vis logs/metrics/instanser.
- [ ] Lav kort forklaring af hvad der er implementeret vs. arkitektonisk retning.

## Milepæle

### M1 – Stabil baseline
- [ ] Få standardiseret config/env vars i alle services
- [ ] Fikse `Shared/Paths.cs` default-værdier hvis de er maskinspecifikke
- [ ] Verificere `docker compose up --build` virker clean
- [ ] Definere Definition of Done for eksamensdemo

### M2 – Redis caching i SearchApi
- [x] Tilføj Redis service i `docker-compose.yml`
- [x] Implementér cache-aside i search flow
- [x] Cache key design: query + case + max + database
- [ ] TTL-strategi + invalidation-strategi dokumenteret kort
- [x] Logging/metrics af cache hit/miss
- [ ] Smoke-test: samme query før/efter cache

### M3 – Performance / latency tests
- [x] Lav performance/cache test-script
- [ ] Lav baseline-test uden cache: p50/p95/p99, RPS, error rate
- [ ] Lav test med cache aktiveret
- [ ] Lav test med 1 vs 2 API-instanser
- [x] Saml resultater i kort testrapport

### M4 – Failover tests
- [x] Test-scenarie: stop `search-api-1` under belastning
- [ ] Verificer svar fortsætter via resterende instanser
- [ ] Mål fejlrate og recovery-tid
- [x] Dokumentér observeret adfærd i testrapport/resultatfil

### M5 – Z-akse databaseplan
- [ ] Beskriv shard key strategi: fx tenant/domæne/tid
- [ ] Beskriv routingansvar: LB/API/router
- [ ] Beskriv read-replica strategi
- [ ] Beskriv migration i trin uden big-bang
- [ ] Lav trade-off tabel: kompleksitet vs skalerbarhed

### M6 – Eksamensmateriale
- [ ] Finalisér projektdefinition på 1 side
- [x] Opdatér C4 container diagram
- [ ] Opdatér Kubernetes production diagram efter feedback
- [ ] Udvælg relevante UML-bilag
- [ ] Forbered 8-10 min demo-script

## Hvilke diagrammer skal bruges til eksamen?

### Primære diagrammer

- [x] C4 Containerdiagram
- [ ] Kubernetes production diagram efter tilretning

### Backup / bilag

- UML class diagram
- UML object diagram
- UML deployment diagram
- C4 context diagram
- C4 component diagram

Disse kan blive i draw.io-filen, men skal ikke nødvendigvis gennemgås aktivt.

## Forslag til præsentationsrækkefølge

1. Kort demo af system i drift.
2. C4 Containerdiagram:
   - hvad består systemet af?
   - hvad skalerer?
   - hvilke arkitekturprincipper bruger vi?
3. Kubernetes production diagram:
   - hvordan ville vi drifte det endeligt?
   - hvordan mappes containere til services/pods?
   - hvordan håndteres storage, cache, observability og secrets?
4. Kode:
   - vis walking skeleton og de centrale komponenter.
5. Kort refleksion:
   - hvad er implementeret nu?
   - hvad er arkitekturretning?
   - tradeoffs og næste skridt.

## Forslag til issues (copy/paste)

1. **[Infra] Add Redis to docker-compose and app config**  
   _AC:_ Redis kører i compose, SearchApi kan forbinde via env var.

2. **[SearchApi] Implement cache-aside for /api/search**  
   _AC:_ Gentagne requests giver cache hit og reduceret latency.

3. **[Observability] Add cache hit/miss logging and dashboard panel**  
   _AC:_ Grafana viser cache-relaterede metrics/log-kurver.

4. **[Perf] Create performance baseline test suite**  
   _AC:_ Script + output med p50/p95/p99, RPS og error rate.

5. **[Perf] Compare baseline vs cache-enabled**  
   _AC:_ Kort rapport med før/efter tal og konklusion.

6. **[Resilience] Failover test during load**  
   _AC:_ Dokumenteret test hvor 1 API-instans fejler og systemet fortsætter.

7. **[Architecture] Z-axis database scaling proposal**  
   _AC:_ Dokument med shard/replica strategi, migrationstrin og risici.

8. **[Docs] Finalize exam diagrams (C4 + Kubernetes + UML backup)**  
   _AC:_ Diagrammer matcher implementeret løsning og er brugbare i fremlæggelse.

## Definition of done inden eksamen

- [ ] C4 Containerdiagram er præsentationsklart.
- [ ] Kubernetes diagram er forsimplet uden unødvendig Control Plane-detalje.
- [ ] Sticky notes forklarer diagrammerne i forhold til arkitekturprincipper.
- [ ] Kodefiler til gennemgang er udvalgt.
- [ ] Demo kan køres stabilt.
- [ ] Vi kan forklare x-akse, y-akse, caching, observability, failover og z-akse/database-retning kort og konkret.

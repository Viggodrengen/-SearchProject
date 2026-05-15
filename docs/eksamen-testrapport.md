# Eksamens-testrapport - performance og failover

Dato: 2026-05-14  
Branch: `Failover-test`  
Miljoe: Docker Compose lokalt paa macOS/Docker Desktop

## 1. Redis cache performance

### Formaal

Formaalet med testen er at dokumentere, om Redis-cache reducerer latency for gentagne soegninger gennem `SearchLoadBalancer` og paa tvaers af flere `SearchApi`-replikaer.

Testen sammenligner to scenarier:

| Scenarie | Beskrivelse |
|---|---|
| `cold-cache` | Redis flushes foer hver request. Det tvinger hvert kald ned i database-/soegelogik-path og giver en baseline uden effektiv cache. |
| `hot-cache` | Redis flushes foerst, derefter primes cachen med en enkelt request. De maalte requests bruger derefter samme query og forventes at ramme Redis. |

### Testsetup

Compose-stack:

- `SearchLoadBalancer` paa `http://localhost:5075`
- `SearchApi` replikaer: `search-api-1` og `search-api-2`
- `PostgreSQL` som persistent index-database
- `Redis` som delt query cache
- `Loki/Grafana` til logs/observability

Testquery:

```json
{
  "query": "socal energy",
  "maxAmount": 10,
  "caseSensitive": false,
  "database": "postgres"
}
```

Kommando:

```bash
ITERATIONS=100 OUTPUT=dokumentation/performance-cache-results.csv scripts/performance-cache-test.sh
```

Raadata ligger i:

- `dokumentation/performance-cache-results.csv`

### Resultater

| Scenarie | Requests | Cache-status | Avg | P50 | P95 | P99 | Min | Max | Throughput |
|---|---:|---|---:|---:|---:|---:|---:|---:|---:|
| `cold-cache` | 100 | 100 miss | 5.47 ms | 4.97 ms | 8.61 ms | 10.67 ms | 3.29 ms | 15.52 ms | 182.8 req/s |
| `hot-cache` | 100 | 100 hit | 4.19 ms | 3.97 ms | 5.64 ms | 6.83 ms | 2.79 ms | 7.12 ms | 238.8 req/s |

Forbedring fra `cold-cache` til `hot-cache`:

| Maal | Forbedring |
|---|---:|
| Avg latency | ca. 23% lavere |
| P50 latency | ca. 20% lavere |
| P95 latency | ca. 35% lavere |
| P99 latency | ca. 36% lavere |
| Throughput | ca. 31% hoejere |

### Observationer

- Alle `cold-cache` requests returnerede `X-Search-Cache: miss`.
- Alle `hot-cache` requests returnerede `X-Search-Cache: hit`.
- Load balanceren fordelte requests mellem `search-api-1` og `search-api-2`.
- Cache-hit virkede ogsaa naar efterfoelgende requests ramte den anden API-replika. Det viser, at Redis fungerer som delt cache paa tvaers af horisontalt skalerede API-instanser.

Eksempel fra raadata:

```csv
cold-cache,1,200,miss,search-api-1 (http://searchapi1:8080),search-api-1,8.334
cold-cache,2,200,miss,search-api-2 (http://searchapi2:8080),search-api-2,8.610
hot-cache,99,200,hit,search-api-2 (http://searchapi2:8080),search-api-2,4.728
hot-cache,100,200,hit,search-api-1 (http://searchapi1:8080),search-api-1,4.063
```

### Konklusion

Redis-cache reducerer latency for gentagne soegninger og forbedrer throughput i det lokale Docker Compose-miljoe. Den vigtigste arkitekturpointe er, at cachen er delt mellem de to `SearchApi`-replikaer, saa en soegning cached af den ene instans kan genbruges af den anden.

Resultatet understoetter C4-/deploymentdiagrammets paastand om Redis som cache-komponent i en x-skaleret soegearkitektur.

### Begraensninger

- Testen er sekventiel og lokal, ikke en fuld belastningstest med mange samtidige brugere.
- `cold-cache` er en tvungen cache-miss baseline, ikke en separat deployment hvor cache-koden er helt slaaet fra.
- Datasettet i Postgres er lille seed-data, saa forbedringen forventes at blive tydeligere ved stoerre indeks eller dyrere soegninger.
- Throughput er beregnet ud fra summerede `curl` response-tider og er derfor bedst som sammenligningsmaal mellem scenarierne.

## 2. Failover-test

### Formaal

Formaalet med failover-testen er at dokumentere, at `SearchLoadBalancer` kan holde soegeflowet koerende, selvom en `SearchApi`-replika stoppes.

Testen viser dermed x-akse pointen i praksis: `SearchApi` kan koere i flere ens replikaer, og load balanceren kan route videre til en overlevende instans, naar en backend fejler.

### Testsetup

Compose-stack:

- `SearchLoadBalancer` paa `http://localhost:5075`
- `search-api-1` paa host-port `5017`
- `search-api-2` paa host-port `5018`
- `PostgreSQL` og `Redis` koerende som delte backend-komponenter

Testflow:

1. Send 10 requests mens begge API-replikaer koerer.
2. Stop `searchapi1` med Docker Compose.
3. Send 40 requests gennem load balanceren mens `searchapi1` er stoppet.
4. Start `searchapi1` igen.
5. Send 10 requests efter genstart.

Kommando:

```bash
BEFORE_REQUESTS=10 DURING_REQUESTS=40 AFTER_REQUESTS=10 REQUEST_DELAY_SECONDS=0.05 scripts/failover-test.sh
```

Raadata ligger i:

- `dokumentation/failover-results.csv`

### Resultater

| Fase | Requests | Succes | Fejl set af klient | Success rate | Avg latency | Min | Max | Instanser brugt |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| `before-stop` | 10 | 10 | 0 | 100% | 36.80 ms | 3.63 ms | 255.14 ms | `search-api-1`: 5, `search-api-2`: 5 |
| `during-stop` | 40 | 40 | 0 | 100% | 11.51 ms | 3.06 ms | 161.07 ms | `search-api-2`: 40 |
| `after-restart` | 10 | 10 | 0 | 100% | 26.18 ms | 3.35 ms | 207.61 ms | `search-api-1`: 3, `search-api-2`: 7 |

Load balancer stats efter testen:

| Backend | Attempts | Successes | Failures |
|---|---:|---:|---:|
| `search-api-1` | 283 | 261 | 22 |
| `search-api-2` | 304 | 304 | 0 |

De 22 failures paa `search-api-1` er interne backend-fejl registreret af load balanceren, mens `searchapi1` var stoppet. Klienten fik stadig HTTP 200, fordi load balanceren provede naeste backend og fik svar fra `search-api-2`.

Eksempel fra raadata:

```csv
2026-05-14T09:09:20Z,before-stop,1,200,0,miss,search-api-2 (http://searchapi2:8080),search-api-2,255.137
2026-05-14T09:09:20Z,before-stop,2,200,0,hit,search-api-1 (http://searchapi1:8080),search-api-1,67.935
2026-05-14T09:09:22Z,during-stop,1,200,0,hit,search-api-2 (http://searchapi2:8080),search-api-2,4.787
2026-05-14T09:09:25Z,during-stop,40,200,0,hit,search-api-2 (http://searchapi2:8080),search-api-2,13.739
2026-05-14T09:09:27Z,after-restart,6,200,0,hit,search-api-1 (http://searchapi1:8080),search-api-1,207.610
```

### Konklusion

Failover-testen viser, at brugeren fortsat kan soege gennem `SearchLoadBalancer`, selvom `search-api-1` stoppes. I testens `during-stop` fase lykkedes 40 ud af 40 requests, og alle blev serveret af `search-api-2`.

Det understoetter arkitekturvalget om en stateless `SearchApi` bag en load balancer: naar en replika fejler, kan trafikken fortsat betjenes af en anden replika.

### Begraensninger

- Testen er sekventiel og lokal, ikke en fuld chaos-/loadtest med mange samtidige klienter.
- Load balanceren har ikke aktiv health probing endnu; den opdager fejlen ved at prove backend og derefter route videre.
- Der er enkelte latency-spikes omkring stop/genstart, hvilket er forventeligt i et lille lokalt Compose-setup.

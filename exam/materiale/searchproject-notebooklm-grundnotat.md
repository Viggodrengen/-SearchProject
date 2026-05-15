# SearchProject – grundigt NotebookLM-notat til eksamen

> Formål: Dette dokument er skrevet som et samlet vidensgrundlag til NotebookLM. Det skal kunne bruges til at lave podcast, øve eksamen og forklare projektets arkitektur med en tydelig rød tråd i undervisningen.

## 0. Den korte fortælling

SearchProject er et walking skeleton for en søgeplatform. Systemet kan indeksere dokumenter, gemme et søgeindeks, modtage søgeforespørgsler, returnere rangerede resultater, cache gentagne søgninger og vise drift/performance via observability.

Det er ikke et færdigt enterprise-produkt. Det er et eksamensprojekt, der demonstrerer arkitekturprincipper i praksis:

- **Y-akse:** systemet er delt funktionelt i WebApp, Console-klient, SearchApi, Indexer, PostgreSQL, Redis, Nginx og observability.
- **X-akse:** SearchApi kan køre som flere stateless replikaer bag Nginx i Docker Compose eller Service/Ingress i Kubernetes.
- **Z-akse:** database-skalering er beskrevet som retning: read replicas, partitionering/sharding eller managed database.
- **Caching:** Redis bruges som cache-aside for gentagne søgninger.
- **Operations/observability:** Loki, Grafana og Prometheus bruges til logs, dashboards og metrics.
- **Deployment:** Docker Compose er demo-miljø; Kubernetes er produktionsretning.

Den vigtigste nyere beslutning er, at den hjemmelavede C# `SearchLoadBalancer` er fjernet. Load balancing/routing er flyttet til infrastrukturen:

- I Docker Compose: **Nginx**.
- I Kubernetes: **Ingress + Service**, eventuelt med Nginx Ingress Controller.

Det er en central eksamenspointe: routing er ikke forretningslogik og bør ikke ligge i applikationskoden.

---

## 1. Projektets komponenter

### 1.1 SearchWebApp

SearchWebApp er webgrænsefladen. Den er bygget som Blazor webapp og har ansvaret for brugerinteraktion:

- bruger skriver en søgning,
- vælger database,
- vælger antal resultater,
- vælger case-sensitivity,
- ser resultater, cache-status og hvilken API-instans der svarede.

SearchWebApp ejer ikke forretningslogikken. Den kalder et API endpoint via `SearchApiClient`.

Struktur:

- `SearchWebApp/Components/` – UI-komponenter.
- `SearchWebApp/Components/Pages/Home.razor` – søgesiden.
- `SearchWebApp/Services/SearchApiClient.cs` – HTTP-klient.
- `SearchWebApp/Interfaces/ISearchApiClient.cs` – interface for klienten.

Arkitekturargument: WebApp er en selvstændig deployable UI-service. Den kan skaleres eller ændres uafhængigt af SearchApi. Den fungerer som en slags lille micro-frontend i projektets walking skeleton.

### 1.2 ConsoleSearch

ConsoleSearch er en CLI-klient. Den viser, at SearchApi ikke er bundet til webappen. Flere klienttyper kan bruge samme API.

Arkitekturargument: Det understøtter separation mellem klienter og backend. API’et er den centrale kontrakt.

### 1.3 Nginx

Nginx er reverse proxy og load balancing-komponent i Docker Compose.

Den lytter på `localhost:5075` og videresender `/api/*` til:

- `searchapi1:8080`
- `searchapi2:8080`

Konfiguration:

- `docker/nginx/search-api.conf`

Hvorfor Nginx?

- Load balancing er et infrastrukturansvar.
- Det er mere realistisk end at skrive egen C# load balancer.
- Det matcher undervisning om deployment, operations og build vs buy: vi bygger ikke platformskomponenter selv, hvis standardkomponenter løser opgaven bedre.
- Det svarer bedre til produktionsretningen i Kubernetes, hvor Ingress/Service håndterer routing.

### 1.4 SearchApi

SearchApi er den vigtigste forretningsservice. Den modtager søgninger og returnerer resultater.

Struktur:

- `SearchApi/Interfaces/IDatabase.cs`
- `SearchApi/Repository/DatabaseFactory.cs`
- `SearchApi/Repository/DatabasePostgres.cs`
- `SearchApi/Repository/DatabaseSqlite.cs`
- `SearchApi/Services/SearchService.cs`
- `SearchApi/Services/SearchLogic.cs`
- `SearchApi/Services/SearchCacheOptions.cs`
- `SearchApi/Services/SearchMetrics.cs`
- `SearchApi/Program.cs`

Den følger en simpel struktur inspireret af tidligere AuthService: Interfaces, Repository og Services. Det er ikke overengineered, men viser god separation:

- Repository-laget skjuler databaseadgang.
- Service-laget orkestrerer cache og søgelogik.
- SearchLogic indeholder den centrale rankinglogik.
- Program.cs er composition root og HTTP endpoint setup.

SearchApi er stateless ift. HTTP requests. State ligger i PostgreSQL og Redis. Derfor kan SearchApi x-skaleres.

### 1.5 Indexer

Indexer opbygger søgeindekset. Den læser dokumenter og skriver ord/dokument-relationer til database.

Struktur:

- `indexer/Interfaces/IDatabase.cs`
- `indexer/Repository/DatabasePostgres.cs`
- `indexer/Repository/DatabaseSqlite.cs`
- `indexer/Services/App.cs`
- `indexer/Services/Crawler.cs`
- `indexer/Services/Renamer.cs`
- `indexer/Program.cs`

Arkitekturargument: Indexer er adskilt fra SearchApi, fordi indeksering og søgning har forskellige ansvar og runtime-profiler. Det er y-akse opdeling.

### 1.6 Shared

Shared indeholder kontrakter og DTO-lignende modeller:

- `SearchRequest`
- `SearchResult`
- `DocumentHit`
- `BEDocument`

Shared skal ikke indeholde forretningslogik. Det er kun fælles dataformer mellem projekterne.

### 1.7 PostgreSQL

PostgreSQL er persistent database. Den gemmer dokumenter, ord og relationer mellem dokumenter og ord.

Arkitekturargument:

- Persistent state er adskilt fra stateless services.
- I Docker Compose bruges volume.
- I Kubernetes kræver Postgres enten StatefulSet/PVC/backup eller managed database.

### 1.8 Redis

Redis er cache. SearchApi bruger cache-aside:

1. Modtag request.
2. Byg cache key.
3. Spørg Redis.
4. Ved hit returneres cached result.
5. Ved miss læses database og resultat gemmes i Redis med TTL.

Arkitekturargument: Redis reducerer latency og databasearbejde ved gentagne søgninger. Det kobler direkte til undervisningen om caching for performance and scale.

### 1.9 Loki, Grafana og Prometheus

Observability-stacken viser drift og runtime-adfærd:

- Loki: logs.
- Grafana: dashboards.
- Prometheus: metrics scraping.

Arkitekturargument: Man kan ikke kun tegne systemet; man skal kunne observere og måle det.

---

## 2. Runtime-flow: hvad sker der ved en søgning?

1. Brugeren åbner SearchWebApp på `localhost:5249`.
2. Brugeren indtaster query, fx “socal energy”.
3. WebApp bygger en `SearchRequest`.
4. WebApp kalder Nginx på `/api/search`.
5. Nginx fordeler requesten til enten `searchapi1` eller `searchapi2`.
6. SearchApi validerer query, databasevalg og maxAmount.
7. SearchService bygger cache key.
8. SearchService spørger Redis.
9. Hvis cache hit: returnér SearchResult.
10. Hvis cache miss: vælg repository via DatabaseFactory.
11. Repository læser relevante word IDs og dokumentmatches fra PostgreSQL.
12. SearchLogic ranker dokumenter efter matchende termer.
13. SearchResult returneres.
14. Ved miss gemmes resultatet i Redis.
15. UI viser resultat, cache-status og API-instans.
16. Logs/metrics kan ses i observability.

---

## 3. Modul-for-modul kobling til undervisningen

Denne sektion er vigtig til oplæsning og podcast: hvert modul kobles til en konkret del af projektet.

### Modul 1 – Introduktion til arkitekturprincipper

Undervisningsfokus:

- Hvad er arkitektur?
- Arkitektur som valg og trade-offs.
- Ansvar, kvalitetsegenskaber og begrundelser.

Projektkobling:

SearchProject viser, at arkitektur ikke kun er mapper og kode. Arkitektur er de valg, vi kan forklare:

- Hvorfor WebApp ikke taler direkte med databasen.
- Hvorfor SearchApi er stateless.
- Hvorfor cache ligger i Redis.
- Hvorfor routing ligger i Nginx/Ingress og ikke i C#.
- Hvorfor PostgreSQL er persistent state.
- Hvorfor observability er en del af systemet.

Eksamenspointe:

> “Vi bruger projektet til at vise arkitekturvalg i praksis: ansvar placeres bevidst, og hvert valg kan kobles til en kvalitetsegenskab som skalerbarhed, performance, drift eller maintainability.”

### Modul 2 – Case og systemforståelse

Undervisningsfokus:

- Forstå domænet/casen.
- Identificere centrale use cases.
- Bygge en arkitektur omkring systemets formål.

Projektkobling:

Use casen er dokumentsøgning. Derfor har systemet:

- en Indexer, der forbereder data,
- en SearchApi, der svarer på søgninger,
- klienter, der viser eller tester søgning,
- cache, fordi samme søgninger kan gentages.

Eksamenspointe:

> “Vi har ikke valgt komponenter tilfældigt. De følger af use casen: documents skal indekseres, queries skal besvares hurtigt, og resultater skal kunne observeres og måles.”

### Modul 3 – Skalering og AKF scaling cube

Undervisningsfokus:

- Scale out, not only scale up.
- X-, Y- og Z-akse.
- Skalering af service/kode og data.

Projektkobling:

Y-akse:

- WebApp, API, Indexer, Nginx, Redis, PostgreSQL og observability har forskellige ansvar.

X-akse:

- SearchApi kører som `searchapi1` og `searchapi2`.
- Nginx fordeler trafik.
- I Kubernetes svarer det til flere pods bag Service.

Z-akse:

- Ikke implementeret fuldt, men beskrevet som database-retning.
- Mulige shard keys: tenant, datadomæne, dokumenttype eller tid.

Eksamenspointe:

> “Det vigtigste implementerede skaleringseksempel er x-akse på SearchApi. Y-aksen ses i serviceopdelingen, mens z-aksen er vores database-retning for fremtidig vækst.”

### Modul 4–5 – Arkitekturarbejde og roadmap

Selvom projektets primære filer især rammer de senere moduler, kan modul 4–5 kobles til arkitekturarbejdets proces:

- identificere scope,
- lave walking skeleton,
- vælge hvad der implementeres nu,
- vælge hvad der kun beskrives som arkitekturretning.

Projektkobling:

Vi implementerer:

- Docker Compose runtime,
- Nginx,
- to API-instanser,
- Redis cache,
- tests og scripts,
- observability stack.

Vi beskriver som retning:

- Kubernetes produktion,
- HPA,
- Ingress/Service,
- database z-skalering,
- managed database eller read replicas.

Eksamenspointe:

> “Walking skeleton betyder, at vi implementerer nok til at demonstrere arkitekturen, men ikke bygger et komplet produktionssetup.”

### Modul 6 – Z-scale og data partitioning

Undervisningsfokus:

- Z-skalering som data partitioning/sharding.
- Dele data i uafhængige partitioner.
- Route requests til den partition, der ejer data.

Projektkobling:

SearchProject bruger én PostgreSQL database i demoen. Det er bevidst simpelt. Men vi kan forklare, hvordan z-aksen kunne udbygges:

- Shard efter tenant eller kunde.
- Shard efter datadomæne, fx mail, rapporter, logs.
- Partitionér efter tid, hvis dokumentmængden vokser historisk.
- Brug read replicas til tunge søgninger.
- Introducer routerlogik i API eller database-lag.

Trade-off:

- Fordel: bedre skalerbarhed og isolation.
- Ulempe: mere kompleks routing, migrationsstrategi, konsistens og drift.

Eksamenspointe:

> “Vi implementerer ikke sharding i walking skeleton’et, fordi kompleksiteten ikke står mål med casens størrelse. Men vi kan forklare konkret, hvordan z-skalering ville se ud.”

### Modul 7 – Intro til operations og Kubernetes

Undervisningsfokus:

- Docker images.
- Kubernetes overview.
- Pods, Services, Secrets, ConfigMaps.
- Miljøer og operations.

Projektkobling:

Docker Compose viser driftsmiljøet lokalt:

- container images for SearchApi, SearchWebApp og Indexer,
- standard images for PostgreSQL, Redis, Nginx, Loki, Grafana og Prometheus.

Kubernetes-diagrammet viser produktionsretningen:

- Ingress for ekstern adgang,
- Services for stabile endpoints,
- Pods/Deployments for workloads,
- Secrets/Vault for credentials,
- PVC for state.

Eksamenspointe:

> “Docker Compose er demo og udvikling; Kubernetes er modellen for produktion. Begge viser samme principper, men på forskellige modenhedsniveauer.”

### Modul 8 – Deployment, build vs buy, barrier conditions og rollback

Undervisningsfokus:

- Build vs buy.
- Barrier conditions.
- Rollback.
- Flere instanser og rolling updates.
- Deploymentstrategier.

Projektkobling:

Build vs buy:

- Vi bygger ikke vores egen load balancer.
- Vi bruger Nginx i Compose og Ingress/Service i Kubernetes.
- Vi bruger Redis, PostgreSQL, Grafana, Loki og Prometheus som standardkomponenter.

Barrier conditions:

- Før deployment bør build og tests være grønne.
- Docker Compose config skal validere.
- Health endpoint skal svare.
- Performance/failover scripts skal kunne køre.

Rollback:

- I Compose kan man rulle tilbage via tidligere image/commit.
- I Kubernetes kan Deployment lave rollout/rollback.

Eksamenspointe:

> “Vi fokuserer vores egen kode på søgedomænet og bruger standard infrastruktur til platformopgaver.”

### Modul 9 – Performance, metrics, OpenTelemetry og Prometheus

Undervisningsfokus:

- Performance and stress testing.
- Metrics.
- OpenTelemetry.
- Prometheus.
- Grafana.

Projektkobling:

SearchProject har:

- performance/cache script,
- Prometheus metrics endpoint,
- Grafana dashboard,
- cache hit/miss metrics,
- HTTP headers der viser cache og API-instans.

Performance-testens pointe:

- Cold-cache viser miss.
- Hot-cache viser hit.
- Det dokumenterer cachingens effekt.

Eksamenspointe:

> “Performance er ikke bare en påstand. Vi har scripts og metrics, så vi kan måle og forklare runtime-adfærd.”

### Modul 10 – Caching med Redis

Undervisningsfokus:

- Caching for performance and scale.
- Redis.
- Query caching.
- Distributed caching i ASP.NET Core.

Projektkobling:

SearchApi bruger `IDistributedCache` med Redis. Cache key bygges ud fra:

- database,
- case sensitivity,
- maxAmount,
- query terms.

Cache-aside flow:

1. Check cache.
2. Hit: returnér.
3. Miss: beregn resultat.
4. Gem i cache.
5. Returnér.

Trade-offs:

- TTL skal vælges fornuftigt.
- Cache invalidation er svært.
- Cache må ikke være eneste sandhed.

Eksamenspointe:

> “Redis er ikke bare en ekstra container. Den er en arkitekturkomponent, der ændrer performanceprofilen for gentagne søgninger.”

### Modul 11 – Databaser i Kubernetes og volumes

Undervisningsfokus:

- Volumes i Kubernetes.
- Databaser i Kubernetes.
- Skal man køre database i cluster eller bruge managed database?

Projektkobling:

PostgreSQL kører i Docker Compose med volume. I Kubernetes vil PostgreSQL kræve:

- PersistentVolumeClaim,
- backup/restore,
- resource limits,
- eventuelt StatefulSet,
- eller managed database udenfor cluster.

Eksamenspointe:

> “Stateless services er nemme at skalere; stateful databaser kræver mere driftsansvar.”

### Modul 12 – Databaser 2, StatefulSets og splitting databases

Undervisningsfokus:

- StatefulSets.
- Splitting databases.
- Read replicas.
- Database-skalering.

Projektkobling:

SearchProject kan vokse sådan:

- Step 1: én PostgreSQL database.
- Step 2: read replica til søgeforespørgsler.
- Step 3: partitionering efter datadomæne.
- Step 4: sharding med routing.
- Step 5: managed database eller specialiseret search engine hvis casen kræver det.

Eksamenspointe:

> “Z-akse er vores database-skalering. Den er bevidst ikke implementeret fuldt, men vi kan forklare en realistisk migrationsvej.”

---

## 4. Diagrammernes rolle

Den aktive diagramfil er:

- `diagrammer/SearchProject diagrams - UML + C4.drawio`

De vigtigste sider er:

1. **C4 Container syntax eksperiment**
2. **Kubernetes syntax eksperiment**

### C4 Container

C4 Container viser runtime-containere og ansvar:

- Bruger.
- SearchWebApp.
- ConsoleSearch.
- Nginx.
- SearchApi.
- Indexer.
- PostgreSQL.
- Redis.
- Observability.

Bruges til at forklare:

- Y-akse.
- X-akse.
- Caching.
- Observability.
- Hvorfor Nginx er infrastruktur.

### Kubernetes diagram

Kubernetes-diagrammet viser produktionsretning:

- Ingress.
- Nginx Ingress Controller.
- Services.
- Deployments.
- Pods.
- HPA.
- StatefulSet/PVC.
- Secrets/Vault.
- Observability namespace.

Bruges til at forklare:

- Hvordan Compose-arkitekturen kan oversættes til Kubernetes.
- Hvor load balancing sker.
- Hvordan stateless og stateful workloads adskilles.
- Hvordan cloud vs on-prem påvirker valg af ingress/load balancer.

---

## 5. Test og kvalitet

Projektet verificeres på flere niveauer:

### Unit tests

`SearchProject.Tests` tester SearchLogic. Det er hurtigt og uafhængigt af Docker.

### Build

Hele solution bygges med `dotnet build`.

### Docker Compose validation

`docker compose config --quiet` sikrer, at Compose-konfigurationen er gyldig.

### Smoke test

Health og search endpoint testes via Nginx på `localhost:5075`.

### Performance/cache script

`performance-cache-test.sh` viser cold-cache vs hot-cache.

### Failover script

`failover-test.sh` stopper en API-instans og verificerer, at trafikken stadig kan gå til den anden.

---

## 6. Fremlæggelsesstruktur

En stærk 8-10 minutters fremlæggelse kan være:

1. Problem og system: dokumentsøgning.
2. C4 Container: ansvar og serviceopdeling.
3. Nginx-beslutningen: routing flyttet til infrastruktur.
4. X/Y/Z-skalering.
5. Redis caching og performance.
6. Kubernetes produktionsretning.
7. Observability og tests.
8. Konklusion og trade-offs.

---

## 7. Svar på forventelige eksamensspørgsmål

### Hvorfor ikke egen C# load balancer?

Fordi load balancing er et infrastrukturansvar. Nginx, Ingress og Services er standardkomponenter, der er bedre egnede. C#-koden skal fokusere på søgedomænet.

### Er projektet microservices?

Det er et samlet repo, men med microservice-inspirerede deployable services. Ideelt kunne hver service ligge i eget repo, og infrastruktur i et separat repo. Til eksamen er mono-repo valgt for overskuelighed.

### Hvorfor Redis?

Fordi gentagne søgninger kan besvares hurtigere fra cache. Det reducerer latency og databasebelastning.

### Hvorfor Kubernetes?

Kubernetes viser produktionsretningen for scaling, deployment, service discovery, routing, secrets og storage.

### Hvad er den største begrænsning?

Database-skalering er mest planlagt, ikke implementeret. Det er bevidst, fordi z-skalering er kompleks og uden for walking skeletonets scope.

---

## 8. Endelig konklusion

SearchProject viser, hvordan undervisningens arkitekturprincipper kan bruges i et konkret system. Projektet forbinder kode, Docker Compose, Nginx, Redis, PostgreSQL, observability, tests og Kubernetes-diagrammer i én samlet fortælling.

Det vigtigste er ikke, at systemet er stort. Det vigtigste er, at det er forklarbart:

- Hver komponent har et ansvar.
- Hvert valg kan begrundes.
- Skalering kan forklares med x, y og z.
- Performance kan måles.
- Drift kan observeres.
- Produktion kan beskrives med Kubernetes.

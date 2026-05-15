# SearchProject – samlet AI-podcast- og oplæsningsnotat

> **Formål:** Dette dokument samler det vigtigste fra undervisningsmaterialet, eksamenskravene og SearchProject-løsningen i én sammenhængende tekst. Det er lavet til to ting:
>
> 1. At du kan læse op uden at skulle åbne 300+ filer i `docs/`.
> 2. At du kan give én samlet fil til NotebookLM/AI-podcast, så podcasten får teori, projektkobling, argumentation, trade-offs og mulige eksamensspørgsmål med.

---

## 0. Sådan skal notatet bruges

Hvis du bruger det til NotebookLM, så bed fx AI’en om:

> “Lav en eksamenspodcast på dansk, hvor I først forklarer arkitekturprincipperne fra undervisningen og derefter kobler dem til SearchProject. Brug konkrete eksempler: Nginx i stedet for custom load balancer, Redis caching, PostgreSQL, Docker Compose, ønsket Kubernetes-produktion, observability, X/Y/Z-skalering og database-roadmap. Stil også kritiske censor-spørgsmål og svar på dem.”

Hvis du bruger det til egen oplæsning, så læs især:

1. Eksamensrammen.
2. Modul 1–12 gennemgangen.
3. Arkitekturbeslutningerne.
4. Kubernetes-produktionsafsnittet.
5. Spørgsmål/svar-banken.

---

## 1. Hvad eksamen går ud på

Kildegrundlaget er især `docs/ai_eksamensprojektet-v1-0-1.md`.

Eksamensprojektet i Arkitekturprincipper i Praksis er ikke en normal afleveringsrapport. Projektet er grundlag for den mundtlige eksamen. Det skal forstås som et **walking skeleton**: et delsystem, hvor en del er implementeret, men hvor det vigtigste er, at arkitekturen kan forklares, begrundes og kvalitetssikres.

Eksamenskravene peger på disse temaer:

- Løsningen skal være i .NET/C#.
- Den skal kunne driftes modulært.
- Udvalgte komponenter bør kunne skaleres i X-aksen.
- Det skal være muligt at monitorere løsningen.
- Fejltolerance og performance skal kunne vurderes.
- Hvis der bruges database, skal der anvises en Z-skaleringsretning.
- Caching skal overvejes og begrundes.
- Til eksamen skal der gives en kort demo, og derefter præsenteres arkitektur, kodebase og driftsmiljø.
- Diagrammer skal bruges aktivt, fx C4/UML/deployment.

Den vigtigste konsekvens er:

> Man skal ikke bare vise, at systemet virker. Man skal vise, at arkitekturen kan forsvares.

Det betyder, at gode svar til eksamen har denne form:

1. Hvad var problemet?
2. Hvilket valg tog vi?
3. Hvilket alternativ fandtes?
4. Hvorfor valgte vi som vi gjorde?
5. Hvad er fordelen?
6. Hvad er ulempen?
7. Hvordan kan vi måle eller verificere det?

---

## 2. Den korte fortælling om SearchProject

SearchProject er et walking skeleton for en intern søgeplatform.

Systemet kan:

- indeksere dokumenter,
- gemme dokumenter og søgeindeks i database,
- modtage søgeforespørgsler,
- returnere rangerede resultater,
- cache gentagne søgninger,
- køre flere API-instanser bag Nginx,
- og vise drift/performance via observability-komponenter.

Den røde tråd er:

> SearchProject viser, hvordan man går fra en simpel søgecase til en mere driftbar og skalerbar arkitektur ved at placere ansvar rigtigt mellem applikationskode og infrastruktur.

Kernekomponenter:

- **SearchWebApp** – web-UI.
- **ConsoleSearch** – CLI-klient.
- **SearchApi** – søgelogik, ranking og cache-orchestration.
- **Indexer** – opbygger søgeindeks.
- **PostgreSQL** – persistent data.
- **Redis** – cache.
- **Nginx** – reverse proxy/load balancing i Docker Compose.
- **Grafana/Loki/Prometheus** – observability.

Den vigtigste arkitekturbeslutning er:

> Den hjemmelavede C# load balancer er fjernet og erstattet med Nginx.

Det er et stærkt fagligt valg, fordi det viser build-vs-buy, ansvarsplacering og produktionsrealistisk drift.

---

## 3. Modul 1 – Arkitekturprincipper

### Hvad handler modulet om?

Modul 1 handler om, hvad arkitekturprincipper er, og hvorfor de betyder noget. Arkitektur er ikke bare mapper og frameworks. Arkitektur er de beslutninger, der påvirker systemets kvalitet over tid.

Eksempler på kvalitetsegenskaber:

- skalerbarhed,
- performance,
- driftbarhed,
- vedligeholdbarhed,
- testbarhed,
- robusthed,
- forståelighed.

Et centralt princip er ansvarsplacering. Hvis et ansvar ligger forkert, bliver systemet sværere at ændre og drifte. Hvis routing ligger i domænekoden, bliver domænekoden ansvarlig for platformadfærd. Hvis cache og databaseadgang er spredt tilfældigt, bliver performance og fejlfinding sværere.

### Fagbegreber

- **Separation of concerns:** forskellige ansvar bør adskilles.
- **High cohesion:** en komponent bør have et fokuseret ansvar.
- **Low coupling:** komponenter bør kende så lidt som muligt til hinandens interne detaljer.
- **Trade-off:** et valg giver både fordele og ulemper.
- **Quality attribute:** en kvalitet som systemet skal opfylde, fx performance eller driftbarhed.

### Kobling til SearchProject

SearchProject bruger disse principper direkte:

- SearchWebApp har UI-ansvar.
- SearchApi har søgeansvar.
- Indexer har indeksansvar.
- PostgreSQL har persistent state.
- Redis har cache-ansvar.
- Nginx har routing/load balancing.
- Observability-stacken har driftssynlighed.

Den vigtigste pointe er, at vi ikke bare har “mange services for at have mange services”. Vi har komponenter, fordi de har forskellige ansvar og forskellige runtime-krav.

### Stærkt eksamenssvar

> “I vores projekt er arkitektur først og fremmest ansvarsplacering. Søgelogik ligger i SearchApi, routing ligger i Nginx, data ligger i PostgreSQL, cache ligger i Redis, og driftssynlighed ligger i observability. Det gør systemet lettere at forklare, teste, ændre og drifte.”

---

## 4. Modul 2 – Caseforståelse

### Hvad handler modulet om?

Modul 2 handler om søgecasen. Man skal forstå problemet før man vælger teknologi. En intern søgemaskine har mindst to forskellige flows:

1. Et **indekseringsflow**, hvor dokumenter bearbejdes og gemmes i et indeks.
2. Et **søgeflow**, hvor brugeren forventer hurtige resultater.

De to flows har forskellige krav. Indeksering kan ofte være batch-orienteret og tungere. Søgning er online og svartidsfølsom.

### Fagbegreber

- **Use case:** konkret bruger- eller systemopgave.
- **Online flow:** flow der skal svare hurtigt til brugeren.
- **Offline/batch flow:** flow der kan køre i baggrunden.
- **Systemgrænse:** hvad komponenten er ansvarlig for, og hvad den ikke er ansvarlig for.

### Kobling til SearchProject

SearchProject deler netop casen i to hoveddele:

- **Indexer** opbygger søgeindeks.
- **SearchApi** svarer på søgeforespørgsler.

Denne opdeling er en direkte casebaseret arkitekturbeslutning. Det ville være enklere at lave alt i én service, men det ville blande to forskellige workloads.

### Stærkt eksamenssvar

> “Vi har adskilt Indexer og SearchApi, fordi casen har to forskellige runtime-profiler: forberedende indeksering og hurtig online søgning. Den opdeling gør systemet lettere at skalere og forklare.”

---

## 5. Modul 3 – Skalering og AKF X/Y/Z

### Hvad handler modulet om?

Modul 3 introducerer skalering. Skalering handler ikke kun om at købe større maskiner. Det handler om strukturelle valg.

AKF-kuben beskriver tre skaleringstyper:

- **X-akse:** flere ens instanser af samme service.
- **Y-akse:** funktionel opdeling af systemet.
- **Z-akse:** opdeling af data efter en nøgle, fx kunde, tenant, geografisk område eller tid.

### SearchProject og X-akse

SearchApi er stateless i forhold til HTTP-requests. State ligger i PostgreSQL og Redis. Derfor kan SearchApi køre som flere replikaer.

I Docker Compose kører der flere API-instanser bag Nginx.

I Kubernetes svarer det til flere pods bag en Service, eventuelt med HPA.

### SearchProject og Y-akse

Y-aksen ses i den funktionelle opdeling:

- WebApp,
- ConsoleSearch,
- SearchApi,
- Indexer,
- PostgreSQL,
- Redis,
- Nginx,
- observability.

### SearchProject og Z-akse

Z-aksen er ikke fuldt implementeret. Den er en roadmap:

1. Start med én PostgreSQL database.
2. Tilføj read replicas for læsetunge søgninger.
3. Partitionér efter fx tenant, dokumenttype, domæne eller tid.
4. Overvej sharding med routing, hvis datavolumen kræver det.

### Stærkt eksamenssvar

> “Vi har implementeret X og Y i walking skeleton. X gennem flere API-instanser bag Nginx, og Y gennem serviceopdeling. Z er bevidst roadmap, fordi dataskalering er dyrere og mere kompleks end API-skalering.”

---

## 6. Modul 4 – Y-skalering i praksis

### Hvad handler modulet om?

Modul 4 handler om at splitte kode og ansvar funktionelt. Det handler ikke nødvendigvis om at deploye alt separat med det samme. Først skal man forstå ansvarsgrænser.

Y-skalering i praksis betyder:

- Hvad er UI?
- Hvad er API?
- Hvad er dataadgang?
- Hvad er domænelogik?
- Hvad er infrastruktur?

### Kobling til SearchProject

SearchWebApp og ConsoleSearch er klienter. De bruger SearchApi via en kontrakt. SearchApi ejer søgeflowet. Repository-laget skjuler databaseadgang. Services-laget orkestrerer søgning, cache og ranking.

Den simple struktur i SearchApi er:

- `Interfaces/`
- `Repository/`
- `Services/`

Det er bevidst simpelt. Det er nok til at vise separation uden at overengineere.

### Stærkt eksamenssvar

> “Y-skalering handler i vores projekt om at splitte ansvar på en måde, der gør systemet forståeligt. Vi har ikke lavet tunge enterprise-lag, fordi projektet er et walking skeleton, men vi har tydelig separation mellem UI, API, dataadgang og services.”

---

## 7. Modul 5 – X-skalering og load balancing

### Hvad handler modulet om?

Modul 5 handler om at køre flere instanser og fordele trafik mellem dem.

Vigtige begreber:

- replica,
- stateless service,
- load balancing,
- health checks,
- failover.

### Kobling til SearchProject

SearchApi kan køre som flere instanser, fordi API-instansen ikke ejer persistent state. Det gør den velegnet til X-skalering.

Tidligere havde projektet en custom C# load balancer. Den blev fjernet.

I stedet bruges:

- **Nginx** lokalt i Docker Compose.
- **Ingress + Service** som produktionsretning i Kubernetes.

### Hvorfor var det rigtige valg?

Custom load balancing i C# viser forståelse, men det er ikke et stærkt produktionsvalg for projektet. Load balancing er en platformopgave. Nginx, Ingress og Kubernetes Service er standardkomponenter med bedre driftsmodel.

### Stærkt eksamenssvar

> “Vi beholdt X-skaleringsprincippet fra modul 5, men ændrede implementationen. Flere API-instanser er stadig centrale, men routing er flyttet fra C#-kode til Nginx og senere Kubernetes Service/Ingress.”

---

## 8. Modul 6 – Z-scale og dataskalering

### Hvad handler modulet om?

Z-skalering handler om at splitte data. Det er ofte sværere end at skalere services, fordi data har konsistens, ejerskab, migration og recovery.

Mulige strategier:

- read replicas,
- partitionering,
- sharding,
- splitting databases efter domæne,
- managed database med skaleringsfunktioner.

### Kobling til SearchProject

SearchProject bruger én PostgreSQL database i demoen. Det er nok til walking skeleton.

Men eksamenskravet siger, at hvis der bruges database, skal man kunne anvise Z-skalering. Derfor har projektet en roadmap:

1. **Single Postgres:** enkel start.
2. **Read replicas:** aflaster læsetrafik.
3. **Partitionering:** fx efter dokumenttype, tenant, tidsperiode eller datadomæne.
4. **Sharding:** hvis datamængde og belastning kræver det.

### Trade-off

Fordele:

- højere kapacitet,
- bedre isolation,
- mulighed for at håndtere større datamængder.

Ulemper:

- kompleks routing,
- vanskeligere migration,
- sværere konsistens,
- mere kompleks backup/restore.

### Stærkt eksamenssvar

> “Vi implementerer ikke fuld Z-skalering i walking skeleton, fordi det ville være overengineering. Men vi har en konkret migrationsvej: read replicas først, partitionering senere og sharding kun ved dokumenteret behov.”

---

## 9. Modul 7 – Operations og Kubernetes-intro

### Hvad handler modulet om?

Modul 7 handler om drift. Det er en vigtig pointe: arkitektur stopper ikke, når koden compiler.

Centrale emner:

- Docker,
- Kubernetes,
- Pods,
- Services,
- Secrets,
- ConfigMaps,
- miljøer,
- deploymentdiagrammer.

### Docker Compose vs Kubernetes

Docker Compose er godt til lokal demo og udvikling. Det kan starte hele stacken hurtigt og reproducibelt.

Kubernetes er mere relevant som produktionsmodel, fordi det giver:

- service discovery,
- rolling updates,
- scaling,
- health checks,
- secrets/config management,
- bedre driftsoverblik.

### Kobling til SearchProject

SearchProject bruger Compose til demo:

- SearchWebApp,
- Nginx,
- SearchApi-instanser,
- Redis,
- PostgreSQL,
- observability.

Kubernetes-diagrammet viser ønsket produktion:

- Ingress,
- Service,
- Deployments,
- Pods,
- HPA,
- Secrets,
- PVC/StatefulSet eller managed DB.

### Stærkt eksamenssvar

> “Compose viser, at systemet virker lokalt. Kubernetes-diagrammet viser, hvordan samme arkitektur kan driftes mere modent i produktion.”

---

## 10. Modul 8 – Deploymentprincipper, Build vs Buy, Barrier Conditions og Rollback

### Build vs Buy

Build-vs-buy handler om, hvad man selv bør bygge, og hvad man bør bruge som standardkomponent.

Man bør typisk bygge det, der er domænespecifikt og differentierende.

Man bør typisk bruge standardkomponenter til generiske platformopgaver.

### Kobling til SearchProject

Vi bygger selv:

- SearchApi,
- SearchLogic,
- cache-orchestration,
- indeksflow.

Vi bruger standardkomponenter til:

- Nginx routing,
- Redis cache,
- PostgreSQL database,
- Grafana dashboards,
- Loki logs,
- Prometheus metrics.

Det vigtigste build-vs-buy eksempel er fjernelsen af custom C# load balancer.

### Barrier Conditions

Barrier conditions er gates før release. I SearchProject kan de være:

- `dotnet build` skal være grøn,
- xUnit tests skal være grønne,
- `docker compose config --quiet` skal være grøn,
- health endpoint skal svare,
- performance/cache script skal kunne køre,
- failover-test skal kunne køre.

### Rollback

Rollback betyder, at man kan gå tilbage, hvis release fejler.

I Compose kan man rulle tilbage via tidligere commit/image.

I Kubernetes kan man bruge deployment rollout/rollback.

### Helm

Helm kan bruges i en fremtidig Kubernetes-produktion til pakkestyring, versionering og rollback.

### Stærkt eksamenssvar

> “Det professionelle valg var ikke at bygge mere kode, men at fjerne platformkode. Vi bygger søgedomænet selv og bruger standardkomponenter til routing, caching og observability.”

---

## 11. Modul 9 – Performance, metrics og observability

### Hvad handler modulet om?

Modul 9 handler om at måle systemets adfærd. Performance skal ikke bare påstås. Den skal måles.

Centrale begreber:

- latency,
- throughput,
- error rate,
- metrics,
- logs,
- traces,
- dashboards,
- OpenTelemetry,
- Prometheus,
- Grafana.

### Kobling til SearchProject

SearchProject har:

- performance/cache script,
- failover script,
- Prometheus metrics,
- Grafana dashboards,
- Loki logs.

Et vigtigt performanceargument er cold cache vs hot cache:

- Cold cache: første søgning rammer database.
- Hot cache: gentagen søgning kan returneres fra Redis.

### SLI/SLO-tænkning

Mulige SLI’er:

- P95 latency for search endpoint,
- error rate,
- cache hit ratio,
- DB query time.

Mulige SLO’er:

- 99% af søgninger under en defineret grænse,
- cache hit ratio over et mål for gentagne queries,
- lav 5xx-rate.

### Stærkt eksamenssvar

> “Vi bruger observability for at gøre arkitekturvalgene målbare. Redis er ikke bare en ekstra container; dens effekt kan demonstreres i cold/hot cache-scenarier.”

---

## 12. Modul 10 – Caching og Redis

### Hvad handler modulet om?

Caching bruges til at forbedre performance og aflaste backend-systemer.

Redis er en in-memory cache, der passer godt til distribuerede systemer.

### Cache-aside pattern

SearchProject bruger cache-aside:

1. SearchApi modtager request.
2. SearchApi bygger cache key.
3. Redis spørges.
4. Ved hit returneres cachet resultat.
5. Ved miss læses database.
6. Resultatet rankes.
7. Resultatet gemmes i Redis med TTL.
8. Resultatet returneres.

### Trade-offs

Fordele:

- lavere latency,
- mindre databasebelastning,
- bedre performance ved gentagne queries.

Ulemper:

- stale data,
- invalidation,
- ekstra infrastruktur,
- risiko for forkert cache key.

### Stærkt eksamenssvar

> “Cache er et bevidst performancevalg. Vi accepterer risikoen for stale data, men styrer den med TTL, cache-key design og fallback til database.”

---

## 13. Modul 11 – Databaser i Kubernetes og Volumes

### Hvad handler modulet om?

Databaser i containerplatforme er sværere end stateless services.

Stateless API-pods kan smides væk og genskabes. Database-state må ikke forsvinde.

Centrale begreber:

- PersistentVolume,
- PersistentVolumeClaim,
- backup,
- restore,
- RPO,
- RTO,
- managed database.

### Kobling til SearchProject

PostgreSQL er stateful. I Docker Compose bruges volume. I Kubernetes kræver Postgres en tydelig strategi:

- StatefulSet/PVC,
- backup/restore,
- resource limits,
- monitoring,
- eller managed database udenfor cluster.

### Stærkt eksamenssvar

> “Den sværeste del af produktionsmodenhed er ikke SearchApi. Det er stateful data. Derfor skal Postgres håndteres med persistence, backup og recovery-strategi.”

---

## 14. Modul 12 – StatefulSets, replicas og splitting databases

### Hvad handler modulet om?

Modul 12 samler database-skalering og stateful drift.

Vigtige emner:

- StatefulSets,
- read replicas,
- database splitting,
- partitionering,
- sharding,
- consistency trade-offs.

### Kobling til SearchProject

SearchProject kan udvikles i trin:

1. Single PostgreSQL.
2. Read replica til søgeforespørgsler.
3. Partitionering efter domæne, tenant eller tid.
4. Sharding med routing.
5. Eventuelt managed database eller specialiseret search engine.

### Stærkt eksamenssvar

> “Database-roadmap’et er vores Z-skaleringsstrategi. Det er ikke implementeret fuldt nu, fordi walking skeleton skal demonstrere arkitektur, ikke al fremtidig kompleksitet.”

---

## 15. Tværgående emner fra undervisningen

### Miljøbeskrivelser

Man skal kunne skelne mellem:

- development,
- test/staging,
- production.

SearchProject bruger Compose som udviklings- og demo-miljø. Kubernetes er målarkitektur for produktion.

### Monitoring af applikationer

Monitorering er ikke pynt. Det er en forudsætning for drift.

Uden monitorering ved man ikke:

- om performance falder,
- om fejl stiger,
- om cache virker,
- om database bliver flaskehals.

### OpenTelemetry og Prometheus

OpenTelemetry er en standard til instrumentering. Prometheus samler metrics. Grafana visualiserer dem.

SearchProject viser denne retning gennem observability-stacken.

### Helm

Helm kan bruges til at pakke Kubernetes deployments, versionere releases og understøtte rollback.

---

## 16. Arkitekturbeslutninger i SearchProject

## Beslutning 1: Nginx i stedet for custom C# load balancer

### Problem

Load balancing skulle håndteres mellem flere SearchApi-instanser.

### Alternativer

- custom C# load balancer,
- Nginx,
- Kubernetes Service/Ingress,
- cloud load balancer.

### Valg

Nginx i Docker Compose og Ingress/Service som Kubernetes-retning.

### Hvorfor

Routing er infrastrukturansvar. Det er ikke søgedomæne.

### Fordele

- mindre egen kode,
- mere realistisk drift,
- bedre alignment med Kubernetes,
- tydelig build-vs-buy.

### Ulemper

- mere infrastrukturkonfiguration,
- endnu en komponent at forstå.

---

## Beslutning 2: Enkel kodearkitektur

SearchApi bruger en enkel struktur:

- Interfaces,
- Repository,
- Services.

Det passer til projektets størrelse og gør koden let at forklare.

Alternativet kunne være tung Domain/Application/Infrastructure-lagdeling. Det blev fravalgt, fordi det ville give mere struktur end projektet reelt har brug for.

---

## Beslutning 3: Redis cache-aside

Redis bruges, fordi søgning er read-heavy og gentagne queries er sandsynlige.

Det giver performancefordel, men kræver TTL og invalidation-overvejelse.

---

## Beslutning 4: Compose nu, Kubernetes senere

Compose bruges, fordi det er stabilt til lokal demo.

Kubernetes bruges som produktionsretning, fordi det passer til drift, skalering, release og observability.

---

## 17. Runtime-flow ved en søgning

1. Brugeren søger i SearchWebApp.
2. WebApp kalder Nginx.
3. Nginx sender request til en SearchApi-instans.
4. SearchApi bygger cache key.
5. Redis spørges.
6. Ved hit returneres resultat hurtigt.
7. Ved miss læses PostgreSQL.
8. SearchLogic ranker resultater.
9. Resultatet gemmes i Redis med TTL.
10. Resultatet returneres til UI.
11. Logs/metrics kan ses i observability.

Dette flow binder flere moduler sammen:

- Modul 2: use case.
- Modul 3/5: skalering og load balancing.
- Modul 8: Nginx som build-vs-buy.
- Modul 9/10: performance og cache.
- Modul 11/12: database som stateful komponent.

---

## 18. Ønsket produktionssetup i Kubernetes

### Målarkitektur

- Ekstern bruger går ind via Ingress.
- Ingress sender trafik til Service.
- Service fordeler trafik til SearchApi pods.
- SearchApi pods bruger Redis og PostgreSQL.
- Observability scraper metrics og samler logs.
- Secrets håndterer credentials.
- ConfigMaps håndterer miljøkonfiguration.
- HPA kan skalere SearchApi.
- PostgreSQL håndteres med PVC/StatefulSet eller managed database.

### Hvorfor Kubernetes?

Ikke fordi det er moderne, men fordi det løser konkrete driftsproblemer:

- standardiseret deployment,
- rolling updates,
- autoskalering,
- service discovery,
- health checks,
- secrets/config management,
- observability integration.

### Ulemper

- højere kompleksitet,
- krav om platformkompetence,
- stateful drift er vanskelig,
- flere komponenter kan fejle.

### Stærkt eksamenssvar

> “Kubernetes er vores målarkitektur, fordi det giver en moden driftsmodel for de samme komponenter, vi demonstrerer i Compose. Men vi er bevidste om, at stateful data og platformkompetence er de største omkostninger.”

---

## 19. Failure-scenarier

### Én SearchApi går ned

Nginx kan fortsætte med at route til den anden instans. Failover-testen demonstrerer dette.

### Redis går ned

Systemet bør kunne falde tilbage til database. Performance bliver dårligere, men korrekthed bevares.

### PostgreSQL bliver flaskehals

Først måles query performance. Derefter kan man optimere queries/indexes, tilføje read replicas eller planlægge partitionering.

### Fejlfuld release

Barrier conditions bør stoppe release. Hvis fejl først opdages efter release, skal rollback være muligt.

---

## 20. Hvad skal vises i kodegennemgangen?

Til 2–3 minutters kodefokus bør man ikke åbne alt.

Vælg 1–2 centrale steder:

1. **SearchService** – viser orchestration mellem request, cache og repository.
2. **SearchLogic** – viser selve domæneadfærden/ranking.
3. **Repository/DatabaseFactory** – viser databaseabstraktion.
4. **Program.cs** – viser composition, endpoints og infrastruktur wiring.

Budskab:

> “Koden er enkel med vilje. Den viser arkitekturprincipper uden at gøre projektet tungere end nødvendigt.”

---

## 21. Spørgsmål og svar til eksamen

### Hvorfor ikke custom load balancer?

Fordi routing er platformansvar. Build-vs-buy siger, at vi bør bruge standardkomponenter til generiske platformopgaver.

### Hvorfor Redis?

Fordi søgning er read-heavy, og gentagne queries kan besvares hurtigere fra cache.

### Hvorfor PostgreSQL?

Fordi projektet har persistent dokument- og indeksdata, der kræver relationel lagring og konsistens.

### Hvorfor ikke fuld Kubernetes nu?

Fordi projektet er et walking skeleton. Compose giver stabil demo, Kubernetes viser målarkitektur.

### Hvor er Z-skalering?

I roadmap: read replicas, partitionering og eventuelt sharding.

### Hvad er største risiko?

Stateful datadrift i produktion: backup, restore, failover og konsistens.

### Hvordan beviser I performance?

Med cold/hot cache tests, metrics og dashboards.

### Er det microservices?

Det er microservice-inspireret. Det er et mono-repo, men runtime er opdelt i deployable komponenter.

---

## 22. Podcast-disposition

Hvis dette bruges til AI-podcast, kan podcasten følge denne struktur:

1. Introduktion til eksamenskrav og walking skeleton.
2. Kort fortælling om SearchProject.
3. Modul 1–3: principper, case og skalering.
4. Modul 4–6: serviceopdeling, X-skalering og Z-roadmap.
5. Modul 7–8: operations, Kubernetes, build-vs-buy og rollback.
6. Modul 9–10: performance, metrics og Redis caching.
7. Modul 11–12: databaser, volumes, StatefulSets og splitting.
8. Arkitekturbeslutningen om Nginx.
9. Kubernetes-produktionssetup med fordele og ulemper.
10. Censor-spørgsmål og stærke svar.

---

## 23. Afsluttende statement

SearchProject er stærkt som eksamensprojekt, fordi det ikke bare viser kode. Det viser arkitekturtænkning:

- ansvar er placeret bevidst,
- skalering kan forklares med X/Y/Z,
- routing er flyttet til infrastruktur,
- cache er begrundet med performance,
- database-roadmap er realistisk,
- drift og observability er tænkt ind,
- Kubernetes-produktionsretningen er konkret,
- og trade-offs er tydelige.

Den bedste afslutningssætning er:

> “Vi har bygget et bevidst afgrænset walking skeleton, hvor koden demonstrerer domænet, og arkitekturen demonstrerer, hvordan løsningen kan skaleres, observeres og modnes i produktion.”

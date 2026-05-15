# SearchProject – NotebookLM grundnotat (teori + argumentation + eksamen)

> **Formål**
> Dette notat er lavet til tre ting:
> 1) at du kan læse dig op på **faglig teori** før eksamen,
> 2) at du kan koble teorien direkte til **SearchProject**,
> 3) at NotebookLM kan lave en podcast med **dyb argumentation** i stedet for overfladiske highlights.

---

## 0. Hvad eksamen faktisk måler (kilde: `docs/ai_eksamensprojektet-v1-0-1.md`)

Eksamensprojektet er et **walking skeleton**. Det betyder:

- I skal ikke levere et færdigt enterprise-produkt.
- I skal vise, at I kan anvende arkitekturprincipper i et realistisk delsystem.
- I skal kunne argumentere for valg i både kode og drift.

Eksamen forventer typisk:

1. kort demo i drift,
2. arkitekturpræsentation (diagrammer),
3. kodegennemgang af nøgledele,
4. forklaring af driftsmiljø, monitorering, caching og skalering.

**Nøgleindsigt:**
Du scorer ikke ved at nævne flest værktøjer. Du scorer ved at vise:

- problemforståelse,
- begrundede valg,
- konsekvensforståelse (fordele/ulemper),
- og realistisk produktionsretning.

---

## 1. Din røde tråd (brug den hele vejen)

Hvis du går i stå, så vend altid tilbage til:

1. **Problem:** intern søgning skal være hurtig, stabil og forklarbar.
2. **Valg:** ansvar fordeles mellem WebApp, SearchApi, Indexer, Nginx, Redis, PostgreSQL, observability.
3. **Bevis:** build/test, failover, performance/cache, og klar Kubernetes-retning.

Korte kernesætninger du kan gentage:

- “Vi prioriterer ansvar før teknologi.”
- “Vi bygger domænelogik selv, men bruger standardkomponenter til platformopgaver.”
- “Vi måler effekter i stedet for at antage dem.”

---

## 2. SearchProject i én arkitektursætning

> SearchProject er en microservice-inspireret søgeløsning, hvor online søgning, indeksering, caching, routing og observability er adskilt, så systemet kan forklares, skaleres og driftes med tydelige trade-offs.

### 2.1 Komponenter

- **SearchWebApp**: UI og brugerflow.
- **ConsoleSearch**: alternativ klient (viser API-kontraktens uafhængighed).
- **SearchApi**: søgelogik, ranking, cache-orchestration.
- **Indexer**: opbygger indeksdata (offline/forberedende flow).
- **PostgreSQL**: persistent data.
- **Redis**: cache for gentagne søgninger.
- **Nginx**: reverse proxy/load balancing i Compose.
- **Grafana/Loki/Prometheus**: observability.

### 2.2 Kodeprincip i SearchApi

Strukturen `Interfaces / Repository / Services` er bevidst enkel:

- nok separation til testbarhed,
- lav kompleksitet,
- hurtig gennemskuelighed til eksamen.

Det er et bevidst valg imod “for tung enterprise layering”.

---

## 3. Arkitekturbeslutninger (ADR-format)

## ADR-1: Custom C# load balancer fjernet → Nginx indført

**Kontekst:**
Tidligere havde projektet en custom load balancing-komponent i C#.

**Beslutning:**
Routing/load balancing flyttes til infrastruktur (Nginx i Compose, Ingress/Service i K8s).

**Hvorfor:**

- build-vs-buy (modul 8),
- tydelig ansvarsplacering,
- færre fejl i egen platformkode,
- bedre alignment med produktionspraksis.

**Alternativer:**

- beholde custom LB,
- bruge .NET YARP i egen service,
- cloud-managed gateway direkte.

**Konsekvenser:**

- plus: mindre domænestøj, bedre driftsmodel,
- minus: mere infrastrukturkonfiguration.

---

## ADR-2: Beholde enkel SearchApi struktur

**Kontekst:**
Der var mulighed for tung Domain/Application/Infrastructure-opdeling.

**Beslutning:**
Enkel struktur bevares for læsbarhed og fokus.

**Hvorfor:**

- passer til projektets størrelse,
- hurtig onboarding,
- stærk eksamensforklarbarhed.

**Konsekvens:**

- plus: lav kognitiv belastning,
- minus: mindre “enterprise-eksplicit” lagdeling.

---

## ADR-3: Redis cache-aside

**Kontekst:**
Søgning har gentagne forespørgsler.

**Beslutning:**
SearchApi bruger cache-aside med TTL.

**Hvorfor:**

- lavere latency ved hits,
- mindre databasebelastning,
- passer til distributed runtime.

**Trade-offs:**

- stale data,
- key design og invalidation,
- ekstra driftspunkt.

---

## ADR-4: Compose som demo, Kubernetes som produktionsretning

**Kontekst:**
Eksamen kræver driftstankegang, men ikke fuld enterprise-udrulning.

**Beslutning:**
Compose bruges til stabil demo; Kubernetes beskrives som ønsket produktion.

**Hvorfor:**

- korrekt modenhedsniveau for walking skeleton,
- viser både praktisk drift nu og realistisk evolution.

---

## ADR-5: Z-skalering som roadmap, ikke big bang implementation

**Kontekst:**
Database-skalering er kompleks og dyr at implementere tidligt.

**Beslutning:**
Roadmap: single DB → read replicas → partitionering → evt. sharding.

**Hvorfor:**

- styrer kompleksitet,
- følger målte behov,
- undgår overengineering.

---

## 4. Modul 1–12: teori + begreber + kobling + mini-svar

Nedenfor er den del, der skal give dig **faglig dybde** før eksamen.

---

## Modul 1 – Intro til arkitekturprincipper

### Hvad modulet handler om

Arkitektur er ikke “hvilket framework vi valgte”. Arkitektur er de valg, der bestemmer:

- hvordan systemet ændres over tid,
- hvordan det fejler,
- hvordan det driftes,
- og hvem der ejer hvilke beslutninger.

### Begreber du skal kunne

- separation of concerns,
- cohesion/coupling,
- quality attributes,
- trade-offs,
- architecture as decisions.

### SearchProject-kobling

Ansvar er opdelt i klient, API, indeks, data, cache, proxy og observability.

### Hvorfor valget er fagligt stærkt

Uden tydelig ansvarsdeling bliver både fejlfinding og skalering dyrere.

### Mini-svar til eksamen

> “Vores vigtigste M1-princip er ansvarsplacering: søgelogik i API, routing i infrastruktur, state i database og driftssynlighed i observability.”

---

## Modul 2 – Intro case

### Hvad modulet handler om

Casearbejde betyder, at systemet skal formes af problemet: hvad er workflow, hvem er bruger, hvad er kritisk svartid, hvor er dataflow?

### Begreber

- use case,
- online vs offline flow,
- bounded responsibility.

### SearchProject-kobling

Vi har to tydelige flows:

1. **Indexer-flow** (forberedende, batch-orienteret),
2. **Search-flow** (online, svartidsfølsomt).

### Argumentation

Denne opdeling gør det muligt at optimere flows forskelligt uden at koble alt sammen i én service.

### Mini-svar

> “Indexer og SearchApi findes, fordi casen har to runtime-profiler: tung forberedelse og hurtig online søgning.”

---

## Modul 3 – Skalering (AKF X/Y/Z)

### Hvad modulet handler om

Skalering er ikke kun flere CPU-kerner. Man skalerer via struktur:

- **X**: flere instanser,
- **Y**: funktionel opdeling,
- **Z**: dataopdeling.

### Begreber

- stateless scaling,
- service decomposition,
- partitioning bias.

### SearchProject-kobling

- X: `searchapi1` + `searchapi2` bag Nginx.
- Y: adskilte komponenter for UI/API/index/cache/data/observability.
- Z: planlagt dataevolution.

### Argumentation

X og Y giver hurtig praktisk gevinst. Z holdes som roadmap pga. høj kompleksitet.

### Mini-svar

> “Vi har implementeret X og Y nu, og vi har en konkret Z-plan, så vi kan skalere data når behovet er målt.”

---

## Modul 4 – Workshop med y-skalering

### Hvad modulet handler om

Y-skalering træner evnen til at splitte systemansvar efter funktion, ikke efter teknologi.

### Begreber

- service boundaries,
- contract-first thinking,
- responsibility-driven decomposition.

### SearchProject-kobling

SearchWebApp/ConsoleSearch er klienter; SearchApi er backendkontrakt. Dette reducerer kobling.

### Argumentation

Når klienter er adskilt fra API-kontrakt, kan UI ændres uden at ødelægge søgelogikken.

### Mini-svar

> “Y-skalering i vores projekt er først og fremmest ansvars-skalering: vi splitter funktioner før vi splitter deployment.”

---

## Modul 5 – X-skalering af kode

### Hvad modulet handler om

Horisontal skalering og trafikfordeling.

### Begreber

- replica,
- load balancing,
- failure isolation.

### SearchProject-kobling

Flere SearchApi-instanser kører bag Nginx.

### Argumentation

Vi tog modul 5-princippet og gjorde det mere professionelt med infrastruktur-routing.

### Mini-svar

> “Vi bevarer X-princippet, men flytter mekanismen ud af domænekoden.”

---

## Modul 6 – Z-scale

### Hvad modulet handler om

Dataskalering med partitionering/splitting, når volumen eller adgangsmønster kræver det.

### Begreber

- read replicas,
- partitioning,
- sharding,
- routing strategy.

### SearchProject-kobling

Roadmap:

1. single Postgres,
2. read replicas,
3. partitionering efter domæne/tenant/tid,
4. evt. sharding.

### Argumentation

Z-skalering giver kapacitet, men gør konsistens, migration og drift markant sværere.

### Mini-svar

> “Vi har bevidst valgt en trinvis Z-roadmap, fordi dataskalering er den dyreste arkitekturændring.”

---

## Modul 7 – Intro til operations

### Hvad modulet handler om

Arkitektur er også driftsarkitektur: miljøer, deploymentmodeller og operationel synlighed.

### Begreber

- dev/test/prod,
- container runtime,
- Kubernetes primitives.

### SearchProject-kobling

Compose bruges til demo/validering. Kubernetes-diagrammet viser ønsket produktion.

### Argumentation

Det er fagligt stærkt at kunne skelne mellem “hvad vi demonstrerer nu” og “hvordan vi drifter i skala”.

### Mini-svar

> “Compose er vores verificerbare demo-miljø; Kubernetes er vores målarkitektur for drift.”

---

## Modul 8 – Deploymentprincipper

### Hvad modulet handler om

At reducere release-risiko gennem:

- build-vs-buy,
- barrier conditions,
- rollback,
- monitorering.

### Begreber

- release gates,
- rollback window,
- operational readiness,
- Helm som deployment-pakkestyring.

### SearchProject-kobling

- build: SearchApi-domænelogik,
- buy/use: Nginx, Redis, Postgres, Prometheus, Grafana, Loki,
- gates: build/test/compose/health/scripts.

### Argumentation

Det mest modne valg var at fjerne custom LB-kode fremfor at “vinde” på egen platformkode.

### Mini-svar

> “Vi bruger build-vs-buy aktivt: vi bygger det der differentierer (søgelogik), og køber det der er commoditized (routing/observability).”

---

## Modul 9 – Performance og metrics

### Hvad modulet handler om

Performance engineering kræver hypoteser, målinger og feedback loops.

### Begreber

- latency/throughput,
- instrumentation,
- telemetry pipeline,
- KPI vs tekniske metrics.

### SearchProject-kobling

- performance-cache script,
- metrics-scraping,
- dashboards,
- synlig cache hit/miss-adfærd.

### Argumentation

Uden målinger bliver arkitekturvalg trosbaserede.

### Mini-svar

> “Vi beviser cache-effekten med cold/hot-scenarier i stedet for at antage forbedring.”

---

## Modul 10 – Caching

### Hvad modulet handler om

Caching som arkitekturprincip: hvor i flowet man cacher, og hvad man betaler for det.

### Begreber

- cache-aside,
- TTL,
- invalidation,
- staleness budget.

### SearchProject-kobling

SearchApi bruger Redis cache-aside med key baseret på query/options.

### Argumentation

Cache passer godt til read-heavy søgescenarier, men kræver disciplin i datafriskhed.

### Mini-svar

> “Cache giver os fart; TTL og cache-nøgledesign beskytter os mod ukontrolleret staleness.”

---

## Modul 11 – Databaser i Kubernetes + volumes

### Hvad modulet handler om

Stateful workloads i containerplatforme kræver persistence-modeller og driftsdisciplin.

### Begreber

- PVC/PV,
- backup/restore,
- RPO/RTO,
- stateful operational burden.

### SearchProject-kobling

Postgres er state. I K8s skal dette håndteres med PVC/backup eller managed DB.

### Argumentation

Det er let at skalere stateless API-pods; det er sværere at drive korrekt stateful data.

### Mini-svar

> “Vores største produktionsrisiko ligger i stateful data, ikke i API-deployment.”

---

## Modul 12 – Databaser 2 (StatefulSets, replicas, splitting)

### Hvad modulet handler om

Hvordan databaser skaleres og stabiliseres over tid i et K8s-landskab.

### Begreber

- StatefulSet,
- replication,
- database split patterns,
- consistency/availability trade-offs.

### SearchProject-kobling

Z-roadmap binder modul 12 sammen med modul 3 og 11.

### Argumentation

StatefulSets/replicas er ikke bare teknik; de ændrer fejlbillede, driftsansvar og recoveryflow.

### Mini-svar

> “Vi bruger modul 12 som plan for næste modenhedstrin, ikke som pynt i diagrammet.”

---

## 5. Ønsket Kubernetes-produktionssetup (fremadrettet)

Dette er den del, der skal vise “reel faglig forståelse” af drift og arkitektur.

### 5.1 Målbillede

- Ingress controller håndterer ekstern trafik.
- Service abstraherer pods.
- SearchApi deployes som stateless Deployment med flere replikaer.
- HPA skalerer på CPU/latency/evt. custom metrics.
- Redis og Postgres håndteres stateful (med persistence-strategi).
- Observability namespace med Prometheus/Grafana/Loki.

### 5.2 Miljømodel

- **Dev:** lokal Compose, hurtig feedback.
- **Test/Staging:** K8s-lignende miljø med release gates.
- **Prod:** kontrollerede rollouts, rollback og SLO-overvågning.

### 5.3 Deploymentdisciplin

- CI build + tests,
- image scan,
- Helm chart versionering,
- progressive rollout,
- rollback ved brud på gates.

### 5.4 Hvad vi vinder

- konsistent runtime-model,
- lettere drift ved vækst,
- bedre fejlisolation,
- observability på tværs af services.

### 5.5 Hvad vi betaler

- øget driftskompleksitet,
- større krav til platformkompetence,
- flere moving parts.

---

## 6. Risiko- og trade-off matrix

| Emne | Fordel | Ulempe | Mitigerende valg |
|---|---|---|---|
| Nginx vs custom LB | moden routing, mindre domænekode | ekstra infra config | versionsstyring + simple config-filer |
| Redis cache | lavere latency | stale data | TTL, key-strategi, fallback til DB |
| X-skalering API | højere robusthed | mere koordinering i drift | health checks + failover test |
| K8s målarkitektur | skalerbar drift | høj kompleksitet | trinvis indførsel + automatisering |
| Z-roadmap | fremtidig datakapacitet | kompleks migration | faseopdelt plan og målebaseret trigger |

---

## 7. 7–8 minutters eksamenspitch (praktisk)

## 0:00–1:00 – Problem + krav

“Vi byggede et walking skeleton, der opfylder krav om skalering, monitorering, caching og database-retning.”

## 1:00–3:30 – Løsning i drift (C4)

- gennemgå komponenter,
- forklar Nginx-beslutning,
- forklar cache-flow.

## 3:30–5:30 – Ønsket produktion (K8s)

- Ingress/Service/Deployments/HPA,
- stateful vs stateless,
- fordele/ulemper.

## 5:30–7:30 – Kode + bevis

- SearchApi struktur,
- SearchService/SearchLogic,
- tests/performance/failover.

---

## 8. Forberedte svar til underviser/censor

### “Hvorfor fjernede I custom load balancer?”
Fordi routing er platformansvar. Build-vs-buy tilsiger, at vi skal fokusere egen kode på søgedomænet.

### “Er jeres K8s-plan realistisk?”
Ja, fordi vi skelner tydeligt mellem stateless og stateful ansvar, og vi har en trinvis drift-/release-model.

### “Hvorfor er z-skalering ikke implementeret fuldt?”
Fordi det er den dyreste kompleksitet i casen. Vi har en konkret roadmap, som aktiveres ved målte behov.

### “Hvordan dokumenterer I performance?”
Med tests/scripts og observability-data, ikke med subjektive oplevelser.

### “Hvad er projektets største svaghed?”
Stateful datadrift i K8s er stadig roadmap-tung og kræver mere operationsmodenhed.

---

## 9. Mini-ordliste til sikker eksamenssprogbrug

- **Walking skeleton:** minimumsløsning der demonstrerer arkitektur i drift.
- **X-skalering:** flere ens instanser.
- **Y-skalering:** funktionel opdeling i services.
- **Z-skalering:** dataopdeling på tværs af partitioner/shards.
- **Cache-aside:** appen styrer cache lookup + populate.
- **Barrier condition:** release gate, som skal være opfyldt.
- **Rollback window:** tidsrum hvor tilbageførsel kan ske sikkert.
- **Observability:** evnen til at forstå driftstilstand via telemetry.

---

## 10. Konklusion

Hvis du kan stå på dette notat, kan du vise:

1. dyb teoriforståelse fra undervisningen,
2. konkret kobling mellem teori og SearchProject,
3. argumentation for arkitekturvalg,
4. realistisk produktionstænkning i Kubernetes,
5. og moden håndtering af fordele, ulemper og fremtidige risici.

Det er præcis den type faglighed, der løfter en mundtlig eksamen fra “demo” til “arkitekturforsvar”.

---

## 11. Produktionsmodenhed: fra demo til drift (konkret plan)

Denne sektion er lavet for at kunne svare på spørgsmålet: “Okay, fint projekt – men hvordan ville I faktisk køre det i produktion?”

### Trin 1 – Stabil containerplatform

- Etabler namespace-struktur (fx `search`, `data`, `observability`).
- Definér resource requests/limits for API og background jobs.
- Kør SearchApi som Deployment med mindst 2 replikaer.
- Konfigurer readiness/liveness probes.

**Hvorfor:**
Uden klare probes og resourcegrænser bliver autoskalering og failover ustabilt.

### Trin 2 – Releasekontrol og rollback

- Introducér Helm chart med versionsstyring.
- Opsæt CI/CD-gates: build, tests, image scan, smoke checks.
- Kør kontrolleret rollout (rolling update) med rollback-plan.

**Hvorfor:**
Hurtige deploys uden kontrol øger risikoen for driftshændelser.

### Trin 3 – Observability som driftskontrakt

- Definér minimumsdashboard for SLI’er (latency, error rate, throughput).
- Etabler alarmer på fejlbudget-/fejlrate-grænser.
- Standardisér log-korrelation med request IDs.

**Hvorfor:**
Hvis man ikke kan observere systemet, kan man ikke styre det.

### Trin 4 – Stateful modenhed

- Vælg database-strategi (self-managed i cluster vs managed service).
- Definér backup/restore testfrekvens.
- Definér RPO/RTO mål for data.

**Hvorfor:**
Stateful data er typisk den største risiko i skalerede systemer.

---

## 12. Uddybede alternativer (og hvorfor de ikke blev valgt nu)

### Alternativ A: Beholde custom C# load balancer

**Fordel:** fuld kontrol i egen kode.
**Ulempe:** høj vedligeholdelsesbyrde, større risiko, dårligere separation mellem domæne og platform.

**Konklusion:** fravalgt pga. build-vs-buy og driftsrealisme.

### Alternativ B: Gå direkte til fuld Kubernetes implementation

**Fordel:** stærk produktionsnær demonstration.
**Ulempe:** tidskrævende setup, høj fejlrisiko, mindre fokus på arkitekturargumentation i selve casen.

**Konklusion:** fravalgt som første step; valgt som målarkitektur.

### Alternativ C: Ingen cache

**Fordel:** simplere datafriskhedsbillede.
**Ulempe:** dårligere svartider ved gentagne søgninger, højere DB-load.

**Konklusion:** fravalgt fordi casen er read-heavy.

### Alternativ D: Fuld z-skalering nu

**Fordel:** tidlig data-skalerbarhed.
**Ulempe:** høj migrations- og driftskompleksitet uden dokumenteret behov.

**Konklusion:** fravalgt nu; roadmap valgt.

---

## 13. Failure-scenarier og hvordan arkitekturen håndterer dem

### Scenarie 1 – Én SearchApi-instans går ned

- Nginx ruter fortsat til den anden instans.
- Failover-script bruges som driftstest.

**Læring:** X-skalering giver robusthed mod enkeltinstans-fejl.

### Scenarie 2 – Redis utilgængelig

- API skal kunne falde tilbage til DB-opslag.
- Ydeevnen falder, men funktionalitet bevares.

**Læring:** Cache er acceleration, ikke eneste sandhed.

### Scenarie 3 – Postgres performanceflaskehals

- Start med query-optimering + index-review.
- Næste trin: read replicas.
- Derefter overvej partitionering.

**Læring:** Z-roadmap aktiveres af målte flaskehalse.

### Scenarie 4 – Fejlfuld deployment

- Barrier conditions bør stoppe release tidligt.
- Hvis fejl passerer gates: rollback.

**Læring:** release-sikkerhed er en proces, ikke et værktøj.

---

## 14. SLI/SLO tænkning (så du kan tale professionelt om drift)

Selvom fulde SLO’er ikke er implementeret, kan du forklare en realistisk model:

### Mulige SLI’er

- P95-latency på `/api/search`.
- Error-rate (5xx).
- Cache hit ratio.
- Query-responstid mod DB.

### Mulige SLO’er (eksempler)

- 99% requests under 300 ms ved normal load.
- 5xx-rate under 0,5% pr. time.
- Cache hit ratio over 60% på hot queries.

### Hvorfor vigtigt i eksamen

Det viser, at I ikke kun tænker “kører det?”, men “kører det godt nok, og hvordan ved vi det?”

---

## 15. Udvidet spørgsmålstræning (kort, men dyb)

### “Hvordan begrunder I at projektet er et walking skeleton?”
Det demonstrerer kernearkitekturen i drift, men afgrænser bevidst enterprise-kompleksitet som roadmap.

### “Hvad ville I gøre først, hvis brugertallet fordobles?”
Skaler SearchApi X-aksen, monitorér bottlenecks, justér cache/DB-kapacitet, og først derefter overvej Z-tiltag.

### “Hvad er forskellen på at kunne deploye og at kunne drifte?”
Deploy er at få software op at køre; drift er at kunne observere, måle, rollbacke og forbedre stabilt over tid.

### “Hvad er den vigtigste arkitekturfaglige læring i jeres case?”
At korrekt ansvarsplacering reducerer både udviklings- og driftskompleksitet.

### “Hvorfor er jeres argumentation bedre end bare ‘det virker’?”
Fordi vi kan forklare sammenhængen mellem teori, valg, konsekvens og målelig effekt.

---

## 16. Afsluttende eksamensstatement (kan læses op)

> “SearchProject er bygget som et bevidst walking skeleton. Vi har valgt en enkel og forklarbar kodearkitektur, flyttet platformansvar til standardkomponenter, målt performance frem for at antage den, og dokumenteret en realistisk Kubernetes-retning med klare trade-offs. Derfor kan vi ikke kun vise en demo – vi kan forsvare en arkitektur.”

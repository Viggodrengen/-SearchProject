# SearchProject – samlet eksamens- og arkitekturnotat

Dette dokument er skrevet til upload i NotebookLM. Formålet er at samle projektets kode, diagrammer, arkitekturvalg og undervisningsfaglige røde tråd i ét sammenhængende materiale, så man kan lave en podcast eller bruge teksten som fælles forståelsesgrundlag før eksamen.

## 1. Kort projektfortælling

SearchProject er et walking skeleton for en søgeplatform. Systemet kan indeksere dokumenter, gemme søgeindeks i database, modtage søgeforespørgsler via web, console eller HTTP API, cache gentagne søgninger i Redis og vise drift/observability via Grafana, Loki og Prometheus.

Projektet er ikke et fuldt produktionssystem. Det er et eksamensprojekt i arkitekturprincipper, hvor pointen er at vise og begrunde arkitekturvalg: opdeling i services, x/y/z-skalering, caching, deployment, observability, performance, failover og Kubernetes-retning.

Den vigtigste arkitekturbeslutning er, at applikationskoden ikke selv skal implementere load balancing. Den tidligere hjemmelavede C# load balancer er fjernet. I Docker Compose bruges Nginx som reverse proxy foran to SearchApi-instanser. I Kubernetes-produktionsretningen håndteres samme ansvar af Ingress og Service, eventuelt med Nginx Ingress Controller.

## 2. Hvad systemet består af

### SearchWebApp

SearchWebApp er brugerens webinterface. Det er en Blazor webapp, som viser en simpel søgeformular og resultater. Den ejer ikke søgelogik og taler ikke direkte med databasen. Den kalder et HTTP endpoint foran SearchApi.

Strukturelt har den:

- `Components/` og `Components/Pages/` til UI.
- `Services/SearchApiClient.cs` til HTTP-kald.
- `Interfaces/ISearchApiClient.cs` for en simpel abstraction, så klienten kan udskiftes/testes.

Dette passer med micro-frontend-tanken: WebApp er sin egen deployable UI-service, men ikke en dataejer.

### ConsoleSearch

ConsoleSearch er en CLI-klient til søgning. Den er praktisk til demo, debugging og scripts. Den kalder samme API-endpoint som webappen. Det viser, at SearchApi er adskilt fra UI, og at flere klienter kan bruge samme service.

### Nginx

Nginx er reverse proxy og lokal Docker Compose load balancer. Den lytter på port `5075` og sender `/api/*` videre til `searchapi1` og `searchapi2`.

Nginx er valgt fordi routing/load balancing er et infrastrukturansvar. Det er mere realistisk end at skrive en C# load balancer selv. I Kubernetes ville dette ansvar ligge i Ingress/Service-laget.

Konfigurationen ligger i:

- `docker/nginx/search-api.conf`

Nginx tilføjer også headers, så UI og test-scripts kan vise, hvilken backend der har svaret.

### SearchApi

SearchApi er projektets centrale backend-service. Den modtager søgeforespørgsler, validerer input, bruger Redis cache, slår data op i repository-laget og returnerer SearchResult.

Den følger en simpel struktur inspireret af tidligere AuthService-projekt:

- `Interfaces/` – kontrakter, fx `IDatabase`.
- `Repository/` – konkrete database-implementeringer for PostgreSQL og SQLite.
- `Services/` – søgelogik, cachekoordination, metrics og use-case logic.
- `Program.cs` – composition root, dependency setup, endpoints, OpenAPI og middleware.

SearchApi er designet til x-akse skalering: den er stateless ift. HTTP requests. Persistent state ligger i PostgreSQL, og cache ligger i Redis. Derfor kan flere SearchApi-instanser køre parallelt.

### Indexer

Indexer er en worker/console service, der opbygger søgeindekset. Den læser dokumenter fra `docker/indexer-data`, finder ord og skriver indeksdata til database.

Den er ryddet op i en simpel struktur:

- `Interfaces/`
- `Repository/`
- `Services/`

Indexer ejer skriveprocessen til indekset, mens SearchApi primært læser fra indekset. Det er en y-akse opdeling: systemet deles funktionelt i søgning og indeksering.

### Shared

Shared indeholder DTOs og fælles modeller:

- `SearchRequest`
- `SearchResult`
- `DocumentHit`
- `BEDocument`

Shared skal ikke indeholde forretningslogik. Det er kun kontrakter/dataobjekter, som flere services bruger.

### PostgreSQL

PostgreSQL er den persistente database. Den gemmer dokumenter, ord og relationer mellem dokumenter og ord. Det er stateful data og kræver en anden driftstænkning end stateless API-services.

I Kubernetes betyder det PersistentVolumeClaim, backup-strategi og eventuelt StatefulSet eller managed database.

### Redis

Redis bruges som cache til søgeresultater. SearchApi bruger cache-aside-mønsteret:

1. Byg cache key ud fra query, database, case-sensitivity og max resultater.
2. Spørg Redis først.
3. Ved hit returneres cached SearchResult.
4. Ved miss hentes resultat fra database/index og gemmes i Redis med TTL.

Redis reducerer latency ved gentagne søgninger og viser caching som arkitekturkomponent.

### Loki, Grafana og Prometheus

Observability er en aktiv del af projektet:

- Loki bruges til logs.
- Grafana bruges til dashboards.
- Prometheus bruges til metrics scraping.

Det understøtter undervisningens fokus på drift, monitorering, performance og failover. Pointen er, at man ikke kun tegner arkitektur – man skal kunne observere systemets adfærd.

## 3. Runtime-flow

Et typisk søgeflow ser sådan ud:

1. Brugeren skriver en query i SearchWebApp.
2. SearchWebApp sender HTTP POST til `http://nginx:8080/api/search` i Docker Compose.
3. Nginx fordeler requesten til enten `searchapi1` eller `searchapi2`.
4. SearchApi validerer requesten.
5. SearchApi bygger cache key og spørger Redis.
6. Ved cache hit returneres resultat direkte.
7. Ved cache miss læser SearchApi via repository fra PostgreSQL.
8. SearchLogic ranker dokumenter efter antal matchede termer.
9. Resultatet gemmes i Redis og returneres til klienten.
10. Logs og metrics kan ses i observability-stacken.

## 4. Arkitekturprincipper fra undervisningen

### Y-akse skalering: funktionel opdeling

Y-aksen handler om at dele systemet efter ansvar/funktion. SearchProject gør dette ved at adskille:

- Web UI (`SearchWebApp`)
- CLI-klient (`ConsoleSearch`)
- Reverse proxy/routing (`Nginx`)
- Søgelogik (`SearchApi`)
- Indeksering (`Indexer`)
- Persistent data (`PostgreSQL`)
- Cache (`Redis`)
- Observability (`Loki/Grafana/Prometheus`)

Dette gør systemet nemmere at forklare, teste, deploye og skalere i mindre dele.

### X-akse skalering: flere ens instanser

X-aksen handler om at klone samme service. SearchApi er stateless, så den kan køre som flere replikaer:

- `searchapi1`
- `searchapi2`

I Docker Compose ligger de bag Nginx. I Kubernetes ville de ligge som pods bag en Service. HorizontalPodAutoscaler kan senere skalere antallet af pods ud fra CPU, memory eller custom metrics.

### Z-akse skalering: dataopdeling

Z-aksen handler om at dele data. I projektet er z-aksen primært en arkitekturretning, ikke fuldt implementeret. Mulige strategier:

- Sharding efter tenant.
- Sharding efter datadomæne.
- Partitionering efter tid eller dokumenttype.
- Read replicas til tunge læseforespørgsler.
- Managed database udenfor Kubernetes.

Det vigtige til eksamen er at kunne forklare, hvordan man ville gå fra én database til en mere skalerbar databasearkitektur uden big bang.

### Caching

Caching-undervisningen bruges konkret i Redis-løsningen. Cache-aside passer godt, fordi SearchApi selv ved, hvornår den kan genbruge et søgeresultat. TTL begrænser hvor længe data ligger i cache.

Trade-off: cache giver lavere latency, men kræver invalidation/TTL-overvejelse. I dette projekt er TTL tilstrækkeligt til demo, fordi dokumentdata ikke ændrer sig konstant.

### Observability

Operations- og monitoring-undervisningen bruges via Loki, Grafana og Prometheus. Systemet kan ikke bare virke – vi skal også kunne se, om det virker.

Vi kan argumentere for:

- Logs til fejlsøgning.
- Metrics til performance.
- Dashboards til driftsoverblik.
- Testresultater til at dokumentere cache/failover-effekt.

### Deployment og miljøer

Docker Compose er den stabile demo-platform. Kubernetes er produktionsretningen.

Docker Compose:

- God til lokal demo.
- Nemt at starte hele stacken.
- Viser services og runtime-relationer.

Kubernetes:

- Bedre model for produktion.
- Har Ingress, Service, Deployment, Pods, HPA, StatefulSet, PVC og Secrets.
- Kan køre i cloud, lokalt eller on-prem.

## 5. Nginx, Kubernetes og cloud/on-prem

I Docker Compose bruger vi Nginx direkte som reverse proxy/load balancer.

I Kubernetes ville vi typisk bruge:

- Ingress til ekstern HTTP/HTTPS-routing.
- Ingress Controller til at implementere Ingress-reglerne, fx Nginx Ingress Controller.
- Service foran SearchApi pods.
- Deployment til SearchApi.
- HPA til autoscaling.

Cloud vs on-prem:

- I cloud kan `Service type LoadBalancer` eller Ingress integrere med cloud providerens load balancer.
- Lokalt/on-prem kan man bruge Nginx Ingress Controller, Traefik, MetalLB, NodePort, port-forward eller minikube tunnel.

Konklusion: valget afhænger af driftsmiljø, men princippet er det samme: routing og load balancing ligger i infrastrukturen, ikke i forretningskoden.

## 6. Kodekvalitet og struktur

Projektet er ikke enterprise-overengineered, men enterprise-ready i den forstand, at det bruger genkendelige lag og ansvar:

- API-projektet har Interfaces, Repository og Services.
- WebApp har Services og Interface til API-klient.
- Indexer har Interfaces, Repository og Services.
- Shared er kun kontrakter/modeller.

Det er simpelt nok til eksamen og tæt på strukturen fra tidligere AuthService-projekt.

## 7. Tests og verificering

Der er et xUnit testprojekt:

- `SearchProject.Tests`

Det tester især SearchLogic:

- ranking af dokumenter
- ukendte/ignorerede termer
- duplicate query terms

Derudover er der scripts:

- `scripts/performance-cache-test.sh`
- `scripts/failover-test.sh`

Performance-scriptet sammenligner cold-cache og hot-cache. Failover-scriptet stopper en API-instans og verificerer, at Nginx kan sende trafik til den anden.

Seneste verificering:

- Build succeeded.
- 0 warnings.
- 0 errors.
- Tests passed.
- Docker Compose config valid.
- Performance script viser miss ved cold-cache og hit ved hot-cache.
- Failover script viser 100% success rate i den korte test.

## 8. Diagrammernes rolle

Den aktive diagramfil er:

- `diagrammer/SearchProject diagrams - UML + C4.drawio`

De vigtigste sider er:

1. C4 Container syntax eksperiment.
2. Kubernetes syntax eksperiment.

C4 Container bruges til at forklare systemets containere og arkitekturprincipper.

Kubernetes diagrammet bruges til at forklare produktionsretningen: Ingress, Services, Pods, Deployments, HPA, stateful data, secrets og observability.

## 9. Eksamensvinkel

Den røde tråd i fremlæggelsen kan være:

1. Vi startede med et søgesystem.
2. Vi opdelte det i deployable services efter ansvar.
3. Vi gjorde SearchApi stateless, så den kan x-skaleres.
4. Vi flyttede routing til Nginx/infrastruktur i stedet for C# kode.
5. Vi brugte Redis til caching og dokumenterede performance-effekt.
6. Vi brugte PostgreSQL som persistent state og diskuterede z-akse database-skalering.
7. Vi brugte observability til at måle og forklare runtime-adfærd.
8. Vi tegnede Kubernetes-retningen, hvor Ingress/Service/Pods erstatter Docker Compose/Nginx som produktionsmodel.

## 10. Vigtigste trade-offs

### Simpelt mono-repo vs én repo per service

Ideelt microservice-setup kunne have én repo per service og et separat infrastructure repo. I dette eksamensprojekt holder vi det samlet, fordi det gør demo, forståelse og aflevering enklere. Arkitekturen er stadig service-orienteret.

### Nginx i Compose vs Kubernetes Ingress

Nginx i Compose er en lokal demo-løsning. Kubernetes Ingress/Service er produktionsretningen. Det er samme princip i to miljøer.

### Redis cache

Fordel: lavere latency og mindre databasearbejde ved gentagne søgninger.
Ulempe: cache invalidation og TTL skal styres.

### PostgreSQL i cluster vs managed database

Til demo kan Postgres køre i Compose/Kubernetes. I produktion kan managed database være bedre pga. backup, drift, failover og storage-kompleksitet.

## 11. Kort konklusion

SearchProject demonstrerer en realistisk arkitekturretning for en søgeplatform. Det er et walking skeleton, men det har nok konkrete elementer til at forsvare arkitekturprincipperne: serviceopdeling, x-akse skalering, cache, failover, observability, deployment og Kubernetes-produktionsretning.

Det vigtigste budskab er, at vi ikke bare har skrevet kode – vi har placeret ansvar de rigtige steder: forretningslogik i SearchApi, data i PostgreSQL, cache i Redis, routing i Nginx/Ingress, drift i observability og skalering i infrastrukturen.

# Kubernetes-demo: sådan køres walking skeleton

Denne guide beskriver, hvordan eksamensdemoen kan startes fra terminalen, og hvad `startup.sh` gør undervejs.

## Forudsætninger

Følgende skal være installeret på maskinen:

- Docker / Docker Desktop
- Minikube
- kubectl
- Helm
- curl

## Start demoen

Kør fra projektroden:

```bash
./startup.sh
```

Scriptet starter en Minikube-baseret Kubernetes-demo af SearchProject.

## Hvad scriptet gør

`startup.sh` gør følgende:

1. Starter Minikube med profilen `searchproject`.
2. Installerer/opfører `kube-prometheus-stack` med Helm.
   - Prometheus
   - Grafana
   - kube-state-metrics
   - Prometheus Operator
3. Bygger Docker images inde i Minikubes Docker daemon:
   - `search-api:local`
   - `search-webapp:local`
4. Deployer SearchProject til Kubernetes:
   - Postgres
   - Redis
   - SearchApi med 2 replikaer
   - Nginx som fælles API-indgang
   - SearchWebApp
5. Opretter en `ServiceMonitor`, så Prometheus scraper SearchApi på `/metrics`.
6. Opretter et Grafana dashboard til performance og metrics.
7. Starter lokale port-forwards.
8. Kører smoke-test mod API’et.
9. Stopper derefter — miljøet er klar til, at demo/test scripts køres manuelt.

## Lokale URLs

Når scriptet er færdigt, kan disse åbnes:

| Del | URL |
|---|---|
| WebApp | `http://localhost:15249` |
| API / Nginx | `http://localhost:15075` |
| Grafana | `http://localhost:13000` |
| Prometheus | `http://localhost:19090` |

Grafana login:

```text
brugernavn: admin
password: admin
```

Dashboardet hedder:

```text
SearchProject Walking Skeleton - Demo Dashboard
```

## Test scripts

Performance/cache-test:

```bash
BASE_URL=http://localhost:15075 scripts/k8s-performance-cache-test.sh
```

Failover/scale-test:

```bash
BASE_URL=http://localhost:15075 scripts/k8s-failover-test.sh
```

Performance-skaleringstest med parallel belastning:

```bash
BASE_URL=http://localhost:15075 LOW_REPLICAS=1 HIGH_REPLICAS=10 REQUESTS=200 CONCURRENCY=30 scripts/k8s-scale-performance-test.sh
```

Denne test skalerer SearchApi ned/op og sender parallelle requests. Brug den sammen med Grafana-panelerne `API traffic`, `API latency` og `SearchApi replicas - desired vs available`. Bemærk: I Minikube på én maskine er flere replikaer ikke altid hurtigere, fordi alle pods deler samme CPU/ressourcer. Testen viser derfor både skaleringsmekanik og eventuelle ressourcebegrænsninger i testmiljøet.

Performance-testen sammenligner:

- `cold-cache`: Redis tømmes før hvert request
- `hot-cache`: cache primes først, derefter måles gentagne requests

Failover-testen gør:

1. Starter med 2 SearchApi-replikaer.
2. Sender requests.
3. Skalerer SearchApi ned til 1 replika.
4. Sender requests igen.
5. Skalerer tilbage til 2 replikaer.

Den mest eksamensegnede demo er story-scriptet. Når `startup.sh` er kørt, er det denne ene kommando der bruges under eksamen:

```bash
BASE_URL=http://localhost:15075 scripts/k8s-demo-story.sh
```

Det starter kontinuerlig søgetrafik og ændrer derefter arkitekturen bagved, så Grafana kan læses som et system i drift:

1. Cache-performance: samme bruger-load kører, mens cache ryddes og derefter får lov at blive varm igen. Her sammenlignes aktuel søgetid og Postgres CPU-pressure.
2. Redis-fallback: Redis skaleres kortvarigt til 0 replikaer, mens søgninger fortsætter. Her skal health stadig være grøn, mens Postgres CPU kan stige.
3. API-skalering: load holdes højt, mens SearchApi skaleres fra få til mange replikaer, så trafikfordeling på pods kan observeres.

Hold især øje med disse paneler i Grafana:

- `Health: succesfulde søgninger`
- `Aktuel søgetid (hit=Redis, miss=Postgres, fallback=Redis→Postgres)`
- `Søgetid over tid: hit vs miss/fallback`
- `Postgres compute over tid: CPU pressure`
- `API pressure lige nu: request rate pr. pod`
- `API compute lige nu: CPU pressure pr. pod`
- `Cache-status lige nu: hit / miss / fallback`
- `Search traffic: status codes`
- `Minikube pod-status lige nu`
- `Pod restarts siden startup`

Story-scriptet indeholder også en Redis-fejlfase. Her skaleres Redis kortvarigt ned til 0 replikaer. Pointen er at vise, at Redis er et performance-lag og ikke source of truth: SearchApi falder tilbage til Postgres, så søgning kan fortsætte, men uden cache-gevinsten.

## Nyttige kommandoer

Se pods:

```bash
kubectl get pods -n searchproject
```

Se services:

```bash
kubectl get services -n searchproject
```

Se Prometheus ServiceMonitor:

```bash
kubectl get servicemonitor -n searchproject
```

Se logs fra API’et:

```bash
kubectl logs -n searchproject deployment/search-api
```

Stop port-forwards, hvis de hænger:

```bash
pkill -f "kubectl.*port-forward"
```

Stop Minikube-profilen:

```bash
minikube stop -p searchproject
```

Slet hele demo-klyngen:

```bash
minikube delete -p searchproject
```

## Kobling til Modul 9

Demoen følger især Modul 9-opgaverne:

- M9.01: performance-test med valgte metrikker og testdesign
- M9.02: Prometheus/Grafana i Kubernetes
- M9.03: instrumenteret Search Engine med `/metrics`, stress-test scripts og dashboard

Det vigtige eksamenspoint er, at performance ikke kun beskrives med løse tal. Tallene kommer fra et testdesign med hypotese, belastning, metrikker og evaluering.

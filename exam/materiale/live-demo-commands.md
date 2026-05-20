# Live demo-kommandoer

Forudsætning: `./startup.sh` er kørt, og Grafana-dashboardet er åbent.

Pointen med denne version er, at Kubernetes-ændringerne vises direkte med `kubectl`, så det er tydeligt hvad der sker i clusteret.

## 1. Start kontinuerlige tilfældige søgninger

Dette script simulerer brugere, der løbende søger på tilfældige termer fra databasen.

```bash
BASE_URL=http://localhost:15075 scripts/demo-search-loop-start.sh
```

Fortæl: “Nu kører der konstant søgetrafik mod systemet. Nogle queries bliver cache hits, andre bliver misses.”

Tjek evt. pods:

```bash
kubectl get pods -n searchproject
```

## 2. Tving cold-cache / databasearbejde

Cache clearing er ikke en Kubernetes-operation, så her bruger vi API’ets cache-clear endpoint.

```bash
BASE_URL=http://localhost:15075 scripts/demo-cache-clear-loop.sh
```

Fortæl: “Nu rydder vi cache gentagne gange. Derfor skal flere søgninger ramme Postgres, og søgetiden bør stige.”

## 3. Lad cache blive varm igen

Gør ingenting i 20-30 sekunder, mens search loopet stadig kører.

Fortæl: “Nu får Redis lov til at cache resultater igen. Derfor bør flere requests blive hits, og søgetiden falde.”

## 4. Fjern Redis med Kubernetes

Skalér Redis ned til 0:

```bash
kubectl scale deployment/redis -n searchproject --replicas=0
```

Se status:

```bash
kubectl get pods -n searchproject
```

Fortæl: “Redis er performance-laget. Når Redis fjernes, bør søgninger stadig kunne fortsætte via Postgres, men cache-gevinsten forsvinder.”

Skalér Redis op igen:

```bash
kubectl scale deployment/redis -n searchproject --replicas=1
kubectl rollout status deployment/redis -n searchproject --timeout=60s
```

## 5. Skalér SearchApi ned med Kubernetes

Skalér API’et ned til én pod:

```bash
kubectl scale deployment/search-api -n searchproject --replicas=1
kubectl rollout status deployment/search-api -n searchproject --timeout=120s
```

Se pods:

```bash
kubectl get pods -n searchproject -l app=search-api
```

Fortæl: “Nu har vi kun én stateless API-pod. Dashboardets API pressure bør samle sig på én pod.”

## 6. Skalér SearchApi op med Kubernetes

Skalér API’et op til flere pods:

```bash
kubectl scale deployment/search-api -n searchproject --replicas=6
kubectl rollout status deployment/search-api -n searchproject --timeout=240s
```

Se pods:

```bash
kubectl get pods -n searchproject -l app=search-api
```

Fortæl: “Nu skalerer vi API-laget horisontalt. Samme type søgetrafik bør blive fordelt over flere API-pods.”

## 7. Ryd op efter demoen

Stop search loopet:

```bash
scripts/demo-search-loop-stop.sh
```

Sæt normal kapacitet tilbage:

```bash
kubectl scale deployment/search-api -n searchproject --replicas=2
kubectl scale deployment/redis -n searchproject --replicas=1
kubectl rollout status deployment/search-api -n searchproject --timeout=120s
kubectl rollout status deployment/redis -n searchproject --timeout=60s
```

Tjek slutstatus:

```bash
kubectl get pods -n searchproject
```

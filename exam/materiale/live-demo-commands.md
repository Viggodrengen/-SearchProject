# Live demo-kommandoer

Forudsætning: `./startup.sh` er kørt, og Grafana-dashboardet er åbent.

## 1. Start kontinuerlige tilfældige søgninger

```bash
BASE_URL=http://localhost:15075 scripts/demo-search-loop-start.sh
```

Fortæl: “Nu simulerer vi brugere, der søger løbende på tilfældige termer fra databasen. Nogle queries bliver cache hits, andre bliver misses.”

Stop loopet til sidst med:

```bash
scripts/demo-search-loop-stop.sh
```

## 2. Tving cold-cache / databasearbejde

```bash
BASE_URL=http://localhost:15075 scripts/demo-cache-clear-loop.sh
```

Fortæl: “Nu rydder vi cache gentagne gange. Derfor skal flere søgninger ramme Postgres, og søgetiden bør stige.”

## 3. Lad cache blive varm igen

Gør ingenting i 20-30 sekunder, mens search loopet stadig kører.

Fortæl: “Nu får Redis lov til at cache resultater igen. Derfor bør flere requests blive hits, og søgetiden falde.”

## 4. Fjern Redis kortvarigt

```bash
scripts/demo-redis-down-up.sh
```

Fortæl: “Redis er kun performance-lag. Når Redis fjernes, skal søgninger stadig kunne fortsætte via Postgres.”

## 5. Vis API-skalering

Skalér ned til én API-pod:

```bash
scripts/demo-api-scale.sh 1
```

Fortæl: “Nu samler vi API-pressure på én pod.”

Skalér op til flere API-pods:

```bash
scripts/demo-api-scale.sh 6
```

Fortæl: “Nu fordeles samme type søgetrafik over flere stateless API-pods.”

## 6. Ryd op efter demoen

```bash
scripts/demo-search-loop-stop.sh
scripts/demo-api-scale.sh 2
```

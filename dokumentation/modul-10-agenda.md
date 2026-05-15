# Modul 10 - Caching

## Tema
- Caching-begreber
- Distribueret caching med Redis

## Forberedelse
- Soerg for containerized soegemaskine
- OpenTelemetry support
- Synlige data i Grafana

## Emne 1: Caching fundamentals
### Begreber
- Cache hit / miss
- Skrivestrategier (write-through, write-back)
- Eviction (fx LRU)
- Cachetyper: lokal vs distribueret

### Laes/skim
- Abbott & Fisher kapitel 25

### Opgave
- Forklar hvor caching giver mening i jeres loesning
- Vaelg data der er oplagte at cache
- Overvej hastighed vs datakonsistens

## Emne 2: Redis i distribueret setup
### Fokus
- Deling af cache paa tvaers af instanser
- Query/object/session caching
- Invalidation, replication, failover

### Laes/skim
- Redis query caching docs
- Distributed caching i ASP.NET Core
- Redis caching pattern for microservices/K8s

### Opgave
- Forbered forklaring af Redis i microservice-miljoe
- Beskriv synkronisering og invalidation

## Praktisk modulopgave
- M10.02: Tilfoej Redis server og instrumenteringsklasse i soegemaskinen

# UML Deployment Diagram - Docker Compose v2 (To-Be)

Fil: `diagram-uml-deployment-searchproject-v2.drawio`

## Formaal
Diagrammet viser den planlagte driftsarkitektur i compose-miljoeet, som I demonstrerer til eksamen.

## Indeholder
- klientadgang til webapp,
- webapp -> load balancer -> flere SearchApi-instanser,
- Redis cache (planlagt),
- Postgres shard A + shard B (B planlagt),
- Loki/Grafana,
- Prometheus/OTel retning (planlagt),
- persistente volumener.

## Eksamen-fokus
- Giver tydelig separation mellem as-is og to-be.
- Viser failover-overvejelse via flere api-instanser bag LB.
- Viser hvor caching og metrics indfoeres uden at bryde eksisterende struktur.

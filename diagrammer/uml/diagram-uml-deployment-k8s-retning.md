# UML Deployment Diagram - Kubernetes/Minikube Retning (To-Be)

Fil: `diagram-uml-deployment-k8s-retning.drawio`

## Formaal
Diagrammet viser migrationsretningen fra compose til Kubernetes uden at paastaa fuld implementering.

## Indeholder
- namespace-opdeling (`searchproject`, `observability`),
- ingress + services + deployments for stateless dele,
- statefulsets for Redis og Postgres-shards,
- PVC-mapping for persistens,
- HPA-retning for SearchApi,
- Prometheus/Grafana/Loki integration.

## Arkitekturpointes
- Stateless workloads skaleres via deployments/HPA.
- Stateful data holdes i statefulsets + PVC.
- Z-akse retning vises som shard-a/shard-b struktur med request-routing.

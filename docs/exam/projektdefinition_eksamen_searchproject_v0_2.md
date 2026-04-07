# Projektdefinition – Eksamensprojekt (udkast v0.2)

**Fag:** Arkitekturprincipper i praksis (6. semester)  
**Periode:** April–maj 2026  
**Gruppe:** _[indsæt navne]_  
**Projekt:** Videreudvikling af eksisterende SearchProject

## Titel
**SearchProject v2: Redis-caching, performance/failover tests og z-akse databaseplan**

## Problem og formål
Vi bygger videre på den nuværende løsning, hvor flere arkitekturprincipper allerede er anvendt:
- modulær/mikroservice-inspireret opdeling (`SearchApi`, `SearchLoadBalancer`, `SearchWebApp`, `Indexer`)
- **x-akse skalering** via flere `SearchApi`-instanser bag load balancer
- observability via **Loki + Grafana**

Projektets formål er at kvalitetssikre arkitekturen yderligere med fokus på **drift, performance og robusthed**.

## Features vi implementerer (walking skeleton)
1. **Tydelig caching-komponent (Redis)**  
   `SearchApi` udvides med cache-aside strategi for `/api/search` (cache key på query + options, TTL, cache-hit/miss logging).

2. **Kvalificerede tests (performance/latency)**  
   Vi laver reproducerbare tests (fx k6/JMeter) med baseline vs. cache-enabled og dokumenterer p50/p95/p99, throughput og fejlrate.

3. **Failover-tests**  
   Vi tester driftsscenarier hvor en `SearchApi`-instans fejler under belastning, og verificerer at load balancer fortsat leverer svar.

4. **Konkret z-akse anvisning for database**  
   Vi beskriver og begrunder en skaleringsstrategi for data (partitionering/sharding + read-replica strategi) samt migrationsplan.

## Arkitektur og drift (inkl. Kubernetes-tanke)
- Primær demo-miljø: Docker Compose (hurtig og stabil demo).
- Vi medtager **Kubernetes/Minikube deployment-design** i UML deployment-diagrammet og evt. et lille PoC-manifest for kernekomponenter.
- Observability udvides med cache-metrics og testmålinger i Grafana.

## Opfyldelse af generelle krav
- **.NET/C#**: implementering i .NET 10/C#.
- **Driftsmiljø + skalering**: containerbaseret miljø, x-akse i drift, z-akse konkret anvisning.
- **Overvågning**: Loki/Grafana anvendes aktivt i demo og analyse.
- **Fejltolerance/ydeevne**: performance- og failover-tests indgår som central leverance.
- **Caching**: Redis indgår som eksplicit arkitekturkomponent.

## Leverancer til eksamen
- Kørende demo (search + cache + failover-scenarie + monitorering).
- Arkitekturpræsentation med:
  - **C4 Container:** `docs/diagrams/c4_container_searchproject.puml`
  - **UML Class:** `docs/diagrams/uml_class_cache_resilience.puml`
  - **UML Deployment (Minikube-orienteret):** `docs/diagrams/uml_deployment_searchproject.puml`
- Kort testrapport med performance/latency-resultater og anbefalinger.

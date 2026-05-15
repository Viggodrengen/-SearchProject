# Modul 11 - Skalering af databaser

## Dagens emner
1. Volumes i Kubernetes
2. Databaser i Kubernetes (kompleksitet, performance, persistens, sikkerhed, skalering)
3. PostgreSQL med chart (praktisk)

## Volumes i Kubernetes
- Hvad er en volume?
- Ephemeral vs persistent volumes
- PV/PVC: binding og anvendelse i Pod specs

### Fokus for database-workloads
- Data maa ikke forsvinde ved genstart
- Access modes (RWO/RWX)
- Reclaim policy
- Latency, throughput, IOPS

## Databaser i Kubernetes - arkitekturtemaer
- Kompleksitet: replikering, backup, failover, storage-klasser
- Performance: I/O og netvaerkslatens
- Persistens: PV/PVC, backup/restore, replikering
- Sikkerhed: adgangskontrol, isolering, kryptering
- Skalering: vertikal vs horisontal, sharding/partitionering

## Forberedelse
- Kapitel 24 (practical use of database cube)
- Building a Scalable Database
- To run or not to run a database on Kubernetes

## Refleksionsspoergsmaal
- Hvilke krav stiller app og DB til performance/persistens/sikkerhed?
- Hvordan vaelger man sharding/partitionering?
- Hvilke trade-offs er der mellem kompleksitet og automatisering?

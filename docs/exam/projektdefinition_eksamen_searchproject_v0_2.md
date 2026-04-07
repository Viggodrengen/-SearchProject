# Projektdefinition – Eksamensprojekt

**Fag:** Arkitekturprincipper i praksis (6. semester)  
**Periode:** April–maj 2026  
**Gruppe:** _[indsæt navne]_  
**Projekt:** Videreudvikling af eksisterende SearchProject

## Titel
**SearchProject v2: caching, performance/failover og database-skalering**

## Formål og udgangspunkt
Projektet tager udgangspunkt i den eksisterende løsning og viderefører de arkitekturvalg, der allerede er implementeret. Løsningen er opdelt i flere services, skalerer horisontalt på API-laget og anvender monitorering i drift.  

Formålet er at styrke kvaliteten af den nuværende arkitektur med fokus på drift, robusthed og dokumenteret performance.

## Fokus i projektperioden
### 1) Caching som tydelig arkitekturkomponent
Søgeløsningen udvides med en separat caching-komponent (Redis), så ofte gentagne forespørgsler kan håndteres mere effektivt og aflaste databasen.

### 2) Performance-, latency- og failover-scenarier
Der gennemføres strukturerede tests, hvor baseline sammenholdes med den udvidede løsning. Der arbejdes både med svartider, stabilitet under belastning og håndtering af fejl i en eller flere service-instanser.

### 3) Z-akse anvisning for database
Der udarbejdes en konkret og begrundet anvisning for dataskalering på z-aksen, inklusive en realistisk migrationsretning.

### 4) Drift og miljø
Udvikling og demo gennemføres i containerbaseret miljø. Derudover beskrives deployment-retning for Kubernetes/Minikube som del af den arkitekturfaglige dokumentation.

## Arkitekturprincipper som del af leverancen
Allerede implementerede principper indgår aktivt i eksamensgrundlaget:
- modulær serviceopdeling
- x-akse skalering
- monitorering/observability

Disse bruges både i den praktiske demo og i den mundtlige gennemgang af arkitekturvalg og konsekvenser.

## Leverancer til eksamen
- Kørende demonstration af den samlede løsning i drift
- Dokumentation af de eksisterende og videreudviklede arkitekturprincipper
- Arkitekturdiagrammer til præsentation og dialog:
  - C4 containerdiagram
  - UML class diagram
  - UML deployment diagram
- Kort testrapport med resultater fra performance-, latency- og failover-scenarier

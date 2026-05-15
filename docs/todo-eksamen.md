# To-Do Eksamen (praesentation + arkitektur QA) - SearchProject v2

Opdateret: 2026-05-05
Scope: Denne fil handler kun om mundtlig eksamen, praesentation, diagrammer og arkitektur-kvalitetssikring.
Teknisk implementering ligger i `todo-projekt.md`.

## 0. Status foer vi gaar i gang

- [x] Projektdefinition er godkendt.
- [ ] Projektets leverancer til eksamen er produceret og kvalitetssikret.

## 1. Kravkort (eksamenskrav -> hvordan vi opfylder)

Kilder:
- `dokumentation/eksamensprojektet_v1_0.md`
- `Arkitekturprincipper projektdefinition.md`
- undervisningsspor: `ai_emne-miljbeskrivelser.md`, `ai_opgavest-m7-02-c4-container.md`, `modul-07-agenda.md`, `modul-05-agenda.md`

- [ ] .NET/C# er tydeligt vist i praesentation (kodebase-overblik slide).
- [ ] Drift i modulart miljoe med x-skalering vises i demo + deploymentdiagram.
- [ ] Monitorering vises med konkret driftseksempel (Loki/Grafana).
- [ ] Fejltolerance/ydeevne vises med testresultater (ikke kun paastand).
- [ ] Database z-akse anvisning forklares som konkret migrationsretning.
- [ ] Caching forklares med "hvorfor her", "hvordan", og "hvad er tradeoff".
- [ ] UML class/object/deployment fremvises eksplicit i praesentation.
- [ ] C4 containerdiagram fremvises eksplicit (jf. projektdefinition).
- [ ] Kort demo i drift + kort arkitektur/kodebase/driftsmiljoe gennemgang er planlagt.

## 2. Diagrampakke (must-have)

### 2.1 C4 containerdiagram (undervisningskrav-aligned)
Filmaal:
- [ ] `diagram-c4-container-searchproject-v2.drawio`
- [ ] `diagram-c4-container-searchproject-v2.png`

Acceptkriterier (fra M7.02 + miljoebeskrivelser):
- [ ] Systemgraense er tydelig markeret.
- [ ] Aktor(er) er med (mindst slutbruger + klientadgang).
- [ ] Containere er med og matcher faktisk loesning.
- [ ] Relationer er navngivet med protokol/retning (HTTP, TCP, etc.).
- [ ] Diagrammet er holdt simpelt nok til slidebrug efter visuelt review.
- [ ] Docker-specifikke detaljer er bevidst flyttet til deploymentdiagram.

### 2.2 UML class diagram
Filmaal:
- [x] `diagram-uml-class-searchproject-v2.xml`
- [ ] `diagram-uml-class-searchproject-v2.png`
- [x] `diagram-uml-class-searchproject-v2.md`

Acceptkriterier:
- [ ] Centrale domaene- og serviceklasser er med (SearchService, SearchLogic, IDatabase, scheduler interfaces).
- [ ] Ansvar og afhængigheder er tydelige.
- [ ] Interfaces vs implementeringer er adskilt korrekt.
- [ ] Diagrammet afspejler faktisk kode (ingen "fantasi-klasser").

### 2.3 UML object diagram
Filmaal:
- [x] `diagram-uml-object-search-request.xml`
- [ ] `diagram-uml-object-search-request.png`
- [x] `diagram-uml-object-search-request.md`

Acceptkriterier:
- [ ] Runtime-snapshot af 1 konkret soege-request.
- [ ] Objektnavne i `objekt:Klasse` stil.
- [ ] Konkrete vaerdier er med (query, backend, instance, hits, tid).
- [ ] Flowet matcher det I faktisk demonstrerer live.

### 2.4 UML deployment diagram
Filmaal:
- [x] `diagram-uml-deployment-searchproject-v2.xml`
- [x] `diagram-uml-deployment-k8s-retning.xml`
- [ ] `diagram-uml-deployment-searchproject-v2.png`
- [ ] `diagram-uml-deployment-k8s-retning.png`
- [x] `diagram-uml-deployment-searchproject-v2.md`
- [x] `diagram-uml-deployment-k8s-retning.md`

Acceptkriterier (fra miljoebeskrivelser):
- [ ] Noder/runtime-enheder er med (host/container/services).
- [ ] Images, env/secrets, volumes og netvaerk er vist.
- [ ] Porte/protokoller er markeret.
- [ ] Deploymentvej (docker compose) er forklaret kort.
- [ ] K8s/Minikube retning er markeret som "next step" (ikke fake-implementering).

## 3. Arkitektur-kvalitetssikring (QA-gates)

### 3.1 Konsistensgate (diagram <-> kode <-> drift)
- [ ] Servicenavne i diagrammer matcher `docker-compose.yml`.
- [ ] API-endpoints i slides matcher faktisk endpoints.
- [ ] DB/cache/observability relationer matcher real setup eller er markeret som planlagt v2.
- [ ] Alle paastande i slides har evidens (fil, log, screenshot, maaling).

### 3.2 Forklaringsgate (mundtlig kvalitet)
- [ ] Hvert arkitekturvalg kan forklares med "hvorfor" + "tradeoff" paa 30-60 sek.
- [ ] I kan skelne mellem "implementeret nu" og "anbefalet naeste trin".
- [ ] I kan svare paa "hvorfor dette diagram, og hvad viser det som de andre ikke viser?".

### 3.3 Driftgate (demo robusthed)
- [ ] Demo kan koeres fra ren start via runbook.
- [ ] Backup-demo klar (screenshots + pre-run output) hvis live-demo fejler.
- [ ] Rollerne er fordelt (hvem præsenterer hvad, og hvem driver demo).

## 4. Praesentationspakke (leverancer)

- [ ] `slides-eksamen.html` (hovedslides)
- [ ] `slides-speaker-notes.md` (hvad siges per slide)
- [ ] `eksamen-demo-script.md` (trinvis kommando-guide)
- [ ] `eksamen-qa-spoergsmaal.md` (forventede spoergsmaal + korte svar)
- [ ] `eksamen-arkitekturvalg.md` (valg, alternativer, tradeoffs)

Foreslaaet slide-raekkefoelge:
- [ ] Slide 1: Problem, scope, maal
- [ ] Slide 2: Nuværende arkitektur (baseline)
- [ ] Slide 3: C4 container (foer/efter)
- [ ] Slide 4: UML deployment (driftsmiljoe)
- [ ] Slide 5: UML class (kodebase)
- [ ] Slide 6: UML object (runtime)
- [ ] Slide 7: Performance/failover resultater
- [ ] Slide 8: Z-akse + K8s retning
- [ ] Slide 9: Konklusion + tradeoffs

## 5. Oevekoersler og go/no-go

- [ ] Oevekoersel 1 gennemfoert med tidstagning.
- [ ] Oevekoersel 2 gennemfoert med Q&A-simulation.
- [ ] Alle kritiske uklarheder rettet efter oevning.

Go/no-go kriterier:
- [ ] Alle must-have diagrammer findes i baade redigerbar + eksporteret version.
- [ ] Demo tager maks 12 min inkl. overgang mellem speaker 1 og 2.
- [ ] Ingen "ukendte" i architecture story (alt kan forklares med evidens).

## 6. Definition of done (eksamen)

- [ ] I kan gennemfoere en fuld fremlaeggelse uden stop.
- [ ] Diagrammer, kodebase-forklaring og driftsmiljoe-forklaring er aligned.
- [ ] Censor kan spoerge ned i detaljer uden at skabe modsigelser i jeres forklaring.

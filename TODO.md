# TODO – eksamensfokus efter feedback fra Henrik

## Kort konklusion

Henriks feedback betyder, at vi ikke skal forsøge at gennemgå alle diagrammer til eksamen. Vi skal bruge diagrammerne som støtte, men præsentationen bør primært fokusere på:

1. **C4 Containerdiagrammet** – forklarer systemets containere, ansvar og skalering.
2. **Kubernetes production diagrammet** – forklarer ønsket produktionsdrift.
3. **Koden** – viser at walking skeleton’et understøtter arkitekturvalgene.

De øvrige UML-diagrammer kan stadig ligge i draw.io-filen som baggrund/dokumentation, men bør ikke være centrale i præsentationen medmindre der bliver spurgt ind til dem.

---

## Hvad skal vi være skarpe på?

### 1. C4 Containerdiagram

Formål: vise den samlede arkitektur på container-niveau.

Skal kunne forklare:

- Hvad hver container har ansvar for:
  - `SearchWebApp` – UI
  - `ConsoleSearch` – CLI/testklient
  - `SearchLoadBalancer` – fordeler requests og giver failover-retning
  - `SearchApi` – stateless søgelogik
  - `Indexer` – opbygger reverse index
  - `PostgreSQL` – persistent data/index
  - `Redis` – cache af gentagne/hyppige søgninger
  - `Loki/Grafana` – logs og driftsoverblik
- Hvad der skalerer:
  - primært `SearchApi` via flere replikaer bag load balancer
  - senere kan `Indexer`/pipeline tænkes som y-skalering
- Hvorfor det er relevant:
  - bedre throughput ved flere søgninger
  - mindre kobling mellem UI, load balancing, søgelogik og data
  - tydelig drift/monitorering
  - cache reducerer latency ved gentagne søgninger

Arkitekturprincipper vi kobler på:

- **X-akse skalering:** flere `SearchApi`-instanser.
- **Y-akse opdeling:** funktionel opdeling mellem WebApp, API, Indexer, database, cache og observability.
- **Caching:** Redis som arkitektonisk komponent til hot queries.
- **Observability:** Loki/Grafana til logs og drift.
- **Failover-retning:** load balancer kan prøve anden API-instans ved fejl.

Status: Diagrammet er godt nok, men sticky note bør evt. gøre skalering og arkitekturprincipper endnu mere eksplicit.

---

### 2. Kubernetes production diagram

Formål: vise hvordan løsningen tænkes deployet i produktion.

Henriks feedback:

- Control Plane er ikke nødvendig at vise. Den er implicit i Kubernetes.
- Diagrammet skal hellere vise workloads, services, pods, database, storage og drift.

Bør forklare:

- Ingress som ekstern indgang.
- Services som stabile endpoints foran pods.
- `SearchWebApp`, `SearchLoadBalancer`, `SearchApi` som Deployments/Pods.
- `SearchApi` med flere pods for x-akse skalering.
- PostgreSQL som stateful del med persistent storage.
- Redis som cache-workload.
- Loki/Grafana som observability namespace.
- Secrets/Vault til credentials/connection strings.

TODO:

- [ ] Tilret Kubernetes-diagrammet og fjern/de-emphasize Control Plane.
- [ ] Gør Kubernetes-diagrammet mere direkte koblet til C4 Containerdiagrammet.
- [ ] Tilføj/tilret sticky note, så den forklarer: Ingress → Services → Pods → data/cache/observability.

---

### 3. Kodegennemgang

Koden skal bruges til at vise, at arkitekturen ikke kun er tegnet.

Vi bør kunne pege på:

- `SearchWebApp` kalder load balancer/API.
- `SearchLoadBalancer` fordeler mellem API backends.
- `SearchApi` indeholder søgelogik og databaseadgang.
- `Shared/Model` indeholder request/result DTO’er.
- Docker Compose viser to API-instanser og drift med PostgreSQL/Loki/Grafana.

TODO:

- [ ] Find 3-5 kodefiler, vi vil kunne åbne og forklare hurtigt.
- [ ] Lav kort demo-script: start system, lav søgning, vis logs/instanser.
- [ ] Lav kort forklaring af hvad der er implementeret vs. arkitektonisk retning.

---

## Hvilke diagrammer skal bruges til eksamen?

### Primære diagrammer

- [x] C4 Containerdiagram
- [ ] Kubernetes production diagram efter tilretning

### Backup / bilag

- UML class diagram
- UML object diagram
- UML deployment diagram
- C4 context diagram
- C4 component diagram

Disse kan blive i draw.io-filen, men skal ikke nødvendigvis gennemgås aktivt.

---

## Forslag til præsentationsrækkefølge

1. Kort demo af system i drift.
2. C4 Containerdiagram:
   - hvad består systemet af?
   - hvad skalerer?
   - hvilke arkitekturprincipper bruger vi?
3. Kubernetes production diagram:
   - hvordan ville vi drifte det endeligt?
   - hvordan mappes containere til services/pods?
   - hvordan håndteres storage, cache, observability og secrets?
4. Kode:
   - vis walking skeleton og de centrale komponenter.
5. Kort refleksion:
   - hvad er implementeret nu?
   - hvad er arkitekturretning?
   - tradeoffs og næste skridt.

---

## Definition of done inden eksamen

- [ ] C4 Containerdiagram er præsentationsklart.
- [ ] Kubernetes diagram er forsimplet uden unødvendig Control Plane-detalje.
- [ ] Sticky notes forklarer diagrammerne i forhold til arkitekturprincipper.
- [ ] Kodefiler til gennemgang er udvalgt.
- [ ] Demo kan køres stabilt.
- [ ] Vi kan forklare x-akse, y-akse, caching, observability, failover og z-akse/database-retning kort og konkret.

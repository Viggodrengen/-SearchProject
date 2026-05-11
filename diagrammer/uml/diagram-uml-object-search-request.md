# UML Object Diagram - Search Request (To-Be)

Fil: `diagram-uml-object-search-request.drawio`

## Formaal
Diagrammet viser et konkret runtime-snapshot af EN request gennem den planlagte v2-kede:
1. klient -> webapp -> load balancer -> valgt api instance,
2. cache key bygning,
3. cache lookup (miss eksempel),
4. shard-resolve,
5. databasekald,
6. response med headers og resultat.

## Hvorfor det er vigtigt
- Giver censor et konkret "hvad sker der nu" billede.
- Binder class-diagrammets klasser sammen med faktisk runtime-flow.
- Giver jer et naturligt sted at forklare failover/cache hit/miss scenarier.

## Noter
- Gule objekter er planlagte v2-objekter.
- Snapshot er med vilje et cache-miss, fordi det forklarer hele flowet.

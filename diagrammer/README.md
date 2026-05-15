# Diagrammer

Denne mappe samler projektets arkitekturdiagrammer.

## Primær fil

- `SearchProject diagrams - UML + C4.drawio`  
  Samlet præsentationsklar draw.io-fil med seks sider:
  1. UML Class
  2. UML Object
  3. UML Deployment
  4. C4 Context
  5. C4 Container
  6. C4 Component

## Arbejdsfiler

- `UML class + object + deployment .drawio`  
  Arbejdsfil med UML-diagrammerne.

- `C4 context + container + component.drawio`  
  Arbejdsfil med C4-diagrammerne.

## Anvendelse til eksamen

C4-diagrammerne bruges til at forklare systemet fra højere abstraktionsniveau:

1. **Context**: hvem bruger systemet, og hvilke eksterne systemer/platforme indgår.
2. **Container**: hvilke applikationer og runtime-komponenter SearchProject består af.
3. **Component**: hvordan den centrale søgedel er opdelt internt.

UML-diagrammerne bruges til at forklare de mere konkrete strukturer:

1. **Class diagram**: datamodellen og DTO-strukturen for søgninger og søgeresultater.
2. **Object diagram**: et konkret runtime-eksempel på en søgning og et cached søgeresultat.
3. **Deployment diagram**: hvordan udviklingsmiljøet kører i Docker Compose med webapp, Nginx/reverse proxy, API-replikaer, database, cache og observability.

## Eksport

Åbn `.drawio`-filerne i draw.io/diagrams.net og eksportér de relevante sider som PNG/SVG til slides.

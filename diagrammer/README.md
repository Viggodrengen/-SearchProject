# Diagrammer

Denne mappe indeholder én samlet, præsentationsklar draw.io-fil:

- `SearchProject diagrams - UML + C4.drawio`

Filen samler de diagrammer vi bruger til eksamen, så vi undgår gamle/parallelle arbejdsfiler med forældet arkitektur.

## Primære sider

De vigtigste sider er:

1. **C4 Containerdiagram** – viser SearchWebApp, ConsoleSearch, Nginx, SearchApi-replikaer, Indexer, PostgreSQL, Redis og observability.
2. **Kubernetes produktionsdiagram** – viser Ingress/Service/Pods/Deployments og hvordan SearchApi skaleres i Kubernetes.
3. **UML Deployment** – viser Docker Compose/walking skeleton med Nginx foran API-replikaer.

## Backup / bilag

Filen kan også indeholde UML class/object og C4 context/component som bilag, men de er sekundære til eksamensfremlæggelsen.

## Eksport

Åbn `.drawio`-filen i draw.io/diagrams.net og eksportér kun de relevante sider som PNG/SVG til slides.

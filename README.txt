SearchProject - Modul 9 (Containerization + Loki + Grafana)

Dato: 2026-03-23

OVERSIGT
========
Projektet er opdelt i disse services/projekter:

1) indexer
   - Indekserer .txt-filer i database.

2) SearchApi
   - Stateless soegelogik (kan koere i flere instanser).
   - Eksponerer /api/search og /api/health.
   - Logger via NLog til console + Loki.

3) SearchLoadBalancer
   - API foran SearchApi-instanser.
   - Fordeler requests (round-robin/random).
   - Eksponerer /api/search, /api/health og /api/lb/stats.
   - Logger via NLog til console + Loki.

4) ConsoleSearch
   - Console-klient, kalder load balancer.

5) SearchWebApp (Blazor)
   - Web-klient, kalder load balancer.

6) Shared
   - Faelles DTO'er/modeller.

7) Observability stack (Docker)
   - Postgres
   - Loki
   - Grafana (med auto-provisioned Loki datasource + dashboard)


DETTE ER LAVET I MODUL-9
========================
1. Containerization
   - Dockerfile til SearchApi, SearchLoadBalancer og SearchWebApp.
   - docker-compose.yml med:
     - postgres
     - loki
     - grafana
     - searchapi1 + searchapi2
     - searchloadbalancer
     - searchwebapp

2. Loki-kompatibel logging (NLog)
   - SearchApi: nlog.config + NLog.Web.AspNetCore + NLog.Targets.Loki.
   - SearchLoadBalancer: nlog.config + samme pakker.
   - Logs sendes baade til console og Loki.

3. Grafana provisioning
   - Loki datasource oprettes automatisk ved opstart.
   - Dashboard "Search Logs Overview" provisioneres automatisk.

4. Container-venlig konfiguration
   - Shared/Paths.cs bruger env-vars:
     - SEARCH_SQLITE_PATH
     - SEARCH_POSTGRES_CONNECTION
   - ConsoleSearch og WebApp default database sat til postgres.


FORUDSAETNINGER
===============
- Docker Desktop (eller Docker Engine + Compose) skal vaere startet.
- .NET SDK 10 installeret til lokal udvikling udenfor containere.


HURTIG START (DOCKER)
=====================
1) Byg og start alt:
   docker compose up -d --build

2) Tjek at services er oppe:
   docker compose ps

3) URL'er:
   - SearchLoadBalancer health:
     http://localhost:5075/api/health
   - SearchWebApp:
     http://localhost:5249
   - Grafana:
     http://localhost:3000

4) Grafana login:
   - Bruger: admin
   - Password: admin

5) Stop stack:
   docker compose down

6) Stop + slet volumes (fresh DB):
   docker compose down -v


SAADAN TESTER DU LOAD BALANCING
===============================
1) Send 4 requests og se routing-headers:

   for i in 1 2 3 4; do
     echo "REQ-$i"
     curl -s -D - -o /tmp/lb_$i.json \
       -X POST http://localhost:5075/api/search \
       -H "Content-Type: application/json" \
       -d '{"query":"search","maxAmount":1,"caseSensitive":false,"database":"postgres"}' \
       | rg -i "HTTP/|X-LB-Backend|X-LB-Strategy|X-SearchApi-Instance"
   done

   Forventning:
   - backend skifter mellem search-api-1 og search-api-2 (round-robin).

2) Se statistik:
   curl -s http://localhost:5075/api/lb/stats


SAADAN TESTER DU LOKI/GRAFANA
=============================
1) Generer trafik (fx command i forrige afsnit).

2) Aaben Grafana: http://localhost:3000

3) Ga til dashboard:
   - SearchProject / Search Logs Overview

4) Eller ga til Explore og koer query:
   {app=~"search-api|search-load-balancer"}

5) Forventning:
   - du kan se logs fra baade SearchApi og SearchLoadBalancer.
   - labels inkluderer app, instance, level.


NYTTIGE COMMANDS
================
- Se logs fra alle services:
  docker compose logs -f

- Se kun load balancer logs:
  docker compose logs -f searchloadbalancer

- Se kun SearchApi instans 1:
  docker compose logs -f searchapi1

- Genbyg kun en service:
  docker compose up -d --build searchloadbalancer


LOKAL KORSEL UDEN DOCKER (VALGFRIT)
===================================
1) Start SearchApi instans 1:
   ASPNETCORE_URLS=http://localhost:5017 SEARCH_INSTANCE_ID=search-api-1 dotnet run --no-launch-profile --project SearchApi/SearchApi.csproj

2) Start SearchApi instans 2:
   ASPNETCORE_URLS=http://localhost:5018 SEARCH_INSTANCE_ID=search-api-2 dotnet run --no-launch-profile --project SearchApi/SearchApi.csproj

3) Start SearchLoadBalancer:
   dotnet run --project SearchLoadBalancer/SearchLoadBalancer.csproj

4) Start klient:
   - Console: dotnet run --project ConsoleSearch/ConsoleSearch.csproj
   - Web: dotnet run --project SearchWebApp/SearchWebApp.csproj


FEJLSOEGNING
============
1) "Cannot connect to the Docker daemon"
   - Start Docker Desktop og proev igen.

2) Ingen logs i Grafana
   - Verificer at Loki kører: docker compose ps
   - Verificer at SearchApi/LB kører: docker compose ps
   - Kig i service logs for fejl: docker compose logs -f searchapi1 searchloadbalancer

3) Ingen data i Postgres soegning
   - Seed koeres kun ved init af ny volume.
   - Koer evt. fresh start: docker compose down -v && docker compose up -d --build

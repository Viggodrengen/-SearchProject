SearchProject - X-skalering med Load Balancer

Dato: 2026-02-17

OVERSIGT
========
Projektet bestaar nu af:

1) indexer
   - Indekserer .txt filer i database.

2) SearchApi
   - Selve soegelogikken (stateless API).
   - Kan koeres i flere instanser samtidigt.

3) SearchLoadBalancer
   - Nyt API foran SearchApi-instanser.
   - Fordeler requests via strategi (default: round-robin).
   - Exponerer statistik paa fordeling.

4) ConsoleSearch
   - Klient der kalder load balanceren.

5) SearchWebApp
   - Web-klient (Blazor) der kalder load balanceren.

6) Shared
   - Fælles DTO'er/modeller.


HVAD ER LAVET I OPGAVE 1
========================
1. Nyt projekt: SearchLoadBalancer
   - POST /api/search (forwarder til backend SearchApi-instanser)
   - GET /api/health
   - GET /api/lb/stats

2. Scheduler-strategi i load balancer
   - Round-robin (default)
   - Random (kan vaelges i appsettings)

3. Synlighed / bevis paa routing
   - LB svarer med headers:
     - X-LB-Strategy
     - X-LB-Backend
     - X-SearchApi-Instance
   - LB holder tællere pr backend paa /api/lb/stats

4. SearchApi udvidet med instance id
   - Health endpoint returnerer instanceId
   - Soege-endpoint saetter X-SearchApi-Instance

5. Klienter peger nu paa load balancer
   - ConsoleSearch default URL: http://localhost:5075
   - SearchWebApp default URL: http://localhost:5075
   - Begge viser hvilken backend der servede requesten


HURTIG START (TRIN FOR TRIN)
============================
A. (Kun hvis noedvendigt) byg/forny indeks:
   dotnet run --project /Users/victorrodam/Downloads/SearchProject-1/indexer/indexer.csproj

B. Start to SearchApi instanser i hver sin terminal:

Terminal 1:
ASPNETCORE_URLS=http://localhost:5017 SEARCH_INSTANCE_ID=search-api-1 dotnet run --no-launch-profile --project /Users/victorrodam/Downloads/SearchProject-1/SearchApi/SearchApi.csproj

Terminal 2:
ASPNETCORE_URLS=http://localhost:5018 SEARCH_INSTANCE_ID=search-api-2 dotnet run --no-launch-profile --project /Users/victorrodam/Downloads/SearchProject-1/SearchApi/SearchApi.csproj

C. Start load balancer:

Terminal 3:
dotnet run --project /Users/victorrodam/Downloads/SearchProject-1/SearchLoadBalancer/SearchLoadBalancer.csproj

D. Start en klient:

Console klient (Terminal 4):
dotnet run --project /Users/victorrodam/Downloads/SearchProject-1/ConsoleSearch/ConsoleSearch.csproj

eller Web klient:
dotnet run --project /Users/victorrodam/Downloads/SearchProject-1/SearchWebApp/SearchWebApp.csproj


SAADAN TESTER DU, AT LOAD BALANCING VIRKER
==========================================
1) Tjek health endpoints:
   curl -s http://localhost:5017/api/health
   curl -s http://localhost:5018/api/health
   curl -s http://localhost:5075/api/health

2) Send flere requests til load balancer og se headers:
   for i in 1 2 3 4 5 6; do
     echo "REQ-$i"
     curl -s -D - -o /tmp/lb_$i.json \
       -X POST http://localhost:5075/api/search \
       -H "Content-Type: application/json" \
       -d '{"query":"SoCal","maxAmount":1,"caseSensitive":false,"database":"sqlite"}' \
       | rg -i "X-LB-Backend|X-LB-Strategy|X-SearchApi-Instance"
   done

   Forventning:
   - Med round-robin skifter backend mellem search-api-1 og search-api-2.

3) Se samlet fordeling:
   curl -s http://localhost:5075/api/lb/stats

   Forventning:
   - attempts/successes er fordelt cirka ligeligt ved round-robin.

4) Verificer i klient:
   - ConsoleSearch printer:
     Served by: <backend> | strategy: <strategy> | instance: <instance-id>
   - Web viser samme info under resultatet.


KONFIGURATION
=============
Load balancer konfiguration findes i:
/Users/victorrodam/Downloads/SearchProject-1/SearchLoadBalancer/appsettings.json

Felter:
- LoadBalancer:Strategy
  - "round-robin" eller "random"
- LoadBalancer:BackendTimeoutSeconds
- LoadBalancer:Backends (liste af backend navn + baseUrl)


KENDTE BEGRAENSNINGER (NUVAERENDE VERSION)
==========================================
1) LB er in-memory/stateless ift. stats (stats nulstilles ved restart).
2) Ingen aktiv health-probing endnu (kun failover per request).
3) Alle SearchApi-instanser laeser samme database (godt til demo af X-skalering, men DB kan blive flaskehals).


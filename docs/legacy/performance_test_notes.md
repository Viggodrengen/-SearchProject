**Opgave A - forskellige kategorier af performance tests**

Vi vælger her:

- API Forespørgselsmetrikker (f.eks. svartider, fejlrater, antal forespørgsler per sekund)
- Brugeroplevelsesmetrikker (f.eks. gennemsnitlig svartid per bruger, tid til første byte)

Dette er valgt da applikationen på nuværende tidspunkt ikke er mere kompleks eller arkitekturmæssigt deployed relevant så det fx på applikationsniveau eller databaseniveau kan betale sig at performance teste. Applikationen kører lige nu blot lokalt på en enkel maskine og bruger helt almindelig sqlLite eller Postgres og er blot containerized. Applikationen er også blot tiltænkt som et internt værktøj der skal bruges fx inde hos politiet hvor der nok maks er logget 20-50 medarbejder på adgangen, derfor giver det ingen mening at tænke i større skalering af database eller deployment af applikationen, men den skal self følge principper og være klargjort til det. Men det er ikke en international SaaS løsning.

**Opgave B - enkel og konkret testplan**

Vi bruger kun det vi allerede har:
- Containerized app
- NLog
- Loki
- Grafana
- Bash + `curl` til at lave mange kald

### 1) Succeskriterier

**Ved normal brug (ca. 20 samtidige brugere):**
- Gennemsnitlig svartid på søgning: under 1 sekund
- Tid til første byte: under 1 sekund
- Fejlrate: under 1%

**Ved høj belastning (ca. 50 samtidige brugere):**
- Gennemsnitlig svartid på søgning: under 2 sekunder
- Tid til første byte: under 2 sekunder
- Fejlrate: under 2%

**Brugeroplevelse:**
- Søgning føles hurtig i webappen
- Resultatlisten vises uden tydelige stop/frys

### 2) Testmiljø

- Samme maskine til alle tests
- Samme data og samme index i alle tests
- Samme endpoint: `http://localhost:5017/api/search`
- Samme container setup hver gang

### 3) Hvad vi måler

**API:**
- Svartid pr. kald
- Tid til første byte
- Antal fejl
- Antal kald pr. sekund
- Svarstørrelse (bytes)

**Brugeroplevelse:**
- Hvor hurtigt første resultat vises i webappen
- Om siden føles langsom ved mange samtidige søgninger

Målinger hentes fra bash/curl-kørsler + logs/visualisering i Loki og Grafana.

### 4) Hvordan vi laver belastning

Belastning laves med et simpelt bash-script:
- Script sender parallelle `curl`-kald mod `/api/search`
- Vi bruger samme script i alle test cases
- Kun antal samtidige kald og varighed ændres

**Test cases:**
1. Let test: 5 samtidige kald i 2 minutter
2. Normal test: 20 samtidige kald i 10 minutter
3. Høj test: 50 samtidige kald i 5 minutter

Hver test køres 3 gange.

### 5) Evaluering af resultater

Efter hver test udfyldes tabellen:

| Test case | Gennemsnitlig svartid | Tid til første byte | Fejlrate | Kald pr. sekund | Vurdering |
|---|---:|---:|---:|---:|---|
| Let (5) | TBA | TBA | TBA | TBA | TBA |
| Normal (20) | TBA | TBA | TBA | TBA | TBA |
| Høj (50) | TBA | TBA | TBA | TBA | TBA |

Til sidst konkluderer vi kort:
- Bestået eller ikke bestået ift. succeskriterier
- Hvad der skal forbedres først (hvis noget fejler)

### 6) Nem step-by-step guide

**Trin 1: Start miljøet**
- Start jeres normale container setup (samme kommando som I plejer).
- Tjek at API svarer:

```bash
curl -s http://localhost:5017/api/health
```

**Trin 2: Lav en test-request**

```bash
echo '{"query":"politi data","maxAmount":10,"caseSensitive":false,"database":"sqlite"}' > payload.json
```

**Trin 3: Indsæt dette i terminalen (engang)**

```bash
run_test () {
  local users=$1
  local seconds=$2
  local out=$3

  : > "$out"
  local end=$((SECONDS + seconds))

  while [ $SECONDS -lt $end ]; do
    for i in $(seq 1 $users); do
      curl -s -o /dev/null \
        -w "%{http_code};%{time_starttransfer};%{time_total};%{size_download}\n" \
        -H "Content-Type: application/json" \
        -d @payload.json \
        http://localhost:5017/api/search >> "$out" &
    done
    wait
  done
}
```

**Trin 4: Kør de 3 test cases**

```bash
run_test 5 120 results_let.csv
run_test 20 600 results_normal.csv
run_test 50 300 results_hoj.csv
```

Kør hver test 3 gange.

**Trin 5: Hent de vigtigste tal fra en resultatfil**

Eksempel med `results_normal.csv`:

```bash
# Gennemsnitlig svartid (sekunder)
awk -F';' '{sum+=$3; n++} END {if(n>0) print sum/n; else print 0}' results_normal.csv

# Gennemsnitlig tid til første byte (sekunder)
awk -F';' '{sum+=$2; n++} END {if(n>0) print sum/n; else print 0}' results_normal.csv

# Fejlrate i procent (HTTP 5xx + 000)
awk -F';' '{all++; if(($1+0)>=500 || $1=="000") err++} END {if(all>0) print (err/all)*100; else print 0}' results_normal.csv

# Antal kald (bruges til kald pr. sekund)
wc -l results_normal.csv
```

Kald pr. sekund regnes som: `antal kald / testens sekunder`.

**Trin 6: Tjek Loki/Grafana i samme tidsrum**
- Bekræft antal fejl, svartidsniveau og eventuelle spikes.
- Sammenlign med tallene fra `curl`-filerne.

**Trin 7: Udfyld tabellen i afsnit 5**
- Indsæt tal for let, normal og høj test.
- Skriv til sidst: bestået/ikke bestået + kort konklusion.
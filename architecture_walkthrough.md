# SearchProject - Arkitektur Walkthrough

En visuel gennemgang af systemets arkitektur, dataflow og skaleringsmuligheder.

---

## 📋 Oversigt

SearchProject er et søgesystem bestående af 5 komponenter der arbejder sammen om at indeksere tekstfiler og give brugere mulighed for at søge i dem.

---

## 🏗️ Nuværende Arkitektur

### Komponenter

```
┌─────────────────────────────────────────────────────────────────────┐
│                        SEARCHPROJECT SYSTEM                          │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│   indexer    │      │  SearchApi   │      │ConsoleSearch │
│  (Console)   │      │  (Web API)   │      │  (Console)   │
└──────┬───────┘      └──────┬───────┘      └──────┬───────┘
       │                     │                     │
       │    ┌────────────────┴─────────────────┐   │
       │    │                                  │   │
       └───►│         Shared Library           │◄──┘
            │    (DTOs, Models, Config)        │
            └────────────────┬─────────────────┘
                             │
                             ▼
                    ┌──────────────┐
                    │SearchWebApp  │
                    │   (Blazor)   │
                    └──────────────┘
```

### Projekt Roller

| Projekt | Type | Ansvar | Dependencies |
|---------|------|--------|--------------|
| **indexer** | Console App | Crawler .txt filer og bygger reverse index | Shared, SQLite/Postgres |
| **SearchApi** | ASP.NET Core API | Håndterer søgeforespørgsler og DB læsning | Shared, SQLite/Postgres |
| **ConsoleSearch** | Console App | Brugerinteraktion via HTTP til API | Shared |
| **SearchWebApp** | Blazor App | Web UI via HTTP til API | Shared |
| **Shared** | Class Library | Delte DTOs, modeller og DB paths | Ingen |

---

## 🔄 Runtime Flows

### 1️⃣ Indexering Flow

Sådan bygges søgeindekset:

```
┌─────────────┐
│  .txt Files │
│   (Folder)  │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────────┐
│  indexer Process                        │
│                                         │
│  1. Scan folder tree                    │
│  2. Read each .txt file                 │
│  3. Split into words (keep casing)      │
│  4. Store in database                   │
└─────────────┬───────────────────────────┘
              │
              ▼
    ┌─────────────────────┐
    │   Database Tables   │
    ├─────────────────────┤
    │ • word              │
    │   (id, name)        │
    │                     │
    │ • document          │
    │   (id, url, times)  │
    │                     │
    │ • Occ               │
    │   (wordId, docId)   │
    └─────────────────────┘
```

**Detaljeret proces:**

1. **Folder Crawl**: Indexer går gennem alle filer i `Config.FOLDER`
2. **Tokenization**: Tekst splittes i ord ved separatorer (mellemrum, tegnsætning)
3. **Case Preservation**: Ord gemmes med original casing (f.eks. "Hello" og "hello" som separate entries)
4. **Database Storage**:
   - `word` tabel: Unikke ord med ID
   - `document` tabel: Fil metadata (sti, tidsstempler)
   - `Occ` tabel: Reverse index (hvilket ord findes i hvilken fil)

---

### 2️⃣ Query Flow

Sådan håndteres en søgeforespørgsel:

```
┌──────┐
│ User │
└───┬──┘
    │ "Enter search query"
    ▼
┌─────────────────────┐
│ Client              │
│ (Console or Web)    │
└──────────┬──────────┘
           │ HTTP POST /api/search
           │ SearchRequest {
           │   terms: ["word1", "word2"],
           │   caseSensitive: true
           │ }
           ▼
┌──────────────────────────────────────┐
│ SearchApi                            │
│                                      │
│ Step 1: Resolve word IDs             │
│ ├─ Case-sensitive: exact match      │
│ └─ Case-insensitive: all variants   │
│                                      │
│ Step 2: Find matching documents      │
│ └─ Query Occ table for docIds       │
│                                      │
│ Step 3: Rank results                 │
│ └─ Count matched terms per doc      │
│                                      │
│ Step 4: Load document details        │
│ └─ Fetch top N documents             │
└──────────┬───────────────────────────┘
           │
           ▼
    ┌──────────────┐
    │   Database   │
    │ SQLite/      │
    │ Postgres     │
    └──────────────┘
           │
           │ SearchResult {
           │   results: [...],
           │   missingTerms: [...],
           │   timings: {...}
           │ }
           ▼
┌─────────────────────┐
│ Client              │
│ Render Results      │
└─────────────────────┘
           │
           ▼
┌──────┐
│ User │
└──────┘
```

**Ranking Logic:**
- Dokumenter rangeres efter antal matchede søgetermer
- Dokumenter med flest matches vises først
- Missing terms rapporteres tilbage til brugeren

---

## 📈 Skalerings Strategier (AKF Cube)

### X-Axis Scaling: Horizontal Cloning

**Koncept**: Klon samme service bag en load balancer

```
                    ┌──────────────────┐
                    │   Load Balancer  │
                    └────────┬─────────┘
                             │
            ┌────────────────┼────────────────┐
            │                │                │
            ▼                ▼                ▼
    ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
    │ SearchApi #1 │ │ SearchApi #2 │ │ SearchApi #3 │
    └──────┬───────┘ └──────┬───────┘ └──────┬───────┘
           │                │                │
           └────────────────┼────────────────┘
                            │
                            ▼
                    ┌──────────────┐
                    │   Database   │
                    │  (Shared)    │
                    └──────────────┘
```

**Fordele:**
- ✅ Håndterer flere samtidige brugere
- ✅ Høj tilgængelighed (hvis én instance fejler)
- ✅ Nem at implementere (API er allerede stateless)

**Krav:**
- Connection pooling på database
- Centraliseret konfiguration
- Optional: Cache for populære queries

---

### Y-Axis Scaling: Functional Decomposition

**Koncept**: Split efter forretningskapabilitet

```
┌─────────────────────────────────────────────────────────┐
│                    CLIENT LAYER                          │
│  ┌──────────────┐              ┌──────────────┐         │
│  │   Console    │              │  Blazor Web  │         │
│  └──────┬───────┘              └──────┬───────┘         │
└─────────┼──────────────────────────────┼────────────────┘
          │                              │
          └──────────────┬───────────────┘
                         │
┌────────────────────────┼────────────────────────────────┐
│                        ▼         API LAYER              │
│              ┌──────────────────┐                       │
│              │   API Gateway    │                       │
│              └────────┬─────────┘                       │
│                       │                                 │
│         ┌─────────────┼─────────────┐                   │
│         │             │             │                   │
│         ▼             ▼             ▼                   │
│  ┌───────────┐ ┌───────────┐ ┌───────────┐             │
│  │  Query    │ │ Indexing  │ │   Admin   │             │
│  │  Service  │ │  Service  │ │  Service  │             │
│  └─────┬─────┘ └─────┬─────┘ └─────┬─────┘             │
└────────┼─────────────┼─────────────┼───────────────────┘
         │             │             │
┌────────┼─────────────┼─────────────┼───────────────────┐
│        ▼             ▼             ▼    DATA LAYER     │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐               │
│  │  Read    │ │  Write   │ │  Admin   │               │
│  │   DB     │ │   DB     │ │   DB     │               │
│  └──────────┘ └──────────┘ └──────────┘               │
└────────────────────────────────────────────────────────┘
```

**Service Opdeling:**

| Service | Ansvar | Database |
|---------|--------|----------|
| **QueryService** | Søgning og læsning | Read-optimized index |
| **IndexingService** | Crawling og indeksering | Write pipeline |
| **AdminService** | Health checks, stats, reindex jobs | Admin/metrics |

**Fordele:**
- ✅ Teams kan arbejde uafhængigt
- ✅ Forskellige skaleringsmønstre per service
- ✅ Bedre fejlisolering

---

### Z-Axis Scaling: Data Partitioning (Sharding)

**Koncept**: Opdel data på tværs af flere databaser

```
┌──────────────────┐
│  Query Router    │
│                  │
│ Routing Logic:   │
│ • By tenant ID   │
│ • By domain      │
│ • By time slice  │
└────────┬─────────┘
         │
    ┌────┼────┬────┬────┐
    │    │    │    │    │
    ▼    ▼    ▼    ▼    ▼
┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐
│Shard A│ │Shard B│ │Shard C│ │Shard N│
│       │ │       │ │       │ │  ...  │
│Tenant │ │Tenant │ │Tenant │ │       │
│ 1-100 │ │101-200│ │201-300│ │       │
└───────┘ └───────┘ └───────┘ └───────┘
```

**Sharding Strategier:**

1. **Tenant-based**: Hver kunde får egen shard
2. **Domain-based**: Forskellige datadomæner (emails, docs, etc.)
3. **Time-based**: Nyere data i hurtigere storage, ældre i cold storage

**Fordele:**
- ✅ Ubegrænset dataskalering
- ✅ Bedre performance (mindre data per query)
- ✅ Geografisk distribution mulig

**Udfordringer:**
- ⚠️ Kompleks routing logic
- ⚠️ Cross-shard queries er dyre
- ⚠️ Rebalancing ved vækst

---

## 🎯 Anbefalede Forbedringer

### Priority 0: Foundation (Høj Impact, Lav-Medium Effort)

```
┌─────────────────────────────────────────────────────┐
│ 1. Configuration Externalization                    │
│    ❌ Before: Hardcoded paths in Shared.Paths       │
│    ✅ After: appsettings.json + env variables       │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ 2. Integration Tests                                │
│    • Case-sensitive vs case-insensitive behavior    │
│    • Multi-term ranking logic                       │
│    • Missing terms reporting                        │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ 3. Health Checks                                    │
│    • Database connectivity                          │
│    • API readiness                                  │
│    • Index freshness                                │
└─────────────────────────────────────────────────────┘
```

### Priority 1: Scaling & Reliability

```
┌─────────────────────────────────────────────────────┐
│ 1. Query Optimization                               │
│    Problem: Loading full word maps per request      │
│    Solution: In-memory cache with refresh strategy  │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ 2. Result Caching                                   │
│    Cache hot queries (e.g., Redis)                  │
│    TTL-based invalidation                           │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ 3. Observability                                    │
│    • Structured logging                             │
│    • Metrics: latency, QPS, cache hit rate          │
│    • Distributed tracing                            │
└─────────────────────────────────────────────────────┘
```

### Priority 2: Architecture Evolution

```
┌─────────────────────────────────────────────────────┐
│ 1. Y-axis Split                                     │
│    Separate Query/Admin/Indexing services           │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ 2. Z-axis Preparation                               │
│    Design shard router abstraction                  │
│    Define shard key strategy                        │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ 3. Contract-First API                               │
│    OpenAPI-generated clients                        │
│    Reduce manual coupling                           │
└─────────────────────────────────────────────────────┘
```

---

## 🚀 Target Architecture (Future State)

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT TIER                              │
│   ┌──────────────────┐              ┌──────────────────┐        │
│   │ Console Client   │              │  Blazor Web App  │        │
│   └────────┬─────────┘              └────────┬─────────┘        │
└────────────┼──────────────────────────────────┼─────────────────┘
             │                                  │
             └──────────────┬───────────────────┘
                            │
┌───────────────────────────┼─────────────────────────────────────┐
│                           ▼          API TIER                    │
│                  ┌──────────────────┐                            │
│                  │  API Gateway /   │                            │
│                  │  Load Balancer   │                            │
│                  └────────┬─────────┘                            │
│                           │                                      │
│              ┌────────────┼────────────┐                         │
│              │            │            │                         │
│              ▼            ▼            ▼                         │
│      ┌──────────────┬──────────────┬──────────────┐             │
│      │SearchQuery   │SearchAdmin   │  Indexing    │             │
│      │Service       │Service       │  Service     │             │
│      │(Replicated)  │              │              │             │
│      └──────┬───────┴──────┬───────┴──────┬───────┘             │
└─────────────┼──────────────┼──────────────┼─────────────────────┘
              │              │              │
┌─────────────┼──────────────┼──────────────┼─────────────────────┐
│             │              │              │      DATA TIER       │
│             └──────────────┼──────────────┘                      │
│                            ▼                                     │
│                   ┌──────────────────┐                           │
│                   │  Shard Router    │                           │
│                   └────────┬─────────┘                           │
│                            │                                     │
│         ┌──────────────────┼──────────────────┐                  │
│         │         │        │        │         │                  │
│         ▼         ▼        ▼        ▼         ▼                  │
│    ┌────────┬────────┬────────┬────────┬────────┐               │
│    │Shard 1 │Shard 2 │Shard 3 │Shard 4 │Shard N │               │
│    │        │        │        │        │  ...   │               │
│    └────────┴────────┴────────┴────────┴────────┘               │
└──────────────────────────────────────────────────────────────────┘
```

**Nøgle Karakteristika:**

- **Horizontal Scalability**: Load balancer + multiple QueryService instances
- **Functional Separation**: Dedicated services for query, admin, indexing
- **Data Partitioning**: Shard router distributing across multiple databases
- **High Availability**: Redundancy på alle niveauer
- **Independent Scaling**: Hver service kan skaleres uafhængigt

---

## 🛤️ Migration Path (Sikker Rækkefølge)

```
Phase 1: Stabilization
├─ 1. Externalize configuration
├─ 2. Add integration tests
└─ 3. Implement health checks

Phase 2: Optimization
├─ 4. Add query result caching
├─ 5. Optimize DB connection pooling
└─ 6. Implement structured logging

Phase 3: Y-axis Scaling
├─ 7. Split Shared library (contracts only)
├─ 8. Extract QueryService
├─ 9. Extract AdminService
└─ 10. Extract IndexingService

Phase 4: X-axis Scaling
├─ 11. Add load balancer
├─ 12. Deploy multiple API instances
└─ 13. Tune connection pooling

Phase 5: Z-axis Scaling
├─ 14. Design shard router
├─ 15. Implement shard key strategy
├─ 16. Gradual rollout of sharding
└─ 17. Add cross-shard query support
```

---

## 📊 Shared Library Refactoring

### Current Problem

```
┌─────────────────────────────────────┐
│         Shared Library              │
│                                     │
│ • SearchRequest / SearchResult      │ ← Good (API contracts)
│ • Paths (hardcoded DB paths)        │ ← Bad (runtime config)
│ • Domain models                     │ ← Bad (tight coupling)
└─────────────────────────────────────┘
         ▲         ▲         ▲
         │         │         │
    ┌────┴───┬─────┴────┬────┴────┐
    │        │          │         │
indexer  SearchApi  Console   WebApp

Problem: Any change forces rebuild of ALL projects
```

### Recommended Solution

```
┌─────────────────────────────────────┐
│      Search.Contracts (tiny)        │
│                                     │
│ • SearchRequest                     │
│ • SearchResult                      │
│ • DTOs only                         │
└─────────────────────────────────────┘
         ▲         ▲         ▲
         │         │         │
    ┌────┴───┬─────┴────┬────┴────┐
    │        │          │         │
    │        │          │         │
┌───┴────┐ ┌┴─────────┐ ┌┴────────┐ ┌────────┐
│indexer │ │SearchApi │ │Console  │ │WebApp  │
│        │ │          │ │         │ │        │
│Own DB  │ │Own DB    │ │Own      │ │Own     │
│models  │ │adapters  │ │config   │ │config  │
└────────┘ └──────────┘ └─────────┘ └────────┘

Benefit: Teams can work independently, less coupling
```

---

## 🔍 Performance Considerations

### Current Bottlenecks

| Area | Issue | Impact | Solution |
|------|-------|--------|----------|
| **Word Resolution** | Loading all case variants per request | High latency | In-memory cache |
| **DB Connections** | No connection pooling | Connection exhaustion | Configure pooling |
| **Hot Queries** | Repeated identical searches | Wasted DB load | Result caching |
| **Full Table Scans** | Missing indexes | Slow queries | Add strategic indexes |

### Caching Strategy

```
┌──────────────────────────────────────────────────────┐
│                  Cache Layers                         │
├──────────────────────────────────────────────────────┤
│                                                       │
│  L1: In-Memory (per API instance)                    │
│  ├─ Word ID mappings                                 │
│  ├─ Case variant lookups                             │
│  └─ TTL: 5 minutes                                   │
│                                                       │
│  L2: Distributed Cache (Redis)                       │
│  ├─ Query results                                    │
│  ├─ Document metadata                                │
│  └─ TTL: 15 minutes                                  │
│                                                       │
│  L3: Database                                        │
│  └─ Source of truth                                  │
│                                                       │
└──────────────────────────────────────────────────────┘
```

---

## 📈 Monitoring & Observability

### Key Metrics to Track

```
┌─────────────────────────────────────────┐
│ Query Performance                        │
├─────────────────────────────────────────┤
│ • P50, P95, P99 latency                 │
│ • Queries per second (QPS)              │
│ • Cache hit rate                        │
│ • Error rate                            │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Indexing Performance                     │
├─────────────────────────────────────────┤
│ • Documents indexed per minute          │
│ • Index build time                      │
│ • Index size growth                     │
│ • Indexing errors                       │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ System Health                            │
├─────────────────────────────────────────┤
│ • Database connection pool usage        │
│ • Memory consumption                    │
│ • CPU utilization                       │
│ • Disk I/O                              │
└─────────────────────────────────────────┘
```

---

## ✅ Summary

Dette SearchProject har en solid grundarkitektur med klar separation mellem indexing og querying. De primære muligheder for forbedring er:

1. **Kort sigt**: Eksternalisér konfiguration, tilføj tests og health checks
2. **Mellem sigt**: Implementer caching og optimér database queries
3. **Lang sigt**: Split i microservices (Y-axis) og implementer sharding (Z-axis)

Systemet er klar til at skalere horisontalt (X-axis) med minimal indsats, hvilket gør det til et godt første skridt når load øges.

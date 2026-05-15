# SearchProject (eksamensklar baseline)

Dette repo er ryddet op og struktureret til eksamensforløbet i **Arkitekturprincipper i praksis**.

## Aktivt eksamensscope
Vi bygger videre på eksisterende løsning med fokus på:
1. **Redis caching** i `SearchApi`
2. **Performance/latency tests** (baseline vs cache)
3. **Failover tests** under load
4. **Z-akse database-anvisning** (partitionering/read replicas)

## Kørsel (nuværende baseline)
```bash
docker compose up -d --build
```

Vigtige endpoints:
- Web: `http://localhost:5249`
- Nginx/API endpoint: `http://localhost:5075/api/health`
- Grafana: `http://localhost:3000` (admin/admin)

## Dokumentation
- Aktiv arkitekturretning: `docs/architecture/architecture.md`
- Projektdefinition: `docs/exam/projektdefinition_eksamen_searchproject_v0_2.md`
- Diagrammer: `diagrammer/SearchProject diagrams - UML + C4.drawio`
- Arbejdsplan: `TODO.md`
- Arkiv/undervisningsnoter: `docs/legacy/`

# Hvad gør `startup.sh`?

`startup.sh` gør miljøet klar til demo. Det er ikke selve demoen.

## Det scriptet gør

1. Starter Minikube-profilen `searchproject`.
2. Installerer/opfører observability stack:
   - Prometheus
   - Grafana
   - Loki
   - Promtail
3. Bygger Docker images til:
   - `search-api:local`
   - `search-webapp:local`
4. Deployer Kubernetes manifests:
   - Postgres
   - Redis
   - SearchApi
   - SearchWebApp
   - Nginx
   - ServiceMonitor
   - Grafana dashboard
5. Restarter relevante deployments, så nye images/configs kommer i brug.
6. Starter lokale port-forwards:
   - API: `http://localhost:15075`
   - WebApp: `http://localhost:15249`
   - Grafana: `http://localhost:13000`
   - Prometheus: `http://localhost:19090`
7. Kører en kort smoke-test mod API’et.

## Hvorfor tager det nogle gange lang tid?

Det tager især tid når:

- Minikube ikke allerede kører.
- Helm charts skal installeres/opdateres.
- Docker images skal bygges igen.
- Grafana/Prometheus/Loki skal rulle nye pods ud.
- C#-koden eller Dockerfile er ændret, så image-cache ikke kan genbruges.

## Hvis koden ikke er ændret

Hvis Minikube allerede kører, og du ikke har ændret C#-kode, kan `startup.sh` stadig køres sikkert, men det kan være mere end nødvendigt.

Hurtige checks:

```bash
kubectl get pods -n searchproject
kubectl get pods -n monitoring
```

Hvis alt kører, kan du ofte nøjes med at åbne Grafana og starte demo-kommandoerne fra:

```text
exam/materiale/live-demo-commands.md
```

## Hvis du har ændret API/WebApp-kode

Kør altid:

```bash
./startup.sh
```

Så bliver images bygget igen og deployments restartet.

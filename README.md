# CustomerApi — a step-by-step DevOps journey with .NET

The goal of this repo is to build up a DevOps experience step by step, from a plain app to a monitored, autoscaled deployment on Kubernetes. .NET is only the context, so the app itself is intentionally simple: a Customer CRUD API.

What was added in each phase is listed in [CHANGELOG.md](CHANGELOG.md).

## Roadmap

| # | Phase |
|---|-------|
| 1 | Build the .NET API (EF Core + PostgreSQL, OpenAPI, health checks) |
| 2 | Dockerize the app (multi-stage Dockerfile, docker-compose for API + DB) |
| 3 | Set up a local Kubernetes cluster (minikube) |
| 4 | Create a Helm chart (Deployment, Service, ConfigMap, Secret, probes, Bitnami PostgreSQL dependency) |
| 5 | Deploy with Helm (install, port-forward, upgrade & rollback) |
| 6 | Scale it (manual scaling, metrics-server, HPA, load testing with k6/hey) |
| 7 | Monitoring with Grafana (kube-prometheus-stack, app metrics, dashboards) |
| 8 | CI/CD with GitHub Actions (build and test on PRs, images and chart on `master`) and automatic database migrations |

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/)
- For Kubernetes only: [minikube](https://minikube.sigs.k8s.io/), [kubectl](https://kubernetes.io/docs/tasks/tools/) and [Helm](https://helm.sh/) 3.8+

## Quick start

All commands are run from the repository root.

**1. Create your `.env`** (git-ignored) from the template and set your own values:
```bash
cp .env.example .env          # PowerShell: Copy-Item .env.example .env
```

**2. Run the whole stack (API + PostgreSQL) in Docker:**
```bash
docker compose up -d --build
curl http://localhost:8080/health
curl http://localhost:8080/customers
```
Stop it with `docker compose down` (add `-v` to also delete the database data).

## Local development (API from the IDE)

Start only the database, point the API at it with user secrets (use the same values as in `.env`), and run:
```bash
docker compose up -d postgres
dotnet user-secrets set "ConnectionStrings:Customers" "Host=localhost;Port=5432;Database=customerdb;Username=<user>;Password=<password>" --project src/CustomerApi
dotnet tool restore
dotnet run --project src/CustomerApi      # http://localhost:5232
```
Run the tests with `dotnet test`.

## Run on Kubernetes (minikube + Helm)

Run from the repository root in PowerShell, with Docker Desktop running. `.env` must contain `POSTGRES_USER=postgres` and `POSTGRES_PASSWORD`.

**1. Cluster, image and credentials**
```powershell
minikube start --driver=docker
./scripts/build-image.ps1                                              # builds customer-api and customer-api-migrations (tag latest) and loads both into minikube
kubectl create secret generic customer-api-db --from-env-file=.env     # DB credentials live in the cluster, not in the chart
```

**2. Install the chart**
```powershell
helm dependency update ./infrastructure/charts/customer-api      # first time only: downloads the PostgreSQL chart
helm install customer-api ./infrastructure/charts/customer-api --timeout 10m
kubectl get jobs,pods -w
```
A migration Job runs after the install and before every upgrade. It waits for Postgres and applies any pending EF Core migrations, so an empty database gets the `Customers` table. The API pods stay `0/1` until it has finished, because `/health` queries that table. Check it with `kubectl logs job/customer-api-migrations -c migrations`.

**3. Try it**
```powershell
kubectl port-forward svc/customer-api 8080:80
curl http://localhost:8080/health
```

**4. Upgrade and roll back**
```powershell
./scripts/build-image.ps1 -Tag 0.1.1                   # new image version
helm upgrade customer-api ./infrastructure/charts/customer-api --reset-values --set image.tag=0.1.1
helm history customer-api
helm rollback customer-api <revision>
```
`--reset-values` uses the chart defaults plus only the values you pass, instead of reusing values from earlier revisions. Check what a release uses with `helm get values customer-api`. A rollback restores the Kubernetes resources only, not the database, so keep migrations backward compatible.

**Clean up**
```powershell
helm uninstall customer-api
```
`helm uninstall` keeps the `data-postgres-0` volume claim, the `customer-api-db` Secret and the last migration Job. Delete them with `kubectl delete pvc data-postgres-0`, `kubectl delete secret customer-api-db` and `kubectl delete job customer-api-migrations` for a clean slate.

Notes:
- Rebuilding the image under the same tag does not restart the pods. Run `kubectl rollout restart deployment/customer-api`, or use a new tag (`-Tag 0.1.1` and `image.tag`).
- Every key in `.env` becomes a key in the Secret. The chart only reads `POSTGRES_USER` and `POSTGRES_PASSWORD`.
- The migration image must exist with the same tag as the API image, otherwise the Job fails with `ImagePullBackOff`. `build-image.ps1` builds both.
- `docker compose` and `kubectl port-forward` both default to `localhost:8080`. Stop one of them or pick another local port (for example `8081:80`).

## Scale it

**1. Metrics for the autoscaler**
```powershell
helm repo add metrics-server https://kubernetes-sigs.github.io/metrics-server/
helm upgrade --install metrics-server metrics-server/metrics-server -n kube-system --set "args={--kubelet-insecure-tls}"   # the flag is for minikube only
kubectl top pods
```

**2. Manual scaling.** Turn the autoscaler off, otherwise it controls the replica count:
```powershell
helm upgrade customer-api ./infrastructure/charts/customer-api --reset-values --set autoscaling.enabled=false --set replicaCount=4
kubectl get endpoints customer-api
```

**3. Autoscaling.** The chart creates a CPU-based HPA by default (2 to 6 pods, 50% of the CPU request):
```powershell
helm upgrade customer-api ./infrastructure/charts/customer-api --reset-values
kubectl get hpa -w                                     # terminal 1
kubectl get pods -w                                    # terminal 2
Get-Content infrastructure/tests/load/customers.js -Raw | kubectl run k6 --rm -i --restart=Never --image=grafana/k6 -- run -   # terminal 3: load
```
Quick alternative to k6: `kubectl run load --rm -it --restart=Never --image=williamyeh/hey -- -z 3m -c 50 http://customer-api/customers`.

Notes:
- Generate the load from inside the cluster. `kubectl port-forward` sends every request to a single pod.
- New pods receive traffic only after the readiness probe passes, and scale-down waits about 5 minutes after the load stops.

## Monitoring (Prometheus + Grafana)

**1. Install the stack**
```powershell
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm install monitoring prometheus-community/kube-prometheus-stack -n monitoring --create-namespace
kubectl get pods -n monitoring -w
```

**2. Deploy the instrumented API.** It exposes `/metrics`, and the chart creates a ServiceMonitor labelled `release: monitoring` so Prometheus scrapes it:
```powershell
./scripts/build-image.ps1 -Tag 0.2.0
helm upgrade customer-api ./infrastructure/charts/customer-api --reset-values --set image.tag=0.2.0
kubectl port-forward svc/monitoring-kube-prometheus-prometheus 9090:9090 -n monitoring    # http://localhost:9090/targets: customer-api should be UP
```

**3. Open Grafana** (user `admin`):
```powershell
kubectl port-forward svc/monitoring-grafana 3000:80 -n monitoring                        # http://localhost:3000
$p = kubectl get secret monitoring-grafana -n monitoring -o jsonpath="{.data.admin-password}"
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($p))
```

**4. Import the dashboards** from [infrastructure/grafana/dashboards](infrastructure/grafana/dashboards) if you haven't done so yet. In Grafana go to **Dashboards > New > Import**, upload one JSON file (or drop all six one after the other) and choose the **Prometheus** data source if asked. There is one dashboard per file:

| File | Shows |
|------|-------|
| `Request rate.json` | Requests per second, health probes excluded |
| `Error rate.json` | Share of 5xx responses |
| `Latency (p95).json` | 95th percentile request duration |
| `Replicas.json` | Running API pods |
| `Pod CPU.json` | CPU per API pod |
| `Pod memory.json` | Memory per API pod |

Run the k6 load from the previous section and watch CPU, latency and replicas move together.

Note: the health probes dominate idle traffic, which is why the queries exclude `/health*`.

## CI/CD (GitHub Actions)

| Workflow | Runs on | Does |
|----------|---------|------|
| `ci.yml` | Pull requests to `master` | Build and test |
| `cicd.yml` | Push to `master` (a merge), or **Run workflow** in the Actions tab | Build and test, push `customer-api` and `customer-api-migrations` to GHCR tagged `sha-<commit>`, then lint, render and package the Helm chart |

Both call reusable templates in `.github/workflows`: `_build-test.yml`, `_docker-image.yml` and `_helm-chart.yml`.

Notes:
- After the first run, set both GHCR packages to public so the cluster can pull them without credentials.
- There is no deploy step yet. GitHub's runners cannot reach a local minikube, so deployment will be pulled into the cluster with Argo CD.

## Reference

### Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/customers?page=1&pageSize=50` | List customers (paged, pageSize 1–100) |
| GET | `/customers/{customerId}` | Get a customer |
| POST | `/customers` | Create a customer |
| PUT | `/customers/{customerId}` | Update a customer |
| DELETE | `/customers/{customerId}` | Delete a customer |
| GET | `/health` | Readiness: checks the API and database connectivity |
| GET | `/health/live` | Liveness: the process is up (no dependency checks) |
| GET | `/metrics` | Prometheus metrics (request count and duration, process and .NET runtime) |
| GET | `/openapi/v1.json` | OpenAPI document (Development only) |

### Database schema in Docker
`db/init.sql` runs only when the Postgres volume is empty. After adding a migration, regenerate it and reset the volume:
```bash
dotnet ef migrations script --idempotent -o db/init.sql --project ./src/CustomerApi.Infrastructure --startup-project ./src/CustomerApi
docker compose down -v
docker compose up -d --build
```
### Tooling
`dotnet-tools.json` pins the `dotnet-ef` CLI so everyone uses the same version. Run `dotnet tool restore` once after cloning.
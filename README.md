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
./scripts/build-image.ps1                                              # builds customer-api:latest and loads it into minikube
kubectl create secret generic customer-api-db --from-env-file=.env     # DB credentials live in the cluster, not in the chart
```

**2. Install the chart**
```powershell
helm dependency update ./cicd/charts/customer-api      # first time only: downloads the PostgreSQL chart
helm install customer-api ./cicd/charts/customer-api
kubectl get pods -w
```
The API pods stay `0/1` until the schema exists, because `/health` queries the `Customers` table.

**3. Apply the migrations through a tunnel**
```powershell
kubectl port-forward svc/postgres 5433:5432            # terminal 1, leave it running
```
```powershell
$e = (Get-Content .env -Raw) | ConvertFrom-StringData  # terminal 2
$env:ConnectionStrings__Customers = "Host=localhost;Port=5433;Database=customerdb;Username=$($e.POSTGRES_USER);Password=$($e.POSTGRES_PASSWORD)"
./scripts/update-database.ps1
```

**4. Try it**
```powershell
kubectl port-forward svc/customer-api 8080:80
curl http://localhost:8080/health
```

**5. Upgrade and roll back**
```powershell
./scripts/build-image.ps1 -Tag 0.1.1                   # new image version
helm upgrade customer-api ./cicd/charts/customer-api --reset-values --set image.tag=0.1.1
helm history customer-api
helm rollback customer-api <revision>
```
`--reset-values` uses the chart defaults plus only the values you pass, instead of reusing values from earlier revisions. Check what a release uses with `helm get values customer-api`. A rollback restores the Kubernetes resources only, not the database.

**Clean up**
```powershell
helm uninstall customer-api
```
`helm uninstall` keeps the `data-postgres-0` volume claim and the `customer-api-db` Secret. Delete them with `kubectl delete pvc data-postgres-0` and `kubectl delete secret customer-api-db` for a clean slate.

Notes:
- Rebuilding the image under the same tag does not restart the pods. Run `kubectl rollout restart deployment/customer-api`, or use a new tag (`-Tag 0.1.1` and `image.tag`).
- Every key in `.env` becomes a key in the Secret. The chart only reads `POSTGRES_USER` and `POSTGRES_PASSWORD`.
- Migrations are not applied automatically on deploy yet.

## Scale it

**1. Metrics for the autoscaler**
```powershell
helm repo add metrics-server https://kubernetes-sigs.github.io/metrics-server/
helm upgrade --install metrics-server metrics-server/metrics-server -n kube-system --set "args={--kubelet-insecure-tls}"   # the flag is for minikube only
kubectl top pods
```

**2. Manual scaling.** Turn the autoscaler off, otherwise it controls the replica count:
```powershell
helm upgrade customer-api ./cicd/charts/customer-api --reset-values --set autoscaling.enabled=false --set replicaCount=4
kubectl get endpoints customer-api
```

**3. Autoscaling.** The chart creates a CPU-based HPA by default (2 to 6 pods, 50% of the CPU request):
```powershell
helm upgrade customer-api ./cicd/charts/customer-api --reset-values
kubectl get hpa -w                                     # terminal 1
kubectl get pods -w                                    # terminal 2
Get-Content cicd/tests/load/customers.js -Raw | kubectl run k6 --rm -i --restart=Never --image=grafana/k6 -- run -   # terminal 3: load
```
Quick alternative to k6: `kubectl run load --rm -it --restart=Never --image=williamyeh/hey -- -z 3m -c 50 http://customer-api/customers`.

Notes:
- Generate the load from inside the cluster. `kubectl port-forward` sends every request to a single pod.
- New pods receive traffic only after the readiness probe passes, and scale-down waits about 5 minutes after the load stops.

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
| GET | `/openapi/v1.json` | OpenAPI document (Development only) |

### Configuration

| Variable | Used by | Purpose |
|----------|---------|---------|
| `POSTGRES_USER` | postgres, api | Database user |
| `POSTGRES_PASSWORD` | postgres, api | Database password |
| `ConnectionStrings__Customers` | api | Connection string. Built in `docker-compose.yml` for the container (host `postgres`), or set via user secrets locally |
| `ASPNETCORE_ENVIRONMENT` | api | Swagger/OpenAPI are enabled only in `Development` |

### Helm values

Defaults are in [cicd/charts/customer-api/values.yaml](cicd/charts/customer-api/values.yaml).

| Value | Default | Purpose |
|-------|---------|---------|
| `replicaCount` | `2` | API pods (ignored while autoscaling is enabled) |
| `image.repository` / `image.tag` | `customer-api` / `latest` | Image to run |
| `resources` | 100m CPU / 128Mi requested, 256Mi memory limit | API container resources |
| `config.aspnetcoreEnvironment` | `Production` | Stored in a ConfigMap and exposed as `ASPNETCORE_ENVIRONMENT` |
| `database.existingSecret` | `customer-api-db` | Pre-created Secret with the DB user and password |
| `database.host` / `port` / `name` | `postgres` / `5432` / `customerdb` | Used to build `ConnectionStrings__Customers` |
| `postgresql.*` | see file | Settings for the Bitnami PostgreSQL dependency |
| `autoscaling.*` | enabled, 2 to 6 pods, 50% CPU | Horizontal Pod Autoscaler |

The probes use `/health` (readiness) and `/health/live` (liveness).

### Project layout
```
src/
  CustomerApi/                 API host: endpoints, health checks, error handling
  CustomerApi.Application/     Services, DTOs, mappers, repository interfaces
  CustomerApi.Domain/          Entities
  CustomerApi.Infrastructure/  EF Core DbContext, repositories, migrations
tests/                         Unit and integration tests
cicd/Dockerfile                Multi-stage image build
cicd/charts/customer-api/      Helm chart (API + Bitnami PostgreSQL dependency)
cicd/kubernetes/               Plain manifests written first as a learning step (reference only)
cicd/tests/load/               k6 load test used to trigger the autoscaler
db/init.sql                    Schema script generated from the EF migrations
scripts/                       EF migration helpers and the image build script
```

### Database schema in Docker
`db/init.sql` runs only when the Postgres volume is empty. After adding a migration, regenerate it and reset the volume:
```bash
dotnet ef migrations script --idempotent -o db/init.sql --project ./src/CustomerApi.Infrastructure --startup-project ./src/CustomerApi
docker compose down -v
docker compose up -d --build
```
### Tooling
`dotnet-tools.json` pins the `dotnet-ef` CLI so everyone uses the same version. Run `dotnet tool restore` once after cloning.
# CustomerApi — a step-by-step DevOps journey with .NET

The goal of this repo is to build up a DevOps experience step by step, from a plain app to a monitored, autoscaled deployment on Kubernetes. .NET is only the context, so the app itself is intentionally simple: a Customer CRUD API.

What was added in each phase is listed in [CHANGELOG.md](CHANGELOG.md).

## Roadmap

| # | Phase | Status |
|---|-------|--------|
| 1 | Build the .NET API (EF Core + PostgreSQL, OpenAPI, health checks) | Done |
| 2 | Dockerize the app (multi-stage Dockerfile, docker-compose for API + DB) | Done |
| 3 | Create a Helm chart (Deployment, Service, ConfigMap, Secret, probes, Bitnami PostgreSQL dependency) | Planned |
| 4 | Set up a local Kubernetes cluster (kind / minikube / k3d) | Planned |
| 5 | Deploy with Helm (namespace, ingress/port-forward, upgrade & rollback) | Planned |
| 6 | Scale it (manual scaling, metrics-server, HPA, load testing with k6/hey) | Planned |
| 7 | Monitoring with Grafana (kube-prometheus-stack, app metrics, dashboards) | Planned |

Next steps after that: a CI/CD pipeline with GitHub Actions and centralized logging with Loki.

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/)

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

### Project layout
```
src/
  CustomerApi/                 API host: endpoints, health checks, error handling
  CustomerApi.Application/     Services, DTOs, mappers, repository interfaces
  CustomerApi.Domain/          Entities
  CustomerApi.Infrastructure/  EF Core DbContext, repositories, migrations
tests/                         Unit and integration tests
cicd/Dockerfile                Multi-stage image build
db/init.sql                    Schema script generated from the EF migrations
scripts/                       EF migration helper scripts
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
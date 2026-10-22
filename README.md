# CustomerApi — a step-by-step DevOps journey with .NET

The goal of this repo is to build up a DevOps experience step by step, from a plain app to a monitored, autoscaled deployment on Kubernetes. .NET is only the context, so the app itself is intentionally simple: a Customer CRUD API.

## Roadmap

| # | Phase | Status |
|---|-------|--------|
| 1 | Build the .NET API (EF Core + PostgreSQL, OpenAPI, health checks) | Done |
| 2 | Dockerize the app (multi-stage Dockerfile, docker-compose for API + DB) | Planned |
| 3 | Create a Helm chart (Deployment, Service, ConfigMap, Secret, probes, Bitnami PostgreSQL dependency) | Planned |
| 4 | Set up a local Kubernetes cluster (kind / minikube / k3d) | Planned |
| 5 | Deploy with Helm (namespace, ingress/port-forward, upgrade & rollback) | Planned |
| 6 | Scale it (manual scaling, metrics-server, HPA, load testing with k6/hey) | Planned |
| 7 | Monitoring with Grafana (kube-prometheus-stack, app metrics, dashboards) | Planned |

Next steps after that: a CI/CD pipeline with GitHub Actions and centralized logging with Loki.

## Phase 1 — The .NET API

### Tech stack
- .NET 10, ASP.NET Core minimal APIs
- Entity Framework Core with PostgreSQL (Npgsql)
- Built-in OpenAPI document (Development only)
- Health checks, which later become the Kubernetes probes

### Solution layout
```
src/
  CustomerApi/                 API host: endpoints, health checks, error handling
  CustomerApi.Application/     Services, DTOs, mappers, repository interfaces
  CustomerApi.Domain/          Entities
  CustomerApi.Infrastructure/  EF Core DbContext and repositories
tests/
  CustomerApi.UnitTests/
  CustomerApi.IntegrationTests/
```

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

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for the local PostgreSQL container)

### Run locally

1. Start PostgreSQL:
   ```bash
   docker compose up -d
   ```
2. Configure the connection string. It is not stored in the repo, so use user secrets:
   ```bash
   cd src/CustomerApi
   dotnet user-secrets set "ConnectionStrings:Customers" "Host=localhost;Port=5432;Database=customerdb;Username=postgres;Password=postgres"
   ```
   Alternatively, set the `ConnectionStrings__Customers` environment variable. This is the same mechanism used in the later phases.
3. Restore tools and run the API:
   ```bash
   dotnet tool restore
   dotnet run --project src/CustomerApi
   ```
4. Try it:
   ```bash
   curl http://localhost:5232/health
   curl http://localhost:5232/customers
   ```

### Run the tests
```bash
dotnet test
```

### Tooling
`dotnet-tools.json` pins the `dotnet-ef` CLI so everyone uses the same version. Run `dotnet tool restore` once after cloning.
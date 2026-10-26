# Changelog

What was added in each phase of the roadmap (see [README.md](README.md)). Newest first.

## Phase 3 — Helm chart

### Added
- `cicd/charts/customer-api`: Helm chart with a Deployment, a Service (ClusterIP, port 80 to container port 8080) and a ConfigMap (`ASPNETCORE_ENVIRONMENT`).
- Readiness probe on `/health` and liveness probe on `/health/live`. Readiness checks the database, liveness does not, so a database outage does not restart healthy API pods.
- `values.yaml` holds the replica count, image repository and tag, resources, probes, environment and database settings.
- Bitnami `postgresql` 18.12.16 as a pinned dependency (`Chart.lock`), with the Service named `postgres` and the `customerdb` database created on first start.
- Credentials come from a Secret created outside the chart (`database.existingSecret`). The API and the PostgreSQL chart both read it, and the connection string is assembled in the pod from the secret keys, so the password never appears in `values.yaml` or the rendered manifests.

### Notes
- The chart assumes `POSTGRES_USER=postgres`, the admin user created by the Bitnami chart.
- The Bitnami image is pulled with the `latest` tag, so it is not pinned.
- Migrations are not part of the chart yet. The API pods stay `0/1` until the schema is applied by hand.

## Phase 4 — Local Kubernetes cluster

### Added
- minikube as the local cluster, documented in the README ("Run on Kubernetes").
- `scripts/build-image.ps1`: builds `customer-api:<tag>` and loads it into minikube.
- `cicd/kubernetes/`: plain manifests (Deployment, Service, ConfigMap, PostgreSQL StatefulSet with a headless Service and a volume claim) written before the chart to learn the resources. A Secret manifest is used locally and git-ignored. They are kept as a reference and are not used by the chart.

### Notes
- Images built locally must be loaded into minikube, and the Deployment uses `imagePullPolicy: IfNotPresent` so the local copy is used.
- Rebuilding under the same tag does not restart pods, so use a new tag or `kubectl rollout restart`.
- Verified: the API is `1/1` after the migrations run, and deleting the PostgreSQL pod or StatefulSet keeps the data in its volume claim.

## Phase 2 — Dockerize the app

### Added
- `cicd/Dockerfile`: multi-stage build. The .NET SDK image restores and publishes, the smaller ASP.NET runtime image runs the app as a non-root user on port 8080. Project files are copied and restored before the source so the package layer is cached.
- `docker-compose.yml`: `api` and `postgres` services. The API waits for the database healthcheck (`depends_on: service_healthy`) and reaches it by service name.
- `db/init.sql`: idempotent schema script generated from the EF migrations and mounted into Postgres, which runs it on first start.
- `.env.example` and `.dockerignore`.
- Configuration through environment variables only (`POSTGRES_USER`, `POSTGRES_PASSWORD`, `ConnectionStrings__Customers`). Nothing is hardcoded.

### Notes
- `db/init.sql` must be regenerated after every new migration, and the volume reset (`docker compose down -v`).
- Swagger/OpenAPI are not available in the container unless `ASPNETCORE_ENVIRONMENT=Development` is set.

## Phase 1 — The .NET API

### Added
- .NET 10 ASP.NET Core minimal API with a layered solution: Domain, Application, Infrastructure and the API host.
- Customer CRUD endpoints (paged list, get, create, update, delete) with validation and problem-details error responses.
- Entity Framework Core with PostgreSQL (Npgsql) and the `InitialCreate` migration.
- Health checks: `/health` (readiness, queries the database) and `/health/live` (liveness).
- OpenAPI document and Swagger UI (Development only).
- Unit and integration tests.
- `docker-compose.yml` with PostgreSQL for local development, pinned `dotnet-ef` tool (`dotnet-tools.json`) and EF migration helper scripts in `scripts/`.

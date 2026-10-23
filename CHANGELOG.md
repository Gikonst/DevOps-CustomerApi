# Changelog

What was added in each phase of the roadmap (see [README.md](README.md)). Newest first.

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

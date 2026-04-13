# NetClaw

NetClaw is an agent platform built with ASP.NET Core and React. The current repository focuses on three core areas:

- Provider management for LLM backends
- Agent and skill management
- Docker-backed sandbox execution and a pluggable runtime

The project is similar in spirit to OpenClaw's structure: keep the root README focused on onboarding, and move deeper implementation details into `docs/`.

## What Exists Today

- ASP.NET Core backend with versioned HTTP APIs under `/api/v1`
- React 19 + Vite frontend for admin flows
- PostgreSQL persistence with automatic database creation and migrations on startup
- Cookie-based authentication with a seeded demo admin account
- Skill CRUD and skill archive upload support
- Agent, provider, user, and role management
- Docker sandbox endpoints for Python, `pip`, and example Excel file generation
- Runtime plugin loading via `plugin.json` manifests
- Example plugins: `sample` and `telegram`

## Repository Layout

```text
.
├── backend/                # ASP.NET Core API, domain/application/infra layers, plugins
├── frontend/               # React + Vite admin UI
├── sandbox/                # Sandbox image definition
├── skills/                 # Runtime skills mounted into the app/sandbox
├── docs/                   # Project documentation
└── docker-compose.yml      # App + sandbox runtime setup
```

## Quick Start

### Prerequisites

- .NET SDK 10
- Node.js 22+
- pnpm
- PostgreSQL
- Docker

### 1. Configure the backend

Copy the sample config:

```bash
cp backend/Src/NetClaw.Api/appsettings.Example.json backend/Src/NetClaw.Api/appsettings.Development.json
```

Update the `ConnectionStrings.NetClawDb` value to point to your PostgreSQL instance.

### 2. Run the backend

```bash
dotnet run --project backend/Src/NetClaw.Api
```

On startup, NetClaw will:

- create the database if it does not exist
- apply EF Core migrations
- seed a demo admin account

The backend serves the API and, when built, the frontend static assets.

### 3. Run the frontend in development

```bash
cd frontend
pnpm install
pnpm dev
```

The Vite dev server runs on port `5173` by default and proxies `/api` requests to `http://localhost:5000`.

### 4. Run the app and sandbox with Docker

```bash
docker compose up --build
```

This starts:

- `app`: the published NetClaw application
- `sandbox`: the long-running container used by sandbox endpoints

`docker-compose.yml` currently does not provision PostgreSQL for you, so the application still needs a reachable database configuration.

## Plugin Runtime

NetClaw includes a lightweight plugin system. At startup, the host scans the runtime `plugins` directory under the application content root, reads each `plugin.json`, loads enabled assemblies, registers plugin services, maps plugin endpoints, and starts plugin lifecycles.

Current examples in this repo:

- `sample.plugin`: reference implementation for lifecycle, DI, and endpoint mapping
- `telegram.plugin`: Telegram bot runtime management endpoints

Plugin development notes are documented in [docs/README.md](docs/README.md) and [docs/developer/plugin-development.md](docs/developer/plugin-development.md).

## Docs

- General docs index: [docs/README.md](docs/README.md)
- Developer docs: [docs/developer/README.md](docs/developer/README.md)
- Plugin development guide: [docs/developer/plugin-development.md](docs/developer/plugin-development.md)

## Notes

- This README only describes capabilities currently visible in this repository.
- Deeper operational and developer guidance should live in `docs/` instead of expanding the root README indefinitely.

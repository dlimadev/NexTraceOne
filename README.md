# NexTraceOne

**The source of truth for services, contracts, changes, and operational knowledge.**

NexTraceOne is a unified platform for service governance, contract management, production change confidence, operational consistency, team-owned service reliability, AI-assisted analysis, and operational optimization.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Repository Structure](#repository-structure)
- [Backend Modules](#backend-modules)
- [Building Blocks (Shared Libraries)](#building-blocks-shared-libraries)
- [Frontend](#frontend)
- [Platform Services](#platform-services)
- [Testing](#testing)
- [Getting Started](#getting-started)
- [Docker Compose](#docker-compose)
- [Documentation](#documentation)

---

## Architecture Overview

NexTraceOne follows a **Modular Monolith** architecture built on **Domain-Driven Design (DDD)** and **Clean Architecture** principles. The system is composed of autonomous domain modules that communicate through integration events and share a set of foundational building blocks.

```
┌─────────────────────────────────────────────────────────┐
│                     Frontend (React)                     │
│              Vite · React Router · TanStack Query         │
└────────────────────────────┬────────────────────────────┘
                             │ /api/v1
┌────────────────────────────▼────────────────────────────┐
│                   ApiHost (ASP.NET Core)                  │
│           Rate Limiting · Auth · Multi-Tenancy            │
├──────────┬───────────┬────────────┬─────────────────────┤
│ Identity │  Catalog   │  Operations │  AI Knowledge  ...  │
│ Access   │& Contracts │& Incidents  │& Governance         │
├──────────┴───────────┴────────────┴─────────────────────┤
│                  Building Blocks                         │
│     Core · Application · Infrastructure · Security       │
│                    · Observability                        │
├─────────────────────────────────────────────────────────┤
│         PostgreSQL (4 logical databases)                  │
│     ClickHouse (observability) · OTel Collector           │
└─────────────────────────────────────────────────────────┘
```

Each module follows a **5-layer structure**:

| Layer | Responsibility |
|-------|---------------|
| **Domain** | Entities, aggregates, value objects, domain events |
| **Application** | Commands, queries, handlers, validators (MediatR/CQRS) |
| **Infrastructure** | Repositories, DbContext, external integrations |
| **API** | Minimal API endpoints |
| **Contracts** | DTOs, integration events shared across modules |

---

## Tech Stack

### Backend

| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET | 10.0 | Runtime & SDK |
| ASP.NET Core | 10.0 | Web framework |
| Entity Framework Core | 10.0 | ORM (PostgreSQL via Npgsql) |
| MediatR | 14.1 | CQRS pipeline |
| FluentValidation | 12.1 | Request validation |
| Serilog | 4.3 | Structured logging (Loki, PostgreSQL sinks) |
| OpenTelemetry | 1.15 | Distributed tracing & metrics |
| Quartz | 3.16 | Background job scheduling |
| PostgreSQL | 16+ | Primary database |
| ClickHouse | 24.8 | Observability data store |

### Frontend

| Technology | Version | Purpose |
|-----------|---------|---------|
| React | 19 | UI framework |
| TypeScript | 5.9 | Type safety |
| Vite | 7 | Build tool & dev server |
| React Router | 7 | Client-side routing (94+ routes) |
| TanStack React Query | 5 | Server state management |
| React Hook Form + Zod | 7 / 4 | Form handling & schema validation |
| Tailwind CSS | 4 | Utility-first styling |
| Axios | 1.13 | HTTP client with token refresh |
| i18next | 25 | Internationalization (EN, PT-BR, PT-PT, ES) |

### Testing

| Technology | Purpose |
|-----------|---------|
| xUnit | Backend unit testing |
| FluentAssertions | Readable assertions |
| NSubstitute | Mocking |
| Testcontainers | Integration tests with real PostgreSQL |
| Vitest | Frontend unit & integration tests |
| Playwright | End-to-end tests |
| MSW | Mock Service Worker for API mocking |

### Infrastructure

| Technology | Purpose |
|-----------|---------|
| Docker / Docker Compose | Containerization & orchestration |
| Nginx | Frontend reverse proxy |
| OpenTelemetry Collector | Telemetry pipeline |
| GitHub Actions | CI/CD workflows |

---

## Repository Structure

```
NexTraceOne/
├── src/
│   ├── building-blocks/            # Shared foundational libraries (5 projects)
│   │   ├── NexTraceOne.BuildingBlocks.Core
│   │   ├── NexTraceOne.BuildingBlocks.Application
│   │   ├── NexTraceOne.BuildingBlocks.Infrastructure
│   │   ├── NexTraceOne.BuildingBlocks.Observability
│   │   └── NexTraceOne.BuildingBlocks.Security
│   │
│   ├── modules/                    # Domain modules (9 modules, 5 layers each)
│   │   ├── aiknowledge/
│   │   ├── auditcompliance/
│   │   ├── catalog/
│   │   ├── changegovernance/
│   │   ├── configuration/
│   │   ├── governance/
│   │   ├── identityaccess/
│   │   ├── notifications/
│   │   └── operationalintelligence/
│   │
│   ├── platform/                   # Host & worker deployable projects
│   │   ├── NexTraceOne.ApiHost
│   │   ├── NexTraceOne.BackgroundWorkers
│   │   └── NexTraceOne.Ingestion.Api
│   │
│   └── frontend/                   # React SPA
│
├── tests/
│   ├── building-blocks/            # Building block unit tests
│   ├── modules/                    # Module unit tests (1 per module)
│   ├── platform/                   # Integration & E2E tests
│   └── load/                       # Load testing scenarios
│
├── docs/                           # Architecture & design documentation
├── tools/                          # CLI utilities
├── infra/                          # Nginx & PostgreSQL configs
├── build/                          # Docker build configs (ClickHouse, OTel)
├── scripts/                        # Build & deploy scripts
│
├── NexTraceOne.sln                 # .NET solution (~70 projects)
├── Directory.Build.props           # Shared .NET build settings
├── Directory.Packages.props        # Centralized NuGet package versions
├── docker-compose.yml              # Full stack orchestration
├── Dockerfile.{apihost,frontend,ingestion,workers}
└── .env.example                    # Environment variable template
```

---

## Backend Modules

Each module is a self-contained bounded context following Clean Architecture:

| Module | Description | Key Entities |
|--------|-------------|-------------|
| **identityaccess** | Authentication, users, roles, permissions, MFA, tenant management | Users, Roles, Permissions, Tenants |
| **catalog** | Service catalog, dependencies, topology, developer portal | Services, Dependencies, Assets |
| **changegovernance** | Release management, promotion workflows, change validation | Changes, Releases, Workflows, Promotions |
| **operationalintelligence** | Incidents, runbooks, reliability metrics, cost intelligence | Incidents, Runbooks, Budgets |
| **governance** | Compliance, risk assessment, FinOps, policies, maturity | Policies, Controls, Risk, FinOps |
| **notifications** | Multi-channel notifications with intelligence & automation | Notifications, Preferences, Templates |
| **aiknowledge** | AI model registry, policies, knowledge sources, governance | Models, Policies, Knowledge Sources |
| **auditcompliance** | Audit logging, compliance tracking, evidence | Audit Entries, Evidence |
| **configuration** | Platform settings, feature flags, environment policies | Definitions, Settings, Feature Flags |

### Module Internals

Each module (e.g., `src/modules/catalog/`) contains:

```
NexTraceOne.Catalog.Domain/           # Entities, value objects, domain events
NexTraceOne.Catalog.Application/      # Commands, queries, handlers, validators
NexTraceOne.Catalog.Infrastructure/   # DbContext, repositories, external adapters
NexTraceOne.Catalog.API/              # Minimal API endpoint definitions
NexTraceOne.Catalog.Contracts/        # Shared DTOs & integration events
```

Modules communicate through **integration events** defined in their `Contracts` projects. The event bus distributes events in-process with outbox pattern support for transactional consistency.

---

## Building Blocks (Shared Libraries)

| Library | Purpose |
|---------|---------|
| **Core** | DDD primitives (`Entity<TId>`, `AggregateRoot`, `ValueObject`), `Result<T>` pattern, strongly-typed IDs, domain/integration events, guard clauses |
| **Application** | CQRS interfaces (`ICommand`, `IQuery`), MediatR pipeline behaviors (validation, transactions, logging, performance, tenant isolation), `IUnitOfWork`, `IEventBus` |
| **Infrastructure** | `NexTraceDbContextBase` with audit trail, generic repository, outbox pattern, EF Core interceptors (soft deletes, encrypted fields), event bus implementation |
| **Security** | JWT authentication, API key auth, cookie sessions with CSRF, permission-based authorization, AES-GCM field encryption, multi-tenant resolution middleware |
| **Observability** | Serilog configuration (console, file, Loki, PostgreSQL), OpenTelemetry tracing & metrics, health check infrastructure (`/health`, `/ready`, `/live`) |

---

## Frontend

The frontend is a **React 19 SPA** organized by domain features that mirror the backend modules.

### Key Directories

```
src/frontend/src/
├── api/            # Centralized Axios client with token refresh & CSRF
├── auth/           # Auth context, permission checks
├── components/     # Shared UI components (Button, Card, Modal, etc.)
│   └── shell/      # App layout (Sidebar, Topbar, PageContainer)
├── contexts/       # React contexts (Auth, Environment, Persona)
├── features/       # Domain feature modules
│   ├── ai-hub/           # AI assistant, model registry, policies
│   ├── catalog/          # Service catalog, contracts, source of truth
│   ├── change-governance/# Releases, workflows, promotions
│   ├── contracts/        # Contract studio, drafts, workspaces
│   ├── governance/       # Compliance, risk, FinOps, policies
│   ├── identity-access/  # Login, users, environments, access reviews
│   ├── integrations/     # Integration hub, connectors
│   ├── notifications/    # Notification center, preferences
│   ├── operations/       # Incidents, runbooks, reliability, automation
│   └── ...
├── locales/        # i18n translations (en, pt-BR, pt-PT, es)
├── hooks/          # Custom React hooks
├── types/          # Centralized TypeScript types
└── utils/          # Utility functions
```

### Design System

- **Styling**: Tailwind CSS with custom design tokens (canvas, panels, surfaces, semantic colors)
- **Typography**: Inter (UI) + JetBrains Mono (code/data)
- **Components**: Custom-built component library (no external UI framework)
- **Icons**: Lucide React
- **Accessibility**: ARIA attributes, keyboard navigation, semantic HTML

### State Management

- **Server state**: TanStack React Query (caching, invalidation, background refetching)
- **Client state**: React Context (Auth, Environment, Persona)
- **Form state**: React Hook Form with Zod schema validation

### Security

- Token storage in sessionStorage (access) and memory (refresh)
- Automatic token refresh with request queuing
- CSRF protection on mutating operations
- Permission-based route protection (`<ProtectedRoute permission="..." />`)

---

## Platform Services

| Service | Dockerfile | Port | Purpose |
|---------|-----------|------|---------|
| **ApiHost** | `Dockerfile.apihost` | 8080 | Main REST API aggregating all modules |
| **BackgroundWorkers** | `Dockerfile.workers` | 8081 | Scheduled jobs (Quartz) |
| **Ingestion API** | `Dockerfile.ingestion` | 8082 | External data ingestion (independently scalable) |
| **Frontend** | `Dockerfile.frontend` | 3000 | React SPA served via Nginx |

### ApiHost Features

- Rate limiting per endpoint category (auth, AI, data-intensive, operations)
- Security headers and CSRF protection
- Multi-tenant and environment resolution middleware
- Auto-migration in development mode
- Health check endpoints (`/health`, `/ready`, `/live`)
- OpenAPI documentation via Scalar

---

## Testing

### Backend Tests

```bash
# Run all unit tests (1658+ tests)
dotnet test --filter "FullyQualifiedName!~E2E&FullyQualifiedName!~IntegrationTests"

# Run a specific module's tests
dotnet test tests/modules/notifications/NexTraceOne.Notifications.Tests/

# Run integration tests (requires Docker for Testcontainers)
dotnet test tests/platform/NexTraceOne.IntegrationTests/
```

### Frontend Tests

```bash
cd src/frontend

# Unit & integration tests (398+ tests)
npm test

# Watch mode
npm run test:watch

# E2E tests (mock backend)
npm run test:e2e

# E2E tests (real backend)
npm run test:e2e:real

# Type checking
npm run typecheck
```

---

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Node.js 20+ / npm 10+
- PostgreSQL 16+
- (Optional) Docker & Docker Compose
- (Optional) Ollama for local AI

### Quick Start with Docker

```bash
# 1. Copy and configure environment variables
cp .env.example .env
# Edit .env with your passwords and secrets

# 2. Start the full stack
docker compose up -d

# 3. Access the application
# Frontend: http://localhost:3000
# API:      http://localhost:8080
# Health:   http://localhost:8080/health
```

### Local Development (without Docker)

```bash
# 1. Create PostgreSQL databases
psql -c "CREATE DATABASE nextraceone_identity;"
psql -c "CREATE DATABASE nextraceone_catalog;"
psql -c "CREATE DATABASE nextraceone_operations;"
psql -c "CREATE DATABASE nextraceone_ai;"

# 2. Configure connection strings via user secrets
cd src/platform/NexTraceOne.ApiHost
dotnet user-secrets set "ConnectionStrings:IdentityDatabase" \
  "Host=localhost;Port=5432;Database=nextraceone_identity;Username=your_username;Password=your_password"
# Repeat for each connection string (see appsettings.json for keys)

# 3. Start the backend (migrations run automatically in Development)
dotnet run --project src/platform/NexTraceOne.ApiHost

# 4. Start the frontend
cd src/frontend
npm install
npm run dev
# UI: http://localhost:5173
```

### Building

```bash
# Build the entire solution
dotnet build NexTraceOne.sln

# Build the frontend
cd src/frontend && npm run build

# Lint the frontend
cd src/frontend && npm run lint
```

---

## Docker Compose

The `docker-compose.yml` orchestrates the full stack:

| Service | Image | Purpose |
|---------|-------|---------|
| `postgres` | postgres:16-alpine | 4 logical databases |
| `clickhouse` | clickhouse-server:24.8-alpine | Observability data store |
| `otel-collector` | opentelemetry-collector-contrib:0.115.0 | Telemetry pipeline |
| `apihost` | nextraceone/apihost | Main API |
| `workers` | nextraceone/workers | Background jobs |
| `ingestion` | nextraceone/ingestion | Data ingestion API |
| `frontend` | nextraceone/frontend | React SPA (Nginx) |

Environment variables are configured through `.env` (see `.env.example` for the full template).

---

## Documentation

Detailed documentation is available in the `docs/` directory:

| Document | Description |
|----------|-------------|
| [LOCAL-SETUP.md](docs/LOCAL-SETUP.md) | Local development setup guide |
| [ARCHITECTURE-OVERVIEW.md](docs/ARCHITECTURE-OVERVIEW.md) | System architecture |
| [DEPLOYMENT-ARCHITECTURE.md](docs/DEPLOYMENT-ARCHITECTURE.md) | Infrastructure topology |
| [SECURITY-ARCHITECTURE.md](docs/SECURITY-ARCHITECTURE.md) | Security design |
| [DESIGN-SYSTEM.md](docs/DESIGN-SYSTEM.md) | UI/UX design system |
| [ENVIRONMENT-VARIABLES.md](docs/ENVIRONMENT-VARIABLES.md) | Configuration reference |
| [AI-ARCHITECTURE.md](docs/AI-ARCHITECTURE.md) | AI integration design |
| [AI-GOVERNANCE.md](docs/AI-GOVERNANCE.md) | AI governance policies |
| [PRODUCT-VISION.md](docs/PRODUCT-VISION.md) | Product vision & roadmap |
| [ROADMAP.md](docs/ROADMAP.md) | Development roadmap |

---

## Key Architectural Decisions

1. **Modular Monolith** — Domain boundaries as autonomous modules with independent database schemas, enabling future extraction to microservices if needed.

2. **DDD + Clean Architecture** — Each module follows Domain → Application → Infrastructure → API → Contracts layering with clear dependency rules.

3. **CQRS with MediatR** — Command/query separation with pipeline behaviors for cross-cutting concerns (validation, transactions, logging, tenant isolation).

4. **Event-Driven Communication** — Modules communicate through integration events with outbox pattern for transactional consistency.

5. **Multi-Tenancy** — Built-in tenant isolation with middleware-based resolution and data filtering.

6. **Observability-First** — OpenTelemetry instrumentation, Serilog structured logging, and ClickHouse for observability data storage.

7. **Persona-Aware UX** — Frontend adapts content, navigation, and available actions based on user persona (Engineer, Tech Lead, Architect, Product, Executive, etc.).

8. **AI as Governed Capability** — Local AI is the default; external AI providers are optional and controlled by policies, quotas, and audit trails.

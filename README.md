# NexTraceOne

**NexTraceOne** is an enterprise-grade unified platform for service governance, contract governance, change intelligence, production change confidence, operational reliability, AI-assisted operations, operational knowledge management, and contextual FinOps.

## What It Does

NexTraceOne serves as the **source of truth** for services, contracts, changes, operations, and operational knowledge in enterprise environments:

- **Service Catalog & Ownership** — Centralized registry of services, teams, dependencies, and topology.
- **Contract Governance** — Full lifecycle management for REST (OpenAPI), SOAP (WSDL), Event (AsyncAPI), and background service contracts with versioning, diff, signing, and policy enforcement.
- **Change Intelligence** — Risk scoring, blast radius analysis, historical pattern matching, feature flag tracking, canary rollout monitoring, and pre-production comparison for every change.
- **Operational Reliability** — SLO/SLA tracking, incident correlation, AI-powered root cause analysis, and mitigation playbook selection.
- **AI-Assisted Operations** — Governed AI with model registry, access policies, token budgets, audit trail, and specialized agents for contract creation, incident investigation, and impact analysis.
- **Knowledge Hub** — Centralized operational documentation, runbooks, and knowledge relations.
- **Compliance & Audit** — Continuous compliance evaluation, evidence export, and audit-ready reports with SHA-256 digital signatures.
- **FinOps** — Cost intelligence contextualized by service, team, environment, and change with budget forecasting and efficiency recommendations.

## Architecture

- **Modular Monolith** — 12 bounded context modules with clean separation (Domain → Application → Infrastructure → API → Contracts).
- **28 DbContexts** — Each sub-domain has its own EF Core context sharing a single PostgreSQL database with table-prefix isolation. Run `./tools/count-dbcontexts.sh --count` to confirm.
- **Cross-module communication** — 20 contract interfaces + outbox pattern (19 processors) for reliable async messaging.
- **Row-Level Security** — PostgreSQL RLS policies for tenant isolation as defence-in-depth.

## Tech Stack

### Backend
- .NET 10 / ASP.NET Core 10
- EF Core 10 + PostgreSQL 16
- MediatR, FluentValidation, Quartz.NET
- OpenTelemetry, Serilog
- HotChocolate (GraphQL)

### Frontend
- React 19 + TypeScript
- Vite, react-router-dom, TanStack Query
- Tailwind CSS, Radix UI, Apache ECharts
- Playwright (E2E), Vitest (unit)

### Infrastructure
- Docker Compose (development + staging + production overlays)
- Elasticsearch (observability provider)
- OpenTelemetry Collector
- IIS + Windows support (via CLR Profiler collection mode)

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/)
- PostgreSQL 16 (or use Docker Compose)

### Quick Start (Development)

```bash
# 1. Copy and configure environment variables
cp .env.example .env
nano .env  # Set POSTGRES_PASSWORD, JWT_SECRET, etc.

# 2. Start infrastructure (PostgreSQL, Elasticsearch, OTel Collector)
docker compose up -d postgres elasticsearch otel-collector

# 3. Configure local secrets
cd src/platform/NexTraceOne.ApiHost
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)"
dotnet user-secrets set "ConnectionStrings:NexTraceOne" "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=YOUR_PASSWORD;Maximum Pool Size=20"

# 4. Run the backend
dotnet run --project src/platform/NexTraceOne.ApiHost

# 5. Run the frontend (in a separate terminal)
cd src/frontend
npm install
npm run dev
```

### Running Tests

```bash
# Backend unit tests (all modules)
dotnet test --filter "FullyQualifiedName!~E2E&FullyQualifiedName!~Selenium&FullyQualifiedName!~IntegrationTests"

# Frontend unit tests
cd src/frontend && npm test

# Frontend E2E tests
cd src/frontend && npm run test:e2e
```

### Docker Compose (Full Stack)

```bash
# Development
docker compose up -d

# Production (with Elasticsearch security + resource limits)
docker compose -f docker-compose.yml -f docker-compose.production.yml up -d
```

## Project Structure

```
src/
├── building-blocks/          # Shared infrastructure (security, observability, core)
├── frontend/                 # React SPA (113 routes, 4 locales)
├── modules/
│   ├── aiknowledge/          # AI governance, orchestration, external AI, runtime
│   ├── auditcompliance/      # Audit events, compliance frameworks, evidence export
│   ├── catalog/              # Service catalog, contracts, templates, developer portal
│   ├── changegovernance/     # Change intelligence, rulesets, workflows, promotion
│   ├── configuration/        # Platform configuration definitions
│   ├── governance/           # Policy engine, risk center, reports
│   ├── identityaccess/       # Authentication, authorization, teams, tenants
│   ├── integrations/         # CI/CD webhooks, multi-cluster, external systems
│   ├── knowledge/            # Documentation hub, knowledge relations
│   ├── notifications/        # Notification channels, preferences, templates
│   ├── operationalintelligence/ # Incidents, runtime, reliability, cost, telemetry
│   └── productanalytics/     # Usage analytics, feature adoption
└── platform/
    └── NexTraceOne.ApiHost/  # Main API host (entry point)
tests/
├── building-blocks/          # Building block tests
├── modules/                  # Module-specific tests
└── platform/                 # Integration + E2E tests
```

## Security

- JWT authentication with HS256 (minimum 32-char key)
- OIDC/SAML federated login support
- CSRF protection (double-submit cookie pattern)
- AES-256-GCM encryption for sensitive audit payloads
- PostgreSQL Row-Level Security for tenant isolation
- Rate limiting (global + per-endpoint policies)
- Security headers (HSTS, CSP, X-Frame-Options, etc.)
- Break Glass Access Protocol + Just-In-Time privileged access

## License

Proprietary — All rights reserved.

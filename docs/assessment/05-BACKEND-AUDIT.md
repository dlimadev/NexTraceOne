# 05 — Backend Audit

**Date:** 2026-03-22

---

## Architecture Overview

- **Pattern:** Modular Monolith with DDD + CQRS (MediatR)
- **Runtime:** .NET 10
- **Database:** PostgreSQL via EF Core
- **Modules:** 7 bounded contexts, each with Domain → Application → API → Infrastructure → Contracts layers
- **Total backend .cs files:** ~1,329 (modules only) + building blocks + platform
- **Total features (commands/queries/handlers):** 382 across all modules

---

## Layer Analysis

### Domain Layer

| Module | Entity Count | Value Objects | Enums | Events | Quality |
|--------|-------------|---------------|-------|--------|---------|
| IdentityAccess | ~15 (User, Tenant, Role, Permission, Environment, Delegation, BreakGlass, JitAccess, AccessReview, Session, etc.) | Present | Present | Present | ✅ Strong |
| Catalog | ~10 (ServiceEntry, Contract, ContractVersion, ContractDraft, CanonicalEntity, DeveloperPortalAsset, etc.) | Present | Present (ContractType, Protocol, ContractState, ApprovalStatus) | Present | ✅ Strong |
| ChangeGovernance | ~12 (Release, Deployment, ChangeAnalysis, FreezePeriod, Promotion, WorkflowInstance, WorkflowTemplate, EvidencePack, Ruleset, etc.) | Present | Present | Present | ✅ Strong |
| AIKnowledge | ~15 (AiProvider, AiModel, AiAgent, AiPolicy, AiTokenBudget, AiAuditEntry, AiRoutingRule, AgentExecution, ToolInvocation, etc.) | Present | Present (SoapDesign, SoapContractDraft added) | Present | ✅ Strong |
| Governance | ~10 (GovernancePack, Team, Domain, ComplianceCheck, Policy, IntegrationConnector, IngestionSource, AnalyticsEvent, etc.) | Present | Present | Present | ⚠️ Partial (TODOs remain) |
| OpIntelligence | ~10 (Incident, Mitigation, Runbook, AutomationWorkflow, CostSnapshot, ReliabilityScore, RuntimeSignal, etc.) | Present | Present | Present | ✅ Good |
| AuditCompliance | 1 (AuditEvent) | None | None | None | ⚠️ Minimal |

**Domain Findings:**
- All modules use `Entity<TId>` base class from `BuildingBlocks.Core`
- Strongly-typed IDs used consistently
- Aggregate roots with factory methods (`Create()` pattern)
- Guard clauses present for invariant protection
- Domain events raised on state changes
- **Gap:** AuditCompliance has only 1 entity — needs CompliancePolicy, ComplianceResult, AuditCampaign at minimum

---

### Application Layer

| Module | Features | Validation | Error Handling | Quality |
|--------|----------|-----------|----------------|---------|
| IdentityAccess | 42 | ✅ FluentValidation | ✅ Result<T> pattern | ✅ |
| Catalog | 83 | ✅ FluentValidation | ✅ Result<T> pattern | ✅ |
| ChangeGovernance | 57 | ✅ FluentValidation | ✅ Result<T> pattern | ✅ |
| AIKnowledge | 68 | ✅ FluentValidation | ✅ Result<T> pattern | ✅ |
| Governance | 73 | ✅ FluentValidation | ✅ Result<T> pattern | ⚠️ 4 TODOs |
| OpIntelligence | 52 | ✅ FluentValidation | ✅ Result<T> pattern | ✅ |
| AuditCompliance | 7 | ✅ FluentValidation | ✅ Result<T> pattern | ⚠️ Minimal scope |

**Application Layer Findings:**
- Consistent CQRS pattern with `ICommand<T>` / `IQuery<T>` / handlers
- `ValidationBehavior` in MediatR pipeline catches invalid requests
- `Result<T>` pattern used for error propagation (no exceptions for business logic)
- CancellationToken passed through all async operations
- **Gap:** Outbox events only dispatched from IdentityDbContext. Other modules create domain events but they may not reach consumers.

---

### API Layer

| Module | Endpoint Files | Auth Coverage | Versioning | Pagination | Status |
|--------|---------------|---------------|-----------|-----------|--------|
| IdentityAccess | 11 | ✅ RequirePermission + AllowAnonymous for auth routes | v1 prefix | ✅ | ✅ |
| Catalog | 5 modules | ✅ RequirePermission | v1 prefix | ✅ | ✅ |
| ChangeGovernance | 10 | ✅ RequirePermission | v1 prefix | ✅ | ✅ |
| AIKnowledge | 5 modules | ✅ RequirePermission | v1 prefix | ✅ | ✅ |
| Governance | 15 modules | ✅ RequirePermission | v1 prefix | ✅ | ✅ |
| OpIntelligence | 7 modules | ✅ RequirePermission | v1 prefix | ✅ | ✅ |
| AuditCompliance | 1 module | ✅ RequirePermission (6 endpoints) | v1 prefix | ✅ | ✅ |

**API Layer Findings:**
- Total of 391 `RequirePermission` decorators across all endpoint modules
- Health endpoints (`/live`, `/ready`, `/health`) correctly use `AllowAnonymous`
- Auth endpoints (login, register, refresh) correctly use `AllowAnonymous`
- API versioning via `/api/v1/` prefix consistently applied
- **Consistent pattern:** Minimal API endpoints with `app.MapGet/Post/Put/Delete`
- DTOs properly separated from domain entities
- **Gap:** No rate limiting configured on business API endpoints (only ingestion API)

---

### Infrastructure Layer

| Module | DbContexts | Repositories | Configurations | Migrations |
|--------|-----------|-------------|----------------|-----------|
| IdentityAccess | 1 | Repository pattern | ✅ Entity configs | 2 |
| Catalog | 3 (Contracts, Graph, Portal) | Repository pattern | ✅ Entity configs | 3 |
| ChangeGovernance | 4 (CI, Promotion, Ruleset, Workflow) | Repository pattern | ✅ Entity configs | 4 |
| AIKnowledge | 3 (Governance, ExternalAI, Orchestration) | Repository pattern + custom repositories | ✅ Entity configs | 5 |
| Governance | 1 | Repository pattern | ✅ Entity configs | 2 |
| OpIntelligence | 5 (Incidents, Automation, Cost, Reliability, Runtime) | Repository pattern | ✅ Entity configs | 5 |
| AuditCompliance | 1 | Direct DbContext usage | ✅ Entity config | 1 |

**Infrastructure Findings:**
- All DbContexts extend `NexTraceDbContextBase` which applies global tenant query filter
- Design-time factories exist for all contexts (migration tooling ready)
- Model snapshots are consistent with latest migrations
- **Gap:** Custom repositories in AIKnowledge manually filter by TenantId (line `AiRuntimeRepositories.cs:94`), which means the global filter may not be applied consistently in all query paths

---

## Building Blocks Quality

| Block | Purpose | Quality | Notes |
|-------|---------|---------|-------|
| `BuildingBlocks.Core` | Base entities, results, guards, IDs, events | ✅ Excellent | 30 tests, strongly-typed IDs, Result monad |
| `BuildingBlocks.Application` | CQRS, behaviors, pagination, context | ✅ Excellent | 34 tests, validation pipeline, correlation |
| `BuildingBlocks.Infrastructure` | EF Core base, outbox, event bus, interceptors | ✅ Good | 16 tests. NexTraceDbContextBase provides tenant filter, soft-delete, audit timestamps. Outbox with IdempotencyKey |
| `BuildingBlocks.Observability` | OTLP, health checks, metrics, logging | ✅ Good | 56 tests. Custom meters for business metrics. Serilog → Loki |
| `BuildingBlocks.Security` | JWT, cookies, permissions, multi-tenancy | ⚠️ Partial | 0 tests! JWT validation, cookie session, permission requirements, startup validation |

---

## Platform Services Quality

### ApiHost
- Registers all 7 modules
- Security headers (CSP, HSTS, X-Frame-Options, etc.)
- Startup validation (JWT secret length, connection strings)
- Seed data for default roles/permissions
- Swagger/OpenAPI in development only
- **Status:** ✅ Production-ready

### BackgroundWorkers
- 3 jobs: IdentityExpirationJob (60s), OutboxProcessorJob (5s), DriftDetectionJob (configurable)
- 6 expiration handlers for IdentityAccess entities
- **Critical Gap:** OutboxProcessorJob only processes IdentityDbContext outbox. Events from Catalog, ChangeGovernance, AIKnowledge, Governance, OperationalIntelligence, AuditCompliance contexts are NOT dispatched.
- **Status:** ⚠️ Incomplete — cross-module event propagation broken

### Ingestion.Api
- Separate entry point for CI/CD deployment events
- API key authentication
- Tenant resolution middleware
- Deployment event processing with connector/source/execution lifecycle
- **Status:** ✅ Well-designed

---

## Critical Backend Gaps

| # | Gap | Severity | Module | Evidence |
|---|-----|----------|--------|----------|
| BG-01 | Outbox processor only covers IdentityDbContext | Critical | Platform/BackgroundWorkers | `OutboxProcessorJob.cs` — only queries IdentityDbContext |
| BG-02 | BuildingBlocks.Security has 0 tests | High | BuildingBlocks | `BuildingBlocks.Security.Tests` — empty project |
| BG-03 | AuditCompliance minimal domain (1 entity) | High | AuditCompliance | Only `AuditEvent.cs` exists |
| BG-04 | Governance application TODOs | Medium | Governance | 4 TODO comments in feature handlers |
| BG-05 | CLI tool not implemented | High | Tools | 7 TODOs, 0 commands |
| BG-06 | No rate limiting on API endpoints | Medium | ApiHost | No rate limiting middleware configured |
| BG-07 | AI TenantId type inconsistency | Medium | AIKnowledge | Some use `string` TenantId, others use `Guid` |
| BG-08 | OIDC/Federated auth incomplete | Medium | IdentityAccess | Endpoints exist but no provider configuration |

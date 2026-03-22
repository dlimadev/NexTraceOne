# 01 — Solution Inventory

**Date:** 2026-03-22

---

## Solution Overview

- **Solution file:** `NexTraceOne.sln`
- **Total projects:** 58 (.csproj)
- **Architecture:** Modular Monolith with DDD, CQRS, MediatR
- **Backend:** .NET 10, PostgreSQL, EF Core
- **Frontend:** React 19, TypeScript, Vite, TailwindCSS
- **Total backend .cs files:** ~1,329 (src/modules only)
- **Total frontend .tsx/.ts files:** ~349

---

## Project Inventory

### Building Blocks (5 projects)

| Project | Role Expected | Role Actual | Notes |
|---------|--------------|-------------|-------|
| `BuildingBlocks.Core` | Domain primitives, base entities, value objects, results | ✅ Fully implemented | StronglyTypedIds, Guards, Result<T>, Events |
| `BuildingBlocks.Application` | CQRS abstractions, behaviors, pagination | ✅ Fully implemented | MediatR pipeline, validation behavior, correlation |
| `BuildingBlocks.Infrastructure` | EF Core base, outbox, event bus, interceptors | ✅ Implemented | NexTraceDbContextBase with global tenant filter, outbox with idempotency |
| `BuildingBlocks.Observability` | OpenTelemetry, health checks, metrics, logging | ✅ Implemented | OTLP export, Serilog→Loki, custom meters, telemetry models |
| `BuildingBlocks.Security` | Auth, authorization, encryption, multi-tenancy | ✅ Implemented | JWT + Cookie, permission requirements, environment access, tenant resolution |

### Platform Services (3 projects)

| Project | Role Expected | Role Actual | Notes |
|---------|--------------|-------------|-------|
| `NexTraceOne.ApiHost` | Main API gateway, module registration, health | ✅ Implemented | Registers all 7 modules, seed data, startup validation, security headers |
| `NexTraceOne.BackgroundWorkers` | Background jobs, outbox processing, drift detection | ⚠️ Partial | 3 jobs (expiration, outbox, drift). Outbox only for Identity context |
| `NexTraceOne.Ingestion.Api` | External integration entry point | ✅ Implemented | Deployment events, API key auth, tenant resolution, 578 lines |

### Module: IdentityAccess (5 projects, 177 .cs files)

| Layer | Project | Status | Key Contents |
|-------|---------|--------|--------------|
| Domain | `IdentityAccess.Domain` | ✅ Complete | User, Tenant, Role, Permission, Environment, Delegation, BreakGlass, JitAccess, AccessReview, Session entities |
| Application | `IdentityAccess.Application` | ✅ Complete | 42 features (commands/queries/handlers) |
| API | `IdentityAccess.API` | ✅ Complete | Auth, User, Tenant, Environment, Role, Delegation, BreakGlass, JitAccess, AccessReview, Session endpoints |
| Infrastructure | `IdentityAccess.Infrastructure` | ✅ Complete | IdentityDbContext, 2 migrations, configurations |
| Contracts | `IdentityAccess.Contracts` | ✅ Present | Integration events/DTOs |

### Module: Catalog (5 projects, 251 .cs files)

| Layer | Project | Status | Key Contents |
|-------|---------|--------|--------------|
| Domain | `Catalog.Domain` | ✅ Complete | ServiceEntry, Contract, ContractVersion, ContractDraft, CanonicalEntity, DeveloperPortalAsset |
| Application | `Catalog.Application` | ✅ Complete | 83 features (contracts, graph, portal, source-of-truth) |
| API | `Catalog.API` | ✅ Complete | ContractStudio, Contracts, ServiceCatalog, DeveloperPortal, SourceOfTruth endpoints |
| Infrastructure | `Catalog.Infrastructure` | ✅ Complete | 3 DbContexts (Contracts, Graph, Portal), 3 migrations |
| Contracts | `Catalog.Contracts` | ✅ Present | Integration events/DTOs |

### Module: ChangeGovernance (5 projects, 226 .cs files)

| Layer | Project | Status | Key Contents |
|-------|---------|--------|--------------|
| Domain | `ChangeGovernance.Domain` | ✅ Complete | Release, Deployment, ChangeAnalysis, FreezePeriod, Promotion, WorkflowInstance, WorkflowTemplate, EvidencePack, Ruleset |
| Application | `ChangeGovernance.Application` | ✅ Complete | 57 features (intelligence, workflow, promotion, ruleset) |
| API | `ChangeGovernance.API` | ✅ Complete | Analysis, Confidence, Deployment, Freeze, Intelligence, Release, Promotion, Approval, Evidence, Template endpoints |
| Infrastructure | `ChangeGovernance.Infrastructure` | ✅ Complete | 4 DbContexts (ChangeIntelligence, Promotion, RulesetGovernance, Workflow), 4 migrations |
| Contracts | `ChangeGovernance.Contracts` | ✅ Present | Integration events/DTOs |

### Module: AIKnowledge (5 projects, 272 .cs files)

| Layer | Project | Status | Key Contents |
|-------|---------|--------|--------------|
| Domain | `AIKnowledge.Domain` | ✅ Complete | AiProvider, AiModel, AiAgent, AiPolicy, AiTokenBudget, AiAuditEntry, AiRoutingRule, ExternalAiRequest, AgentExecution, ToolInvocation |
| Application | `AIKnowledge.Application` | ✅ Complete | 68 features (governance, external AI, orchestration, runtime) |
| API | `AIKnowledge.API` | ✅ Complete | AiGovernance, AiIde, ExternalAi, AiOrchestration, AiRuntime endpoints |
| Infrastructure | `AIKnowledge.Infrastructure` | ✅ Complete | 3 DbContexts (Governance, ExternalAI, Orchestration), 5 migrations |
| Contracts | `AIKnowledge.Contracts` | ✅ Present | Integration events/DTOs |

### Module: Governance (5 projects, 175 .cs files)

| Layer | Project | Status | Key Contents |
|-------|---------|--------|--------------|
| Domain | `Governance.Domain` | ⚠️ Partial | GovernancePack, Team, Domain, ComplianceCheck, Policy, IntegrationConnector, IngestionSource, AnalyticsEvent, plus Phase 5 enrichment entities |
| Application | `Governance.Application` | ⚠️ Partial | 73 features. TODOs: scope counting, team enrichment, ingestion last-processed field |
| API | `Governance.API` | ✅ Complete | 15 endpoint modules (Executive, Packs, Teams, Domains, Compliance, FinOps, Risk, Waivers, Evidence, Controls, Reports, Analytics, Policy, Integrations, Onboarding) |
| Infrastructure | `Governance.Infrastructure` | ✅ Complete | GovernanceDbContext, 2 migrations (initial + Phase 5 enrichment) |
| Contracts | `Governance.Contracts` | ✅ Present | Integration events/DTOs |

### Module: OperationalIntelligence (5 projects, 197 .cs files)

| Layer | Project | Status | Key Contents |
|-------|---------|--------|--------------|
| Domain | `OperationalIntelligence.Domain` | ✅ Complete | Incident, Mitigation, Runbook, AutomationWorkflow, CostSnapshot, ReliabilityScore, RuntimeSignal |
| Application | `OperationalIntelligence.Application` | ✅ Complete | 52 features (incidents, automation, cost, reliability, runtime) |
| API | `OperationalIntelligence.API` | ✅ Complete | Incident, Mitigation, Runbook, Automation, CostIntelligence, Reliability, RuntimeIntelligence endpoints |
| Infrastructure | `OperationalIntelligence.Infrastructure` | ✅ Complete | 5 DbContexts (Incidents, Automation, Cost, Reliability, Runtime), 5 migrations |
| Contracts | `OperationalIntelligence.Contracts` | ✅ Present | Integration events/DTOs |

### Module: AuditCompliance (5 projects, 31 .cs files)

| Layer | Project | Status | Key Contents |
|-------|---------|--------|--------------|
| Domain | `AuditCompliance.Domain` | ⚠️ Minimal | AuditEvent entity only |
| Application | `AuditCompliance.Application` | ⚠️ Minimal | 7 features (record, query, export) |
| API | `AuditCompliance.API` | ✅ Present | AuditEndpointModule with 6 endpoints |
| Infrastructure | `AuditCompliance.Infrastructure` | ✅ Present | AuditDbContext, 1 migration |
| Contracts | `AuditCompliance.Contracts` | ✅ Present | Integration events |

### Tests (14 projects)

| Project | Test Count | Status |
|---------|-----------|--------|
| `BuildingBlocks.Application.Tests` | 32 | ✅ |
| `BuildingBlocks.Core.Tests` | 19 | ✅ |
| `BuildingBlocks.Infrastructure.Tests` | 16 | ✅ |
| `BuildingBlocks.Observability.Tests` | 56 | ✅ |
| `BuildingBlocks.Security.Tests` | 0 | ⚠️ Empty |
| `AIKnowledge.Tests` | 356 | ✅ |
| `Catalog.Tests` | 422 | ✅ |
| `IdentityAccess.Tests` | 253 | ✅ |
| `OperationalIntelligence.Tests` | 232 | ✅ |
| `ChangeGovernance.Tests` | 181 | ✅ |
| `Governance.Tests` | 25 | ⚠️ Low |
| `AuditCompliance.Tests` | 0 | ❌ Empty |
| `IntegrationTests` | 66 | ✅ |
| `E2E.Tests` | 51 | ✅ |

### Tools (1 project)

| Project | Status |
|---------|--------|
| `NexTraceOne.CLI` | ❌ Stub only — 7 TODO commands, 0 implemented |

### Frontend (1 project)

| Metric | Count |
|--------|-------|
| Pages (.tsx) | 96 |
| Components | 64 |
| API modules | 34+ |
| Test files | 52 |
| Locales | 4 (en, pt-BR, pt-PT, es) |

---

## Dependency Map

```
ApiHost → all 7 module APIs → Application → Domain + Contracts
       → BuildingBlocks (Core, Application, Infrastructure, Observability, Security)

BackgroundWorkers → IdentityAccess.Infrastructure (outbox)
                  → OperationalIntelligence.Application (drift)
                  → BuildingBlocks

Ingestion.Api → Governance.Infrastructure (connectors/sources)
             → BuildingBlocks

Frontend → ApiHost (REST)
        → Ingestion.Api (deployment events)

CLI → Module Contracts (external consumer)
```

---

## Database Architecture

4 PostgreSQL databases hosting 16 DbContexts:

| Database | DbContexts |
|----------|-----------|
| `nextraceone_identity` | IdentityDbContext |
| `nextraceone_catalog` | ContractsDbContext, CatalogGraphDbContext, DeveloperPortalDbContext |
| `nextraceone_operations` | ChangeIntelligenceDbContext, PromotionDbContext, RulesetGovernanceDbContext, WorkflowDbContext, IncidentDbContext, AutomationDbContext, CostIntelligenceDbContext, ReliabilityDbContext, RuntimeIntelligenceDbContext, GovernanceDbContext, AuditDbContext |
| `nextraceone_ai` | AiGovernanceDbContext, ExternalAiDbContext, AiOrchestrationDbContext |

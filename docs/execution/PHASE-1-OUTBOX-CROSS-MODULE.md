# Phase 1, Block B — Outbox Cross-Module

> **Status:** Complete  
> **Risk Treated:** Domain events silently lost across module boundaries

---

## Problem

The Phase 0 audit identified that outbox processing was only wired for a subset of
module DbContexts. Domain events published by modules without a registered outbox
processor were silently discarded, breaking eventual consistency guarantees.

## Solution

### Generic Outbox Processor

Created `ModuleOutboxProcessorJob<TContext>` — a generic background service that
processes outbox messages for any EF Core DbContext.

**Location:** `src/platform/NexTraceOne.BackgroundWorkers/Jobs/ModuleOutboxProcessorJob.cs`

**Characteristics:**

| Parameter | Value |
|-----------|-------|
| Batch size | 50 messages per cycle |
| Cycle interval | 5 seconds |
| Max retries | 5 per message |
| Failure isolation | Per-processor (one failing module does not block others) |

### Registered Processors (18 total)

Registered in `BackgroundWorkers/Program.cs`:

| Module | DbContext |
|--------|-----------|
| **Identity** | `IdentityDbContext` |
| **Catalog** | `CatalogGraphDbContext` |
| **Catalog** | `ContractsDbContext` |
| **Catalog** | `DeveloperPortalDbContext` |
| **ChangeGovernance** | `ChangeIntelligenceDbContext` |
| **ChangeGovernance** | `RulesetGovernanceDbContext` |
| **ChangeGovernance** | `WorkflowDbContext` |
| **ChangeGovernance** | `PromotionDbContext` |
| **AIKnowledge** | `AiGovernanceDbContext` |
| **AIKnowledge** | `ExternalAiDbContext` |
| **AIKnowledge** | `AiOrchestrationDbContext` |
| **Governance** | `GovernanceDbContext` |
| **AuditCompliance** | `AuditDbContext` |
| **OperationalIntelligence** | `RuntimeIntelligenceDbContext` |
| **OperationalIntelligence** | `ReliabilityDbContext` |
| **OperationalIntelligence** | `CostIntelligenceDbContext` |
| **OperationalIntelligence** | `IncidentDbContext` |
| **OperationalIntelligence** | `AutomationDbContext` |

### Additional Changes

- Added Governance API project reference to BackgroundWorkers to resolve
  the `GovernanceDbContext` dependency.

## Architecture

```
BackgroundWorkers Host
├── ModuleOutboxProcessorJob<IdentityDbContext>
├── ModuleOutboxProcessorJob<CatalogGraphDbContext>
├── ModuleOutboxProcessorJob<ContractsDbContext>
│   ... (15 more)
└── ModuleOutboxProcessorJob<AutomationDbContext>
```

Each processor:
1. Opens a scoped `IServiceProvider`
2. Queries the outbox table for pending messages (batch of 50)
3. Dispatches each message to the appropriate handler
4. Marks messages as processed or increments retry count
5. Sleeps for 5 seconds before the next cycle

Failures in one processor do not propagate to others. Each processor logs errors
independently and continues operating.

## Verification

- All existing tests pass with the new processors registered
- No cross-module event loss observed in integration scenarios

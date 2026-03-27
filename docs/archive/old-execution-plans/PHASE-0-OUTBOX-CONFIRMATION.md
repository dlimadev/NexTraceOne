# Phase 0 — Outbox Gap Confirmation (GAP-001)

**Date:** 2026-03-22
**Severity:** Critical
**Status:** Confirmed with evidence

---

## 1. Current Implementation

### OutboxProcessorJob

**File:** `src/platform/NexTraceOne.BackgroundWorkers/Jobs/OutboxProcessorJob.cs`

The `OutboxProcessorJob` is a `BackgroundService` that:
- Runs every **5 seconds** using a `PeriodicTimer`
- Processes messages in batches of **50**
- Retries up to **5 times** per message
- Reports health via `WorkerJobHealthRegistry`

**Critical limitation (confirmed):** The job contains a single method `ProcessIdentityOutboxAsync()` that resolves **only** `IdentityDbContext`:

```csharp
private async Task ProcessIdentityOutboxAsync(CancellationToken cancellationToken)
{
    using var scope = serviceScopeFactory.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    // ... processes only this context's outbox_messages table
}
```

The code itself contains an explicit comment acknowledging this limitation:

> *"LIMITATION: Currently only processes IdentityDbContext outbox. Other module DbContexts (Governance, ChangeIntelligence, Contracts, AI, etc.) have outbox tables but no processor."*

### Outbox Population Mechanism

**File:** `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Persistence/NexTraceDbContextBase.cs`

The `NexTraceDbContextBase` class overrides `SaveChangesAsync()` and calls `WriteDomainEventsToOutbox()` before the base save. This method:

1. Scans `ChangeTracker` entries for aggregate roots
2. Extracts domain events from each aggregate
3. Creates `OutboxMessage` entities (with idempotency key)
4. Adds them to the `Set<OutboxMessage>()`
5. Clears domain events from the aggregate

**This means every DbContext that inherits `NexTraceDbContextBase` writes outbox messages.** The issue is that only IdentityDbContext messages are ever **read and dispatched**.

---

## 2. DbContexts Covered vs Not Covered

### ✅ Covered by OutboxProcessorJob

| DbContext | Database | Module |
|-----------|----------|--------|
| IdentityDbContext | nextraceone_identity | IdentityAccess |

### ❌ NOT Covered (messages written but never dispatched)

| DbContext | Database | Module |
|-----------|----------|--------|
| AuditDbContext | nextraceone_identity | AuditCompliance |
| ContractsDbContext | nextraceone_catalog | Catalog |
| CatalogGraphDbContext | nextraceone_catalog | Catalog |
| DeveloperPortalDbContext | nextraceone_catalog | Catalog |
| ChangeIntelligenceDbContext | nextraceone_operations | ChangeGovernance |
| WorkflowDbContext | nextraceone_operations | ChangeGovernance |
| RulesetGovernanceDbContext | nextraceone_operations | ChangeGovernance |
| PromotionDbContext | nextraceone_operations | ChangeGovernance |
| GovernanceDbContext | nextraceone_operations | Governance |
| IncidentDbContext | nextraceone_operations | OperationalIntelligence |
| CostIntelligenceDbContext | nextraceone_operations | OperationalIntelligence |
| RuntimeIntelligenceDbContext | nextraceone_operations | OperationalIntelligence |
| ReliabilityDbContext | nextraceone_operations | OperationalIntelligence |
| AutomationDbContext | nextraceone_operations | OperationalIntelligence |
| AiGovernanceDbContext | nextraceone_ai | AIKnowledge |
| ExternalAiDbContext | nextraceone_ai | AIKnowledge |
| AiOrchestrationDbContext | nextraceone_ai | AIKnowledge |

**Coverage:** 1 out of 18 DbContexts (5.6%)

---

## 3. Impact by Module

| Module | DbContexts Affected | Impact | Severity |
|--------|---------------------|--------|----------|
| **Catalog** | ContractsDbContext, CatalogGraphDbContext, DeveloperPortalDbContext | Contract lifecycle events, service graph updates, portal changes never propagated | High |
| **ChangeGovernance** | ChangeIntelligenceDbContext, WorkflowDbContext, RulesetGovernanceDbContext, PromotionDbContext | Change intelligence events, workflow state transitions, promotion events not dispatched | Critical |
| **Governance** | GovernanceDbContext | Governance policy events not dispatched | High |
| **OperationalIntelligence** | IncidentDbContext, CostIntelligenceDbContext, RuntimeIntelligenceDbContext, ReliabilityDbContext, AutomationDbContext | Incident lifecycle events, reliability alerts, automation triggers not dispatched | Critical |
| **AIKnowledge** | AiGovernanceDbContext, ExternalAiDbContext, AiOrchestrationDbContext | AI model registry events, policy changes, orchestration events not dispatched | High |
| **AuditCompliance** | AuditDbContext | Audit trail events not dispatched cross-module | High |

---

## 4. Nature of the Problem

The problem is **structural and total** for non-Identity modules:

- **Structural:** The `OutboxProcessorJob` was designed for a single context and never extended as new modules were added.
- **Total:** No partial workaround exists. Non-Identity outbox messages accumulate in their respective database tables indefinitely with `ProcessedAt = NULL`.
- **Silent:** There are no error logs, health check failures, or alerts when messages go unprocessed. The system appears to work normally because the write path succeeds.

### Evidence of Silent Failure

When a domain event is raised in (e.g.) the Catalog module:
1. `ContractsDbContext.SaveChangesAsync()` → `WriteDomainEventsToOutbox()` → writes `OutboxMessage` to `nextraceone_catalog.outbox_messages`
2. `OutboxProcessorJob` only queries `nextraceone_identity.outbox_messages`
3. The Catalog outbox message sits unprocessed forever
4. Any cross-module handler subscribed to that event **never fires**

---

## 5. Recommended Fix for Phase 1

### Option A: Per-Database Outbox Processors (Recommended)

Create 3 additional outbox processor methods in `OutboxProcessorJob`:
- `ProcessCatalogOutboxAsync()` → processes `ContractsDbContext` (covers all Catalog DbContexts via shared database)
- `ProcessOperationsOutboxAsync()` → processes one representative DbContext for `nextraceone_operations`
- `ProcessAiOutboxAsync()` → processes one representative DbContext for `nextraceone_ai`

**Note:** Since multiple DbContexts share the same physical database (and thus the same `outbox_messages` table), processing one DbContext per physical database is sufficient.

### Option B: Generic Outbox Processor

Refactor `OutboxProcessorJob` to accept a list of DbContext types and process each in turn. This is more maintainable but requires more upfront design.

### Recommendation

**Option A** for Phase 1 (smallest safe change, immediate coverage), followed by **Option B** refactoring in Phase 2 if needed.

---

## 6. Database-Level Analysis

Since multiple DbContexts map to the same physical database, the actual number of distinct `outbox_messages` tables is **4** (one per physical database):

| Physical Database | DbContexts | Outbox Table | Processor Status |
|-------------------|-----------|--------------|-----------------|
| nextraceone_identity | IdentityDbContext, AuditDbContext | ✅ Processed | ✅ Active |
| nextraceone_catalog | ContractsDbContext, CatalogGraphDbContext, DeveloperPortalDbContext | ❌ Not processed | ❌ Missing |
| nextraceone_operations | 10 DbContexts (ChangeIntelligence, Workflow, etc.) | ❌ Not processed | ❌ Missing |
| nextraceone_ai | AiGovernanceDbContext, ExternalAiDbContext, AiOrchestrationDbContext | ❌ Not processed | ❌ Missing |

**Minimum fix:** Add 3 processors (one per uncovered database) to achieve 100% outbox coverage.

# Wave Final — Remove Last Stubs

## Scope

This wave eliminates all remaining MVP stubs, placeholder scores, template fallbacks,
and "not configured" behaviors from exposed capabilities in NexTraceOne.

## Targets

| # | Target | Previous State | Final State |
|---|--------|---------------|-------------|
| 1 | `ApplyGovernancePack` | MVP stub — returned `Guid.NewGuid()` without persistence | Real — lookups pack, resolves version, persists `GovernanceRolloutRecord` |
| 2 | `CreatePackVersion` | MVP stub — returned `Guid.NewGuid()` without persistence | Real — validates pack, creates `GovernancePackVersion`, persists |
| 3 | `GenerateDraftFromAi` | Template fallback presented as AI output | Real — `AiGenerated` field distinguishes AI vs template; AI integration via `AiDraftGeneratorService` |
| 4 | `DocumentRetrievalService` | Labeled as empty/stub | Already real — queries `IAiKnowledgeSourceRepository` for active sources |
| 5 | `TelemetryRetrievalService` | Labeled as empty/stub | Already real — queries `IObservabilityProvider` for logs |
| 6 | `IncidentContextSurface` | Docstring said "stub" | Already real — queries `IncidentDbContext` with tenant isolation; docstring corrected |
| 7 | `ReleaseContextSurface` | Docstring said "stub" | Already real — queries `ChangeIntelligenceDbContext` with tenant isolation; docstring corrected |
| 8 | `GetBenchmarking` | Hardcoded `50.0m` for ReliabilityScore, ChangeSafetyScore, MaturityScore, RiskScore | Real — nullable scores; returns `null` when data unavailable; real FinOps efficiency, context, strengths/gaps |
| 9 | `SyncJiraWorkItems` | Returned success with "not configured" message | Formal deferral — returns `JIRA_INTEGRATION_DEFERRED` error |

## What Stopped Being Stub/MVP/Fallback

- `ApplyGovernancePack.Handler` — no longer returns fake UUID
- `CreatePackVersion.Handler` — no longer returns fake UUID
- `GenerateDraftFromAi.Response` — now includes `AiGenerated` boolean for transparency
- `GetBenchmarking` — no longer uses `50.0m` placeholder for any score
- `SyncJiraWorkItems` — no longer returns misleading success

## What Was Already Real (Documentation Corrected)

- `DocumentRetrievalService` — real implementation using `IAiKnowledgeSourceRepository`
- `TelemetryRetrievalService` — real implementation using `IObservabilityProvider`
- `IncidentContextSurface` — real EF Core queries with tenant isolation
- `ReleaseContextSurface` — real EF Core queries with tenant isolation

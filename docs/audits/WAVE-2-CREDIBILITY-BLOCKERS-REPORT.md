# WAVE-2: Credibility Blockers Report

## Initial State

The NexTraceOne core had 10 credibility blockers identified in the master audit:

| # | Component | Issue |
|---|---|---|
| 1 | `GetEfficiencyIndicators` | Hardcoded demo data with `IsSimulated: true` |
| 2 | `GetWasteSignals` | Hardcoded demo data with `IsSimulated: true` |
| 3 | `GetFrictionIndicators` | Hardcoded demo data with `IsSimulated: true` |
| 4 | `RunComplianceChecks` | 15 hardcoded compliance checks |
| 5 | `GenerateDraftFromAi` | Static template, no AI integration |
| 6 | `DocumentRetrievalService` | Returns `Array.Empty` |
| 7 | `TelemetryRetrievalService` | Returns `Array.Empty` |
| 8 | `GetExecutiveDrillDown` | `IsSimulated: true` despite using real data |
| 9 | `EvidencePackagesPage` | Preview badge on mature feature |
| 10 | `GovernancePackDetailPage` | Preview badge |

## What Was Demo/Stub/Mock

- **Demo**: GetEfficiencyIndicators, GetWasteSignals, GetFrictionIndicators — fabricated data
- **Mock**: RunComplianceChecks — 15 fake compliance results
- **Stub**: DocumentRetrievalService, TelemetryRetrievalService — empty returns
- **Template**: GenerateDraftFromAi — static YAML/XML templates
- **Inconsistency**: GetExecutiveDrillDown — `IsSimulated: true` on real data

## What Was Corrected

| # | Component | Resolution |
|---|---|---|
| 1 | `GetEfficiencyIndicators` | ✅ Real data from CostIntelligence |
| 2 | `GetWasteSignals` | ✅ Real waste detection from CostIntelligence |
| 3 | `GetFrictionIndicators` | ✅ Real analytics events from AnalyticsEventRepository |
| 4 | `RunComplianceChecks` | ✅ Real checks against governance entities |
| 5 | `GenerateDraftFromAi` | ✅ AI provider integration with template fallback |
| 6 | `DocumentRetrievalService` | ✅ Real search in AIKnowledgeSources |
| 7 | `TelemetryRetrievalService` | ✅ Real query via IObservabilityProvider |
| 8 | `GetExecutiveDrillDown` | ✅ Default changed to `IsSimulated: false` |
| 9 | `EvidencePackagesPage` | ✅ Preview badge removed |
| 10 | `GovernancePackDetailPage` | ⚠️ Preview badge kept on Simulation tab (legitimate — impact model needs production data) |

## What Remains Pending

- `GovernancePackDetailPage` Simulation tab preview badge — requires production impact model data
- `DocumentRetrievalService` uses text matching only — semantic/vector search is a future enhancement
- `TelemetryRetrievalService` depends on ClickHouse/Elastic operational availability

## Recommendation for Wave 3

1. Complete the simulation impact model integration for GovernancePackDetailPage
2. Add semantic search to DocumentRetrievalService (RAG/embeddings)
3. Add distributed trace correlation to TelemetryRetrievalService
4. Expand compliance checks to cover contracts and service catalog entities
5. Add AI audit trail for GenerateDraftFromAi operations

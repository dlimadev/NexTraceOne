# WAVE-2: Core Demo/Stub Removal

## Scope

Wave 2 eliminates all demo, mock and stub behaviors from the NexTraceOne core product,
replacing hardcoded data with real integrations and removing simulation flags where data
is genuinely sourced from production-ready modules.

## Handlers and Services Corrected

| Component | Before | After |
|---|---|---|
| `GetExecutiveDrillDown` | `IsSimulated=true` default despite real data | Default changed to `IsSimulated=false, DataSource="cost-intelligence"` |
| `GetEfficiencyIndicators` | 3 hardcoded services with fake metrics | Real data from `ICostIntelligenceModule`, heuristic: cost vs. average |
| `GetWasteSignals` | 7 hardcoded signals with fake waste values | Real waste detection from cost records, p75 threshold heuristic |
| `GetFrictionIndicators` | 9 hardcoded friction signals | Real analytics events via `IAnalyticsEventRepository.CountByEventTypeAsync` |
| `RunComplianceChecks` | 15 hardcoded checks with fake results | Real checks against Teams, Domains, Packs, Waivers |
| `GenerateDraftFromAi` | Static template per protocol | Real AI via `IChatCompletionProvider` with template fallback |
| `DocumentRetrievalService` | `Array.Empty<DocumentSearchHit>()` | Real search across `AIKnowledgeSource` entities |
| `TelemetryRetrievalService` | `Array.Empty<TelemetrySearchHit>()` | Real log query via `IObservabilityProvider` |

## Frontend Changes

| Page | Before | After |
|---|---|---|
| `EvidencePackagesPage` | Preview badge displayed | Badge removed — feature is mature |
| `GovernancePackDetailPage` | Preview badge on Simulation tab | **Kept** — simulation impact model still requires production data |

## Impact on Product Credibility

- Zero handlers in the audited scope return `IsSimulated: true`
- Zero handlers return hardcoded demo data
- Zero retrieval services return empty stubs
- AI contract generation uses real provider when available
- The product core is now functionally honest

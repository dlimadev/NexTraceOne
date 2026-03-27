# WAVE-2: Governance Real Data

## Indicators Implemented

### Efficiency Indicators (`GetEfficiencyIndicators`)
- **Source**: `ICostIntelligenceModule.GetCostRecordsAsync()`
- **Heuristic**: Cost per service vs. tenant average cost
- **Classification**: Efficient (≤0.8x), Acceptable (0.8–1.5x), Inefficient (1.5–2x), Wasteful (>2x)
- **Metrics**: Cost vs Average with currency-aware assessment
- **Empty state**: Returns empty list with `OverallEfficiencyScore: 0` when no cost data

### Waste Signals (`GetWasteSignals`)
- **Source**: `ICostIntelligenceModule.GetCostRecordsAsync()`
- **Heuristic**: Services above p75 cost threshold with waste = cost − average
- **Classification**: OverProvisioned (>3x), IdleCostlyResource (>2x), DegradedCostAmplification (>p75)
- **Severity**: High (>avg), Medium (>0.5×avg), Low (≤0.5×avg)
- **Empty state**: Returns empty signals with `TotalWaste: 0`

### Friction Indicators (`GetFrictionIndicators`)
- **Source**: `IAnalyticsEventRepository.CountByEventTypeAsync()`
- **Event types**: ZeroResultSearch, EmptyStateEncountered, JourneyAbandoned
- **Trend**: Compares current period vs. previous period (±5% threshold)
- **Empty state**: Returns empty indicators with `OverallFrictionScore: 0`

## Compliance Checks (`RunComplianceChecks`)
- **Source**: `ITeamRepository`, `IGovernanceDomainRepository`, `IGovernancePackRepository`, `IGovernanceWaiverRepository`
- **Checks implemented**:
  1. Team Active Status — verifies teams are active
  2. Domain Criticality Defined — verifies domains have criticality ≥ Medium
  3. Published Governance Packs — at least one pack published
  4. Pending Waivers Review — no excessive pending waivers
  5. Pack Version Control — published packs have version tracking

## Simulation Flags Removed

| Handler | `IsSimulated` | `DataSource` |
|---|---|---|
| `GetExecutiveDrillDown` | `false` | `cost-intelligence` |
| `GetEfficiencyIndicators` | `false` | `cost-intelligence` |
| `GetWasteSignals` | `false` | `cost-intelligence` |
| `GetFrictionIndicators` | `false` | `analytics` |
| `RunComplianceChecks` | N/A (never had flag) | Real governance entities |

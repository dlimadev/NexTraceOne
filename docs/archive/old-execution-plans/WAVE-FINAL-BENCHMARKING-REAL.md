# Wave Final — Benchmarking Real Implementation

## Previous State

`GetBenchmarking` used hardcoded placeholder values for scores that had no real data source:

```csharp
ReliabilityScore: 50.0m,       // ← placeholder
ChangeSafetyScore: 50.0m,      // ← placeholder
MaturityScore: 50.0m,          // ← placeholder
RiskScore: 50.0m,              // ← placeholder
Criticality: "Medium",         // ← placeholder
IncidentRecurrenceRate: 0m,    // ← placeholder
ReliabilityTrend: TrendDirection.Stable,  // ← placeholder
```

## Current Implementation

### Nullable Scores
All scores that cannot be computed from available data are now nullable:

| Score | Type | Status |
|-------|------|--------|
| `ReliabilityScore` | `decimal?` | `null` — requires incident/SLA data not yet available in cost module |
| `ChangeSafetyScore` | `decimal?` | `null` — requires release correlation data not yet available in cost module |
| `MaturityScore` | `decimal?` | `null` — requires multi-dimensional governance assessment |
| `RiskScore` | `decimal?` | `null` — requires compound calculation from multiple domains |
| `Criticality` | `string?` | `null` — requires service criticality classification |
| `ReliabilityTrend` | `TrendDirection?` | `null` — requires time-series reliability data |
| `IncidentRecurrenceRate` | `decimal?` | `null` — requires incident module cross-reference |
| `FinopsEfficiency` | `CostEfficiency` | **Real** — computed from average cost data |

### Real Computations
- **FinopsEfficiency**: Computed from average cost per group using tiered thresholds
- **ServiceCount**: Computed from distinct service IDs per group
- **Context**: Describes the actual data basis (cost records count, service count, average cost)
- **Strengths**: Derived from real cost efficiency and per-service cost analysis
- **Gaps**: Derived from real cost efficiency analysis

### Score Formulas

#### FinOps Efficiency
```
avgCost > 15000 → Wasteful
avgCost > 10000 → Inefficient
avgCost > 5000  → Acceptable
avgCost ≤ 5000  → Efficient
```

### Honest Limitations
The following scores return `null` because computing them correctly requires
cross-module data that is not available through the `ICostIntelligenceModule` contract:

- **ReliabilityScore** — Would need incident frequency, MTTR, and SLA compliance data
- **ChangeSafetyScore** — Would need release failure rates and rollback frequency
- **MaturityScore** — Would need multi-dimensional governance assessment scores
- **RiskScore** — Would need compound calculation from reliability + change safety + cost

When these data sources become available, the scores can be computed with documented,
reproducible formulas. Until then, `null` is the honest answer.

### Frontend Impact
The `BenchmarkComparisonDto` TypeScript interface was updated to use `| null` for
all nullable fields. The `BenchmarkingPage` component renders `—` for null scores
instead of displaying misleading numeric values.

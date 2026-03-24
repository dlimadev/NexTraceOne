# Configuration — Benchmarking Weights, Thresholds & Formulas

## Score Weights (`benchmarking.score.weights`)

Top-level benchmarking dimension weights (must sum to 100):

| Dimension | Default Weight |
|-----------|---------------|
| Reliability | 25 |
| Performance | 20 |
| Cost Efficiency | 20 |
| Security | 15 |
| Operational Excellence | 10 |
| Documentation | 10 |

Scope: System, Tenant — allows tenants to prioritize different dimensions.

## Score Thresholds (`benchmarking.score.thresholds`)

Classification thresholds:

| Band | Min Score |
|------|-----------|
| Excellent | 90 |
| Good | 70 |
| Needs Improvement | 50 |
| Critical | 0 |

Scope: System, Tenant, **Environment** — Production can have stricter thresholds.

## Score Bands (`benchmarking.score.bands`)

Visual configuration with labels, colors, and minimum scores per band:
- Excellent: #10B981 (green), minScore 90
- Good: #3B82F6 (blue), minScore 70
- Needs Improvement: #F59E0B (amber), minScore 50
- Critical: #DC2626 (red), minScore 0

## Formula Components (`benchmarking.formula.components`)

Sub-weights within each dimension:

### Reliability
- Uptime weight: 0.5
- MTTR weight: 0.3
- Incident rate weight: 0.2

### Performance
- P99 latency weight: 0.4
- Throughput weight: 0.3
- Error rate weight: 0.3

### Cost Efficiency
- Budget adherence weight: 0.5
- Waste reduction weight: 0.3
- Optimization adoption weight: 0.2

## Dimension Sub-Weights (`benchmarking.score.by_dimension`)

Detailed weight distribution within each scoring dimension (percentage points):
- Reliability: uptime 50, MTTR 30, incident rate 20
- Performance: latency 40, throughput 30, error rate 30
- Security: vulnerabilities 40, compliance 30, patch currency 30
- And so on for each dimension.

## Environment Overrides (`benchmarking.thresholds.by_environment`)

Per-environment threshold overrides:
- Production: Excellent ≥ 95, Good ≥ 80
- Development: Excellent ≥ 80, Good ≥ 60

## Missing Data Handling

### Policy (`benchmarking.missing_data.policy`)
Options:
- **SkipDimension**: Exclude from score calculation
- **UseDefault**: Use default score value
- **Penalize**: Score as zero for missing dimension

Default: UseDefault

### Default Score (`benchmarking.missing_data.default_score`)
- Default: 50
- Range: 0–100 (validated)

## Safeguards

- Score weights must sum to 100 (validation responsibility of consuming service)
- Thresholds must be ordered descending (Excellent > Good > NeedsImprovement > Critical)
- Formula sub-weights must sum to 1.0 within each dimension
- Missing data handling prevents undefined scoring behavior
- All changes are auditable through the configuration audit trail

## Effective Settings

The effective settings explorer shows:
- Active weights per tenant/environment
- Active thresholds with inheritance source
- Formula configuration with override indicators
- Missing data policy resolution

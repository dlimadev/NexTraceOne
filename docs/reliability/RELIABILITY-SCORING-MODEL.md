# Reliability Scoring Model

## Overview

The `OverallScore` for a service is a deterministic, explainable composite score (0–100)
derived from three real data sources available within the OperationalIntelligence module.

## Formula

```
OverallScore = (RuntimeHealthScore × 0.50) + (IncidentImpactScore × 0.30) + (ObservabilityScore × 0.20)
```

## Component Definitions

### RuntimeHealthScore (weight: 50%)

**Source**: Latest `RuntimeSnapshot` from `oi_runtime_snapshots`, filtered by `ServiceName`
and optionally by `Environment`.

| HealthStatus | Score |
|-------------|-------|
| `Healthy` | 100 |
| `Degraded` | 60 |
| `Unhealthy` | 20 |
| `Unknown` (no data) | 50 |

**Interpretation**: The runtime health is the primary signal of current service status.
It is weighted most heavily (50%) because it reflects real-time operational behavior.

### IncidentImpactScore (weight: 30%)

**Source**: Active `IncidentRecord` entries from `oi_incidents` in the last 30 days,
filtered by `ServiceId` and `TenantId`, with `Status` in `{Open, Investigating, Mitigating, Monitoring}`.

**Formula**:
```
IncidentImpactScore = max(0, 100 - (criticalCount × 30 + highCount × 20 + mediumCount × 10 + lowCount × 5))
```

| Incident Severity | Penalty |
|------------------|---------|
| `Critical` | −30 per incident |
| `High` | −20 per incident |
| `Medium` | −10 per incident |
| `Low` | −5 per incident |

Score is clamped to minimum 0.

**Interpretation**: Active incidents are the clearest indicator of reliability problems.
A single Critical incident reduces the impact score by 30 points; two Critical incidents
would reduce it by 60 points (yielding an impact score of 40).

### ObservabilityScore (weight: 20%)

**Source**: `ObservabilityProfile` from `oi_observability_profiles`, latest record
for the `ServiceName`.

| Condition | Score |
|-----------|-------|
| Profile exists | `ObservabilityProfile.Score × 100` (profile score is 0.0–1.0) |
| No profile found | 50 (neutral — no reward, no penalty) |

**Interpretation**: Observability quality affects the ability to detect and respond to issues.
The 20% weight reflects its supporting role relative to runtime health and incident impact.

## TrendDirection Calculation

Compares the latest `OverallScore` to the previous `ReliabilitySnapshot` for the same service:

| Condition | TrendDirection |
|-----------|---------------|
| `currentScore > previousScore + 5` | `Improving` |
| `currentScore < previousScore - 5` | `Declining` |
| Within ±5 points, or no history | `Stable` |

## ReliabilityStatus Derivation

The `ReliabilityStatus` enum is derived from the `OverallScore` and supplementary signals:

| Condition | Status |
|-----------|--------|
| HealthStatus = `Unhealthy` OR (openIncidents > 0 AND overallScore < 40) | `Unavailable` |
| HealthStatus = `Degraded` OR overallScore < 60 | `Degraded` |
| overallScore < 75 OR openIncidents > 0 | `NeedsAttention` |
| Otherwise | `Healthy` |

## Limitations

1. **No SLO data**: SLO compliance is not computed because there are no SLO definitions
   yet in the platform. This field is intentionally omitted (not faked).

2. **No MTTR**: Mean Time To Resolve cannot be computed without resolved incident history
   linked to services with timestamps. Omitted until incident lifecycle tracking matures.

3. **Service discovery from runtime/incidents only**: Services are discovered from runtime
   snapshots and incident records. Services with no observability data or incidents will not
   appear until the Service Catalog integration is complete.

4. **Team/domain derivation**: `teamName` and `domain` on `ServiceReliabilityItem` are
   derived from the `OwnerTeam` and `ImpactedDomain` fields in `IncidentRecord`.
   Services known only from RuntimeSnapshot will show empty team/domain until Catalog
   integration is complete.

## Score Reproducibility

The score is fully reproducible given the same inputs. To reproduce:
1. Query `oi_runtime_snapshots` for latest by `ServiceName`
2. Query `oi_incidents` for active incidents in last 30 days by `ServiceId`
3. Query `oi_observability_profiles` for latest by `ServiceName`
4. Apply the formula above

The formula is deterministic — the same inputs always yield the same output.

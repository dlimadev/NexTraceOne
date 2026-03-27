# Change Governance — Score and Blast Radius Review

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Change Score — Current Implementation

### 1.1 How the Score Works Today

| Aspect | Detail |
|--------|--------|
| **Handler** | `ComputeChangeScore.cs` in `ChangeIntelligence/Features/ComputeChangeScore/` |
| **Endpoint** | `POST /api/v1/analysis/score` with `releaseId` in body |
| **Output** | `ChangeIntelligenceScore` entity with `Score` (0.0–1.0) |
| **Formula** | Composite of `BreakingChangeWeight`, `BlastRadiusWeight`, `EnvironmentWeight` |
| **Storage** | `ci_change_intelligence_scores` table, linked to `ReleaseId` |
| **Permission** | `change-intelligence:write` |

### 1.2 Score Components

| Component | Weight Name | Source | Description |
|-----------|-----------|--------|-------------|
| **Breaking Change** | `BreakingChangeWeight` | `ChangeLevel` enum on Release | Operational=0, NonBreaking=0.1, Additive=0.3, Breaking=0.7, Publication=1.0 |
| **Blast Radius** | `BlastRadiusWeight` | `BlastRadiusReport.TotalAffectedConsumers` | Normalised count of affected consumers |
| **Environment** | `EnvironmentWeight` | Release `Environment` + env criticality | Higher weight for production environments |

### 1.3 Is the Score Real or Cosmetic?

**Assessment: ✅ The score is REAL — with limitations.**

- ✅ The formula computes from actual data (change level, consumer count, environment criticality)
- ✅ The score is persisted and queryable
- ✅ The score is displayed on ChangeDetailPage and feeds into workflow decisions
- ⚠️ The `BlastRadiusWeight` depends on blast radius accuracy (which is partial — see below)
- ⚠️ No configurable weight tuning per tenant/organisation
- ⚠️ No historical calibration (model doesn't learn from past outcomes)

### 1.4 Score Gaps

| ID | Gap | Impact | Priority |
|----|-----|--------|----------|
| S-01 | No domain-level validation that `Score` stays in 0.0–1.0 range | Could store invalid scores | P1 |
| S-02 | Weight formula is hardcoded, not configurable per tenant | All tenants get same risk model | P2 |
| S-03 | No calibration against historical outcomes (e.g., which score ranges correlated with incidents) | Score doesn't improve over time | P3 |
| S-04 | `BlastRadiusWeight` depends on transitive resolution, which is incomplete | Score accuracy reduced | P1 |
| S-05 | No composite score that includes ruleset lint results | Ruleset findings don't feed into change confidence score | P2 |

---

## 2. Blast Radius — Current Implementation

### 2.1 How Blast Radius Works Today

| Aspect | Detail |
|--------|--------|
| **Handler** | `CalculateBlastRadius.cs` in `ChangeIntelligence/Features/CalculateBlastRadius/` |
| **Endpoint** | `POST /api/v1/analysis/blast-radius/{releaseId}` |
| **Output** | `BlastRadiusReport` entity with `TotalAffectedConsumers`, `DirectConsumers[]`, `TransitiveConsumers[]` |
| **Storage** | `ci_blast_radius_reports` table, linked to `ReleaseId` |
| **Permission** | `change-intelligence:write` |
| **Read endpoint** | `GET /api/v1/analysis/blast-radius/{releaseId}` |

### 2.2 Blast Radius Depth

| Level | Status | Implementation |
|-------|--------|---------------|
| **Direct consumers** | ✅ Implemented | Queries services that directly depend on the changed API asset |
| **Transitive consumers** | ⚠️ Partial | `TransitiveConsumers[]` field exists and is populated, but resolution depends on Catalog Graph depth, which is not fully operational |
| **Cross-environment impact** | ❌ Not implemented | No analysis of how a change in staging affects production consumers |
| **Contract-level impact** | ❌ Not implemented | No analysis at the OpenAPI/AsyncAPI field level (e.g., which fields changed and which consumers use those specific fields) |

### 2.3 Is Blast Radius Real or Cosmetic?

**Assessment: ⚠️ PARTIALLY REAL — direct is real, transitive is structural.**

- ✅ Direct consumer calculation is functional and produces real data
- ✅ The `BlastRadiusReport` entity stores concrete consumer lists (not just counts)
- ✅ The data feeds into the change score and is displayed on ChangeDetailPage
- ⚠️ Transitive resolution depends on Catalog dependency graph depth — current depth may be limited
- ❌ No contract-level field-by-field impact analysis
- ❌ No cross-environment propagation analysis

### 2.4 Blast Radius Data Model

```
BlastRadiusReport
├── Id: BlastRadiusReportId
├── ReleaseId: ReleaseId (FK to Release)
├── TotalAffectedConsumers: int
├── DirectConsumers: string[] (JSON array of service names)
├── TransitiveConsumers: string[] (JSON array of service names)
└── CalculatedAt: DateTimeOffset
```

### 2.5 Dependency on Catalog Graph

The blast radius calculation depends on the **Service Catalog dependency graph**:

| Catalog Capability | Status | Impact on Blast Radius |
|-------------------|--------|----------------------|
| Service → Service dependencies | ✅ Available | Direct consumers can be resolved |
| Transitive dependency traversal | ⚠️ Partial | Depth of graph traversal may be limited |
| API → Consumer mapping | ⚠️ Partial | `ApiAssetId` references exist but mapping completeness varies |
| Contract field → Consumer field mapping | ❌ Not available | Cannot do field-level blast radius |

### 2.6 Blast Radius Gaps

| ID | Gap | Impact | Priority |
|----|-----|--------|----------|
| BR-01 | Transitive resolution depth limited by Catalog Graph completeness | Underestimates blast radius for deep dependency chains | P1 |
| BR-02 | No contract-level (field-by-field) impact analysis | Cannot identify which consumers are affected by specific field changes | P2 |
| BR-03 | No cross-environment propagation analysis | Change in staging doesn't show impact on production consumers | P2 |
| BR-04 | `DirectConsumers` and `TransitiveConsumers` stored as JSON arrays | Limits queryability and join potential; should consider normalisation | P3 |
| BR-05 | No caching of blast radius calculations | Repeated calculations for same release version | P3 |
| BR-06 | No incremental blast radius update (full recalculation each time) | Performance impact for large dependency graphs | P3 |

---

## 3. Score + Blast Radius Integration

### 3.1 Current Flow

```
Release Created
  └─→ ClassifyChangeLevel → ChangeLevel stored on Release
  └─→ CalculateBlastRadius → BlastRadiusReport stored
  └─→ ComputeChangeScore → ChangeIntelligenceScore stored
         ├── Uses ChangeLevel → BreakingChangeWeight
         ├── Uses BlastRadiusReport.TotalAffectedConsumers → BlastRadiusWeight
         └── Uses Environment criticality → EnvironmentWeight
```

### 3.2 What the Score Feeds Into

| Consumer | How Score Is Used |
|----------|------------------|
| ChangeDetailPage | Displays score prominently with colour coding |
| Workflow template selection | Templates can be matched by API criticality (indirectly related to score) |
| Change advisory | `GetChangeAdvisory` uses score context for AI recommendations |
| Change decision history | Score is available when recording approval/rejection decisions |

### 3.3 What Is Missing in Integration

| Gap | Description | Priority |
|-----|-------------|----------|
| I-01 | Score does not automatically trigger workflow if above threshold | P2 |
| I-02 | Score does not feed into promotion gate evaluation | P1 |
| I-03 | Ruleset lint score not included in composite change score | P2 |
| I-04 | No score history tracking for trend analysis | P3 |
| I-05 | No score comparison between environments (staging vs production) | P3 |

---

## 4. Minimum Viable Requirements for Production

### Score

1. ✅ Composite formula with real weights — done
2. ✅ Persistence and queryability — done
3. ⚠️ Domain-level range validation (0.0–1.0) — needs adding
4. ⚠️ Score feeding into promotion gates — needs wiring
5. 🟡 Configurable weights per tenant — nice-to-have

### Blast Radius

1. ✅ Direct consumer resolution — done
2. ⚠️ Transitive resolution via Catalog Graph — needs depth improvement
3. 🟡 Contract-level impact analysis — future enhancement
4. 🟡 Cross-environment propagation — future enhancement

---

## 5. Recommendations

| # | Recommendation | Priority | Effort |
|---|---------------|----------|--------|
| 1 | Add domain validation: `Score` must be 0.0–1.0 | P0 | 1h |
| 2 | Wire score into promotion gate evaluation (score gate type) | P1 | 8h |
| 3 | Improve Catalog Graph integration for transitive depth | P1 | 2 weeks |
| 4 | Include ruleset lint score in composite change score | P2 | 4h |
| 5 | Add score threshold-based auto-workflow triggering | P2 | 8h |
| 6 | Normalise blast radius consumer lists (separate table) | P3 | 8h |

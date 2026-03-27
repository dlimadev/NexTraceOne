# Phase 8 — AI Validation and Readiness

## AI Capability Status

The NexTraceOne AI is a **transversal, context-aware, governed capability** — not a generic chat.

## Validated Use Cases

### ✅ Non-Prod Risk Analysis (`POST /api/v1/aiorchestration/analysis/non-prod`)

**Validated scenarios:**
- QA environment with contract drift → HIGH risk finding
- HML environment with elevated error rate → MEDIUM risk
- DEV environment clean → LOW risk, no findings
- Provider unavailable → safe error returned (no exception leak)
- TenantId included in grounding (isolation verified)
- EnvironmentId echoed in response (traceability verified)
- CorrelationId unique per execution (auditability verified)

**Parsing validated:**
- `FINDING: severity | category | description` → `AnalysisFinding[]`
- `OVERALL_RISK: HIGH/MEDIUM/LOW` → `OverallRiskLevel`
- `RECOMMENDATION: ...` → `Recommendation`

### ✅ Environment Comparison (`POST /api/v1/aiorchestration/analysis/compare-environments`)

**Validated scenarios:**
- UAT vs PROD: topology + contract divergences → BLOCK_PROMOTION
- Clean environments → SAFE_TO_PROMOTE
- Grounding explicitly states "same tenant" constraint
- Same-environment comparison rejected by validator
- Empty TenantId rejected by validator

**Parsing validated:**
- `DIVERGENCE: severity | dimension | description` → `EnvironmentDivergence[]`
- `PROMOTION_RECOMMENDATION: SAFE_TO_PROMOTE/REVIEW_REQUIRED/BLOCK_PROMOTION`
- `SUMMARY: ...` → `Summary`

### ✅ Promotion Readiness Assessment (`POST /api/v1/aiorchestration/analysis/promotion-readiness`)

**Validated scenarios:**
- payment-service with contract blocker → NOT_READY, ShouldBlock: true, score 42
- api-gateway healthy → READY, ShouldBlock: false, score 94
- Score clamping: >100 → 100
- Source == Target rejected by validator
- Empty ServiceName/Version rejected
- ObservationWindowDays outside 1..90 rejected
- ReleaseId carried through grounding and response

**Parsing validated:**
- `READINESS_SCORE: N` → `ReadinessScore` (clamped 0..100)
- `READINESS_LEVEL: NOT_READY/NEEDS_REVIEW/READY`
- `BLOCKER: category | description` → `Blockers[]`
- `WARNING: category | description` → `Warnings[]`
- `SHOULD_BLOCK: YES/NO` → `ShouldBlock`
- `SUMMARY: ...` → `Summary`

## Tenant Isolation Evidence

The AI isolation is enforced at multiple layers:

1. **Command validator** — TenantId/EnvironmentId must be non-empty
2. **Guard clauses** — `Guard.Against.NullOrWhiteSpace` at handler entry
3. **Grounding** — TenantId is explicitly included in grounding context
4. **Response** — TenantId echoed back for audit trail
5. **Logging** — TenantId+EnvironmentId+CorrelationId in every log entry
6. **Intra-tenant comparison** — Grounding text: "Both environments belong to the same tenant"

## Non-Production Analysis Readiness

The AI is prepared for:
- DEV → `development` profile
- QA → `qa` profile
- UAT → `uat` profile
- HML → `hml` profile
- STAGING → `staging` profile

The `EnvironmentProfile` is passed explicitly in the command (not inferred from name) — the backend is the source of truth.

## Frontend AI Surface

The `AiAnalysisPage` provides:
- Context indicator: "Analyzing: {environment}"
- Non-production badge for non-prod environments
- "Run Analysis" not rendered for production-like environments
- No-context guard when tenant/environment not selected
- 3-tab UI: Non-Prod Risk / Compare / Readiness

## Remaining Gaps

1. Real LLM integration (Phase 9)
2. Persistent AI execution audit log (Phase 9)
3. Real environment list from API (Phase 9)
4. E2E tests covering full flow (Phase 9)

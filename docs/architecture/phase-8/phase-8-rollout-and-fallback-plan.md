# Phase 8 — Rollout and Fallback Plan

## Rollout Strategy

NexTraceOne follows an **incremental, environment-scoped rollout** strategy aligned with its multi-tenant architecture.

## Pre-Rollout Checklist

### Infrastructure
- [ ] PostgreSQL databases provisioned (5: identity, operationalintelligence, changegovernance, governance, catalog)
- [ ] AI databases provisioned (3: aigovernance, externalai, aiorchestration)
- [ ] EF migrations applied via `WebApplicationExtensions.cs` pipeline
- [ ] Redis/correlation storage available for distributed context

### Configuration
- [ ] `X-Tenant-Id` header whitelist configured in CORS
- [ ] `X-Environment-Id` header whitelist configured in CORS
- [ ] AI provider config present (even if using fallback provider)
- [ ] JWT signing key configured

### Validation
- [ ] Backend: `dotnet build NexTraceOne.sln` — 0 errors
- [ ] Backend tests: all pass except pre-existing failures
- [ ] Frontend: `npx vitest run` — 360+ pass
- [ ] `GET /api/v1/identity/context/runtime` returns valid context
- [ ] `POST /api/v1/aiorchestration/analysis/non-prod` returns 200 with fallback response

## Recommended Rollout Order

### Stage 1 — Identity & Foundation
1. IdentityAccess module (tenant, user, environment management)
2. BuildingBlocks (context, observability, security)
3. Verify: `POST /api/v1/identity/auth/login` works

### Stage 2 — Operational Modules
4. OperationalIntelligence module (incidents, correlation)
5. ChangeGovernance module (releases, blast radius)
6. Verify: `POST /api/v1/incidents` and `POST /api/v1/releases` work

### Stage 3 — AI Capabilities
7. AIKnowledge module (governance, orchestration, external AI)
8. Verify AI analysis endpoints with fallback provider
9. Verify context isolation (TenantId in response)

### Stage 4 — Frontend
10. Deploy frontend build
11. Verify WorkspaceSwitcher shows correct environments
12. Verify EnvironmentBanner appears for non-prod
13. Verify AiAnalysisPage renders with correct context

## Monitoring During Rollout

### Signals to Watch
- `nexttrace.tenant_id` tag in traces (must never be null post-Stage 1)
- `nexttrace.environment_id` tag in traces (must never be null post-Stage 2)
- AI endpoint response time (< 5s for fallback, < 30s for real LLM)
- AI `IsFallback` field (should be `false` when real provider is configured)
- HTTP 401/403 rate (should not spike — indicates auth regression)
- Correlation ID presence in AI responses (audit trail)

### Metrics
- Requests with valid TenantId: target 100%
- AI analysis with valid context: target 100%
- Safe failures (error returned, no exception leak): target 100%

## Rollback Criteria

Trigger rollback if:
1. Any AI response leaks TenantId from another tenant
2. EnvironmentId missing in AI response (traceability broken)
3. HTTP 500 rate exceeds 1% on AI endpoints
4. Authentication failures exceed 0.1%
5. Frontend shows wrong environment context

## Rollback Procedure

1. Revert frontend to previous build
2. Backend: redeploy previous container image
3. Verify database migrations are backward compatible (additive only)
4. Monitor for 15 minutes after rollback

## Fallback Mode

If real AI provider is unavailable, the system degrades gracefully:
- `IsFallback: true` in AI response
- `[FALLBACK_PROVIDER_UNAVAILABLE]` prefix in `RawAnalysis`
- UI should show "AI unavailable - fallback mode" message (Phase 9 enhancement)
- `CorrelationId` still generated for audit

## Known Operational Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Real LLM not configured | HIGH | MEDIUM | Fallback mode works; configure before GA |
| Environment list API not implemented | HIGH | LOW | Mock loader works; implement in Phase 9 |
| AI audit trail not persistent | MEDIUM | LOW | Correlation IDs in logs; persist in Phase 9 |
| E2E tests missing | MEDIUM | LOW | Unit+integration tests cover critical paths |

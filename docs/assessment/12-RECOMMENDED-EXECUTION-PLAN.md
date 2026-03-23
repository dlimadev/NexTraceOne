# 12 — Recommended Execution Plan

> **Nota:** Este documento é um plano de execução original. Referências a Grafana dashboard provisioning refletem a stack planeada inicialmente. A stack de observabilidade foi migrada para provider configurável (ClickHouse ou Elastic). Ver `docs/audits/NEXTRACEONE-UPDATED-WAVES-PLAN.md` para o plano oficial atualizado.

**Date:** 2026-03-22

---

## Principles

1. **Do not break what works.** Every phase must maintain backward compatibility.
2. **Fix security first.** Security gaps addressed before functional completion.
3. **Recover hidden features incrementally.** Validate each excluded feature before re-enabling.
4. **Test as you go.** Each phase includes testing requirements.
5. **Smallest safe changes.** Prefer incremental improvements over rewrites.

---

## Phase 0: Stabilization & Diagnostic Confirmation (1 sprint)

**Objective:** Confirm assessment findings, establish baseline, prepare for execution.

### Tasks:
- [ ] Review all 13 assessment reports with team
- [ ] Verify backend build + all tests pass (current baseline)
- [ ] Verify frontend build + all tests pass (current baseline)
- [ ] Run integration tests to confirm DB connectivity patterns
- [ ] Confirm outbox processor limitation (GAP-001) with test
- [ ] Tag current state as `v0.9.0-assessment-baseline`

### Acceptance Criteria:
- Assessment findings confirmed or corrected
- Build/test baseline documented
- Team aligned on execution plan

---

## Phase 1: Critical Security & Infrastructure Fixes (1 sprint)

**Objective:** Close security gaps and infrastructure risks.

### Tasks:
- [ ] **GAP-009:** Add rate limiting middleware for auth endpoints (login, register, refresh, forgot-password)
- [ ] **GAP-010:** Standardize AI TenantId to `Guid` across all entities; verify global query filter coverage; create migration
- [ ] **GAP-004:** Add unit tests for `BuildingBlocks.Security` (JWT validation, permission requirements, cookie session, encryption, tenant resolution) — target >80% coverage
- [ ] **GAP-001:** Extend OutboxProcessorJob to process all DbContexts (or create per-database processors for catalog, operations, AI databases)
- [ ] Verify all endpoints have appropriate authorization (audit for any missed `RequirePermission`)
- [ ] Verify CORS configuration for production restrictiveness

### Acceptance Criteria:
- Rate limiting on auth endpoints functional
- AI TenantId standardized with migration
- Security building blocks tested (>80% coverage)
- Outbox processor dispatches events from ALL modules
- Security audit clean

### Dependencies:
- None (Phase 0 complete)

---

## Phase 2: Test Coverage & Quality Hardening (1 sprint)

**Objective:** Close critical test gaps before making functional changes.

### Tasks:
- [ ] **GAP-005:** Add unit tests for all 7 AuditCompliance features
- [ ] **GAP-012:** Add unit tests for Governance module (pack management, team management, compliance checks, FinOps queries) — target >100 tests
- [ ] **GAP-014:** Add frontend tests for top 20 untested pages (prioritize production-scope pages)
- [ ] Run full test suite, document any pre-existing failures, establish green baseline

### Acceptance Criteria:
- AuditCompliance >90% feature test coverage
- Governance >70% feature test coverage (>100 tests)
- Frontend test coverage >70% of production-scope pages
- Full test suite green (or pre-existing failures documented)

### Dependencies:
- Phase 1 (outbox fix may affect event-driven tests)

---

## Phase 3: Governance & Backend Functional Completion (1 sprint)

**Objective:** Close backend TODOs and complete governance features.

### Tasks:
- [ ] **GAP-008:** Resolve all 4 Governance application TODOs:
  - Implement scope counting for GovernancePack
  - Implement real scope count in ListGovernancePacks
  - Add LastProcessedAt field to IngestionSource
  - Implement cross-team contract/dependency enrichment for GetTeamDetail
- [ ] **GAP-006:** Expand AuditCompliance domain model:
  - Add CompliancePolicy entity
  - Add ComplianceResult entity
  - Add AuditCampaign entity
  - Add corresponding features (CRUD + query)
  - Create migration
- [ ] **GAP-015:** Implement OIDC provider configuration (at minimum Azure AD support)
- [ ] **GAP-016:** Verify and complete all IProductStore implementations (topology, anomaly, metrics writers/readers)

### Acceptance Criteria:
- All Governance TODOs resolved
- AuditCompliance has complete domain model with 4+ entities
- OIDC login functional with at least one provider
- IProductStore implementations wired and tested

### Dependencies:
- Phase 2 (tests must be in place before changes)

---

## Phase 4: Recover Excluded Features — AI Governance (1 sprint)

**Objective:** Validate and re-enable AI governance features in production scope.

### Tasks:
- [ ] Validate AI Model Registry (backend + frontend + persistence) — fix any issues
- [ ] Validate AI Policies (backend + frontend + persistence) — fix any issues
- [ ] Validate AI Routing (backend + frontend + persistence) — fix any issues
- [ ] Validate AI Token Budget (backend + frontend + persistence) — fix any issues
- [ ] Validate AI Audit (backend + frontend + persistence) — fix any issues
- [ ] Validate AI IDE Integrations (backend + frontend) — fix any issues
- [ ] Remove `/ai/models`, `/ai/policies`, `/ai/routing`, `/ai/ide`, `/ai/budgets`, `/ai/audit` from `finalProductionExcludedRoutePrefixes`
- [ ] Add tests for all re-enabled features
- [ ] Update sidebar navigation to remove `preview` badges for validated AI features

### Acceptance Criteria:
- All 6 AI features validated end-to-end
- Exclusions removed from `releaseScope.ts`
- Preview badges removed
- Tests added for each feature

### Dependencies:
- Phase 1 (TenantId fix), Phase 2 (test infrastructure)

---

## Phase 5: Recover Excluded Features — Governance & Operations (1 sprint)

**Objective:** Validate and re-enable governance, operations, and integration features.

### Tasks:
- [ ] Validate Governance Teams (backend + frontend) — fix any issues
- [ ] Validate Governance Packs (backend + frontend, scopes implemented in Phase 3) — fix any issues
- [ ] Validate Operations Runbooks (backend + frontend) — fix any issues
- [ ] Validate Team Reliability (backend + frontend) — fix any issues
- [ ] Validate Operations Automation (backend + frontend) — fix **GAP-011** stub page
- [ ] Validate Ingestion Executions (backend + frontend) — fix any issues
- [ ] Validate Analytics Value Tracking (backend + frontend) — fix any issues
- [ ] Validate Developer Portal (backend + frontend) — fix any issues
- [ ] Remove all validated routes from `finalProductionExcludedRoutePrefixes`
- [ ] Add tests for all re-enabled features

### Acceptance Criteria:
- All 8 excluded feature areas validated and included in production scope
- `finalProductionExcludedRoutePrefixes` empty or minimal
- Automation workflow detail page functional (not stub)
- Tests added for each feature

### Dependencies:
- Phase 3 (governance TODOs resolved), Phase 4 (AI features)

---

## Phase 6: FinOps & Data Pipeline (1 sprint)

**Objective:** Connect FinOps pages to real data, remove DemoBanner.

### Tasks:
- [ ] **GAP-003:** Implement cost data ingestion pipeline:
  - Define cost data source integration (cloud billing APIs or manual import)
  - Connect CostIntelligence backend to real data sources
  - Wire FinOps frontend pages to CostIntelligence endpoints
- [ ] Remove `DemoBanner` from all 6 pages:
  - ExecutiveDrillDownPage
  - ServiceFinOpsPage
  - BenchmarkingPage
  - FinOpsPage
  - TeamFinOpsPage
  - DomainFinOpsPage
- [ ] Add tests for FinOps data flow
- [ ] Verify data accuracy and rendering

### Acceptance Criteria:
- All 6 FinOps pages display real data (no DemoBanner)
- Cost data ingestion pipeline functional
- Tests verify data flow end-to-end

### Dependencies:
- Phase 5 (routes included)

---

## Phase 7: Observability, CLI & Operational Completeness (1 sprint)

**Objective:** Complete observability pipeline, CLI tooling, and operational features.

### Tasks:
- [ ] **GAP-007:** Implement CLI commands (at minimum `nex validate` and `nex catalog`)
- [ ] **GAP-017:** Implement alerting gateway (webhook + email at minimum)
- [ ] Verify OTLP integration (traces reaching Tempo, logs reaching Loki)
- [ ] Verify Grafana dashboard provisioning
- [ ] Verify metrics aggregation pipeline (1m/1h/1d partitions)
- [ ] **GAP-019:** Create backup/restore strategy and scripts
- [ ] **GAP-013:** Create production deployment pipeline with approval gate
- [ ] Add rollback automation to production pipeline

### Acceptance Criteria:
- CLI has at least 2 functional commands
- Alerting sends notifications via webhook and email
- OTLP pipeline verified end-to-end
- Backup/restore strategy documented and scripts tested
- Production deploy pipeline with approval gate exists

### Dependencies:
- Phase 6 (data pipelines stable)

---

## Phase 8: Testing, Hardening & Polish (1 sprint)

**Objective:** Comprehensive testing, UX polish, documentation.

### Tasks:
- [ ] **GAP-018:** Fix VisualRestBuilder hardcoded placeholders (i18n)
- [ ] **GAP-014:** Complete frontend test coverage to >80% of pages
- [ ] **GAP-020:** Create user-facing documentation (getting started, core workflows)
- [ ] Run full E2E test suite against complete product
- [ ] Run security scan pipeline (CodeQL, Trivy, npm audit, NuGet audit)
- [ ] Performance testing: verify response times under load
- [ ] Verify locale completeness across all 4 languages
- [ ] Fix any remaining bugs found during testing
- [ ] Update all assessment reports with final status

### Acceptance Criteria:
- All frontend tests pass (>80% page coverage)
- E2E suite green
- Security scans clean
- User documentation available
- Locales complete

### Dependencies:
- Phase 7 (all features complete)

---

## Phase 9: Release Readiness & Go-Live (1 sprint)

**Objective:** Final verification and production release.

### Tasks:
- [ ] Full regression test suite (backend + frontend + integration + E2E)
- [ ] Security penetration testing (manual or automated)
- [ ] Verify production configuration (env vars, secrets, CORS, HSTS)
- [ ] Deploy to staging, run smoke tests
- [ ] Verify health checks, monitoring, alerting in staging
- [ ] Final go-live checklist verification (`docs/checklists/GO-LIVE-CHECKLIST.md`)
- [ ] Production deployment via approved pipeline
- [ ] Post-deployment verification
- [ ] Tag release `v1.0.0`

### Acceptance Criteria:
- All tests pass
- Security review complete
- Staging deployment verified
- Production deployment successful
- All monitoring and alerting functional
- Product 100% complete

### Dependencies:
- Phase 8 (all hardening complete)

---

## Timeline Summary

| Phase | Duration | Focus | Key GAPs Addressed |
|-------|----------|-------|-------------------|
| Phase 0 | 1 sprint | Stabilization & baseline | Confirm findings |
| Phase 1 | 1 sprint | Security & infrastructure | GAP-001, 004, 009, 010 |
| Phase 2 | 1 sprint | Test coverage | GAP-005, 012, 014 |
| Phase 3 | 1 sprint | Backend completion | GAP-006, 008, 015, 016 |
| Phase 4 | 1 sprint | Recover AI features | GAP-002 (partial) |
| Phase 5 | 1 sprint | Recover Gov/Ops features | GAP-002 (complete), 011 |
| Phase 6 | 1 sprint | FinOps data pipeline | GAP-003 |
| Phase 7 | 1 sprint | Observability/CLI/Ops | GAP-007, 013, 017, 019 |
| Phase 8 | 1 sprint | Testing & polish | GAP-014, 018, 020 |
| Phase 9 | 1 sprint | Release readiness | Final verification |
| **Total** | **10 sprints** | | **20 GAPs closed** |

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Feature recovery breaks existing functionality | Phase 2 establishes comprehensive test baseline before changes |
| Database migrations during feature phases | All migration changes in Phase 1 (TenantId) and Phase 3 (AuditCompliance); no migration changes in Phases 4-5 |
| FinOps data source unavailable | Phase 6 can start with manual/import-based data while API integration develops |
| Timeline slippage | Phases 7-8 are lower priority and can be deferred without blocking core product delivery |
| Cross-module event failures after outbox fix | Phase 1 includes integration tests for outbox processor |

---

## Success Criteria

The NexTraceOne platform is considered **100% complete and production-ready** when:

1. ✅ All 14 previously excluded route prefixes are included in production scope
2. ✅ All 6 DemoBanner pages display real data
3. ✅ Outbox processor dispatches events from all modules
4. ✅ All test suites pass with >80% coverage
5. ✅ Security scan clean
6. ✅ Rate limiting on auth endpoints
7. ✅ CLI has functional commands
8. ✅ Production deploy pipeline with approval and rollback
9. ✅ Backup/restore strategy verified
10. ✅ User documentation available
11. ✅ OIDC SSO functional
12. ✅ All observability pipelines verified end-to-end

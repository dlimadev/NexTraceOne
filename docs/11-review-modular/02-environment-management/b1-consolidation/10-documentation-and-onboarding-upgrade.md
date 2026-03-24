# Environment Management Module — Documentation and Onboarding Upgrade

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Documentation State

### 1.1 Existing Documents

| # | Document | Location | Status | Filled? |
|---|----------|----------|--------|---------|
| 1 | `README.md` | `docs/11-review-modular/02-environment-management/` | Template | ❌ Mostly `[A PREENCHER]` |
| 2 | `module-consolidated-review.md` | Same directory | Partially filled | ⚠️ ~35% maturity assessment present |
| 3 | `module-overview.md` | Same directory | Template | ❌ All `[A PREENCHER]` |
| 4 | `backend/endpoints.md` | `backend/` | Template | ❌ No endpoints documented |
| 5 | `backend/authorization-rules.md` | `backend/` | Template | ❌ No rules documented |
| 6 | `database/schema-review.md` | `database/` | Template | ❌ No schema documented |
| 7 | `database/migrations-review.md` | `database/` | Template | ❌ Not reviewed |
| 8 | `database/seed-data-review.md` | `database/` | Template | ❌ Not reviewed |
| 9 | `documentation/code-comments-review.md` | `documentation/` | Template | ❌ Not reviewed |
| 10 | `documentation/developer-onboarding-notes.md` | `documentation/` | Template | ❌ Not started |
| 11 | `quality/technical-debt.md` | `quality/` | Template | ❌ No debt identified |
| 12 | `quality/bugs-and-gaps.md` | `quality/` | Template | ❌ No issues catalogued |
| 13 | `quality/acceptance-checklist.md` | `quality/` | Template | ❌ Not started |
| 14 | `quality/test-scenarios.md` | `quality/` | Template | ❌ Not started |

### 1.2 Architecture-Level Documentation

| # | Document | Location | Status |
|---|----------|----------|--------|
| 1 | `ADR-001-tenant-environment-context-refactor.md` | `docs/architecture/phase-0/` | ✅ Filled — covers tenant-environment context design |
| 2 | `environment-production-designation.md` | `docs/architecture/environments/` | ✅ Filled — covers primary production logic |
| 3 | `environment-control-audit.md` | `docs/architecture/environments/` | ✅ Filled — covers audit strategy |
| 4 | `environment-management-design.md` | `docs/architecture/environments/` | ✅ Filled — covers module design vision |
| 5 | `environment-control-transition-notes.md` | `docs/architecture/environments/` | ✅ Filled — covers migration strategy |
| 6 | `non-prod-to-prod-risk-analysis.md` | `docs/architecture/environments/` | ✅ Filled — covers promotion risks |

**Assessment:** Architecture-level documentation is mature. Module-level documentation is empty templates.

---

## 2. Documentation Gaps Analysis

### 2.1 Critical Gaps (Block Development)

| # | Gap | Impact | Priority |
|---|-----|--------|----------|
| 1 | No module README with developer setup | New developers cannot onboard to the module | **CRITICAL** |
| 2 | No endpoint documentation | Cannot build frontend against backend API | **CRITICAL** |
| 3 | No domain model documentation | Cannot understand entity relationships and invariants | **HIGH** |
| 4 | No permission model documentation | Cannot implement correct authorization | **HIGH** |

### 2.2 Important Gaps (Slow Development)

| # | Gap | Impact | Priority |
|---|-----|--------|----------|
| 5 | No database schema documentation | Migration planning requires schema reverse-engineering | **HIGH** |
| 6 | No test scenario documentation | QA and test automation lack requirements | **MEDIUM** |
| 7 | No code comments on entities | Business rules hidden in implementation | **MEDIUM** |
| 8 | No acceptance criteria documented | Definition of done unclear for each feature | **MEDIUM** |

### 2.3 Nice-to-Have Gaps

| # | Gap | Impact | Priority |
|---|-----|--------|----------|
| 9 | No architecture decision records for module extraction | Decision rationale not captured | **LOW** |
| 10 | No operational runbook for environment management | Incident response unclear | **LOW** |
| 11 | No FinOps context documentation | Cost implications not tracked | **LOW** |

---

## 3. Minimum Documentation Requirements

### 3.1 For Module Extraction (Phase 1)

Before starting module extraction, the following documents MUST be completed:

| # | Document | Audience | Content |
|---|----------|----------|---------|
| 1 | **Module README** | All developers | Module purpose, structure, setup, dependencies, contribution guidelines |
| 2 | **Domain Model Guide** | Backend developers | Entity descriptions, relationships, invariants, aggregate boundaries |
| 3 | **API Contract Documentation** | Frontend + backend | All endpoints with request/response schemas, permissions, error codes |
| 4 | **Database Schema Documentation** | Backend + DBA | Table definitions, indices, migration strategy, data types |
| 5 | **Permission Model Documentation** | Full stack + security | All permissions, role mappings, persona access matrix |

### 3.2 For Feature Development (Phase 2)

Before starting new features (promotion paths, baselines, drift):

| # | Document | Audience | Content |
|---|----------|----------|---------|
| 6 | **Feature Design Documents** | All developers | Per-feature design with domain model, API contract, UI mockup references |
| 7 | **Test Scenario Matrix** | QA + developers | Happy path, edge cases, error scenarios per feature |
| 8 | **Integration Guide** | Cross-team | How other modules interact with Environment Management |

### 3.3 For Production Readiness (Phase 3)

| # | Document | Audience | Content |
|---|----------|----------|---------|
| 9 | **Operational Runbook** | SRE + Platform Admin | Common operations, troubleshooting, incident response |
| 10 | **Monitoring & Alerting Guide** | SRE | What to monitor, alert thresholds, dashboard descriptions |

---

## 4. Document Templates and Standards

### 4.1 Module README Template

```markdown
# Environment Management Module

## Purpose
[One-paragraph description of what this module does]

## Module Structure
[Directory tree with descriptions]

## Key Concepts
[Domain terminology and definitions]

## Getting Started
[How to set up, run, and test the module locally]

## API Endpoints
[Quick reference table with links to detailed docs]

## Permissions
[Quick reference table of all permissions]

## Dependencies
[What this module depends on and what depends on it]

## Contributing
[Guidelines for contributing to this module]
```

### 4.2 Endpoint Documentation Standard

Each endpoint MUST document:
- HTTP method and route
- Required permissions
- Request parameters (path, query, body) with types and validation rules
- Response schema with examples
- Error codes with descriptions
- Audit events emitted
- Frontend pages/actions that consume it
- Related endpoints

### 4.3 Entity Documentation Standard

Each entity MUST document:
- XML doc comments on the class
- XML doc comments on each property
- Business invariants as comments
- Domain events emitted
- Aggregate boundary (root vs child vs standalone)

### 4.4 Code Comment Standards

| What | Where | Example |
|------|-------|---------|
| Class purpose | XML doc on class | `/// <summary>Represents a runtime environment within a tenant.</summary>` |
| Property meaning | XML doc on property | `/// <summary>Whether this environment mirrors production characteristics (e.g., Staging, DR).</summary>` |
| Business rule | Comment before validation | `// Only Production-profile environments can be designated as primary production` |
| Non-obvious logic | Inline comment | `// Slug uniqueness is enforced per-tenant via unique index` |
| TODO/HACK | Inline with ticket ref | `// TODO(NXT-1234): Extract to dedicated module` |

---

## 5. Onboarding Plan for New Developers

### 5.1 Onboarding Path

| Step | Duration | Activity | Resources |
|------|----------|----------|-----------|
| 1 | 30 min | Read Module README | `docs/11-review-modular/02-environment-management/README.md` |
| 2 | 30 min | Read architecture design docs | `docs/architecture/environments/` |
| 3 | 1 hour | Explore domain model | Entity files + domain model guide |
| 4 | 30 min | Explore API endpoints | Endpoint documentation + Swagger/OpenAPI |
| 5 | 30 min | Explore frontend pages | Navigate EnvironmentsPage in dev environment |
| 6 | 30 min | Run tests | Execute module unit and integration tests |
| 7 | 1 hour | Make a small change | Add a validation rule or i18n key |

**Total estimated onboarding time: ~4.5 hours**

### 5.2 Key Questions for New Developers

A new developer joining the module should be able to answer:

1. What environments exist in a tenant and how are they classified?
2. What is a promotion path and how does it relate to Change Governance?
3. Why is this module being extracted from Identity & Access?
4. What permissions are needed to create/modify environments?
5. What happens when an environment is deactivated?
6. How does drift detection work?
7. Where is the EnvironmentContext used across the frontend?
8. How does multi-tenancy work in this module?

### 5.3 Common Pitfalls

| # | Pitfall | Why It Happens | How to Avoid |
|---|--------|---------------|-------------|
| 1 | Editing environment code in IdentityAccess | Historical location | Always work in `src/modules/environmentmanagement/` (after extraction) |
| 2 | Using `identity:users:read` permission | Legacy permission name | Use `env:environments:read` |
| 3 | Adding environment frontend to identity-access feature | Historical location | Use `features/environment-management/` |
| 4 | Creating physical FK across DbContexts | Cross-module reference | Use strongly-typed ID references only |
| 5 | Not filtering by TenantId | Multi-tenancy oversight | Always include TenantId in queries |
| 6 | Using `DateTime.Now` | Codebase convention | Use `DateTimeOffset.UtcNow` via time provider |
| 7 | Hardcoding UI strings | i18n requirement | Always use translation keys |

---

## 6. Documentation Remediation Plan

### 6.1 Immediate Actions (This Sprint)

| # | Action | Owner | Effort | Output |
|---|--------|-------|--------|--------|
| 1 | Fill `README.md` with actual module content | Module lead | 2h | Complete README |
| 2 | Fill `module-overview.md` with real data | Module lead | 2h | Complete overview |
| 3 | Document existing 6 endpoints in `endpoints.md` | Backend dev | 3h | Endpoint reference |
| 4 | Document current schema in `schema-review.md` | Backend dev | 2h | Schema reference |
| 5 | Document permissions in `authorization-rules.md` | Security | 2h | Permission matrix |

### 6.2 Short-Term Actions (Next Sprint)

| # | Action | Owner | Effort | Output |
|---|--------|-------|--------|--------|
| 6 | Add XML doc comments to all entities | Backend dev | 3h | Documented entities |
| 7 | Create test scenarios in `test-scenarios.md` | QA | 3h | Test matrix |
| 8 | Fill `technical-debt.md` with identified issues | Module lead | 2h | Debt inventory |
| 9 | Fill `bugs-and-gaps.md` with known issues | QA | 2h | Issue inventory |
| 10 | Create integration guide for consuming modules | Module lead | 3h | Integration doc |

### 6.3 Medium-Term Actions (Following Sprints)

| # | Action | Owner | Effort | Output |
|---|--------|-------|--------|--------|
| 11 | Create ADR for module extraction decision | Architect | 2h | ADR document |
| 12 | Create feature design docs for new features | Backend dev | 4h each | Feature specs |
| 13 | Create operational runbook | SRE | 3h | Runbook |
| 14 | Create acceptance checklist in `acceptance-checklist.md` | Product | 2h | Checklist |

---

## 7. Cross-Reference with Existing Architecture Docs

### 7.1 Documents to Reference from Module Docs

| Architecture Doc | Relevant Section | Link from Module Docs |
|-----------------|-----------------|----------------------|
| `ADR-001-tenant-environment-context-refactor.md` | Tenant-environment context resolution | Reference in domain model guide |
| `environment-production-designation.md` | Primary production logic | Reference in endpoint docs for SetPrimaryProduction |
| `environment-control-audit.md` | Audit requirements | Reference in security review |
| `environment-management-design.md` | Overall module design vision | Reference in README |
| `non-prod-to-prod-risk-analysis.md` | Promotion risks | Reference in promotion path feature design |

### 7.2 Architecture Docs That Need Updates

| Document | Update Needed | Priority |
|----------|-------------|----------|
| `environment-management-design.md` | Update to reflect module extraction plan | **HIGH** |
| `environment-control-transition-notes.md` | Add B1 consolidation status | **MEDIUM** |

---

## 8. Documentation Quality Metrics

### 8.1 Current State

| Metric | Current | Target | Gap |
|--------|---------|--------|-----|
| Documents filled (of 14 templates) | 1 partial | 14 | 13 documents |
| Entities with XML docs | 0 of 5 | 5 | 5 entities |
| Endpoints documented | 0 of 6 | 6 (then 21) | 6 endpoints |
| i18n keys documented | 0 | All | ~30+ keys |
| Permissions documented | 0 of 3 | 8 (new model) | 8 permissions |
| Onboarding time (estimated) | Unknown | < 5h | Need to measure |
| Code comment coverage | Unknown | > 80% | Need to assess |

### 8.2 Definition of Done for Documentation

A module's documentation is considered complete when:

- [ ] README is filled with all sections
- [ ] Module overview is complete with real data
- [ ] All endpoints are documented with request/response schemas
- [ ] All permissions are documented with role mappings
- [ ] Database schema is documented with all tables and indices
- [ ] All entities have XML doc comments
- [ ] Test scenarios cover happy path and error cases
- [ ] Technical debt is catalogued
- [ ] Known bugs and gaps are tracked
- [ ] Integration guide exists for consuming modules
- [ ] A new developer can onboard in < 5 hours

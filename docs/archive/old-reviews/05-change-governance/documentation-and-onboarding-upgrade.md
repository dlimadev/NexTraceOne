# Change Governance — Documentation and Onboarding Upgrade

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Current Documentation State

### 1.1 Existing Documentation

| Document | Location | Content Quality |
|----------|----------|----------------|
| `module-review.md` | `docs/11-review-modular/05-change-governance/` | ✅ Good — 153 lines covering purpose, pages, entities, endpoints, integrations |
| `module-consolidated-review.md` | `docs/11-review-modular/05-change-governance/` | ✅ Good — 302 lines with 81% maturity score, detailed gap analysis |
| Architecture boundary matrix | `docs/architecture/module-boundary-matrix.md` | ✅ Good — Change Governance section with clear boundary status |
| Table prefix definition | `docs/architecture/database-table-prefixes.md` | ✅ Good — `chg_` prefix documented |

### 1.2 Missing Documentation

| Document | Expected Location | Status |
|----------|------------------|--------|
| Module README.md | `src/modules/changegovernance/README.md` | ❌ Missing |
| API documentation (request/response/error) | `docs/api/change-governance/` | ❌ Missing |
| End-to-end flow diagram | `docs/11-review-modular/05-change-governance/` | ❌ Missing |
| Domain model diagram | `docs/11-review-modular/05-change-governance/` | ❌ Missing |
| Permission matrix documentation | Inline in module docs | ⚠️ Partial — exists in consolidated review but not standalone |
| XML docs on domain entities | `src/modules/changegovernance/.../Domain/` | ⚠️ Partial — some entities have docs, many don't |
| XML docs on handlers | `src/modules/changegovernance/.../Application/` | ⚠️ Partial |

---

## 2. Documentation Gaps by Area

### 2.1 Code-Level Documentation

| Area | Files Affected | Gap Description |
|------|---------------|-----------------|
| Domain entities | 27 entity classes | Missing XML summary docs explaining business rules, invariants, and state transitions |
| Enums | 13+ enum files | Missing XML docs on each value's meaning and usage context |
| Handlers | 40+ handler classes | Missing XML docs on command/query purpose, expected inputs, side effects |
| Repositories | 27 repository classes | Missing XML docs on query patterns and performance characteristics |
| Entity configurations | 27 configuration classes | Missing comments on index rationale and constraint purposes |

### 2.2 API Documentation

| Gap | Description |
|-----|-------------|
| No Swagger/OpenAPI annotations | Endpoints lack `[ProducesResponseType]`, `[SwaggerOperation]` attributes |
| No request/response examples | 46+ endpoints without documented examples |
| No error response documentation | No documented error codes, messages, or recovery guidance |
| No rate limiting documentation | No documented limits on analysis endpoints |

### 2.3 Architecture Documentation

| Gap | Description |
|-----|-------------|
| No subdomain interaction diagram | How ChangeIntelligence, Workflow, Promotion, RulesetGovernance interact internally |
| No event flow diagram | How outbox events flow to OI, Audit, Notifications |
| No state machine diagrams | Release, WorkflowInstance, PromotionRequest state transitions not visualised |
| No deployment/infrastructure notes | How the 4 DbContexts map to database schemas |

---

## 3. Onboarding Guide — What a New Developer Needs

### 3.1 Module README.md (Proposed Structure)

```markdown
# Change Governance Module

## Purpose
Central module for production change confidence: risk scoring, blast radius,
approval workflows, promotion gates, and ruleset governance.

## Architecture
- 4 subdomains: ChangeIntelligence, Workflow, Promotion, RulesetGovernance
- 4 DbContexts with Outbox pattern
- 27 domain entities, 5 aggregate roots
- 46+ API endpoints

## Getting Started
1. Prerequisites (PostgreSQL, API host running)
2. How to run migrations
3. How to seed test data
4. Key configuration values

## Key Flows
- Release → Score → Blast Radius → Review → Decision
- Workflow → Stages → Approval → Evidence Pack
- Promotion → Gates → Evaluation → Override/Approve

## Permissions
- change-intelligence:read/write
- workflow:read/write, workflow:templates:read/write
- promotion:read/write, promotion:admin:write
- rulesets:read/write

## Dependencies
- Service Catalog (blast radius)
- Environment Management (env context)
- Contracts (ruleset linting)
- Identity & Access (auth)

## Testing
- 179+ tests
- How to run unit tests
- How to run integration tests
```

### 3.2 Key Flows Documentation

| Flow | Documentation Need |
|------|-------------------|
| Release lifecycle | Sequence diagram: notification → classification → scoring → blast radius → review → decision |
| Approval workflow | Sequence diagram: initiation → stage progression → approval/rejection → evidence pack |
| Promotion governance | Sequence diagram: request → gate evaluation → pass/fail → override → approval |
| Ruleset linting | Sequence diagram: upload → binding → execution → findings → scoring |

### 3.3 Onboarding Checklist

A new developer joining the Change Governance team should:

1. Read the module README.md (to be created)
2. Understand the 4 subdomains and their boundaries
3. Review the domain model (27 entities, 5 aggregates)
4. Trace the release lifecycle flow end-to-end
5. Understand the permission model (10+ permission scopes)
6. Run the test suite (179+ tests)
7. Create a test release via the API
8. Approve a workflow instance
9. Execute a promotion with gates
10. Upload and execute a ruleset

---

## 4. Documentation Correction Backlog

| ID | Item | Area | Priority | Effort |
|----|------|------|----------|--------|
| D-01 | Create `src/modules/changegovernance/README.md` | Onboarding | P0 | 4h |
| D-02 | Add XML summary docs to all 27 domain entities | Code docs | P1 | 8h |
| D-03 | Add XML summary docs to all 40+ handlers | Code docs | P1 | 8h |
| D-04 | Create API documentation with request/response examples | API docs | P1 | 16h |
| D-05 | Create end-to-end flow diagrams (PlantUML/Mermaid) | Architecture | P1 | 8h |
| D-06 | Create state machine diagrams for Release, Workflow, Promotion | Architecture | P2 | 4h |
| D-07 | Create subdomain interaction diagram | Architecture | P2 | 2h |
| D-08 | Create event flow diagram (outbox → consumers) | Architecture | P2 | 2h |
| D-09 | Add Swagger annotations to all endpoints | API docs | P2 | 8h |
| D-10 | Add XML docs to all 13+ enums | Code docs | P3 | 2h |

**Total estimated effort:** ~62 hours

# ADR-004: Functional Honesty — Simulated Data Policy

**Status:** Accepted  
**Date:** 2026-03-21  
**Context:** Many handlers return fabricated/demo data. The product must be transparent.

## Decision

### Backend Markers

| Situation | Marker | Location |
|-----------|--------|----------|
| Handler returns fully fabricated data | `IsSimulated = true, DataSource = "demo"` | Response DTO default params |
| Handler returns partial real + placeholder fields | `DeferredFields = [...]` | Response DTO property |
| Interface has no implementation | `// IMPLEMENTATION STATUS: Planned` | Source file header |
| Endpoint records metadata but doesn't process payload | `processingStatus: "metadata_recorded"` | Response body |

### Frontend Markers

| Situation | Component | Behavior |
|-----------|-----------|----------|
| Page consumes a SIM handler | `<DemoBanner />` | Warning banner via i18n (4 locales) |
| Module not yet functional | Preview badge | Gated in release scope |

### Release Scope (ZR-6)

`releaseScope.ts` defines which routes are included/excluded from the production scope.
Routes for PLAN/SIM/PREV features are excluded:
- `/portal`, `/governance/teams`, `/governance/packs`
- `/ai/models`, `/ai/policies`, `/ai/routing`, `/ai/ide`, `/ai/budgets`, `/ai/audit`
- `/operations/runbooks`, `/operations/reliability`, `/operations/automation`
- `/integrations/executions`, `/analytics/value`

### Transition Protocol

When a feature moves from SIM → IMPL:
1. Remove `IsSimulated`/`DeferredFields` from the Response DTO
2. Remove `<DemoBanner />` from the frontend page
3. Update `IMPLEMENTATION-STATUS.md` matrix
4. Remove route from `finalProductionExcludedRoutePrefixes`
5. Add/update tests for real data paths

## Consequences

- No demo data can be mistaken for real data by any stakeholder
- The product is honest about what works and what doesn't
- Clear upgrade path documented for each simulated feature

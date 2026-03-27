# Configuration Phase 3 — Workflows, Approvals & Promotion Governance

## Objective

Phase 3 of the NexTraceOne configuration parameterization delivers governable, auditable, multi-tenant configuration for **approval workflows**, **promotion governance**, **SLAs**, **gates**, **escalation policies**, **release windows** and **freeze policies**.

This phase externalizes workflow and promotion rules from hardcoded constants into the configuration platform built in Phases 0–2, making the behavior of NexTraceOne's change governance flows **administrable by product configuration**.

## Scope Delivered

### 45 Configuration Definitions across 7 domains:

| Domain | Count | Sort Range | Key Prefix |
|--------|-------|------------|------------|
| Workflow Types & Templates | 4 | 2000–2030 | `workflow.types.*`, `workflow.templates.*` |
| Stages, Sequencing & Quorum | 5 | 2100–2140 | `workflow.stages.*`, `workflow.quorum.*` |
| Approvers, Fallback & Escalation | 7 | 2200–2260 | `workflow.approvers.*`, `workflow.escalation.*` |
| SLA, Deadlines, Timeout & Expiry | 8 | 2300–2370 | `workflow.sla.*`, `workflow.timeout.*`, `workflow.expiry.*`, `workflow.resubmission.*` |
| Gates, Checklists & Auto-Approval | 10 | 2400–2490 | `workflow.gates.*`, `workflow.checklist.*`, `workflow.auto_approval.*`, `workflow.evidence_pack.*`, `workflow.rejection.*` |
| Promotion Governance | 5 | 2500–2540 | `promotion.paths.*`, `promotion.production.*`, `promotion.restrictions.*`, `promotion.rollback.*` |
| Release Windows & Freeze Policies | 6 | 2600–2650 | `promotion.release_window.*`, `promotion.freeze.*` |

### Backend

- 45 new `ConfigurationDefinition` entries in `ConfigurationDefinitionSeeder.cs`
- All definitions follow `Functional` category, `workflow.*` / `promotion.*` key prefixes
- Proper scope constraints (System, Tenant, Environment where appropriate)
- Non-inheritable flags for critical system-only settings (auto-approval blocked environments, freeze override controls)
- JSON validation rules for complex structured data
- Default values aligned with enterprise governance best practices

### Frontend

- `WorkflowConfigurationPage.tsx` — full admin UI at `/platform/configuration/workflows`
- 7 section tabs: Types & Templates, Stages & Quorum, Approvers & Escalation, SLA/Timeout/Expiry, Gates/Checklists/Auto-Approval, Promotion Governance, Release Windows & Freeze
- Effective settings explorer per definition (shows resolved value, inheritance source, override/default badges)
- Inline editing with change reason tracking
- Audit history expansion per key
- Scope-aware filtering (System → Tenant → Environment)
- Full i18n support (en, pt-BR, pt-PT, es)

### Tests

- 31 new backend unit tests in `WorkflowPromotionConfigurationDefinitionsTests.cs`
- 15 new frontend tests in `WorkflowConfigurationPage.test.tsx`
- All 116 backend configuration tests pass
- All 15 frontend page tests pass

## Impact on Next Phases

Phase 3 prepares the product for:

- **Phase 4**: Governance, compliance, waivers and governance packs parameterization
- **Workflow consumers**: Services that resolve workflow templates, approver policies, SLAs and gates can now read from configuration instead of hardcoded values
- **Promotion engine**: Gate evaluation, environment-specific rules, and freeze window enforcement can reference parameterized configuration
- **Admin operations**: Platform administrators can tune workflow behavior per tenant and environment without code changes

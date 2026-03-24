# Configuration Phase 4 — Governance, Compliance, Waivers & Packs

## Objective

Phase 4 of the NexTraceOne configuration parameterization delivers governable, auditable, multi-tenant configuration for **governance policies**, **compliance profiles**, **evidence requirements**, **waiver lifecycle**, **governance packs**, **scorecards**, and **risk matrix**.

This phase externalizes governance and compliance rules from hardcoded constants into the configuration platform built in Phases 0–3, making the behavior of NexTraceOne's governance layer **administrable by product configuration**.

## Scope Delivered

### 44 Configuration Definitions across 6 domains:

| Domain | Count | Sort Range | Key Prefix |
|--------|-------|------------|------------|
| Policy Catalog & Compliance Profiles | 9 | 3000–3080 | `governance.policies.*`, `governance.compliance.*` |
| Evidence Requirements | 6 | 3100–3150 | `governance.evidence.*` |
| Waiver Rules | 10 | 3200–3290 | `governance.waiver.*` |
| Governance Packs & Bindings | 6 | 3300–3350 | `governance.packs.*` |
| Scorecards, Thresholds & Risk Matrix | 8 | 3400–3470 | `governance.scorecard.*`, `governance.risk.*` |
| Minimum Requirements by System/API Type | 5 | 3500–3540 | `governance.requirements.*` |

### Backend

- 44 new `ConfigurationDefinition` entries in `ConfigurationDefinitionSeeder.cs`
- All definitions follow `Functional` category, `governance.*` key prefix
- Proper scope constraints (System, Tenant, Environment where appropriate)
- Non-inheritable flags for critical system-only settings (waiver blocked severities, waiver blocked environments)
- JSON validation rules for complex structured data
- Enum validation for select-type definitions
- Default values aligned with enterprise governance best practices

### Frontend

- `GovernanceConfigurationPage.tsx` — full admin UI at `/platform/configuration/governance`
- 6 section tabs: Policies & Profiles, Evidence, Waivers, Packs & Bindings, Scorecards & Risk, Minimum Requirements
- Effective settings explorer per definition (shows resolved value, inheritance source, override/default badges)
- Inline editing with change reason tracking
- Audit history expansion per key
- Scope-aware filtering (System → Tenant → Environment)
- Full i18n support (en, pt-BR, pt-PT, es)

### Tests

- 31 new backend unit tests in `GovernanceComplianceConfigurationDefinitionsTests.cs`
- 14 new frontend tests in `GovernanceConfigurationPage.test.tsx`
- All 147 backend configuration tests pass
- All 14 frontend page tests pass

## Impact on Next Phases

Phase 4 prepares the product for:

- **Phase 5**: Catalog, contracts, APIs and change governance parameterization
- **Governance consumers**: Services that resolve policies, compliance profiles, evidence requirements and packs can now read from configuration instead of hardcoded values
- **Risk engine**: Risk matrix, scorecard thresholds and weights can be tuned per tenant/environment
- **Waiver engine**: Waiver eligibility, validity, approval requirements and renewal limits are now governable
- **Admin operations**: Platform administrators can tune governance behavior per tenant and environment without code changes

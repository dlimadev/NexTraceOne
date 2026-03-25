# Governance Module — Boundary Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## 1. What Is Governance in NexTraceOne

Governance in NexTraceOne means **organizational control, compliance, and oversight of services, teams, and operational practices.** It answers: "Are we following our own rules? Where are the gaps? What is the risk posture? How mature are our practices?"

Governance is NOT:
- Integration management (that's Integrations)
- Product usage analytics (that's Product Analytics)
- Platform health monitoring (that's Operational Intelligence)
- Change approval workflows (that's Change Governance)
- Security event auditing (that's Audit & Compliance)

---

## 2. What Is Policy/Compliance/Reporting in Governance

| Concept | Definition in NexTraceOne |
|---------|--------------------------|
| **Policy** | A defined rule that teams/services must comply with (e.g., "All APIs must have Spectral validation") |
| **Compliance** | The assessment of whether teams/services meet policy requirements |
| **Risk** | The measured exposure from non-compliance, gaps, or vulnerabilities |
| **Evidence** | Proof artifacts that demonstrate compliance (e.g., audit reports, test results) |
| **Waiver** | A formal exception to a policy with justification and expiration |
| **Control** | An enterprise-level control mechanism to enforce governance |
| **FinOps** | Cost governance — efficiency, waste detection, cost optimization by service/team/domain |
| **Report** | Generated governance summaries for executive/auditor consumption |
| **Maturity** | Scorecard measuring organizational governance maturity |

---

## 3. What Stays Inside Governance (Final)

| Responsibility | Entities | Endpoints | Pages |
|---------------|----------|-----------|-------|
| **Teams & Domains** | Team, TeamDomainLink, GovernanceDomain | TeamEndpointModule, DomainEndpointModule | TeamsOverview, TeamDetail, DomainsOverview, DomainDetail |
| **Governance Packs** | GovernancePack, GovernancePackVersion, GovernanceRolloutRecord, GovernanceRuleBinding | GovernancePacksEndpointModule | GovernancePacksOverview, GovernancePackDetail |
| **Waivers** | GovernanceWaiver | GovernanceWaiversEndpointModule | WaiversPage |
| **Delegated Admin** | DelegatedAdministration | DelegatedAdminEndpointModule | DelegatedAdminPage |
| **Policies** | (read-model/computed) | PolicyCatalogEndpointModule | PolicyCatalogPage |
| **Compliance** | (read-model/computed) | ComplianceChecksEndpointModule, GovernanceComplianceEndpointModule | CompliancePage |
| **Risk** | (read-model/computed) | GovernanceRiskEndpointModule | RiskCenterPage, RiskHeatmapPage |
| **FinOps** | (read-model/computed) | GovernanceFinOpsEndpointModule | FinOpsPage, ServiceFinOps, TeamFinOps, DomainFinOps |
| **Evidence** | (read-model/computed) | EvidencePackagesEndpointModule | EvidencePackagesPage |
| **Controls** | (read-model/computed) | EnterpriseControlsEndpointModule | EnterpriseControlsPage |
| **Executive** | (read-model/computed) | ExecutiveOverviewEndpointModule | ExecutiveOverview, ExecutiveDrillDown, ExecutiveFinOps |
| **Reports** | (read-model/computed) | GovernanceReportsEndpointModule | ReportsPage |
| **Maturity** | (read-model/computed) | (via executive) | MaturityScorecardsPage |
| **Benchmarking** | (read-model/computed) | (via executive) | BenchmarkingPage |
| **Scoped Context** | (read-model) | ScopedContextEndpointModule | (internal use) |
| **Configuration** | (admin) | (via Configuration module) | GovernanceConfigurationPage |

---

## 4. What Goes to Integrations

| Component | Current Location | Target Location |
|-----------|-----------------|----------------|
| `IntegrationConnector` entity | `Governance.Domain/Entities/` | `Integrations.Domain/Entities/` |
| `IngestionSource` entity | `Governance.Domain/Entities/` | `Integrations.Domain/Entities/` |
| `IngestionExecution` entity | `Governance.Domain/Entities/` | `Integrations.Domain/Entities/` |
| `IntegrationHubEndpointModule` | `Governance.API/Endpoints/` | `Integrations.API/Endpoints/` |
| 8 CQRS handlers | `Governance.Application/Features/` | `Integrations.Application/Features/` |
| 3 repositories | `Governance.Application/Abstractions/` | `Integrations.Application/Abstractions/` |
| 3 EF configurations | `Governance.Infrastructure/Persistence/Configurations/` | `Integrations.Infrastructure/Persistence/Configurations/` |
| 6 enums | `Governance.Domain/Enums/` | `Integrations.Domain/Enums/` |
| 3 DbSets | `GovernanceDbContext` | → `IntegrationsDbContext` |
| 3 tables | `gov_integration_connectors`, `gov_ingestion_sources`, `gov_ingestion_executions` | → `int_*` prefix |

**Frontend:** Already separated (`features/integrations/`).

---

## 5. What Goes to Product Analytics

| Component | Current Location | Target Location |
|-----------|-----------------|----------------|
| `AnalyticsEvent` entity | `Governance.Domain/Entities/` | `ProductAnalytics.Domain/Entities/` |
| `ProductAnalyticsEndpointModule` | `Governance.API/Endpoints/` | `ProductAnalytics.API/Endpoints/` |
| 7 CQRS handlers | `Governance.Application/Features/` | `ProductAnalytics.Application/Features/` |
| 1 repository | `Governance.Application/Abstractions/` | `ProductAnalytics.Application/Abstractions/` |
| 1 EF configuration | `Governance.Infrastructure/Persistence/Configurations/` | `ProductAnalytics.Infrastructure/Persistence/Configurations/` |
| 6 enums | `Governance.Domain/Enums/` | `ProductAnalytics.Domain/Enums/` |
| 1 DbSet | `GovernanceDbContext` | → `ProductAnalyticsDbContext` |
| 1 table | `gov_analytics_events` | → `pan_analytics_events` |

**Frontend:** Already separated (`features/product-analytics/`).

---

## 6. What Is Just a Dependency of Audit & Compliance

| Responsibility | How Governance Relates | Who Owns It |
|---------------|----------------------|-------------|
| Security event logging | Governance may query security events for risk reports | Audit & Compliance |
| Audit trail of governance actions | Governance actions are audited | Audit & Compliance |
| Compliance certification | Governance produces compliance evidence | Audit & Compliance stores/certifies |

Governance produces evidence and compliance data; Audit & Compliance stores, certifies, and provides the audit trail.

---

## 7. What Is Just a Dependency of Change Governance

| Responsibility | How Governance Relates | Who Owns It |
|---------------|----------------------|-------------|
| Change approval workflows | Governance packs may define required approvals | Change Governance |
| Promotion validation | Governance policies may gate promotions | Change Governance |
| Release management | Executive views may show release data | Change Governance |

Governance defines policies and rules; Change Governance enforces them in change workflows.

---

## 8. What Must Never Enter Governance Again

| Category | Examples | Correct Module |
|----------|---------|---------------|
| External system connectors | DataDog, PagerDuty, SonarQube connectors | Integrations |
| Usage analytics events | Module adoption, persona usage, journey funnels | Product Analytics |
| Platform health monitoring | Platform jobs, queues, subsystem status | Operational Intelligence |
| User onboarding flows | Feature tours, setup wizards | Platform / Identity |
| Service catalog data | Service metadata, dependencies | Catalog |
| Change workflows | Release pipelines, promotion steps | Change Governance |
| Environment management | Environment creation, comparison | Environment Management |
| Contract validation | Spectral rules, contract governance | Contracts |

---

## 9. Concrete Examples

### Example 1: "Is team X compliant with governance packs?"
- **Governance:** Evaluates compliance check against packs assigned to team X → returns compliance score
- **NOT Governance:** The service metadata itself (Catalog), the change history (Change Governance)

### Example 2: "Show FinOps for domain Y"
- **Governance:** Aggregates cost data by domain → returns efficiency metrics, waste signals
- **NOT Governance:** The actual cost data source (Operational Intelligence / external systems)

### Example 3: "List integration connectors"
- **NOT Governance:** This is connector management → belongs to Integrations
- **Governance:** May report on integration health as part of risk assessment

### Example 4: "Record a product analytics event"
- **NOT Governance:** This is usage tracking → belongs to Product Analytics
- **Governance:** May consume analytics data for executive reports

### Example 5: "Create a waiver for policy violation"
- **Governance:** Creates waiver with justification, expiration, and approval workflow
- **Audit & Compliance:** Records the waiver creation as an auditable event

---

## 10. Boundary Verdict

| Aspect | Status |
|--------|--------|
| Module scope after extraction | **CLEAN** — 9 entities, 16 endpoint modules, 25 pages |
| Entities to extract | **4** (3 to Integrations, 1 to Product Analytics) |
| Endpoints to extract | **2** (IntegrationHub, ProductAnalytics) |
| Frontend already clean | ✅ (Integrations and ProductAnalytics already separate) |
| Remaining responsibilities | **COHERENT** — all focused on governance, compliance, risk, FinOps |
| Permission model | **NEEDS FIX** — frontend too broad, backend appropriately granular |

**Conclusion:** Governance will be a well-scoped module once Integrations and Product Analytics entities/endpoints are extracted. The remaining 25 pages and 16 endpoint modules form a coherent governance bounded context.

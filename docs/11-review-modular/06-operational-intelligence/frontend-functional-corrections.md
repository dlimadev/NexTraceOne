# Operational Intelligence — Frontend Functional Corrections (Part 8)

> **Status:** DRAFT  
> **Date:** 2026-03-25  
> **Module:** 06 — Operational Intelligence  
> **Phase:** B1 — Module Consolidation  
> **Source:** Code analysis of `src/frontend/src/features/operations/`

---

## 1. Page Inventory (10 Pages)

All pages located in `src/frontend/src/features/operations/pages/`.

| # | Page | File | Size | Route | Permission | Menu |
|---|---|---|---|---|---|---|
| 1 | IncidentsPage | `IncidentsPage.tsx` | 21.6 KB | `/operations/incidents` | `operations:incidents:read` | ✅ Sidebar |
| 2 | IncidentDetailPage | `IncidentDetailPage.tsx` | 25.4 KB | `/operations/incidents/:incidentId` | `operations:incidents:read` | Navigation |
| 3 | RunbooksPage | `RunbooksPage.tsx` | 5.5 KB | `/operations/runbooks` | `operations:incidents:read` ⚠️ | ✅ Sidebar |
| 4 | TeamReliabilityPage | `TeamReliabilityPage.tsx` | 7.3 KB | `/operations/reliability` | `operations:reliability:read` | ✅ Sidebar |
| 5 | ServiceReliabilityDetailPage | `ServiceReliabilityDetailPage.tsx` | 12.5 KB | `/operations/reliability/:serviceId` | `operations:reliability:read` | Navigation |
| 6 | AutomationWorkflowsPage | `AutomationWorkflowsPage.tsx` | 9.4 KB | `/operations/automation` | `operations:automation:read` | ✅ Sidebar |
| 7 | AutomationWorkflowDetailPage | `AutomationWorkflowDetailPage.tsx` | 14.3 KB | `/operations/automation/:workflowId` | `operations:automation:read` | Navigation |
| 8 | AutomationAdminPage | `AutomationAdminPage.tsx` | 9.1 KB | `/operations/automation/admin` | `operations:automation:read` | Via admin |
| 9 | EnvironmentComparisonPage | `EnvironmentComparisonPage.tsx` | 27.7 KB | `/operations/runtime-comparison` | `operations:runtime:read` | ✅ Sidebar |
| 10 | PlatformOperationsPage | `PlatformOperationsPage.tsx` | 20.1 KB | `/platform/operations` | `platform:admin:read` | ✅ Admin sidebar |

**Note:** Page #10 (PlatformOperationsPage) is routed under `/platform/operations` not `/operations/`, and uses `platform:admin:read` permission — it belongs to the admin section.

---

## 2. Route Review

### 2.1 Route Definitions

All routes defined in `src/App.tsx` (lines 349–419) using `<Route>` + `<ProtectedRoute>` pattern.

| Route | Component | Lazy Loaded | ProtectedRoute | Status |
|---|---|---|---|---|
| `/operations/incidents` | `IncidentsPage` | ✅ | `operations:incidents:read` | ✅ Correct |
| `/operations/incidents/:incidentId` | `IncidentDetailPage` | ✅ | `operations:incidents:read` | ✅ Correct |
| `/operations/runbooks` | `RunbooksPage` | ✅ | `operations:incidents:read` | ⚠️ Should be `operations:runbooks:read` |
| `/operations/reliability` | `TeamReliabilityPage` | ✅ | `operations:reliability:read` | ✅ Correct |
| `/operations/reliability/:serviceId` | `ServiceReliabilityDetailPage` | ✅ | `operations:reliability:read` | ✅ Correct |
| `/operations/automation` | `AutomationWorkflowsPage` | ✅ | `operations:automation:read` | ✅ Correct |
| `/operations/automation/admin` | `AutomationAdminPage` | ✅ | `operations:automation:read` | ⚠️ Should require `operations:automation:admin` or higher |
| `/operations/automation/:workflowId` | `AutomationWorkflowDetailPage` | ✅ | `operations:automation:read` | ✅ Correct |
| `/operations/runtime-comparison` | `EnvironmentComparisonPage` | ✅ | `operations:runtime:read` | ✅ Correct |
| `/platform/operations` | `PlatformOperationsPage` | ✅ | `platform:admin:read` | ✅ Correct |

### 2.2 Route Issues

| # | Issue | Severity |
|---|---|---|
| 1 | **RunbooksPage route permission:** Uses `operations:incidents:read` instead of `operations:runbooks:read`. Users with runbook access but not incident access cannot view runbooks. | 🟠 Medium |
| 2 | **AutomationAdminPage permission:** Uses `operations:automation:read` — same as non-admin pages. Admin functionality (action catalog management) should require elevated permission. | 🟡 Low |
| 3 | **Missing cost routes:** No dedicated `/operations/cost` route. Cost functionality only accessible via `EnvironmentComparisonPage` (partial) and PlatformOperationsPage. | 🟡 Low |

---

## 3. Menu Review

### 3.1 Sidebar Configuration

**File:** `src/components/shell/AppSidebar.tsx` (lines 51–55)

| # | Label Key | Route | Icon | Permission | Section |
|---|---|---|---|---|---|
| 1 | `sidebar.incidents` | `/operations/incidents` | AlertTriangle | `operations:incidents:read` | operations |
| 2 | `sidebar.runbooks` | `/operations/runbooks` | FileCode | `operations:runbooks:read` | operations |
| 3 | `sidebar.reliability` | `/operations/reliability` | Activity | `operations:reliability:read` | operations |
| 4 | `sidebar.automation` | `/operations/automation` | Zap | `operations:automation:read` | operations |
| 5 | `sidebar.environmentComparison` | `/operations/runtime-comparison` | BarChart3 | `operations:runtime:read` | operations |

**Section label:** `sidebar.sectionOperations` → "Operations"

### 3.2 Menu Issues

| # | Issue | Severity |
|---|---|---|
| 1 | **Sidebar Runbooks permission vs route permission mismatch:** Sidebar uses `operations:runbooks:read` but route ProtectedRoute uses `operations:incidents:read`. A user with `operations:runbooks:read` but not `operations:incidents:read` will see the menu item but get access denied on the route. | 🟠 Medium |
| 2 | **No Cost menu entry:** Cost Intelligence has 9 backend endpoints but no dedicated sidebar entry. Users must navigate indirectly. | 🟡 Low |
| 3 | **No PlatformOperations in operations section:** PlatformOperationsPage is in admin section, which is correct but worth noting for completeness. | 🟢 Info |

---

## 4. Dashboard & Panel Review

### 4.1 IncidentsPage — Summary Dashboard

**Location:** `IncidentsPage.tsx` (header section)  
**Data source:** `getIncidentSummary()` → `['incidents-summary']` query key

| Panel | Content | Status |
|---|---|---|
| Open Incidents (total) | Count of open incidents | ✅ Functional |
| Critical | Count of critical severity incidents | ✅ Functional |
| With Correlated Changes | Incidents with change correlation | ✅ Functional |
| Mitigation Available | Incidents with active mitigation | ✅ Functional |
| Services Impacted | Count of impacted services | ✅ Functional |

**Assessment:** ✅ Complete summary dashboard with real-time data via React Query.

### 4.2 TeamReliabilityPage — Reliability Overview

**Location:** `TeamReliabilityPage.tsx`  
**Data source:** `listServices()` → `['reliability-services', filter, search]` with `staleTime: 30_000`

| Panel | Content | Status |
|---|---|---|
| Total Services | Count of all services | ✅ Functional |
| Healthy | Count with Healthy status | ✅ Functional |
| Degraded | Count with Degraded status | ✅ Functional |
| Needs Attention | Count requiring attention | ✅ Functional |
| Service list with health indicators | Filterable list with status badges | ✅ Functional |

**Assessment:** ✅ Complete reliability overview with health status filtering.

### 4.3 EnvironmentComparisonPage — Runtime Health

**Location:** `EnvironmentComparisonPage.tsx` (27.7 KB — largest operations page)  
**Data sources:** Multiple queries with conditional enablement

| Panel | Content | Query Key | Status |
|---|---|---|---|
| Runtime Comparison (before/after) | Latency, error rate, throughput delta | `['runtime-compare', submitted]` | ✅ Functional |
| Drift Findings | Detected metric drifts | `['runtime-drift', service, env]` | ✅ Functional |
| Observability Score | Score with grade breakdown | `['observability-score', service, env]` | ✅ Functional |
| Health Timeline | Time-series health points | `['runtime-timeline', service, env, window]` | ✅ Functional |

**Assessment:** ✅ Comprehensive environment comparison with multiple coordinated queries.

### 4.4 PlatformOperationsPage — Platform Health

**Location:** `PlatformOperationsPage.tsx`  
**Data sources:** Independent queries with `staleTime: 15_000`

| Tab/Panel | Content | Query Key | Status |
|---|---|---|---|
| Platform Health | System health summary | `['platform-health']` | ✅ Functional |
| Background Jobs | Job list with status filter | `['platform-jobs', jobFilter]` | ✅ Functional |
| Message Queues | Queue status overview | `['platform-queues']` | ✅ Functional |
| Platform Events | Event log with severity filter | `['platform-events', severityFilter]` | ✅ Functional |

**Assessment:** ✅ Complete platform monitoring dashboard with tab-based navigation.

---

## 5. Forms & Configuration Review

### 5.1 IncidentsPage — Incident Creation Form

**Location:** `IncidentsPage.tsx` (lines 239–298)

| Field | Placeholder Key | Validation | Status |
|---|---|---|---|
| Title | `incidents.create.titlePlaceholder` | Required | ✅ |
| Service ID | `incidents.create.serviceIdPlaceholder` | Required | ✅ |
| Service Name | `incidents.create.serviceNamePlaceholder` | Required | ✅ |
| Owner Team | `incidents.create.ownerTeamPlaceholder` | Required | ✅ |
| Environment | `incidents.create.environmentPlaceholder` | Required | ✅ |
| Domain | `incidents.create.domainPlaceholder` | Optional | ✅ |
| Description | `incidents.create.descriptionPlaceholder` | Optional | ✅ |

**Submit button:** Disabled when `!isCreateFormValid || createIncidentMutation.isPending` with `disabled:opacity-60`.

**Assessment:** ✅ Functional form with proper validation state and loading feedback.

### 5.2 AutomationAdminPage — Action Catalog & Audit

**Location:** `AutomationAdminPage.tsx`

| Section | Content | Status |
|---|---|---|
| Action Catalog | Read-only list of available automation actions | ✅ Functional (read-only) |
| Audit Trail | Filterable audit log | ✅ Functional |
| Action CRUD | Create/edit/delete actions | ❌ Missing |

**Assessment:** ⚠️ Admin page is read-only. No forms for managing the action catalog.

### 5.3 EnvironmentComparisonPage — Comparison Form

**Location:** `EnvironmentComparisonPage.tsx` (lines 70–100)

| Field | Content | Status |
|---|---|---|
| Service Name | Text input with placeholder | ✅ |
| Environment | Text input | ✅ |
| Before Period (Start/End) | Date inputs | ✅ |
| After Period (Start/End) | Date inputs | ✅ |
| Compare button | Triggers query submission | ✅ |

**Assessment:** ✅ Complete comparison form with conditional query enablement.

### 5.4 FinOps Configuration (Embryonic)

**Location:** `src/frontend/src/features/operational-intelligence/` (separate feature folder)  
**Route:** `/platform/configuration/operations-finops`

| Status | Notes |
|---|---|
| ⚠️ Embryonic | Separate feature folder from main operations module. Configuration page exists at admin route but functionality is minimal. No corresponding backend endpoints for ServiceCostProfile management. |

---

## 6. Listings & Filters Review

### 6.1 IncidentsPage — Incident List

| Feature | Implementation | Status |
|---|---|---|
| Status filter | Dropdown: All, Open, Investigating, Mitigating, Monitoring, Resolved, Closed | ✅ |
| Search | Text input with debounce | ✅ |
| Pagination | Page/PageSize with previous/next buttons | ✅ |
| Severity badges | Color-coded severity indicators | ✅ |
| Empty state | i18n: `incidents.emptyTitle` / `incidents.emptyDescription` | ✅ |

### 6.2 AutomationWorkflowsPage — Workflow List

| Feature | Implementation | Status |
|---|---|---|
| Status filter | Dropdown: Created, PendingApproval, Approved, Executing, Completed, Failed, Cancelled, Rejected | ✅ |
| Search | Text input with placeholder | ✅ |
| Empty state | i18n: `automation.emptyTitle` / `automation.emptyDescription` | ✅ |

### 6.3 TeamReliabilityPage — Service List

| Feature | Implementation | Status |
|---|---|---|
| Health filter | Dropdown: All, Healthy, Degraded, Unavailable, NeedsAttention | ✅ |
| Search | Text input with placeholder | ✅ |
| Score indicators | Overall reliability score display | ✅ |
| Navigate to detail | Click to `/operations/reliability/:serviceId` | ✅ |

### 6.4 RunbooksPage — Runbook List

| Feature | Implementation | Status |
|---|---|---|
| Search | Text input with placeholder | ✅ |
| Stats summary | Total count, linked services, avg steps | ✅ |
| Empty state | i18n: `runbooks.emptyTitle` / `runbooks.emptyDescription` | ✅ |
| Filtering by service/incident type | ❌ Available in API but not exposed in UI | ⚠️ Gap |

**Assessment:** All listings are functional with filters and empty states. Runbook page could expose additional filters.

---

## 7. API Integration Review

### 7.1 API Client Files

All API clients located in `src/frontend/src/features/operations/api/`.

| # | File | Size | Endpoints Covered | Status |
|---|---|---|---|---|
| 1 | `incidents.ts` | 477 lines | Incidents (10), Mitigation (7), Runbooks (2) — 19 total | ✅ Complete |
| 2 | `automation.ts` | 140 lines | Automation actions (2), workflows (3), audit (1) — 6 total | ✅ Complete |
| 3 | `reliability.ts` | 29 lines | Service list (1), detail (1), team summary (1) — 3 total | ⚠️ Partial — missing trend, coverage, team trend, domain summary |
| 4 | `runtimeIntelligence.ts` | 112 lines | Compare (1), drift findings (1), timeline (1), observability (1) — 4 total | ⚠️ Partial — missing snapshots ingestion, health, drift detect, assess |
| 5 | `platformOps.ts` | 26 lines | Health (1), jobs (1), queues (1), events (1), config (1) — 5 total | ✅ Complete |

**Total covered by frontend:** 37 of 58 backend endpoints (64%)

### 7.2 API Integration Gaps

| # | Backend Endpoint | API Client | Frontend Usage | Gap |
|---|---|---|---|---|
| 1 | `GET /reliability/services/{id}/trend` | ❌ Not in `reliability.ts` | ServiceReliabilityDetailPage may use inline fetch | 🟡 Should be in API client |
| 2 | `GET /reliability/services/{id}/coverage` | ❌ Not in `reliability.ts` | ServiceReliabilityDetailPage may use inline fetch | 🟡 Should be in API client |
| 3 | `GET /reliability/teams/{id}/trend` | ❌ Not in `reliability.ts` | Not used in UI | 🟢 API-only endpoint |
| 4 | `GET /reliability/domains/{id}/summary` | ❌ Not in `reliability.ts` | Not used in UI | 🟢 API-only endpoint |
| 5 | `POST /runtime/snapshots` | ❌ Not in `runtimeIntelligence.ts` | Ingestion — not needed in frontend | 🟢 Backend-only |
| 6 | `GET /runtime/health` | ❌ Not in `runtimeIntelligence.ts` | Not exposed in UI | 🟡 Should be available |
| 7 | `POST /runtime/drift/detect` | ❌ Not in `runtimeIntelligence.ts` | Not exposed in UI | 🟡 Consider triggerable from UI |
| 8 | `POST /runtime/observability/assess` | ❌ Not in `runtimeIntelligence.ts` | Not exposed in UI | 🟡 Consider triggerable from UI |
| 9 | `POST /cost/*` (all write endpoints) | ❌ No cost API client | No cost management UI | 🟠 Missing cost management frontend |
| 10 | `GET /cost/*` (all read endpoints) | ❌ No cost API client | No cost viewing UI | 🟠 Missing cost viewing frontend |
| 11 | Automation write endpoints (workflow actions) | ⚠️ Partially in `automation.ts` | AutomationWorkflowDetailPage handles inline | 🟡 Should be in API client |

---

## 8. i18n Review

### 8.1 Namespace Coverage

**File:** `src/frontend/src/locales/en.json`

| Namespace | Keys Found | Status |
|---|---|---|
| `sidebar.*` | `incidents`, `runbooks`, `reliability`, `automation`, `environmentComparison`, `platformOperations`, `sectionOperations` | ✅ Complete |
| `incidents.*` | title, subtitle, emptyTitle, emptyDescription, totalOpen, critical, withCorrelation, withMitigation, servicesImpacted, searchPlaceholder, filter.*, detail.*, create.* | ✅ Complete |
| `runbooks.*` | title, subtitle, emptyTitle, emptyDescription, searchPlaceholder | ✅ Complete |
| `reliability.*` | title, subtitle, loadError, totalServices, healthyServices, degradedServices, needsAttention, searchPlaceholder, filter.* | ✅ Complete |
| `automation.*` | title, subtitle, emptyTitle, emptyDescription, searchPlaceholder, catalog.*, workflows.* | ✅ Complete |
| `environmentComparison.*` | _(check for dedicated namespace)_ | ⚠️ Needs verification |
| `platformOperations.*` | _(check for dedicated namespace)_ | ⚠️ Needs verification |

### 8.2 i18n Gaps

| # | Gap | Location | Severity |
|---|---|---|---|
| 1 | **EnvironmentComparisonPage:** Verify all labels use i18n — page is 27.7 KB with form fields, metric labels, and comparison panels that may contain hardcoded English | 🟡 Medium |
| 2 | **PlatformOperationsPage:** Verify all tab labels, status labels, and event severity labels use i18n | 🟡 Medium |
| 3 | **IncidentDetailPage:** 25.4 KB page with multiple sections — verify all section headers and labels use `t()` | 🟡 Medium |
| 4 | **AutomationWorkflowDetailPage:** Workflow state labels, precondition messages, and step descriptions need i18n verification | 🟡 Medium |
| 5 | **No `pt.json` or other locale files verified** — only `en.json` confirmed. Portuguese and other locales may have gaps. | 🟠 High |
| 6 | **Error messages from API:** Backend returns English error templates. Frontend should use `localizer` for user-facing error display. | 🟡 Medium |

### 8.3 Fallback Pattern

All i18n calls use the pattern `t('key', 'Fallback text')` — this means hardcoded English fallbacks exist inline. While this prevents missing-key rendering issues, it means:

- New locales automatically show English instead of blank text (✅ good)
- Hardcoded fallbacks may diverge from translation file values (⚠️ risk)
- Search for untranslated text requires checking both `en.json` and inline fallbacks (⚠️ maintenance burden)

---

## 9. Button & Placeholder Review

### 9.1 Disabled Buttons (Functional — Not Placeholders)

| Page | Button | Condition | Purpose |
|---|---|---|---|
| IncidentsPage | Create Incident submit | `!isCreateFormValid \|\| createIncidentMutation.isPending` | Prevent invalid/duplicate submission |
| IncidentsPage | Previous page | `!canGoToPreviousPage \|\| incidentsQuery.isFetching` | Pagination boundary |
| IncidentsPage | Next page | `!canGoToNextPage \|\| incidentsQuery.isFetching` | Pagination boundary |
| IncidentDetailPage | Refresh Correlation | `refreshCorrelationMutation.isPending` | Prevent concurrent refresh |

**Assessment:** ✅ All disabled states are functional (loading/validation) — no placeholder "coming soon" buttons found.

### 9.2 Placeholder Content

| Page | Content | Type | Status |
|---|---|---|---|
| IncidentsPage | "Incident tracking and correlation will be available here." | Empty state message | ✅ Appropriate |
| RunbooksPage | "Operational runbooks and mitigation playbooks will be managed here." | Empty state message | ✅ Appropriate |
| AutomationWorkflowsPage | "Operational automation workflows will appear here when created." | Empty state message | ✅ Appropriate |

**Assessment:** ✅ Empty states are appropriate (data-dependent, not feature-placeholder). No "Coming Soon" or "Under Construction" placeholders found.

### 9.3 Search Placeholders

All search inputs use i18n placeholders:
- `t('incidents.searchPlaceholder', 'Search incidents...')`
- `t('runbooks.searchPlaceholder', 'Search runbooks...')`
- `t('reliability.searchPlaceholder', 'Search services...')`
- `t('automation.searchPlaceholder', 'Search workflows...')`
- `t('environmentComparison.serviceNamePlaceholder', '...')`

---

## 10. Technical Field Exposure Review

| # | Page | Field | Risk | Status |
|---|---|---|---|---|
| 1 | IncidentDetailPage | `ExternalRef` | Displays external reference ID — may be internal system identifier | 🟢 Low — useful for cross-system correlation |
| 2 | IncidentDetailPage | JSON column data (timeline, correlations) | Rendered as structured UI — not raw JSON | ✅ Appropriate |
| 3 | AutomationWorkflowDetailPage | `ActionId`, `ServiceId`, `IncidentId`, `ChangeId` | Internal IDs displayed to admin users | 🟡 Low — admin context acceptable |
| 4 | EnvironmentComparisonPage | Metric values (latency_ms, error_rate %) | Technical metrics — appropriate for target persona (Engineer/Tech Lead) | ✅ Appropriate |
| 5 | PlatformOperationsPage | Job IDs, queue names, event subsystems | Technical infrastructure details — appropriate for Platform Admin persona | ✅ Appropriate |

**Assessment:** ✅ No inappropriate technical field exposure. All technical data is shown in persona-appropriate contexts.

---

## 11. Operational UX Clarity

### 11.1 User Flows

| Flow | Pages Involved | Steps | Assessment |
|---|---|---|---|
| **Incident investigation** | IncidentsPage → IncidentDetailPage | List → Filter → Select → View detail/correlation/evidence/mitigation | ✅ Clear |
| **Mitigation execution** | IncidentDetailPage (inline) | View recommendations → Create workflow → Execute steps → Validate | ✅ Clear |
| **Runbook lookup** | RunbooksPage | Search → View detail with steps | ✅ Clear |
| **Automation execution** | AutomationWorkflowsPage → AutomationWorkflowDetailPage | List → Create → Request approval → Approve → Execute → Validate | ✅ Clear |
| **Reliability monitoring** | TeamReliabilityPage → ServiceReliabilityDetailPage | Overview → Filter → Drill into service | ✅ Clear |
| **Environment comparison** | EnvironmentComparisonPage | Fill form → Compare → View drift/score/timeline | ✅ Clear |
| **Cost analysis** | _(no dedicated page)_ | ❌ No user flow | ❌ Missing |

### 11.2 UX Gaps

| # | Gap | Impact | Severity |
|---|---|---|---|
| 1 | **No dedicated Cost Intelligence page** | Users cannot view cost reports, trends, or anomalies through the UI despite 9 backend endpoints being available | 🟠 Medium |
| 2 | **No cross-navigation between incident and automation** | Incidents may reference automation workflows but no direct link from incident detail to automation workflow detail | 🟡 Low |
| 3 | **No AI Assistant integration on reliability pages** | IncidentDetailPage integrates `AssistantPanel` from AI Hub but reliability and automation pages do not | 🟡 Low |
| 4 | **No notification/toast feedback on mutations** | Incident creation, correlation refresh show loading state but no explicit success/error toast notification | 🟡 Low |
| 5 | **No breadcrumb navigation** | Detail pages lack breadcrumb trail (e.g., Operations > Incidents > INC-123) | 🟡 Low |

---

## 12. React Query Patterns Review

### 12.1 Query Configuration Summary

| Page | staleTime | Retry | Conditional | Mutation Pattern |
|---|---|---|---|---|
| IncidentsPage | Default | Default | No | Dual invalidate + refetch |
| IncidentDetailPage | Default | Default | No | Invalidate + cascade |
| RunbooksPage | Default | Default | No | N/A (read-only) |
| TeamReliabilityPage | 30s | Default | No | N/A (read-only) |
| ServiceReliabilityDetailPage | 30s | `false` | `enabled: !!serviceId` | N/A (read-only) |
| AutomationWorkflowsPage | Default | Default | No | N/A (read-only list) |
| AutomationWorkflowDetailPage | Default | Default | `enabled: !!workflowId` | N/A (read-only) |
| AutomationAdminPage | 15s | Default | No | N/A (read-only) |
| EnvironmentComparisonPage | 30s | Default | `enabled: !!submitted` | N/A (form-triggered) |
| PlatformOperationsPage | 15s | Default | No | N/A (read-only) |

### 12.2 React Query Concerns

| # | Concern | Details | Severity |
|---|---|---|---|
| 1 | **Inconsistent staleTime** | Some pages use default (0), others use 15s/30s. Operations data should have consistent cache behavior. | 🟡 Low |
| 2 | **Dual invalidate + refetch** | IncidentsPage does both `invalidateQueries` and `refetchQueries` on mutation success — redundant (invalidate triggers refetch automatically for active queries). | 🟡 Low |
| 3 | **No error boundary integration** | Query errors are handled per-component with inline error states, but no React Error Boundary wraps operations pages. | 🟡 Low |
| 4 | **Missing mutations in automation pages** | AutomationWorkflowDetailPage has approval/execution actions but mutations may be handled inline rather than through the API client. | 🟡 Low |

---

## 13. Frontend Correction Backlog

| # | Correction | Severity | Effort | Category |
|---|---|---|---|---|
| 1 | **Fix RunbooksPage route permission** — change from `operations:incidents:read` to `operations:runbooks:read` in `App.tsx` | 🟠 P2 | 15min | Security |
| 2 | **Create Cost Intelligence page** — dedicated page with cost report, trends, delta, anomaly views | 🟠 P2 | 2–3 days | Completeness |
| 3 | **Add cost API client** — `cost.ts` covering all 9 cost endpoints | 🟠 P2 | 4h | API Integration |
| 4 | **Add Cost sidebar menu entry** — under operations section | 🟠 P2 | 30min | Navigation |
| 5 | **Complete `reliability.ts` API client** — add trend, coverage, team trend, domain summary methods | 🟡 P3 | 2h | API Integration |
| 6 | **Complete `runtimeIntelligence.ts` API client** — add health, drift detect, assess methods | 🟡 P3 | 2h | API Integration |
| 7 | **Move automation write actions to `automation.ts` API client** — centralize workflow action methods | 🟡 P3 | 2h | Maintainability |
| 8 | **Verify i18n coverage on large pages** — EnvironmentComparisonPage (27.7 KB), PlatformOperationsPage (20.1 KB), IncidentDetailPage (25.4 KB) | 🟡 P3 | 4h | i18n |
| 9 | **Add RunbooksPage filter UI** — expose existing serviceId and incidentType API filters in the UI | 🟡 P3 | 2h | UX |
| 10 | **Standardize staleTime** across operations pages (recommend 30s for all operational data queries) | 🟡 P3 | 1h | Consistency |
| 11 | **Remove redundant dual invalidate+refetch** in IncidentsPage mutation handlers | 🟡 P3 | 30min | Code Quality |
| 12 | **Add breadcrumb navigation** to detail pages (IncidentDetail, ServiceReliabilityDetail, AutomationWorkflowDetail) | 🟡 P3 | 4h | UX |
| 13 | **Add AI Assistant integration** to ReliabilityDetailPage and AutomationWorkflowDetailPage | 🟡 P3 | 4h | AI Integration |
| 14 | **Verify Portuguese (pt.json) locale coverage** for all operations namespaces | 🟠 P2 | 4h | i18n |
| 15 | **Add toast/notification feedback** for mutation success/error states | 🟡 P3 | 2h | UX |
| 16 | **Elevate AutomationAdminPage permission** — consider `operations:automation:admin` or platform-level permission | 🟡 P3 | 1h | Security |

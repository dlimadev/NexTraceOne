# Audit & Compliance — Frontend Functional Corrections

> **Module:** 10 — Audit & Compliance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Page Inventory (1 Page)

| # | Page | File | Route | Permission | Status |
|---|------|------|-------|-----------|--------|
| 1 | AuditPage | `src/frontend/src/features/audit-compliance/pages/AuditPage.tsx` | `/audit` | `audit:read` | ✅ Working |

### AuditPage Features

| Feature | Status | Details |
|---------|--------|---------|
| Event listing table | ✅ Working | Columns: Event Type, Actor, Aggregate, Timestamp, Source Module |
| Pagination | ✅ Working | Previous/Next buttons with page display |
| Event type filter | ✅ Working | Text input filter for event type |
| Verify Integrity button | ✅ Working | Calls `auditApi.verifyIntegrity()`, shows success/failure banner |
| Report export | ⚠️ Partial | `auditApi.exportReport()` exists in API client but no visible UI trigger |

---

## 2. Sidebar Menu Items

**Source:** `src/frontend/src/components/shell/AppSidebar.tsx`

| Menu Item | Route | Permission | i18n Key | Section | Status |
|-----------|-------|-----------|----------|---------|--------|
| Audit | `/audit` | `audit:read` | `sidebar.audit` | admin | ✅ Working |

Only 1 sidebar item for the entire module. All compliance, campaign, and retention features are inaccessible via the UI.

---

## 3. Routing Configuration

**Source:** `src/frontend/src/App.tsx`

```typescript
const AuditPage = lazy(() =>
  import('./features/audit-compliance/pages/AuditPage')
    .then(m => ({ default: m.AuditPage }))
);

<Route path="/audit" element={<ProtectedRoute permission="audit:read"><AuditPage /></ProtectedRoute>} />
```

✅ Route is correctly mapped, imported, and protected.

---

## 4. API Client

**File:** `src/frontend/src/features/audit-compliance/api/audit.ts`

| Method | Backend Endpoint | Used By | Status |
|--------|-----------------|---------|--------|
| `listEvents(params)` | `GET /audit/search` | AuditPage | ✅ Working |
| `verifyIntegrity()` | `GET /audit/verify-chain` | AuditPage | ✅ Working |
| `exportReport(from, to)` | `GET /audit/report` | — | ⚠️ Exists but no UI trigger |

### Missing API Client Methods

| Method Needed | Backend Endpoint | Priority |
|--------------|-----------------|----------|
| `listCompliancePolicies(params)` | `GET /audit/compliance/policies` | P1 |
| `getCompliancePolicy(id)` | `GET /audit/compliance/policies/{id}` | P1 |
| `createCompliancePolicy(data)` | `POST /audit/compliance/policies` | P1 |
| `listComplianceResults(params)` | `GET /audit/compliance/results` | P1 |
| `recordComplianceResult(data)` | `POST /audit/compliance/results` | P2 |
| `listCampaigns(params)` | `GET /audit/campaigns` | P1 |
| `getCampaign(id)` | `GET /audit/campaigns/{id}` | P1 |
| `createCampaign(data)` | `POST /audit/campaigns` | P2 |
| `getComplianceReport(from, to)` | `GET /audit/compliance` | P1 |
| `getAuditTrail(resourceType, resourceId)` | `GET /audit/trail` | P2 |

---

## 5. Missing Frontend Pages

| # | Page Needed | Route | Features | Priority |
|---|------------|-------|----------|----------|
| 1 | CompliancePoliciesPage | `/audit/compliance/policies` | List policies, create policy, activate/deactivate, view details | P1 |
| 2 | ComplianceResultsPage | `/audit/compliance/results` | List results by policy/campaign/outcome, record result | P1 |
| 3 | AuditCampaignsPage | `/audit/campaigns` | List campaigns, create campaign, start/complete/cancel lifecycle | P2 |
| 4 | AuditTrailDetailPage | `/audit/trail/:resourceType/:resourceId` | Full audit trail for a specific resource | P2 |
| 5 | ComplianceReportPage | `/audit/compliance/report` | Period-based compliance report with module breakdown and chain status | P1 |
| 6 | RetentionConfigPage | `/audit/retention` | Configure retention policies, view active policies | P2 |

---

## 6. Identified Frontend Issues

| ID | Description | Page | Severity |
|----|-------------|------|----------|
| F-01 | Only 1 page for 15 backend endpoints — 60% of functionality inaccessible | All | 🔴 High |
| F-02 | No compliance policy management UI | — | 🔴 High |
| F-03 | No campaign management UI | — | 🟠 Medium |
| F-04 | No audit trail detail view (per-resource) | — | 🟠 Medium |
| F-05 | No compliance report UI | — | 🟠 Medium |
| F-06 | Export report button not visible in AuditPage | AuditPage | 🟡 Medium |
| F-07 | No date range filter on AuditPage (only event type) | AuditPage | 🟡 Medium |
| F-08 | No source module filter on AuditPage | AuditPage | 🟡 Medium |
| F-09 | No actor filter on AuditPage | AuditPage | 🟡 Medium |
| F-10 | Hash chain status not shown per event row | AuditPage | 🟢 Low |

---

## 7. i18n Coverage

| Namespace | en | pt-BR | pt-PT | es | Status |
|-----------|----|----|-------|----|----|
| `sidebar.audit` | ✅ | ⚠️ Check | ⚠️ Check | ⚠️ Check | Needs validation |
| `audit.*` (page labels) | ✅ Partial | ⚠️ | ⚠️ | ⚠️ | Only AuditPage keys present |
| `audit.compliance.*` | ❌ Missing | — | — | — | No compliance pages yet |
| `audit.campaigns.*` | ❌ Missing | — | — | — | No campaign pages yet |

---

## 8. Frontend Correction Backlog

| ID | Item | Area | Priority | Effort |
|----|------|------|----------|--------|
| FC-01 | Create CompliancePoliciesPage with CRUD | Pages | P1 | 16h |
| FC-02 | Create ComplianceResultsPage with listing/recording | Pages | P1 | 12h |
| FC-03 | Create ComplianceReportPage | Pages | P1 | 8h |
| FC-04 | Create AuditCampaignsPage with lifecycle | Pages | P2 | 12h |
| FC-05 | Create AuditTrailDetailPage | Pages | P2 | 8h |
| FC-06 | Create RetentionConfigPage | Pages | P2 | 8h |
| FC-07 | Add date range and module filters to AuditPage | AuditPage | P1 | 4h |
| FC-08 | Add export report button to AuditPage | AuditPage | P2 | 2h |
| FC-09 | Add API client methods for all backend endpoints | API | P1 | 4h |
| FC-10 | Add sidebar items for new pages | Navigation | P1 | 2h |
| FC-11 | Add i18n keys for all new pages across 4 locales | i18n | P2 | 8h |
| FC-12 | Add routes and lazy imports for new pages | Routing | P1 | 2h |

**Total estimated effort:** ~86 hours

The frontend is the **single largest gap** in this module. Backend is at 80% maturity; frontend at 40%. Closing this gap requires building 5–6 new pages.

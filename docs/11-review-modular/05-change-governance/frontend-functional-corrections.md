# Change Governance — Frontend Functional Corrections

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Page Inventory (6 Pages)

| # | Page | File | Route | Permission | Status |
|---|------|------|-------|-----------|--------|
| 1 | ChangeCatalogPage | `src/frontend/src/features/change-governance/pages/ChangeCatalogPage.tsx` | `/changes` | `change-intelligence:read` | ✅ Working |
| 2 | ChangeDetailPage | `src/frontend/src/features/change-governance/pages/ChangeDetailPage.tsx` | `/changes/:changeId` | `change-intelligence:read` | ✅ Working |
| 3 | ReleasesPage | `src/frontend/src/features/change-governance/pages/ReleasesPage.tsx` | `/releases` | `change-intelligence:releases:read` | ✅ Working |
| 4 | WorkflowPage | `src/frontend/src/features/change-governance/pages/WorkflowPage.tsx` | `/workflow` | `workflow:read` | ✅ Working |
| 5 | PromotionPage | `src/frontend/src/features/change-governance/pages/PromotionPage.tsx` | `/promotion` | `promotion:read` | ✅ Working |
| 6 | WorkflowConfigurationPage | `src/frontend/src/features/change-governance/pages/WorkflowConfigurationPage.tsx` | `/platform/configuration/workflows` | `platform:admin:read` | ✅ Working |

---

## 2. Sidebar Menu Items

**Source:** `src/frontend/src/components/shell/AppSidebar.tsx`

| Menu Item | Route | Permission | i18n Key | Status |
|-----------|-------|-----------|----------|--------|
| Change Confidence | `/changes` | `change-intelligence:read` | `sidebar.changeConfidence` | ✅ Working |
| Change Intelligence | `/releases` | `change-intelligence:releases:read` | `sidebar.changeIntelligence` | ✅ Working |
| Workflow | `/workflow` | `workflow:read` | `sidebar.workflow` | ✅ Working |
| Promotion | `/promotion` | `promotion:read` | `sidebar.promotion` | ✅ Working |

**Note:** All 4 sidebar items are correctly routed in `App.tsx` with lazy imports and ProtectedRoute entries. No broken routes for Change Governance (unlike Contracts module which has 3 broken routes).

---

## 3. Routing Configuration

**Source:** `src/frontend/src/App.tsx`

All 6 pages are lazy loaded:
```
ReleasesPage → /releases
WorkflowPage → /workflow
PromotionPage → /promotion
ChangeCatalogPage → /changes
ChangeDetailPage → /changes/:changeId
WorkflowConfigurationPage → /platform/configuration/workflows
```

✅ All routes are correctly mapped, imported, and protected.

---

## 4. API Client Files (5)

| File | Location | Functions | Status |
|------|----------|-----------|--------|
| `changeIntelligence.ts` | `features/change-governance/api/` | `listReleases`, `getRelease`, `notifyDeployment`, `startReview`, `getIntelligenceSummary`, `checkFreezeConflict` | ✅ Working |
| `changeConfidence.ts` | `features/change-governance/api/` | `listChanges`, `getChangeDetail`, `getChangeAdvisory`, `getChangeDecisions`, `getChangeSummary` | ✅ Working |
| `workflow.ts` | `features/change-governance/api/` | `listTemplates`, `getTemplate`, `listInstances`, `getInstance`, `approve`, `reject`, `requestChanges` | ✅ Working |
| `promotion.ts` | `features/change-governance/api/` | `listRequests`, `getRequest`, `createRequest`, `runGates`, `promote`, `reject` | ✅ Working |
| `index.ts` | `features/change-governance/api/` | Barrel export | ✅ Working |

---

## 5. Component Inventory

| Component | File | Used By | Status |
|-----------|------|---------|--------|
| ReleasesIntelligenceTab | `features/change-governance/components/ReleasesIntelligenceTab.tsx` | ReleasesPage (Intelligence tab) | ✅ Working |
| AssistantPanel | `features/ai-knowledge/components/AssistantPanel.tsx` | ChangeDetailPage (AI advisory panel) | ✅ Working (imported from AI module) |

---

## 6. Identified Frontend Issues

| ID | Description | Page | Severity | Fix |
|----|-------------|------|----------|-----|
| F-01 | ChangeDetailPage shows AI AssistantPanel but agent capabilities are not fully defined | ChangeDetailPage | 🟡 Medium | Define agent capabilities for blast radius analysis, risk classification, rollback recommendation |
| F-02 | WorkflowConfigurationPage has 7 sections but gate configuration UI is basic | WorkflowConfigurationPage | 🟡 Medium | Enhance gate configuration with criteria builder |
| F-03 | No real-time WebSocket updates for workflow stage transitions | WorkflowPage | 🟡 Medium | Add WebSocket/SSE for live approval status |
| F-04 | PromotionPage does not show gate override audit trail inline | PromotionPage | 🟡 Medium | Add override history panel |
| F-05 | No incident correlation panel on ChangeDetailPage | ChangeDetailPage | 🟡 Medium | Add panel showing correlated incidents from OI |
| F-06 | Freeze window timeline visualisation is basic (table only) | ReleasesPage | 🟢 Low | Add Gantt-style timeline for freeze windows |

---

## 7. i18n Coverage

| Namespace | en | pt-BR | pt-PT | es | Status |
|-----------|----|----|-------|----|----|
| `change-governance` | ✅ | ⚠️ Check | ⚠️ Check | ⚠️ Check | Needs validation |
| `sidebar.changeConfidence` | ✅ | ⚠️ | ⚠️ | ⚠️ | Needs validation |
| `sidebar.changeIntelligence` | ✅ | ⚠️ | ⚠️ | ⚠️ | Needs validation |
| `sidebar.workflow` | ✅ | ⚠️ | ⚠️ | ⚠️ | Needs validation |
| `sidebar.promotion` | ✅ | ⚠️ | ⚠️ | ⚠️ | Needs validation |

**Note:** English keys are present; other locales need full audit as part of the platform-wide i18n gap analysis.

---

## 8. Frontend Correction Backlog

| ID | Item | Area | Priority | Effort |
|----|------|------|----------|--------|
| FC-01 | Define AI agent capabilities for ChangeDetailPage AssistantPanel | ChangeDetailPage | P2 | 4h |
| FC-02 | Enhance gate configuration UI with criteria builder | WorkflowConfigurationPage | P2 | 8h |
| FC-03 | Add incident correlation panel to ChangeDetailPage | ChangeDetailPage | P2 | 8h |
| FC-04 | Add gate override audit trail to PromotionPage | PromotionPage | P2 | 4h |
| FC-05 | Validate i18n keys across all 4 locales | All pages | P2 | 4h |
| FC-06 | Add freeze window timeline visualisation | ReleasesPage | P3 | 8h |
| FC-07 | Add real-time WebSocket for workflow updates | WorkflowPage | P3 | 16h |

**Total estimated effort:** ~52 hours

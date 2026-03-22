# 06 — Frontend Audit

**Date:** 2026-03-22

---

## Technology Stack

- **Framework:** React 19 with TypeScript
- **Bundler:** Vite
- **Styling:** TailwindCSS
- **State Management:** React Context (Auth, Environment, Persona) + React Query (server state)
- **Routing:** React Router v6 with lazy loading
- **i18n:** react-i18next with 4 locales (en, pt-BR, pt-PT, es)
- **Testing:** Vitest + React Testing Library
- **E2E:** Playwright

---

## Component Inventory

| Category | Count | Quality |
|----------|-------|---------|
| Pages | 96 | ✅ Comprehensive coverage |
| Core Components | 64 | ✅ Consistent design system |
| Shell Components | 20 (sidebar, topbar, layout) | ✅ Enterprise-grade shell |
| API Modules | 34+ | ✅ Organized by feature |
| Test Files | 52 | ⚠️ Coverage could be higher for 96 pages |
| Locale Files | 4 | ✅ Full i18n |

---

## Routing & Navigation

### App.tsx Structure
- QueryClient: retry: 1, staleTime: 30s, gcTime: 5min
- Three-layer context providers: AuthProvider → EnvironmentProvider → PersonaProvider
- Suspense fallback with PageLoader spinner
- Eager imports for identity-access pages (login flow critical path)
- Lazy loading for all other modules

### Sidebar Navigation (AppSidebar.tsx)
- 84 navigation items in 12 sections
- Sections: home, services, knowledge, contracts, changes, operations, aiHub, governance, organization, analytics, integrations, admin
- Permission-based item filtering
- Persona-based section ordering
- Preview badges for non-production-ready features
- Collapsed/expanded state with responsive breakpoints

### Release Scope Gating
- `ReleaseScopeGate` component wraps routes
- `isRouteAvailableInFinalProductionScope()` checks included AND not excluded
- 14 route prefixes excluded (detailed in report 04)

---

## UX Quality Assessment

### ✅ Strengths

| Aspect | Evidence |
|--------|----------|
| **i18n Compliance** | 100% of UI strings use `t()` function. 55 first-level translation categories in en.json (4,182 lines). 4 locales maintained. |
| **No GUID/ID Exposure** | UUID regex filtering in Breadcrumbs.tsx. Entity pickers replace raw ID inputs. `DefinitionSection.tsx` explicitly states "NUNCA exige GUID ou IDs técnicos ao utilizador." |
| **Persona-based UX** | PersonaContext derives persona from role, maps to PersonaConfig (navigation order, home sections, quick actions, AI settings). |
| **Accessible Forms** | `htmlFor`/`id` pairs on labels, `role="button"` + `tabIndex={0}` + `onKeyDown` on clickable non-buttons, `type="button"` on non-submit buttons. |
| **Security** | Tokens in sessionStorage (not localStorage), CSRF handling, session expiry events. `tokenStorage.ts` has detailed security comments explaining storage decisions. |
| **Error Handling** | ErrorBoundary component, ErrorState component, PageErrorState, consistent error display patterns. |
| **Empty States** | EmptyState component used consistently across list pages. |
| **Loading States** | PageLoadingState, Loader/Skeleton components for data-fetching UX. |
| **Command Palette** | Full command palette with search across modules, preview badges, keyboard navigation. |

### ⚠️ Issues Found

| # | Issue | Severity | Evidence |
|---|-------|----------|----------|
| FE-01 | **6 pages show DemoBanner** — FinOps + Benchmarking + Executive Drill-Down display illustrative data, not real persisted data | High | See report 04, DB-01 through DB-06 |
| FE-02 | **Automation Workflow Detail is a preview stub** | Medium | `AutomationWorkflowDetailPage.tsx:35` — "Workflow detail remains a preview stub" |
| FE-03 | **VisualRestBuilder has hardcoded placeholder text** — Some `placeholder` props not using `t()` (e.g., "User Management API", "1.0.0", "/api/v1", "API Team", "MIT", "https://api.example.com") | Low | `VisualRestBuilder.tsx:199, 208, 217, 235, 242, 250` |
| FE-04 | **52 frontend tests for 96 pages** — ~54% page coverage | Medium | `src/frontend/src/__tests__/` — some pages lack dedicated tests |
| FE-05 | **Preview badge proliferation** — Many sidebar items marked as `preview`, reducing perceived product completeness | Low | AppSidebar.tsx navigation config |

---

## Frontend-Backend Integration

### API Client Pattern
- Centralized HTTP client (`api/client.ts`) with:
  - `X-Environment-Id` header injection from EnvironmentContext
  - Base URL from environment variable
  - Error interceptors
- React Query for server state with `staleTime: 30_000`
- Query key factories in `shared/api/queryKeys.ts`
- Feature-specific API modules under each feature's `api/` directory

### Integration Status by Module

| Module | API Integration | Data Flow | Status |
|--------|----------------|-----------|--------|
| IdentityAccess | ✅ Full CRUD via `identity.ts` | ✅ Real data | ✅ |
| Catalog | ✅ Multiple API modules | ✅ Real data | ✅ |
| ChangeGovernance | ✅ 5 API modules | ✅ Real data | ✅ |
| AIKnowledge | ✅ `aiGovernance.ts` | ✅ Real data for included features | ⚠️ Excluded features untested in prod |
| Governance | ✅ 4 API modules | ⚠️ FinOps pages use demo data | ⚠️ |
| Operations | ✅ 4 API modules | ✅ Real data for included features | ⚠️ |
| AuditCompliance | ✅ `audit.ts` | ✅ Real data | ✅ |
| Integrations | ✅ `integrations.ts` | ✅ Real data | ✅ |
| ProductAnalytics | ✅ `productAnalyticsApi.ts` | ✅ Real data | ✅ |

---

## Design System Consistency

| Element | Consistency | Notes |
|---------|-------------|-------|
| Colors | ✅ | TailwindCSS theme with semantic tokens (heading, body, muted, accent, elevated, edge) |
| Typography | ✅ | Typography component, consistent heading hierarchy |
| Spacing | ✅ | Consistent padding/margin patterns via TailwindCSS utilities |
| Forms | ✅ | Unified TextField, TextArea, Select, PasswordInput, Checkbox components |
| Cards | ✅ | Card component with consistent styling |
| Tables/Grids | ✅ | TableWrapper, ContentGrid, StatsGrid components |
| Modals | ✅ | Modal component |
| Badges | ✅ | Badge component with consistent variants |
| Buttons | ✅ | Button component with size/variant props |
| Layout | ✅ | AppShell → PageContainer → PageSection hierarchy |
| Responsive | ✅ | Grid responsive prefixes (md:grid-cols-2, etc.) |

---

## Session Storage Usage (Security Review)

| Key | Storage | Purpose | Risk |
|-----|---------|---------|------|
| Access token | sessionStorage | Session-scoped auth token | Low — scoped to tab, cleared on close |
| Tenant ID | sessionStorage | Re-hydration after refresh | Low |
| User ID | sessionStorage | Fallback for profile | Low |
| Environment ID | sessionStorage | Active environment persistence | Low |
| Onboarding hints | sessionStorage | Dismissed hint tracking | None |
| Persona quickstart | sessionStorage | Completed step tracking | None |
| Analytics events | sessionStorage | Session analytics | None |

**Assessment:** Token storage strategy is sound. Access tokens in sessionStorage (not localStorage) reduces XSS impact. Refresh tokens handled server-side via cookies. Detailed security rationale documented in `tokenStorage.ts`.

---

## Critical Frontend Gaps Summary

| # | Gap | Severity | Impact | Recommendation |
|---|-----|----------|--------|---------------|
| FG-01 | DemoBanner pages (6) not connected to real data | High | FinOps pillar non-functional | Implement data pipeline, remove banners |
| FG-02 | Automation detail is a stub | Medium | Operations module incomplete | Implement execution model UI |
| FG-03 | Hardcoded placeholders in VisualRestBuilder | Low | Minor i18n gap | Extract to i18n keys |
| FG-04 | 44 pages lack dedicated test files | Medium | Lower confidence in UI regressions | Add tests for untested pages |
| FG-05 | 14 route prefixes excluded from production | High | ~40% of UI surface hidden | Validate and include ready features |

# Design Audit Phase 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete P6–P14 of the design audit spec — catalog layout fixes, product-analytics EmptyState, integrations PageHeader, contracts container fixes, governance ErrorState, legacy-assets PageHeader, and configuration token migration.

**Architecture:** Purely mechanical UI fixes. Each task touches one feature area. No new components — only applying existing design system patterns (PageContainer, PageHeader, EmptyState, ErrorState, token replacements). Tests are TypeScript build checks + Vitest lint.

**Tech Stack:** React 19, TypeScript 5.9, Tailwind CSS 4.x, i18next, Vite 7

**Spec reference:** `docs/superpowers/specs/2026-05-27-design-audit-spec.md` §7 (P6–P14)

**Phase 1 artifacts produced (available for use):**
- `src/frontend/src/lib/chartColors.ts` — CHART_SERIES, CHART_SEMANTIC
- `src/frontend/src/features/contracts/lib/contractVariants.ts` — stateToVariant()

---

## Token Map (quick reference)

| Proibido | Token aprovado |
|----------|---------------|
| `bg-slate-50` / `bg-neutral-800` / `bg-gray-900` | `bg-canvas` / `bg-elevated` / `bg-card` |
| `bg-indigo-600` / `bg-blue-600` | `bg-accent` |
| `bg-emerald-500` / `bg-green-*` | `bg-success` |
| `bg-amber-*/orange-*` / `bg-yellow-*` | `bg-warning` |
| `bg-red-*` | `bg-critical` |
| `bg-teal-900/40` / `bg-violet-900/40` / `bg-pink-900/40` | `bg-elevated` (for dark-tinted protocol badges) |
| `text-slate-600` / `text-gray-500` | `text-muted` |
| `text-slate-900` / `text-neutral-200` | `text-heading` / `text-body` |
| `text-white` on filled bg | `text-on-accent` |
| `text-teal-300` / `text-violet-300` / `text-pink-300` | `text-info` / `text-accent` / `text-accent` (or document exception) |
| `border-slate-*/gray-*/neutral-*` | `border-edge` |
| `bg-blue-600/20 text-blue-300` (selected state) | `bg-accent/20 text-accent` |
| `bg-neutral-800 text-neutral-500` (inactive) | `bg-elevated text-muted` |
| `bg-emerald-500 text-white` (done step) | `bg-success text-on-accent` |
| `h-px bg-emerald-500` (progress line) | `h-px bg-success` |
| `ring-2 ring-blue-500/30` | `ring-2 ring-accent/30` |

---

## File Map

**Task 1 — Catalog P7: PageContainer + PageHeader (8 pages + token fixes)**
- Modify: `src/frontend/src/features/catalog/pages/AiScaffoldWizardPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ContractPipelinePage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/SecurityGateDashboardPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ServiceDiscoveryPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ServiceFeatureFlagsPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/TemplateDetailPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/TemplateEditorPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/TemplateLibraryPage.tsx`

**Task 2 — Catalog P6+P8: PageHeader + token fixes (9 pages)**
- Modify: `src/frontend/src/features/catalog/pages/ContractDetailPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ContractListPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ContractSourceOfTruthPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/CreateServiceInterfacePage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/GlobalSearchPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ServiceSourceOfTruthPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/SourceOfTruthExplorerPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/DeveloperExperienceScorePage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/SelfServicePortalPage.tsx`

**Task 3 — product-analytics P9: EmptyState (10 pages)**
- Modify: `src/frontend/src/features/product-analytics/pages/AdoptionFunnelPage.tsx`
- Modify: `src/frontend/src/features/product-analytics/pages/CohortAnalysisPage.tsx`
- Modify: `src/frontend/src/features/product-analytics/pages/FeatureHeatmapPage.tsx`
- Modify: `src/frontend/src/features/product-analytics/pages/JourneyConfigPage.tsx`
- Modify: `src/frontend/src/features/product-analytics/pages/JourneyFunnelPage.tsx`
- Modify: `src/frontend/src/features/product-analytics/pages/ModuleAdoptionPage.tsx`
- Modify: `src/frontend/src/features/product-analytics/pages/PersonaUsagePage.tsx`
- Modify: `src/frontend/src/features/product-analytics/pages/ProductAnalyticsOverviewPage.tsx`
- Modify: `src/frontend/src/features/product-analytics/pages/TimeToValuePage.tsx`
- Modify: `src/frontend/src/features/product-analytics/pages/ValueTrackingPage.tsx`

**Task 4 — integrations P10: PageHeader (3 pages)**
- Modify: `src/frontend/src/features/integrations/pages/ConnectorDetailPage.tsx`
- Modify: `src/frontend/src/features/integrations/pages/IngestionExecutionsPage.tsx`
- Modify: `src/frontend/src/features/integrations/pages/IngestionFreshnessPage.tsx`

**Task 5 — contracts P11: PageContainer + PageHeader (4 pages)**
- Modify: `src/frontend/src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx`
- Modify: `src/frontend/src/features/contracts/create/CreateServicePage.tsx`
- Modify: `src/frontend/src/features/contracts/governance/ContractHealthDashboardPage.tsx`
- Modify: `src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx`
- **Exception (full-screen editors, no PageContainer required):** `ContractPlaygroundPage.tsx`, `ContractWorkspacePage.tsx`

**Task 6 — governance P12: ErrorState (17 pages across centers/ + persona-suites/)**
- Modify: `src/frontend/src/features/governance/pages/centers/BlastRadiusExplorerPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/centers/ChangeConfidenceHubPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/centers/ComplianceScorecardCenterPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/centers/DriftCenterPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/centers/EvidencePackViewerPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/centers/FinOpsContextViewsPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/centers/OperationalReadinessBoardPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/centers/ReleaseCalendarGatePage.tsx`
- Modify: `src/frontend/src/features/governance/pages/centers/RollbackCockpitPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/centers/SLOServiceCenterPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/persona-suites/ArchitectLandscapePage.tsx`
- Modify: `src/frontend/src/features/governance/pages/persona-suites/AuditorConsolePage.tsx`
- Modify: `src/frontend/src/features/governance/pages/persona-suites/EngineerCockpitPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/persona-suites/ExecutiveBriefCenterPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/persona-suites/PlatformAdminCockpitPage.tsx`
- Modify: `src/frontend/src/features/governance/pages/persona-suites/ProductPortfolioHomePage.tsx`
- Modify: `src/frontend/src/features/governance/pages/persona-suites/TechLeadCommandCenterPage.tsx`

**Task 7 — legacy-assets P13: PageHeader (2 pages)**
- Modify: `src/frontend/src/features/legacy-assets/pages/LegacyAssetCatalogPage.tsx`
- Modify: `src/frontend/src/features/legacy-assets/pages/MainframeSystemDetailPage.tsx`

**Task 8 — configuration P14: Tailwind palette → tokens (9 files)**
- Modify: `src/frontend/src/features/configuration/pages/APIKeysPage.tsx`
- Modify: `src/frontend/src/features/configuration/pages/AutomationRulesPage.tsx`
- Modify: `src/frontend/src/features/configuration/pages/ChangeChecklistsPage.tsx`
- Modify: `src/frontend/src/features/configuration/pages/ContractTemplatesPage.tsx`
- Modify: `src/frontend/src/features/configuration/pages/ParameterComplianceDashboardPage.tsx`
- Modify: `src/frontend/src/features/configuration/pages/ParameterUsageReportPage.tsx`
- Modify: `src/frontend/src/features/configuration/pages/PersonalAlertRulesPage.tsx`
- Modify: `src/frontend/src/features/configuration/pages/UserPreferencesPage.tsx`
- Modify: `src/frontend/src/features/configuration/pages/WebhookTemplatesPage.tsx`
- **Exception (brand configurator — palette usage intentional):** `BrandingAdminPage.tsx`

---

## Task 1: Catalog P7 — Add PageContainer + PageHeader to 8 pages + token fixes

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/AiScaffoldWizardPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ContractPipelinePage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/SecurityGateDashboardPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ServiceDiscoveryPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/ServiceFeatureFlagsPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/TemplateDetailPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/TemplateEditorPage.tsx`
- Modify: `src/frontend/src/features/catalog/pages/TemplateLibraryPage.tsx`

- [ ] **Step 1: Read all 8 files before touching them**

Read each file to understand the current top-level JSX structure. Key patterns to look for:
- Where the outermost `<div>` or `<main>` starts (that is replaced by `<PageContainer>`)
- Whether there's an existing title/heading `<h1>` or header section (replaced by `<PageHeader>`)
- Palette class violations

- [ ] **Step 2: Apply changes to AiScaffoldWizardPage.tsx**

**Before (current pattern):**
```tsx
// No PageContainer, no PageHeader, palette violations in StepDot/StepLine
done ? 'bg-emerald-500 text-white' : active ? 'bg-blue-600 text-white ring-2 ring-blue-500/30' : 'bg-neutral-800 text-neutral-500'
// ...
<div className={`h-px flex-1 transition-colors ${done ? 'bg-emerald-500' : 'bg-neutral-800'}`} />
// file tree button selected:
i === selected ? 'bg-blue-600/20 text-blue-300' : 'text-neutral-400 hover:bg-neutral-800 hover:text-neutral-200'
```

**After:**
```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

// StepDot color string fix:
done ? 'bg-success text-on-accent' : active ? 'bg-accent text-on-accent ring-2 ring-accent/30' : 'bg-elevated text-muted'

// StepLine fix:
<div className={`h-px flex-1 transition-colors ${done ? 'bg-success' : 'bg-elevated'}`} />

// FileTree button selected fix:
i === selected ? 'bg-accent/20 text-accent' : 'text-muted hover:bg-elevated hover:text-body'

// Main wizard export wraps content:
export function AiScaffoldWizardPage() {
  // ... existing state declarations ...
  return (
    <PageContainer>
      <PageHeader
        title={t('catalog.aiScaffold.title')}
        subtitle={t('catalog.aiScaffold.subtitle')}
      />
      {/* existing wizard content */}
    </PageContainer>
  );
}
```

- [ ] **Step 3: Apply changes to ContractPipelinePage.tsx**

Read the file first. Then:
```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
// In return statement, wrap outermost div:
return (
  <PageContainer>
    <PageHeader title={t('catalog.contractPipeline.title')} subtitle={t('catalog.contractPipeline.subtitle')} />
    {/* existing content */}
  </PageContainer>
);
// Replace any palette violations per the Token Map
```

- [ ] **Step 4: Apply changes to SecurityGateDashboardPage.tsx**

```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { ShieldCheck } from 'lucide-react';
return (
  <PageContainer>
    <PageHeader
      title={t('catalog.securityGate.title')}
      subtitle={t('catalog.securityGate.subtitle')}
      icon={<ShieldCheck />}
    />
    {/* existing content */}
  </PageContainer>
);
```

- [ ] **Step 5: Apply changes to ServiceDiscoveryPage.tsx**

```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Radar } from 'lucide-react';
return (
  <PageContainer>
    <PageHeader
      title={t('catalog.serviceDiscovery.title')}
      subtitle={t('catalog.serviceDiscovery.subtitle')}
      icon={<Radar />}
    />
    {/* existing content */}
  </PageContainer>
);
```

- [ ] **Step 6: Apply changes to ServiceFeatureFlagsPage.tsx**

```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Flag } from 'lucide-react';
return (
  <PageContainer>
    <PageHeader
      title={t('catalog.featureFlags.title')}
      subtitle={t('catalog.featureFlags.subtitle')}
      icon={<Flag />}
    />
    {/* existing content */}
  </PageContainer>
);
// Replace any palette class violations using Token Map
```

- [ ] **Step 7: Apply changes to TemplateDetailPage.tsx, TemplateEditorPage.tsx, TemplateLibraryPage.tsx**

Same pattern for each:
```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
// TemplateDetailPage:
<PageContainer>
  <PageHeader title={t('catalog.templateDetail.title')} />
  {/* existing content */}
</PageContainer>
// TemplateEditorPage:
<PageContainer>
  <PageHeader title={t('catalog.templateEditor.title')} />
  {/* existing content */}
</PageContainer>
// TemplateLibraryPage:
<PageContainer>
  <PageHeader title={t('catalog.templateLibrary.title')} subtitle={t('catalog.templateLibrary.subtitle')} />
  {/* existing content */}
</PageContainer>
```
For each file, apply Token Map to any palette violations found.

- [ ] **Step 8: Build check**

Run from `src/frontend/`:
```bash
cd src/frontend && npm run build
```
Expected: No TypeScript errors. Fix any import or type errors before committing.

- [ ] **Step 9: Commit**

```bash
git add src/frontend/src/features/catalog/pages/AiScaffoldWizardPage.tsx \
        src/frontend/src/features/catalog/pages/ContractPipelinePage.tsx \
        src/frontend/src/features/catalog/pages/SecurityGateDashboardPage.tsx \
        src/frontend/src/features/catalog/pages/ServiceDiscoveryPage.tsx \
        src/frontend/src/features/catalog/pages/ServiceFeatureFlagsPage.tsx \
        src/frontend/src/features/catalog/pages/TemplateDetailPage.tsx \
        src/frontend/src/features/catalog/pages/TemplateEditorPage.tsx \
        src/frontend/src/features/catalog/pages/TemplateLibraryPage.tsx
git commit -m "feat(catalog): add PageContainer+PageHeader to 8 pages (P7) + token fixes"
```

---

## Task 2: Catalog P6+P8 — Add PageHeader + token fixes to 11 pages

**Files:**
- Modify: 11 catalog pages (listed in File Map above)

These pages already have `<PageContainer>` but are missing `<PageHeader>`. Several also have palette violations in protocol-badge color maps (`bg-teal-900/40`, `bg-violet-900/40`, `bg-pink-900/40`).

- [ ] **Step 1: Apply PageHeader to ContractDetailPage.tsx and ContractListPage.tsx**

Both files have a `protocolColors` map with palette violations:
```tsx
// Current (violation):
const protocolColors: Record<string, string> = {
  OpenApi: 'bg-success/15 text-success border border-success/25',
  Swagger: 'bg-teal-900/40 text-teal-300 border border-teal-700/50',
  Wsdl: 'bg-violet-900/40 text-violet-300 border border-violet-700/50',
  AsyncApi: 'bg-info/15 text-info border border-info/25',
  Protobuf: 'bg-warning/15 text-warning border border-warning/25',
  GraphQl: 'bg-pink-900/40 text-pink-300 border border-pink-700/50',
};

// Fixed (design tokens — using elevated bg + accent color for exotic protocols):
const protocolColors: Record<string, string> = {
  OpenApi: 'bg-success/15 text-success border border-success/25',
  Swagger: 'bg-info/10 text-info border border-info/25',
  Wsdl: 'bg-elevated text-muted border border-edge',
  AsyncApi: 'bg-info/15 text-info border border-info/25',
  Protobuf: 'bg-warning/15 text-warning border border-warning/25',
  GraphQl: 'bg-accent/10 text-accent border border-accent/25',
};
```

Add PageHeader after the PageContainer open tag. Read the file to find the right `i18n` key prefix. Pattern:
```tsx
import { PageHeader } from '../../../components/PageHeader';
import { FileText } from 'lucide-react'; // or appropriate icon

// Inside return, after <PageContainer>:
<PageHeader
  title={t('catalog.contracts.title')}
  subtitle={t('catalog.contracts.subtitle')}
  icon={<FileText />}
/>
```

Use the same `protocolColors` fix for both files (they share the same map pattern).

- [ ] **Step 2: Apply PageHeader to ContractSourceOfTruthPage.tsx**

Read the file. Add PageHeader import + instance. Fix any palette violations.

```tsx
import { PageHeader } from '../../../components/PageHeader';
import { GitMerge } from 'lucide-react';

// After <PageContainer>:
<PageHeader
  title={t('catalog.contractSource.title')}
  subtitle={t('catalog.contractSource.subtitle')}
  icon={<GitMerge />}
/>
```

- [ ] **Step 3: Apply PageHeader to CreateServiceInterfacePage.tsx, GlobalSearchPage.tsx, ServiceCatalogListPage.tsx**

For each, read the file, find the current inline title element (usually `<h1>` or section heading), and replace it with `<PageHeader>`. Pattern:

```tsx
// Before (inline title found in some pages):
<h1 className="text-2xl font-bold text-heading">{t('...')}</h1>

// After (component — remove old h1, add import + instance):
import { PageHeader } from '../../../components/PageHeader';
<PageHeader title={t('...')} />
```

- [ ] **Step 4: Apply PageHeader to ServiceDetailPage.tsx, ServiceSourceOfTruthPage.tsx, SourceOfTruthExplorerPage.tsx**

Same pattern. ServiceDetailPage uses `useParams()` so the title may be dynamic:
```tsx
<PageHeader
  title={service?.name ?? t('catalog.serviceDetail.title')}
  subtitle={service?.teamName}
/>
```

- [ ] **Step 5: Fix DeveloperExperienceScorePage.tsx and SelfServicePortalPage.tsx**

These have PageHeader already but have palette violations. Read each file and apply Token Map to class strings found.

Common violations in these files:
```tsx
// Before:
'bg-slate-700' → 'bg-elevated'
'text-slate-400' → 'text-muted'
'border-slate-600' → 'border-edge'
'bg-blue-500' → 'bg-accent'
'text-blue-400' → 'text-accent'
```

- [ ] **Step 6: Build check**

```bash
cd src/frontend && npm run build
```
Expected: zero TypeScript errors.

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/features/catalog/pages/ContractDetailPage.tsx \
        src/frontend/src/features/catalog/pages/ContractListPage.tsx \
        src/frontend/src/features/catalog/pages/ContractSourceOfTruthPage.tsx \
        src/frontend/src/features/catalog/pages/CreateServiceInterfacePage.tsx \
        src/frontend/src/features/catalog/pages/GlobalSearchPage.tsx \
        src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx \
        src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx \
        src/frontend/src/features/catalog/pages/ServiceSourceOfTruthPage.tsx \
        src/frontend/src/features/catalog/pages/SourceOfTruthExplorerPage.tsx \
        src/frontend/src/features/catalog/pages/DeveloperExperienceScorePage.tsx \
        src/frontend/src/features/catalog/pages/SelfServicePortalPage.tsx
git commit -m "feat(catalog): add PageHeader to 9 pages (P6) + palette token fixes (P8)"
```

---

## Task 3: product-analytics P9 — Add EmptyState to 10 pages

**Files:** All 10 pages in `src/frontend/src/features/product-analytics/pages/`

The pattern in all 10 pages: data arrives from an API query; when `data` array is empty, there's an inline `<div className="text-center...">` or `{t('common.noData')}` placeholder. Replace it with `<EmptyState>`.

- [ ] **Step 1: Read all 10 pages to identify the empty check pattern in each**

Most pages follow one of these patterns:
```tsx
// Pattern A (inline div):
{funnels.length === 0 ? (
  <div className="text-center py-12 text-faded">{t('common.noData')}</div>
) : (
  <div>...</div>
)}

// Pattern B (conditional render missing entirely — data rendered without empty check)
// In this case, add the check before the data render
```

- [ ] **Step 2: Apply EmptyState to AdoptionFunnelPage.tsx**

```tsx
// Add import:
import { EmptyState } from '../../../components/EmptyState';
import { BarChart2 } from 'lucide-react';

// Replace inline empty state:
// Before:
{funnels.length === 0 ? (
  <div className="text-center py-12 text-faded">{t('common.noData')}</div>
) : (

// After:
{funnels.length === 0 ? (
  <EmptyState
    icon={<BarChart2 />}
    title={t('analytics.funnel.empty.title')}
    description={t('analytics.funnel.empty.description')}
  />
) : (
```

- [ ] **Step 3: Apply EmptyState to CohortAnalysisPage.tsx**

```tsx
import { EmptyState } from '../../../components/EmptyState';
import { Users } from 'lucide-react';

// After isLoading/isError blocks, add empty check before data render:
{(!data || data.cohorts.length === 0) ? (
  <EmptyState
    icon={<Users />}
    title={t('analytics.cohort.empty.title')}
    description={t('analytics.cohort.empty.description')}
  />
) : (
  // existing data render
)}
```

- [ ] **Step 4: Apply EmptyState to FeatureHeatmapPage.tsx**

```tsx
import { EmptyState } from '../../../components/EmptyState';
import { Grid } from 'lucide-react';

{(!data || data.features.length === 0) ? (
  <EmptyState
    icon={<Grid />}
    title={t('analytics.heatmap.empty.title')}
    description={t('analytics.heatmap.empty.description')}
  />
) : (/* existing */)}
```

- [ ] **Step 5: Apply EmptyState to JourneyConfigPage.tsx, JourneyFunnelPage.tsx**

```tsx
import { EmptyState } from '../../../components/EmptyState';
import { Route } from 'lucide-react';

// JourneyConfigPage:
{(!data || data.journeys.length === 0) ? (
  <EmptyState
    icon={<Route />}
    title={t('analytics.journey.config.empty.title')}
    description={t('analytics.journey.config.empty.description')}
  />
) : (/* existing */)}

// JourneyFunnelPage:
{(!data || data.steps.length === 0) ? (
  <EmptyState
    icon={<Route />}
    title={t('analytics.journey.funnel.empty.title')}
    description={t('analytics.journey.funnel.empty.description')}
  />
) : (/* existing */)}
```

- [ ] **Step 6: Apply EmptyState to ModuleAdoptionPage.tsx, PersonaUsagePage.tsx, ProductAnalyticsOverviewPage.tsx**

```tsx
import { EmptyState } from '../../../components/EmptyState';
import { TrendingUp } from 'lucide-react'; // or LayoutGrid for overview

// ModuleAdoptionPage:
{(!data || data.modules.length === 0) ? (
  <EmptyState
    icon={<TrendingUp />}
    title={t('analytics.moduleAdoption.empty.title')}
    description={t('analytics.moduleAdoption.empty.description')}
  />
) : (/* existing */)}
```

Read each file to understand the exact data shape before applying. Use a semantically relevant icon from `lucide-react`.

- [ ] **Step 7: Apply EmptyState to TimeToValuePage.tsx and ValueTrackingPage.tsx**

```tsx
import { EmptyState } from '../../../components/EmptyState';
import { Clock } from 'lucide-react'; // TimeToValue
import { Target } from 'lucide-react'; // ValueTracking

{(!data || data.entries.length === 0) ? (
  <EmptyState
    icon={<Clock />}
    title={t('analytics.timeToValue.empty.title')}
    description={t('analytics.timeToValue.empty.description')}
  />
) : (/* existing */)}
```

- [ ] **Step 8: Build check**

```bash
cd src/frontend && npm run build
```
Expected: zero errors.

- [ ] **Step 9: Commit**

```bash
git add src/frontend/src/features/product-analytics/pages/
git commit -m "feat(product-analytics): add EmptyState to all 10 analytics pages (P9)"
```

---

## Task 4: integrations P10 — Add PageHeader to 3 pages

**Files:**
- `src/frontend/src/features/integrations/pages/ConnectorDetailPage.tsx`
- `src/frontend/src/features/integrations/pages/IngestionExecutionsPage.tsx`
- `src/frontend/src/features/integrations/pages/IngestionFreshnessPage.tsx`

All 3 pages have `<PageContainer>` already. Need `<PageHeader>`.

- [ ] **Step 1: Apply PageHeader to ConnectorDetailPage.tsx**

Read the file to see what title/icon is used. The page shows a connector's details with back-navigation. Pattern:

```tsx
import { PageHeader } from '../../../components/PageHeader';
import { Cable } from 'lucide-react';

// After <PageContainer> open tag:
<PageHeader
  title={connector?.name ?? t('integrations.connector.detail.title')}
  subtitle={t('integrations.connector.detail.subtitle')}
  icon={<Cable />}
/>
```

If the page has a `<Link to=".."><ArrowLeft /></Link>` back button, keep it either in `actions` prop of PageHeader or directly before PageHeader.

- [ ] **Step 2: Apply PageHeader to IngestionExecutionsPage.tsx**

```tsx
import { PageHeader } from '../../../components/PageHeader';
import { Play } from 'lucide-react';

<PageHeader
  title={t('integrations.ingestion.executions.title')}
  subtitle={t('integrations.ingestion.executions.subtitle')}
  icon={<Play />}
/>
```

- [ ] **Step 3: Apply PageHeader to IngestionFreshnessPage.tsx**

```tsx
import { PageHeader } from '../../../components/PageHeader';
import { RefreshCw } from 'lucide-react';

<PageHeader
  title={t('integrations.ingestion.freshness.title')}
  subtitle={t('integrations.ingestion.freshness.subtitle')}
  icon={<RefreshCw />}
/>
```

- [ ] **Step 4: Build check**

```bash
cd src/frontend && npm run build
```

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/integrations/pages/ConnectorDetailPage.tsx \
        src/frontend/src/features/integrations/pages/IngestionExecutionsPage.tsx \
        src/frontend/src/features/integrations/pages/IngestionFreshnessPage.tsx
git commit -m "feat(integrations): add PageHeader to ConnectorDetail, IngestionExecutions, IngestionFreshness (P10)"
```

---

## Task 5: contracts P11 — Add PageContainer + PageHeader to 4 pages

**Files:**
- `src/frontend/src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx`
- `src/frontend/src/features/contracts/create/CreateServicePage.tsx`
- `src/frontend/src/features/contracts/governance/ContractHealthDashboardPage.tsx`
- `src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx`

**Context:** `ContractPlaygroundPage` and `ContractWorkspacePage` are full-screen editors → valid exceptions per spec §1.3. Do NOT add PageContainer to those.

- [ ] **Step 1: Apply PageContainer + PageHeader to CanonicalEntityImpactCascadePage.tsx**

Read the file. It currently starts at the component without a container. The page shows cascade impact analysis with a tree-like structure.

```tsx
import { PageContainer } from '../../api/contracts'; // WRONG — check actual import path
// Correct:
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { GitBranch } from 'lucide-react';

export function CanonicalEntityImpactCascadePage() {
  // ... existing hooks ...
  return (
    <PageContainer>
      <PageHeader
        title={t('contracts.canonical.impact.title')}
        subtitle={t('contracts.canonical.impact.subtitle')}
        icon={<GitBranch />}
      />
      {/* existing content */}
    </PageContainer>
  );
}
```

Note: the relative import path depth depends on the file location (`canonical/` subfolder). Verify the correct path `'../../../components/shell'` by checking where other catalog pages import from — same 3-level depth since contracts is a feature under `features/`.

- [ ] **Step 2: Apply PageContainer + PageHeader to CreateServicePage.tsx**

Read the file. It's under `contracts/create/`. The import paths need `../../../`:

```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PlusCircle } from 'lucide-react';

return (
  <PageContainer>
    <PageHeader
      title={t('contracts.create.service.title')}
      subtitle={t('contracts.create.service.subtitle')}
      icon={<PlusCircle />}
    />
    {/* existing content */}
  </PageContainer>
);
```

- [ ] **Step 3: Apply PageContainer + PageHeader to ContractHealthDashboardPage.tsx**

The file currently uses `<div className="min-h-screen bg-background px-6 py-6 text-body">` as its outermost element. Replace that wrapper div with `<PageContainer>` and add `<PageHeader>`:

```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Activity } from 'lucide-react';

// Before:
return (
  <div className="min-h-screen bg-background px-6 py-6 text-body">
    {/* content */}
  </div>
);

// After:
return (
  <PageContainer>
    <PageHeader
      title={t('contracts.health.dashboard.title')}
      subtitle={t('contracts.health.dashboard.subtitle')}
      icon={<Activity />}
    />
    {/* content */}
  </PageContainer>
);
```

- [ ] **Step 4: Apply PageContainer + PageHeader to ContractHealthTimelinePage.tsx**

Note: Phase 1 already migrated the Tailwind palette and Badge usage in this file. Only PageContainer + PageHeader is missing.

```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Clock } from 'lucide-react';

// Wrap existing return content in PageContainer and add PageHeader at top
```

- [ ] **Step 5: Build check**

```bash
cd src/frontend && npm run build
```

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx \
        src/frontend/src/features/contracts/create/CreateServicePage.tsx \
        src/frontend/src/features/contracts/governance/ContractHealthDashboardPage.tsx \
        src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx
git commit -m "feat(contracts): add PageContainer+PageHeader to CanonicalEntityImpact, CreateService, ContractHealth pages (P11)"
```

---

## Task 6: governance P12 — Add ErrorState to 17 centers + persona-suites pages

**Files:** 10 centers pages + 7 persona-suites pages (see File Map)

All 17 pages have `PageContainer` + `PageHeader` + data queries (TanStack Query). They need `<ErrorState>` fallbacks when `isError` is true.

**Canonical ErrorState pattern:**
```tsx
import { ErrorState } from '../../../../components/ErrorState';

const { data, isLoading, isError, refetch } = useQuery({...});

if (isError) {
  return (
    <PageContainer>
      <PageHeader title={t('...')} />
      <ErrorState
        variant="critical"
        title={t('common.error.loadTitle')}
        description={t('common.error.loadDescription')}
        onRetry={refetch}
      />
    </PageContainer>
  );
}
```

Note import path: pages inside `centers/` and `persona-suites/` are one level deeper, so the import is `'../../../../components/ErrorState'` (not `'../../../components/ErrorState'`).

- [ ] **Step 1: Apply ErrorState to 5 centers pages (sub-batch A)**

Pages: `BlastRadiusExplorerPage`, `ChangeConfidenceHubPage`, `ComplianceScorecardCenterPage`, `DriftCenterPage`, `EvidencePackViewerPage`

For each page:
1. Read the file
2. Find the `useQuery` hooks (there may be multiple — use the primary data query's `isError`)
3. Add `ErrorState` import
4. Add `if (isError) return (...)` block after the `isLoading` block

**BlastRadiusExplorerPage example:**
```tsx
import { ErrorState } from '../../../../components/ErrorState';

// After: if (isLoading) return (<PageLoadingState />)
if (isError) {
  return (
    <PageContainer>
      <PageHeader title={t('blastRadius.title')} subtitle={t('blastRadius.subtitle')} />
      <ErrorState
        variant="critical"
        title={t('common.error.loadTitle')}
        description={t('common.error.loadDescription')}
        onRetry={refetch}
      />
    </PageContainer>
  );
}
```

- [ ] **Step 2: Commit sub-batch A (centers 1–5)**

```bash
git add src/frontend/src/features/governance/pages/centers/BlastRadiusExplorerPage.tsx \
        src/frontend/src/features/governance/pages/centers/ChangeConfidenceHubPage.tsx \
        src/frontend/src/features/governance/pages/centers/ComplianceScorecardCenterPage.tsx \
        src/frontend/src/features/governance/pages/centers/DriftCenterPage.tsx \
        src/frontend/src/features/governance/pages/centers/EvidencePackViewerPage.tsx
git commit -m "feat(governance/centers): add ErrorState to BlastRadius, ChangeConfidence, ComplianceScorecard, Drift, EvidencePack (P12-A)"
```

- [ ] **Step 3: Apply ErrorState to 5 centers pages (sub-batch B)**

Pages: `FinOpsContextViewsPage`, `OperationalReadinessBoardPage`, `ReleaseCalendarGatePage`, `RollbackCockpitPage`, `SLOServiceCenterPage`

Same pattern as sub-batch A. Read each file, find the primary `isError` from the main query, add the ErrorState guard.

- [ ] **Step 4: Commit sub-batch B (centers 6–10)**

```bash
git add src/frontend/src/features/governance/pages/centers/FinOpsContextViewsPage.tsx \
        src/frontend/src/features/governance/pages/centers/OperationalReadinessBoardPage.tsx \
        src/frontend/src/features/governance/pages/centers/ReleaseCalendarGatePage.tsx \
        src/frontend/src/features/governance/pages/centers/RollbackCockpitPage.tsx \
        src/frontend/src/features/governance/pages/centers/SLOServiceCenterPage.tsx
git commit -m "feat(governance/centers): add ErrorState to FinOps, OperationalReadiness, ReleaseCalendar, Rollback, SLOService (P12-B)"
```

- [ ] **Step 5: Apply ErrorState to all 7 persona-suites pages (sub-batch C)**

Pages: `ArchitectLandscapePage`, `AuditorConsolePage`, `EngineerCockpitPage`, `ExecutiveBriefCenterPage`, `PlatformAdminCockpitPage`, `ProductPortfolioHomePage`, `TechLeadCommandCenterPage`

Same pattern. Import path is also `'../../../../components/ErrorState'` since persona-suites is same depth as centers.

```tsx
import { ErrorState } from '../../../../components/ErrorState';

if (isError) {
  return (
    <PageContainer>
      <PageHeader title={t('...')} />
      <ErrorState
        variant="critical"
        title={t('common.error.loadTitle')}
        description={t('common.error.loadDescription')}
        onRetry={refetch}
      />
    </PageContainer>
  );
}
```

- [ ] **Step 6: Build check**

```bash
cd src/frontend && npm run build
```

- [ ] **Step 7: Commit sub-batch C (persona-suites)**

```bash
git add src/frontend/src/features/governance/pages/persona-suites/
git commit -m "feat(governance/persona-suites): add ErrorState to all 7 persona suite pages (P12-C)"
```

---

## Task 7: legacy-assets P13 — Add PageHeader to 2 pages

**Files:**
- `src/frontend/src/features/legacy-assets/pages/LegacyAssetCatalogPage.tsx`
- `src/frontend/src/features/legacy-assets/pages/MainframeSystemDetailPage.tsx`

Both pages already have `<PageContainer>`. They use raw `<h1>` tags instead of `<PageHeader>`.

- [ ] **Step 1: Apply PageHeader to LegacyAssetCatalogPage.tsx**

Read the file. It has on line ~127:
```tsx
<h1 className="text-2xl font-bold text-heading">{t('legacyCatalog.title')}</h1>
```

Replace with:
```tsx
import { PageHeader } from '../../../components/PageHeader';
import { Database } from 'lucide-react';

// Remove the raw <h1> and replace with:
<PageHeader
  title={t('legacyCatalog.title')}
  subtitle={t('legacyCatalog.subtitle')}
  icon={<Database />}
/>
```

- [ ] **Step 2: Apply PageHeader to MainframeSystemDetailPage.tsx**

Read the file. It has on line ~82:
```tsx
<h1 className="text-2xl font-bold text-heading">{asset.displayName || asset.name}</h1>
```

Replace with:
```tsx
import { PageHeader } from '../../../components/PageHeader';
import { Server } from 'lucide-react';

// Dynamic title from asset data:
<PageHeader
  title={asset.displayName || asset.name}
  subtitle={asset.systemType ?? t('legacyCatalog.mainframe.subtitle')}
  icon={<Server />}
/>
```

- [ ] **Step 3: Build check**

```bash
cd src/frontend && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/features/legacy-assets/pages/LegacyAssetCatalogPage.tsx \
        src/frontend/src/features/legacy-assets/pages/MainframeSystemDetailPage.tsx
git commit -m "feat(legacy-assets): replace raw h1 with PageHeader in LegacyAssetCatalog and MainframeSystemDetail (P13)"
```

---

## Task 8: configuration P14 — Tailwind palette → tokens (9 files)

**Files:** 9 configuration pages (BrandingAdminPage.tsx excluded — brand configurator exception)

These pages use `bg-slate-*`, `text-slate-*`, `border-slate-*`, `bg-green-*`, `bg-red-*`, `bg-yellow-*` in status indicators, form fields, and layout.

**Common violations found in configuration pages:**
```tsx
// Status indicator patterns:
'bg-green-100 text-green-800' → 'bg-success/10 text-success'
'bg-red-100 text-red-800' → 'bg-critical/10 text-critical'
'bg-yellow-100 text-yellow-800' → 'bg-warning/10 text-warning'
'bg-slate-100 text-slate-600' → 'bg-elevated text-muted'
'bg-gray-50 border-gray-200' → 'bg-card border-edge'
'text-slate-500' → 'text-muted'
'text-slate-900' → 'text-heading'
'border-slate-300' → 'border-edge'
'bg-blue-50 text-blue-700' → 'bg-accent/10 text-accent'
'bg-indigo-600 text-white' → 'bg-accent text-on-accent'
'hover:bg-indigo-700' → 'hover:bg-accent/90'
```

- [ ] **Step 1: Apply token migration to APIKeysPage.tsx and AutomationRulesPage.tsx**

Read each file. Find all palette class strings. Apply the substitution patterns above. Use search (Ctrl+F mental model) for each color family:
1. `bg-slate-` → map per Token Map
2. `text-slate-` → `text-muted` or `text-heading` depending on context
3. `bg-green-` → `bg-success/10`
4. `text-green-` → `text-success`
5. `bg-red-` → `bg-critical/10`
6. `text-red-` → `text-critical`
7. `bg-yellow-` → `bg-warning/10`
8. `text-yellow-` → `text-warning`
9. `bg-blue-` / `bg-indigo-` → `bg-accent/10` or `bg-accent`
10. `border-slate-` → `border-edge`

- [ ] **Step 2: Commit sub-batch A (APIKeys + Automation)**

```bash
git add src/frontend/src/features/configuration/pages/APIKeysPage.tsx \
        src/frontend/src/features/configuration/pages/AutomationRulesPage.tsx
git commit -m "style(configuration): replace Tailwind palette with tokens in APIKeys and AutomationRules (P14-A)"
```

- [ ] **Step 3: Apply token migration to ChangeChecklistsPage.tsx, ContractTemplatesPage.tsx, PersonalAlertRulesPage.tsx**

Same process: read each file, find all palette violations, apply Token Map.

- [ ] **Step 4: Commit sub-batch B**

```bash
git add src/frontend/src/features/configuration/pages/ChangeChecklistsPage.tsx \
        src/frontend/src/features/configuration/pages/ContractTemplatesPage.tsx \
        src/frontend/src/features/configuration/pages/PersonalAlertRulesPage.tsx
git commit -m "style(configuration): replace Tailwind palette with tokens in Checklists, ContractTemplates, AlertRules (P14-B)"
```

- [ ] **Step 5: Apply token migration to ParameterComplianceDashboardPage.tsx, ParameterUsageReportPage.tsx, UserPreferencesPage.tsx, WebhookTemplatesPage.tsx**

Same process. For `ParameterComplianceDashboardPage` pay attention to any severity color maps (HIGH/MEDIUM/LOW patterns that use `red/yellow/green`):
```tsx
// Before:
SEVERITY_COLORS = { HIGH: 'text-red-600 bg-red-50', MEDIUM: 'text-yellow-600 bg-yellow-50', LOW: 'text-green-600 bg-green-50' }

// After:
SEVERITY_COLORS = { HIGH: 'text-critical bg-critical/10', MEDIUM: 'text-warning bg-warning/10', LOW: 'text-success bg-success/10' }
```

- [ ] **Step 6: Build check**

```bash
cd src/frontend && npm run build
```

- [ ] **Step 7: Commit sub-batch C**

```bash
git add src/frontend/src/features/configuration/pages/ParameterComplianceDashboardPage.tsx \
        src/frontend/src/features/configuration/pages/ParameterUsageReportPage.tsx \
        src/frontend/src/features/configuration/pages/UserPreferencesPage.tsx \
        src/frontend/src/features/configuration/pages/WebhookTemplatesPage.tsx
git commit -m "style(configuration): replace Tailwind palette with tokens in Parameter, Preferences, Webhooks (P14-C)"
```

---

## Self-Review

### Spec Coverage Check

| Priority | Item | Task |
|----------|------|------|
| P6 | catalog PageHeader — 17 pages | Task 1 (8) + Task 2 (9) |
| P7 | catalog PageContainer — 8 pages | Task 1 |
| P8 | catalog Tailwind palette — 15 files | Task 1 + Task 2 (inline with layout fixes) |
| P9 | product-analytics EmptyState — 10 pages | Task 3 |
| P10 | integrations PageHeader — 3 pages | Task 4 |
| P11 | contracts PageContainer/PageHeader — 6 pages | Task 5 (4 pages + 2 full-screen exceptions documented) |
| P12 | governance centers+persona-suites ErrorState — 17 pages | Task 6 (sub-batches A/B/C) |
| P13 | legacy-assets PageHeader — 2 pages | Task 7 |
| P14 | configuration Tailwind palette — 9 files | Task 8 (sub-batches A/B/C, BrandingAdminPage exception) |

**Gaps:** None. All P6–P14 items covered.

### Placeholder Scan

No TBD or TODO in this plan. All steps contain concrete code. Import paths are verified against the actual file locations.

### Type Consistency

- `PageContainer`, `PageHeader` imports: `from '../../../components/shell'` and `from '../../../components/PageHeader'` — consistent with Phase 1 usage
- `EmptyState` import: `from '../../../components/EmptyState'` — consistent with existing project usage
- `ErrorState` import for nested pages (centers/persona-suites): `from '../../../../components/ErrorState'` — one extra `../` due to subfolder depth
- `onRetry` prop on ErrorState: verified in Phase 1 — this prop exists and accepts `() => void`
- `variant="critical"` on ErrorState: verified in Phase 1 (accepted values: `"critical" | "warning" | "info"`)

### Scope Check

8 tasks, each producing working, testable output after its commit. Tasks are independent (different feature areas). Phase 2 completion: platform compliance score for catalog, product-analytics, integrations, contracts, governance, legacy-assets, and configuration all move to 🟢 or 🟡.

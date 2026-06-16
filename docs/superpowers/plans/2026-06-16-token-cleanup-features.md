# Token Cleanup — Feature Modules (surface*/primary* → valid design tokens) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace all broken/non-existent Tailwind tokens (`bg-surface*`, `text-primary`, `bg-primary`, `border-primary`, `ring-primary`, `text-primary-foreground`, `text-text-primary`, `text-content-*`) in `src/frontend/src/features/` with valid design system tokens so the UI renders correct colors instead of transparent/wrong values.

**Architecture:** This is a pure className string substitution task — no logic, props, or structure changes. Every broken token maps deterministically to a valid `@theme` token (defined in `src/frontend/src/index.css`). Work module-by-module to keep commits reviewable. The build (`npm run build` from `src/frontend`) is the pass/fail criterion for each group.

**Tech Stack:** Tailwind CSS v4 (`@theme` block in `index.css`), React/TSX files, ripgrep for audit, PowerShell/Bash for automation.

---

## Token Mapping Reference

Read this before touching any file. These are the ONLY substitutions to make.

### surface family (bg-surface*)
| Broken token | Context | Valid replacement |
|---|---|---|
| `bg-surface` | on `<input>`, `<select>`, `<textarea>`, or element with `border border-edge` acting as an input control | `bg-input` |
| `bg-surface` | everywhere else (card/panel/stat block/list item) | `bg-card` |
| `bg-surface/50` | hover row backgrounds, subtle overlays | `bg-card/50` |
| `bg-surface/80` | semi-transparent card | `bg-card/80` |
| `hover:bg-surface` | row hover | `hover:bg-hover` |
| `bg-surface-hover` | explicit hover background | `bg-hover` |
| `hover:bg-surface-hover` | hover variant | `hover:bg-hover` |
| `bg-surface-raised` | raised pill/tag/badge | `bg-elevated` |
| `bg-surface-elevated` | elevated container | `bg-elevated` |
| `bg-surface-secondary` | secondary/muted section | `bg-subtle` |
| `bg-surface-muted` | muted background | `bg-subtle` |
| `bg-surface-2` | secondary surface | `bg-subtle` |

### primary family (text-primary / bg-primary / etc.)
| Broken token | Valid replacement |
|---|---|
| `text-primary` | `text-accent` |
| `bg-primary` | `bg-accent` |
| `bg-primary/NN` | `bg-accent/NN` |
| `border-primary` | `border-accent` |
| `border-primary/NN` | `border-accent/NN` |
| `ring-primary` | `ring-accent` |
| `text-primary-foreground` | `text-on-accent` |
| `bg-primary text-primary-foreground` | `bg-accent text-on-accent` |
| `bg-primary text-white` | `bg-accent text-on-accent` |

### non-standard text tokens (only in 5 specific files)
| Broken token | Valid replacement |
|---|---|
| `text-text-primary` | `text-heading` |
| `text-content-primary` | `text-heading` |
| `text-content-secondary` | `text-body` |
| `text-content-tertiary` | `text-muted` |
| `text-content-muted` | `text-muted` |

### Determining bg-surface → bg-input vs bg-card
Look at the surrounding JSX. If the element is:
- `<input`, `<select`, `<textarea` — always `bg-input`
- `className="... border border-edge ..."` AND the element functions as a search/filter/form field → `bg-input`
- A `<div>` or `<span>` displaying data (stat card, list row, pill, timeline item, code block) → `bg-card`
- A progress bar track (`<div className="... rounded-full h-2">`) → `bg-card` (track background)
- A dropdown list container (`<div ... shadow-lg>`) → `bg-card`

---

## File Structure (what changes)

**Modified only** — no new files created.

### Group 1 — change-governance (19 files, 50 occurrences)
- `src/frontend/src/features/change-governance/components/DeployReadinessPanel.tsx`
- `src/frontend/src/features/change-governance/components/EnvironmentPromotionPathPanel.tsx`
- `src/frontend/src/features/change-governance/components/ReleaseSelector.tsx`
- `src/frontend/src/features/change-governance/components/RiskScoreTrendPanel.tsx`
- `src/frontend/src/features/change-governance/pages/ChangeDetailPage.tsx`
- `src/frontend/src/features/change-governance/pages/PostReleaseReviewPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseApprovalGatewayPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseApprovalPoliciesPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseCalendarPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseChecklistExecutionPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseCommitPoolPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseControlParametersPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseGatesDashboardPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseImpactReportPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseNotesPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseParameterAuditPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseParameterEnvironmentOverridePage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseRollbackPage.tsx`
- `src/frontend/src/features/change-governance/pages/ReleaseTrainPage.tsx`

### Group 2 — governance (23 files, 40 occurrences)
- All files in `src/frontend/src/features/governance/pages/` with broken tokens (16 files)
- All files in `src/frontend/src/features/governance/pages/persona-suites/` (4 files)

### Group 3 — contracts (1 file, 7 occurrences)
- `src/frontend/src/features/contracts/publication/PublicationCenterPage.tsx`

### Group 4 — platform-admin (7 files, 16 occurrences)
- Files in `src/frontend/src/features/platform-admin/pages/` with broken tokens

### Group 5 — knowledge (4 files, 60 occurrences)
- `src/frontend/src/features/knowledge/pages/KnowledgeDocumentPage.tsx`
- `src/frontend/src/features/knowledge/pages/KnowledgeHubPage.tsx`
- `src/frontend/src/features/knowledge/pages/OperationalNotesPage.tsx`
- `src/frontend/src/features/knowledge/pages/ServiceTimelinePage.tsx`

### Group 6 — ai-hub (9 files, 31 occurrences)
- `src/frontend/src/features/ai-hub/pages/AiAuditPage.tsx`
- `src/frontend/src/features/ai-hub/pages/AiMemoryIntelligencePage.tsx`
- `src/frontend/src/features/ai-hub/pages/AiPoliciesPage.tsx`
- `src/frontend/src/features/ai-hub/pages/FeatureModelBindingsPage.tsx`
- `src/frontend/src/features/ai-hub/pages/IdeIntegrationsPage.tsx`
- `src/frontend/src/features/ai-hub/pages/McpServerPage.tsx`
- `src/frontend/src/features/ai-hub/pages/TokenBudgetPage.tsx`
- `src/frontend/src/features/ai-hub/pages/UserModelPoliciesPage.tsx`
- `src/frontend/src/features/ai-hub/pages/UserTokenQuotasPage.tsx`

### Group 7 — remaining feature dirs (configuration, integrations, identity-access, operations)
- `src/frontend/src/features/configuration/pages/ConfigurationAdminPage.tsx` (2 occurrences)
- `src/frontend/src/features/integrations/pages/WebhookSubscriptionsPage.tsx` (3 occurrences)
- `src/frontend/src/features/identity-access/pages/OnboardingWizardPage.tsx` (4 occurrences)
- All operations pages with broken tokens (~20 files, 49 occurrences)

---

## Task 1: Setup — verify branch and baseline grep

**Files:** read-only audit

- [ ] **Step 1.1: Confirm you are on the right branch**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne
git branch --show-current
# Expected: redesign/betterstack-foundation
```

- [ ] **Step 1.2: Run baseline grep to capture the full count**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|bg-primary/\|border-primary\b\|border-primary/\|ring-primary\b\|text-primary-foreground\|text-text-primary\|text-content-primary\|text-content-secondary\|text-content-tertiary\|text-content-muted" src/features/ --include="*.tsx" --include="*.ts" | wc -l
# Save this number — target is 0 after all tasks complete.
# Baseline is approximately 325 occurrences across 86 files.
```

---

## Task 2: Group 1 — change-governance

**Files:** 19 files in `src/frontend/src/features/change-governance/`

- [ ] **Step 2.1: Audit the module**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|bg-primary/\|border-primary\b\|border-primary/\|ring-primary\b\|text-primary-foreground" src/features/change-governance/ --include="*.tsx"
# Review output: change-governance has only bg-surface variants (no text-primary/bg-primary occurrences here).
```

- [ ] **Step 2.2: Apply replacements in change-governance components and pages**

Open each file listed below. For every match, apply the mapping from the Token Mapping Reference table at the top of this plan.

**`src/frontend/src/features/change-governance/components/DeployReadinessPanel.tsx`**
- Line ~104: `bg-surface border border-edge` → `bg-card border border-edge` (list item, not an input)

**`src/frontend/src/features/change-governance/components/EnvironmentPromotionPathPanel.tsx`**
- Line ~43: `bg-surface` in a `<div>` circle → `bg-card`

**`src/frontend/src/features/change-governance/components/ReleaseSelector.tsx`**
- Line ~67: `bg-surface shadow-lg` dropdown list → `bg-card shadow-lg`

**`src/frontend/src/features/change-governance/components/RiskScoreTrendPanel.tsx`**
- Line ~38: `bg-surface rounded-full h-1.5` — progress bar track → `bg-card`
- Line ~252: `hover:bg-surface/50` — row hover → `hover:bg-card/50`

**`src/frontend/src/features/change-governance/pages/ChangeDetailPage.tsx`**
- Lines ~565, ~579: `bg-surface border border-edge` on `<textarea>` elements → `bg-input border border-edge`
- Line ~756: `bg-surface border border-edge` on a link-styled `<button>` → `bg-card border border-edge`

**`src/frontend/src/features/change-governance/pages/PostReleaseReviewPage.tsx`**
- Lines ~182, ~188, ~194, ~200, ~211: `bg-surface rounded-lg` stat blocks → `bg-card rounded-lg`
- Line ~257: `hover:bg-surface transition-colors` on `<tr>` → `hover:bg-hover transition-colors`
- Line ~306: `bg-surface rounded-lg` → `bg-card rounded-lg`

**`src/frontend/src/features/change-governance/pages/ReleaseApprovalGatewayPage.tsx`**
- Line ~223: `bg-surface border border-edge` list item → `bg-card border border-edge`

**`src/frontend/src/features/change-governance/pages/ReleaseApprovalPoliciesPage.tsx`**
- Line ~294: `bg-surface p-4` card → `bg-card p-4`

**`src/frontend/src/features/change-governance/pages/ReleaseCalendarPage.tsx`**
- Line ~539: `bg-surface border border-edge/50` item → `bg-card border border-edge/50`
- Line ~763: `hover:bg-surface` button → `hover:bg-hover`

**`src/frontend/src/features/change-governance/pages/ReleaseChecklistExecutionPage.tsx`**
- Line ~274: `bg-surface rounded-full h-2` progress track → `bg-card rounded-full h-2`

**`src/frontend/src/features/change-governance/pages/ReleaseCommitPoolPage.tsx`**
- Lines ~168, ~294: `bg-surface border border-edge` list items → `bg-card border border-edge`

**`src/frontend/src/features/change-governance/pages/ReleaseControlParametersPage.tsx`**
- Line ~128: `bg-surface border border-edge p-4` → `bg-card border border-edge p-4`

**`src/frontend/src/features/change-governance/pages/ReleaseGatesDashboardPage.tsx`**
- Line ~91: `hover:bg-surface` in tab style string → `hover:bg-hover`

**`src/frontend/src/features/change-governance/pages/ReleaseImpactReportPage.tsx`**
- Lines ~104, ~110, ~116, ~122: `bg-surface` stat blocks → `bg-card`

**`src/frontend/src/features/change-governance/pages/ReleaseNotesPage.tsx`**
- Lines ~216, ~223, ~230, ~237: `bg-surface rounded-lg` metric blocks → `bg-card rounded-lg`
- Line ~280: `bg-surface rounded-lg p-4 border border-edge` on `<pre>` → `bg-card rounded-lg p-4 border border-edge`

**`src/frontend/src/features/change-governance/pages/ReleaseParameterAuditPage.tsx`**
- Line ~90: `bg-surface px-4 py-2` button → `bg-card px-4 py-2`
- Line ~121: `bg-surface border border-edge text-muted` tab inactive style → `bg-card border border-edge text-muted`
- Line ~163: `hover:bg-surface/50` row → `hover:bg-card/50`

**`src/frontend/src/features/change-governance/pages/ReleaseParameterEnvironmentOverridePage.tsx`**
- Line ~258: `hover:bg-surface/50` row → `hover:bg-card/50`
- Line ~276: `hover:bg-surface` icon button → `hover:bg-hover`

**`src/frontend/src/features/change-governance/pages/ReleaseRollbackPage.tsx`**
- Lines ~292, ~299, ~313, ~322, ~331: `bg-surface rounded-lg` blocks → `bg-card rounded-lg`

**`src/frontend/src/features/change-governance/pages/ReleaseTrainPage.tsx`**
- Line ~91: `hover:bg-surface/50` `<tr>` → `hover:bg-card/50`
- Lines ~221, ~240, ~246: `bg-surface rounded-md` blocks → `bg-card rounded-md`

- [ ] **Step 2.3: Verify zero broken tokens remain in change-governance**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|bg-primary/\|border-primary\b\|border-primary/\|ring-primary\b\|text-primary-foreground" src/features/change-governance/ --include="*.tsx"
# Expected: no output (zero matches)
```

- [ ] **Step 2.4: Build**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
npm run build 2>&1 | tail -5
# Expected: ✓ built in Xs  (no errors)
```

- [ ] **Step 2.5: Commit**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne
git add src/frontend/src/features/change-governance/components/DeployReadinessPanel.tsx \
        src/frontend/src/features/change-governance/components/EnvironmentPromotionPathPanel.tsx \
        src/frontend/src/features/change-governance/components/ReleaseSelector.tsx \
        src/frontend/src/features/change-governance/components/RiskScoreTrendPanel.tsx \
        src/frontend/src/features/change-governance/pages/ChangeDetailPage.tsx \
        src/frontend/src/features/change-governance/pages/PostReleaseReviewPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseApprovalGatewayPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseApprovalPoliciesPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseCalendarPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseChecklistExecutionPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseCommitPoolPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseControlParametersPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseGatesDashboardPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseImpactReportPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseNotesPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseParameterAuditPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseParameterEnvironmentOverridePage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseRollbackPage.tsx \
        src/frontend/src/features/change-governance/pages/ReleaseTrainPage.tsx
git commit -m "fix(change-governance): tokens quebrados surface/primary → tokens válidos"
```

---

## Task 3: Group 2 — governance

**Files:** ~23 files in `src/frontend/src/features/governance/`

- [ ] **Step 3.1: Audit the module**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|bg-primary/\|border-primary\b\|border-primary/\|ring-primary\b\|text-primary-foreground" src/features/governance/ --include="*.tsx"
```

- [ ] **Step 3.2: Apply replacements in governance pages**

**`src/frontend/src/features/governance/pages/FinOpsBudgetApprovalsPage.tsx`**
- Line ~138: `bg-surface-hover rounded` info block → `bg-hover rounded`

**`src/frontend/src/features/governance/pages/FinOpsConfigurationPage.tsx`**
- Line ~215: `bg-surface-hover rounded-md` tag badge → `bg-hover rounded-md`
- Line ~322: `bg-surface-hover rounded p-2` `<pre>` code block → `bg-hover rounded p-2`

**`src/frontend/src/features/governance/pages/GovernanceGatesPage.tsx`**
- Lines ~190, ~236, ~274: `bg-surface-secondary` detail panels → `bg-subtle`

**`src/frontend/src/features/governance/pages/IdeExtensionsConsolePage.tsx`**
- Line ~47: `text-primary` on icon `className` → `text-accent`

**`src/frontend/src/features/governance/pages/WarRoomPage.tsx`**
- Line ~89: `bg-surface-elevated` header block → `bg-elevated`
- Line ~124: `bg-surface-elevated` comment card → `bg-elevated`

**`src/frontend/src/features/governance/pages/GovernancePacksOverviewPage.tsx`** (3 occurrences — audit to confirm exact lines)
- `bg-surface` occurrences → `bg-card`

**`src/frontend/src/features/governance/pages/PolicyCatalogPage.tsx`** (3 occurrences)
- `bg-surface` occurrences → `bg-card` (stat/card blocks, not inputs)

**`src/frontend/src/features/governance/pages/MaturityScorecardsPage.tsx`** (3 occurrences)
- `bg-surface` occurrences → `bg-card`

**`src/frontend/src/features/governance/pages/CompliancePage.tsx`** (3 occurrences)
- `bg-surface` occurrences → `bg-card`

**`src/frontend/src/features/governance/pages/RiskCenterPage.tsx`** (2 occurrences)
- `bg-surface` → `bg-card`

**`src/frontend/src/features/governance/pages/ExecutiveDrillDownPage.tsx`** (2 occurrences)
- `bg-surface` → `bg-card`

**`src/frontend/src/features/governance/pages/EvidencePackagesPage.tsx`** (2 occurrences)
- `bg-surface` → `bg-card`

**`src/frontend/src/features/governance/pages/EnterpriseControlsPage.tsx`** (2 occurrences)
- `bg-surface` → `bg-card`

**`src/frontend/src/features/governance/pages/WaiversPage.tsx`** (2 occurrences)
- `bg-surface` → `bg-card`

**`src/frontend/src/features/governance/pages/PluginMarketplacePage.tsx`**
- Line ~192: `bg-primary text-white` button active state → `bg-accent text-on-accent`

**`src/frontend/src/features/governance/pages/ReportsPage.tsx`** (1 occurrence)
- `bg-surface` → `bg-card`

**`src/frontend/src/features/governance/pages/RiskHeatmapPage.tsx`** (1 occurrence)
- `bg-surface` → `bg-card`

**`src/frontend/src/features/governance/pages/ExecutiveOverviewPage.tsx`** (1 occurrence)
- `bg-surface` → `bg-card`

**`src/frontend/src/features/governance/pages/BenchmarkingPage.tsx`** (1 occurrence)
- `bg-surface` → `bg-card`

**`src/frontend/src/features/governance/pages/FinOpsBudgetApprovalsPage.tsx`** — already done above.

**`src/frontend/src/features/governance/pages/persona-suites/ArchitectLandscapePage.tsx`**
- Line ~15: `color: 'text-primary'` in a data array string literal → `'text-accent'`

**`src/frontend/src/features/governance/pages/persona-suites/AuditorConsolePage.tsx`**
- Line ~84: `color: 'text-primary'` in data array → `'text-accent'`

**`src/frontend/src/features/governance/pages/persona-suites/PlatformAdminCockpitPage.tsx`**
- Line ~35: `color: 'text-primary'` → `'text-accent'`

**`src/frontend/src/features/governance/pages/persona-suites/TechLeadCommandCenterPage.tsx`**
- Line ~70: `color: 'text-primary'` → `'text-accent'`

**For remaining governance pages** with only `bg-surface` occurrences (those you find in step 3.1 audit not listed above): apply `bg-surface` → `bg-card` uniformly — these are always stat/card blocks, not form inputs.

- [ ] **Step 3.3: Verify zero broken tokens remain in governance**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|bg-primary/\|border-primary\b\|border-primary/\|ring-primary\b\|text-primary-foreground" src/features/governance/ --include="*.tsx"
# Expected: no output
```

- [ ] **Step 3.4: Build**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
npm run build 2>&1 | tail -5
# Expected: ✓ built in Xs
```

- [ ] **Step 3.5: Commit — stage all changed files in governance explicitly**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne
# Stage all changed files in governance (there are ~23 — use directory add but verify scope):
git diff --name-only src/frontend/src/features/governance/
# Then stage exactly those files:
git add $(git diff --name-only src/frontend/src/features/governance/)
git commit -m "fix(governance): tokens quebrados surface/primary → tokens válidos"
```

---

## Task 4: Group 3 — contracts

**Files:** 1 file — `src/frontend/src/features/contracts/publication/PublicationCenterPage.tsx`

- [ ] **Step 4.1: Audit**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -n "text-primary\b\|bg-primary\b\|border-primary\b\|ring-primary\b\|bg-surface" src/features/contracts/publication/PublicationCenterPage.tsx
```

- [ ] **Step 4.2: Apply replacements**

All 7 occurrences are `text-primary` (headings, table cells, link-style buttons):
- Line ~57: `text-primary` heading → `text-accent`
- Line ~88: `hover:text-primary` in tab inactive class → `hover:text-accent`
- Line ~144: `text-primary` table cell → `text-accent`
- Line ~156: `hover:text-primary` link → `hover:text-accent`
- Line ~175: `text-primary` panel heading → `text-accent`
- Line ~197: `text-primary` on `<textarea>` (it is a text color, not background) → `text-accent`
- Line ~203: `hover:text-primary` button → `hover:text-accent`

Note: Line 197 has `bg-bg` — that is NOT in scope; do not change it.

- [ ] **Step 4.3: Verify**

```bash
grep -n "text-primary\|bg-primary\|border-primary\|bg-surface" src/features/contracts/publication/PublicationCenterPage.tsx
# Expected: no output
```

- [ ] **Step 4.4: Build**

```bash
npm run build 2>&1 | tail -5
```

- [ ] **Step 4.5: Commit**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne
git add src/frontend/src/features/contracts/publication/PublicationCenterPage.tsx
git commit -m "fix(contracts): tokens quebrados surface/primary → tokens válidos"
```

---

## Task 5: Group 4 — platform-admin

**Files:** ~7 files in `src/frontend/src/features/platform-admin/pages/`

- [ ] **Step 5.1: Audit**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|border-primary\b\|ring-primary\b\|text-primary-foreground" src/features/platform-admin/ --include="*.tsx"
```

- [ ] **Step 5.2: Apply replacements**

From the baseline audit:
- `SetupWizardPage.tsx` (4): `bg-surface` stat/card blocks → `bg-card`
- `PlatformHealthDashboardPage.tsx` (3): `bg-surface` → `bg-card`
- `BackupCoordinatorPage.tsx` (3): `bg-surface` → `bg-card`
- `StartupReportPage.tsx` (2): `bg-surface` → `bg-card`
- `ResourceBudgetPage.tsx` (2): `bg-surface` → `bg-card`
- `SupportBundlePage.tsx` (1): `bg-surface` → `bg-card`
- `PreflightPage.tsx` (1): `bg-surface` → `bg-card`

For each: read the file, confirm `bg-surface` is on a card/stat block (not an `<input>`), apply `bg-card`.

- [ ] **Step 5.3: Verify**

```bash
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|border-primary\b\|ring-primary\b" src/features/platform-admin/ --include="*.tsx"
# Expected: no output
```

- [ ] **Step 5.4: Build**

```bash
npm run build 2>&1 | tail -5
```

- [ ] **Step 5.5: Commit**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne
git add $(git diff --name-only src/frontend/src/features/platform-admin/)
git commit -m "fix(platform-admin): tokens quebrados surface/primary → tokens válidos"
```

---

## Task 6: Group 5 — knowledge

**Files:** 4 files — heavy use of non-standard token families (`bg-surface-raised`, `text-content-*`, `bg-surface-hover`)

- [ ] **Step 6.1: Audit**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|text-text-primary\|text-content-primary\|text-content-secondary\|text-content-tertiary\|text-content-muted" src/features/knowledge/ --include="*.tsx"
```

- [ ] **Step 6.2: Apply replacements in KnowledgeHubPage.tsx**

**`src/frontend/src/features/knowledge/pages/KnowledgeHubPage.tsx`** (~5 occurrences):
- Line ~150: `bg-surface-raised border border-edge` on search `<input>` → `bg-input border border-edge`
- Line ~161: `bg-surface-raised text-content-secondary hover:bg-surface-hover` tab inactive → `bg-elevated text-body hover:bg-hover`
- Line ~173: `bg-surface-raised text-content-secondary hover:bg-surface-hover` → `bg-elevated text-body hover:bg-hover`
- Line ~215: `bg-surface-raised text-content-secondary` icon badge → `bg-elevated text-body`
- Line ~229: `bg-surface-raised` tag span → `bg-elevated`

Also fix `text-content-primary` and `text-content-tertiary` in this file:
- `text-content-primary` → `text-heading`
- `text-content-tertiary` → `text-muted`

- [ ] **Step 6.3: Apply replacements in OperationalNotesPage.tsx**

**`src/frontend/src/features/knowledge/pages/OperationalNotesPage.tsx`** (~8 occurrences):
- Lines ~76, ~88, ~102, ~112, ~122: `bg-surface-raised text-content-secondary hover:bg-surface-hover` tab/filter buttons → `bg-elevated text-body hover:bg-hover`
- Line ~161: `bg-surface-raised` tag → `bg-elevated`
- Lines ~200, ~208: `bg-surface-raised border border-edge text-content-secondary hover:text-content-primary` pagination buttons → `bg-elevated border border-edge text-body hover:text-heading`

Also fix any `text-content-tertiary` → `text-muted` in this file.

- [ ] **Step 6.4: Apply replacements in KnowledgeDocumentPage.tsx**

**`src/frontend/src/features/knowledge/pages/KnowledgeDocumentPage.tsx`** (~1 occurrence):
- Line ~150: `bg-surface-raised` tag badge → `bg-elevated`

Also fix `text-content-secondary` → `text-body` and `text-content-primary` → `text-heading` where present.

- [ ] **Step 6.5: Apply replacements in ServiceTimelinePage.tsx**

**`src/frontend/src/features/knowledge/pages/ServiceTimelinePage.tsx`** (~2 occurrences):
- Line ~86: `bg-surface-raised` tag → `bg-elevated`

Also fix any `text-content-*` tokens.

- [ ] **Step 6.6: Verify**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|text-text-primary\|text-content-primary\|text-content-secondary\|text-content-tertiary\|text-content-muted" src/features/knowledge/ --include="*.tsx"
# Expected: no output
```

- [ ] **Step 6.7: Build**

```bash
npm run build 2>&1 | tail -5
```

- [ ] **Step 6.8: Commit**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne
git add src/frontend/src/features/knowledge/pages/KnowledgeDocumentPage.tsx \
        src/frontend/src/features/knowledge/pages/KnowledgeHubPage.tsx \
        src/frontend/src/features/knowledge/pages/OperationalNotesPage.tsx \
        src/frontend/src/features/knowledge/pages/ServiceTimelinePage.tsx
git commit -m "fix(knowledge): tokens quebrados surface/primary → tokens válidos"
```

---

## Task 7: Group 6 — ai-hub

**Files:** 9 files in `src/frontend/src/features/ai-hub/pages/`

- [ ] **Step 7.1: Audit**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|border-primary\b\|ring-primary\b\|text-primary-foreground\|text-text-primary\|text-content-secondary" src/features/ai-hub/ --include="*.tsx"
```

- [ ] **Step 7.2: Apply replacements**

**`AiAuditPage.tsx`** (1 occurrence):
- Line ~161: `bg-surface border border-edge` on `<input>` search field → `bg-input border border-edge`

**`AiMemoryIntelligencePage.tsx`** (2 occurrences):
- Line ~285: `bg-surface` on a display `<div>` with `border border-edge` → `bg-card` (it's a tier tag, not an input)
- Line ~422: `bg-surface` progress bar track `<div>` → `bg-card`

**`AiPoliciesPage.tsx`** (1 occurrence):
- Line ~119: `bg-surface border border-edge` on `<input>` search → `bg-input border border-edge`

**`FeatureModelBindingsPage.tsx`** (2 occurrences):
- Lines ~328, ~335: `hover:bg-surface-hover` icon buttons → `hover:bg-hover`

**`IdeIntegrationsPage.tsx`** (1 occurrence):
- Line ~355: `bg-surface border border-edge` on `<input>` search → `bg-input border border-edge`

**`McpServerPage.tsx`** (1 occurrence + content-secondary tokens):
- Line ~207: `bg-surface-elevated rounded-md` block → `bg-elevated rounded-md`
- Any `text-content-secondary` → `text-body`

**`TokenBudgetPage.tsx`** (1 occurrence):
- Line ~124: `bg-surface border border-edge` on `<input>` → `bg-input border border-edge`

**`UserModelPoliciesPage.tsx`** (2 occurrences):
- Lines ~295, ~302: `hover:bg-surface-hover` icon buttons → `hover:bg-hover`

**`UserTokenQuotasPage.tsx`** (2 occurrences):
- Lines ~342, ~349: `hover:bg-surface-hover` icon buttons → `hover:bg-hover`

- [ ] **Step 7.3: Verify**

```bash
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|border-primary\b\|ring-primary\b\|text-primary-foreground\|text-content-secondary" src/features/ai-hub/ --include="*.tsx"
# Expected: no output
```

- [ ] **Step 7.4: Build**

```bash
npm run build 2>&1 | tail -5
```

- [ ] **Step 7.5: Commit**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne
git add src/frontend/src/features/ai-hub/pages/AiAuditPage.tsx \
        src/frontend/src/features/ai-hub/pages/AiMemoryIntelligencePage.tsx \
        src/frontend/src/features/ai-hub/pages/AiPoliciesPage.tsx \
        src/frontend/src/features/ai-hub/pages/FeatureModelBindingsPage.tsx \
        src/frontend/src/features/ai-hub/pages/IdeIntegrationsPage.tsx \
        src/frontend/src/features/ai-hub/pages/McpServerPage.tsx \
        src/frontend/src/features/ai-hub/pages/TokenBudgetPage.tsx \
        src/frontend/src/features/ai-hub/pages/UserModelPoliciesPage.tsx \
        src/frontend/src/features/ai-hub/pages/UserTokenQuotasPage.tsx
git commit -m "fix(ai-hub): tokens quebrados surface/primary → tokens válidos"
```

---

## Task 8: Group 7 — remaining modules (configuration, integrations, identity-access, operations)

**Files:** ~24 files total

- [ ] **Step 8.1: Audit all remaining modules**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|border-primary\b\|ring-primary\b\|text-primary-foreground\|text-text-primary\|text-content-primary\|text-content-secondary\|text-content-tertiary\|text-content-muted" \
  src/features/configuration/ \
  src/features/integrations/ \
  src/features/identity-access/ \
  src/features/operations/ \
  --include="*.tsx"
```

- [ ] **Step 8.2: Apply replacements in configuration**

**`src/frontend/src/features/configuration/pages/ConfigurationAdminPage.tsx`** (2 occurrences):
- Both are `bg-surface` on stat/card blocks → `bg-card`

- [ ] **Step 8.3: Apply replacements in integrations**

**`src/frontend/src/features/integrations/pages/WebhookSubscriptionsPage.tsx`** (3 occurrences):
- Lines ~160, ~190, ~333: `text-primary` headings/labels → `text-accent`

- [ ] **Step 8.4: Apply replacements in identity-access**

**`src/frontend/src/features/identity-access/pages/OnboardingWizardPage.tsx`** (4 occurrences):
- Line ~175: `text-primary` spinner icon → `text-accent`
- Line ~229: `border-primary shadow-md` active step border → `border-accent shadow-md`
- Line ~239: `bg-primary text-primary-foreground` step badge active → `bg-accent text-on-accent`
- Line ~253: `bg-primary text-primary-foreground` step label badge → `bg-accent text-on-accent`

- [ ] **Step 8.5: Apply replacements in operations (~20 files)**

The operations module has 49 occurrences, mostly:
1. `bg-primary text-primary-foreground` on selected tab buttons (AiAnomalyPage, AiIncidentSummarizerPage, AiRunbookSuggesterPage, ApiRegressionPage, DbExplorerPage, DependencyRiskPage, and more)
2. `text-text-primary` non-standard text tokens (EnvironmentComparisonPage)
3. `bg-surface` card blocks (SreDashboardPage, EnvironmentComparisonPage, etc.)

For each file found in step 8.1 audit in operations:

**Tab-button pattern** (most common in operations): `bg-primary text-primary-foreground` → `bg-accent text-on-accent`

Example from `AiAnomalyPage.tsx`:
```tsx
// Before:
className={`px-3 py-1.5 transition-colors ${timeRange === opt.value ? 'bg-primary text-primary-foreground font-semibold' : 'hover:bg-muted text-muted'}`}

// After:
className={`px-3 py-1.5 transition-colors ${timeRange === opt.value ? 'bg-accent text-on-accent font-semibold' : 'hover:bg-muted text-muted'}`}
```

Apply this pattern to all files matching this tab-button idiom.

**`EnvironmentComparisonPage.tsx`** — `text-text-primary` → `text-heading` throughout.

**`SreDashboardPage.tsx`** (10 occurrences) — audit to confirm all `bg-surface` uses then apply `bg-card`.

**All other operations files** with only `bg-surface` → apply `bg-card` (these are stat blocks, list items, card containers — none are form inputs based on the page names).

- [ ] **Step 8.6: Verify all remaining modules**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|border-primary\b\|ring-primary\b\|text-primary-foreground\|text-text-primary\|text-content-primary\|text-content-secondary\|text-content-tertiary\|text-content-muted" \
  src/features/configuration/ \
  src/features/integrations/ \
  src/features/identity-access/ \
  src/features/operations/ \
  --include="*.tsx"
# Expected: no output
```

- [ ] **Step 8.7: Build**

```bash
npm run build 2>&1 | tail -5
```

- [ ] **Step 8.8: Commit**

```bash
cd C:/Users/dlima/Documents\GitHub\NexTraceOne
git add $(git diff --name-only src/frontend/src/features/configuration/ src/frontend/src/features/integrations/ src/frontend/src/features/identity-access/ src/frontend/src/features/operations/)
git commit -m "fix(operations,integrations,identity-access,configuration): tokens quebrados surface/primary → tokens válidos"
```

---

## Task 9: Final verification — zero broken tokens across all features

- [ ] **Step 9.1: Re-grep all features for broken tokens**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
grep -rn "bg-surface\b\|bg-surface-\|text-primary\b\|bg-primary\b\|bg-primary/\|border-primary\b\|border-primary/\|ring-primary\b\|text-primary-foreground\|text-text-primary\|text-content-primary\|text-content-secondary\|text-content-tertiary\|text-content-muted" \
  src/features/ \
  --include="*.tsx" --include="*.ts"
# MUST be zero output. If any matches remain, fix them now before the final build.
```

- [ ] **Step 9.2: Final full build**

```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend
npm run build
# Expected: ✓ built in Xs (no type errors, no Vite build errors)
```

- [ ] **Step 9.3: Report counts**

Record the count of replacements per module from the commit diffs:
```bash
cd C:/Users/dlima/Documents/GitHub/NexTraceOne
git log --oneline -10
# Confirm 7 commits from this plan are present (one per group + no stray changes)
```

---

## Self-Review Checklist

1. **Spec coverage:**
   - bg-surface → bg-input (form controls): covered in ChangeDetailPage (textarea), search inputs (ai-hub), KnowledgeHubPage search, ConfigurationAdminPage
   - bg-surface → bg-card (everything else): covered uniformly
   - bg-surface-hover / hover:bg-surface-hover → bg-hover / hover:bg-hover: covered (ai-hub, governance, change-governance)
   - bg-surface-raised → bg-elevated: covered (knowledge)
   - bg-surface-elevated → bg-elevated: covered (ai-hub McpServerPage, governance WarRoomPage)
   - bg-surface-secondary / bg-surface-muted → bg-subtle: covered (governance GovernanceGatesPage)
   - bg-surface/50 → bg-card/50: covered (change-governance row hovers)
   - bg-surface/80 → bg-card/80: covered (ReleaseParameterAuditPage)
   - text-primary → text-accent: covered (contracts, governance, integrations, identity-access)
   - bg-primary → bg-accent: covered (operations tab buttons, governance PluginMarketplace, identity-access)
   - bg-primary text-primary-foreground → bg-accent text-on-accent: covered (operations, identity-access)
   - bg-primary text-white → bg-accent text-on-accent: covered (governance PluginMarketplace)
   - text-primary-foreground → text-on-accent: covered as part of bg-primary pairs
   - border-primary → border-accent: covered (identity-access OnboardingWizardPage)
   - ring-primary → ring-accent: zero found in baseline grep — no action needed
   - text-text-primary → text-heading: covered (operations EnvironmentComparisonPage)
   - text-content-primary → text-heading: covered (knowledge)
   - text-content-secondary → text-body: covered (knowledge, ai-hub McpServerPage)
   - text-content-tertiary → text-muted: covered (knowledge)
   - text-content-muted → text-muted: covered (knowledge OperationalNotesPage)
   - catalog, notifications, shared — zero occurrences confirmed; no action needed

2. **Placeholder scan:** All steps contain explicit token strings and grep commands. No TBD or TODO language.

3. **Type consistency:** No type signatures changed — this is className string substitution only.

---

## Notes for Executor

- Never use `git add -A` or `git add .` — always add specific files or use `git diff --name-only <path>` to get the list
- `bg-surface-2` was found zero times in features (only `bg-surface/50`, `bg-surface/80`)
- `hover:bg-surface` (without suffix) appears in change-governance (ReleaseCalendarPage, ReleaseGatesDashboard) — map to `hover:bg-hover`
- `bg-surface/50` in RiskScoreTrendPanel and row hover contexts → `bg-card/50` (not `bg-hover/50` because the hover is already encoded in adjacent `transition-colors`, these are conditional class strings, not hover-prefixed utilities)
- `text-primary` in `color:` string literal props inside JS arrays (persona-suites) → still replace with `'text-accent'`; these are passed as Tailwind className strings at runtime
- Do NOT change `bg-bg` on line 197 of PublicationCenterPage — that is not in scope
- Do NOT change any `bg-cyan`, `text-cyan`, `border-cyan` — out of scope per instructions
- If `npm run build` fails with a TypeScript error unrelated to token changes, note it but do not fix it — only fix what is caused by this plan's edits

# Contract Workspace v5 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. UI tasks should also invoke `frontend-design:frontend-design`.

**Goal:** Restyle `ContractWorkspacePage` to the v5 idiom — a sticky `ContractWorkspaceIdentityCard` first column, two-tier (group→section) tabs, a slim governance rail, and a DS `PageHeader` with DS `Button` actions — keeping the 16-section IA and all behavior.

**Architecture:** Presentation-only. New presentational components (identity card, two-tier tabs, shared identity primitives) compose the existing `StudioContract` view-model. `WorkspaceLayout` becomes a dumb 3-column grid shell; `ContractWorkspacePage` owns `activeSection` state and renders the existing 16 section components unchanged. The right `StudioRail` is slimmed (Status/Owners move into the card; transitions move into the header). No backend/API/hook changes. Monaco/visual-builder internals untouched.

**Tech Stack:** React 19, TypeScript 5.9, TanStack Query 5, react-router-dom 7, Tailwind 4, DS primitives from `src/frontend/src/shared/ui` + `src/frontend/src/components/PageHeader`, Vitest + Testing Library, i18next (en/es/pt-BR/pt-PT).

---

## Conventions

- Paths relative to repo root. Frontend root: `src/frontend/`. Run tests from `src/frontend/`: `npx vitest run <path>`. Lint: `npm run lint` (errors fail; warnings OK).
- Branch `redesign/betterstack-contract-workspace` is checked out. Commit after each task.
- **Do not touch** `workspace/sections/ContractSection.tsx` *logic* or `workspace/builders/*` or `workspace/editor/*` internals (Monaco/visual builders = sub-projects C/D). Section polish (Task 7) is chrome-only.
- DS imports: `Button, Badge` from `../../../shared/ui` (depth-adjust per file); `PageHeader` from `../../../components/PageHeader`.

## File structure

| File | Responsibility |
|---|---|
| `features/contracts/shared/components/identityCardPrimitives.tsx` | **Create.** `IdentityMiniStat`, `IdentityMetaRow` — tiny shared presentational helpers. |
| `features/contracts/create/ContractIdentityCard.tsx` | **Modify.** Use the shared primitives (replace local `MiniStat`/`MetaRow`). |
| `features/contracts/workspace/components/ContractWorkspaceIdentityCard.tsx` | **Create.** Sticky left card from `StudioContract`. |
| `features/contracts/workspace/components/WorkspaceTabs.tsx` | **Create.** Two-tier group→section tab nav. |
| `features/contracts/workspace/components/StudioRail.tsx` | **Modify.** Slim: keep Risks/Activity/Locked; remove Status/Owners/Approvals/Policies/Transitions. |
| `features/contracts/workspace/WorkspaceLayout.tsx` | **Rewrite.** Dumb 3-column grid shell (no section state). |
| `features/contracts/workspace/ContractWorkspacePage.tsx` | **Modify.** PageHeader + DS actions + identity card + WorkspaceTabs + slim rail; owns `activeSection`. |
| `__tests__/pages/ContractWorkspacePage.test.tsx` | **Modify.** Populated `StudioContract`; structural + tab-nav coverage. |
| `workspace/sections/*` (selected) | **Modify (Task 7).** DS-swap remaining raw controls + DS state components + token tighten. |

---

## Task 1: Shared identity-card primitives

**Files:**
- Create: `src/frontend/src/features/contracts/shared/components/identityCardPrimitives.tsx`
- Modify: `src/frontend/src/features/contracts/create/ContractIdentityCard.tsx`
- Test: `src/frontend/src/__tests__/contracts/identityCardPrimitives.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/identityCardPrimitives.test.tsx
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { IdentityMiniStat, IdentityMetaRow } from '../../features/contracts/shared/components/identityCardPrimitives';

describe('identityCardPrimitives', () => {
  it('IdentityMiniStat shows value and label', () => {
    render(<IdentityMiniStat value="3/3" label="Approvals" />);
    expect(screen.getByText('3/3')).toBeInTheDocument();
    expect(screen.getByText('Approvals')).toBeInTheDocument();
  });
  it('IdentityMetaRow shows label and value', () => {
    render(<IdentityMetaRow label="Owner" value="@ana" />);
    expect(screen.getByText('Owner')).toBeInTheDocument();
    expect(screen.getByText('@ana')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/identityCardPrimitives.test.tsx`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement the primitives**

```tsx
// src/frontend/src/features/contracts/shared/components/identityCardPrimitives.tsx
import { cn } from '../../../../lib/cn';

/** Coluna de mini-estatística no topo de um identity card (ex.: Approvals 3/3). */
export function IdentityMiniStat({ value, label, mono, muted }: { value: string; label: string; mono?: boolean; muted?: boolean }) {
  return (
    <div className="bg-deep text-center py-3">
      <p className={cn('text-sm font-bold', muted ? 'text-muted' : 'text-heading', mono && 'font-mono')}>{value}</p>
      <p className="text-[10px] text-muted mt-0.5">{label}</p>
    </div>
  );
}

/** Linha de meta-dado (label à esquerda, valor à direita) num identity card. */
export function IdentityMetaRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-2 text-xs">
      <span className="text-muted">{label}</span>
      <span className="text-heading font-medium truncate ml-2">{value}</span>
    </div>
  );
}
```

- [ ] **Step 4: Refactor the create card to use them**

In `src/frontend/src/features/contracts/create/ContractIdentityCard.tsx`:
- Remove the local `MiniStat` and `MetaRow` function declarations.
- Add import: `import { IdentityMiniStat, IdentityMetaRow } from '../shared/components/identityCardPrimitives';`
- Replace `<MiniStat .../>` usages with `<IdentityMiniStat .../>` and `<MetaRow .../>` with `<IdentityMetaRow .../>` (same props).

- [ ] **Step 5: Run tests (primitives + create card unchanged)**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/identityCardPrimitives.test.tsx src/__tests__/contracts/ContractIdentityCard.test.tsx`
Expected: PASS (2 + 3 tests). The create card renders identically.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/contracts/shared/components/identityCardPrimitives.tsx src/frontend/src/features/contracts/create/ContractIdentityCard.tsx src/frontend/src/__tests__/contracts/identityCardPrimitives.test.tsx
git commit -m "refactor(contracts): extrai primitives partilhados do identity card"
```

---

## Task 2: `ContractWorkspaceIdentityCard`

Sticky left card from `StudioContract`. Visual language matches the create card.

**Files:**
- Create: `src/frontend/src/features/contracts/workspace/components/ContractWorkspaceIdentityCard.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractWorkspaceIdentityCard.test.tsx`

Relevant `StudioContract` fields (from `workspace/studioTypes.ts`): `technicalName, friendlyName, protocol, serviceType, semVer, lifecycleState, isLocked, signedBy, owner, team, domain, approvalState, complianceScore, approvalChecklist[], policyChecks[]`. `approvalChecklist` items have `.state` ('Approved' counts); `policyChecks` items have `.passed`.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/ContractWorkspaceIdentityCard.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { ContractWorkspaceIdentityCard } from '../../features/contracts/workspace/components/ContractWorkspaceIdentityCard';
import type { StudioContract } from '../../features/contracts/workspace/studioTypes';

const contract = {
  technicalName: 'payments-api', friendlyName: 'Payments API', protocol: 'OpenApi', serviceType: 'RestApi',
  semVer: '2.1.0', lifecycleState: 'Approved', isLocked: false, signedBy: undefined,
  owner: 'ana', team: 'Payments', domain: 'payments', complianceScore: 92,
  approvalChecklist: [{ role: 'Tech', state: 'Approved' }, { role: 'Sec', state: 'Pending' }],
  policyChecks: [{ policyId: 'p1', policyName: 'x', passed: true }],
} as unknown as StudioContract;

describe('ContractWorkspaceIdentityCard', () => {
  it('shows technical name, version and owner', () => {
    render(<ContractWorkspaceIdentityCard contract={contract} />);
    expect(screen.getByText('payments-api')).toBeInTheDocument();
    expect(screen.getByText(/2\.1\.0/)).toBeInTheDocument();
    expect(screen.getByText(/Payments/)).toBeInTheDocument();
  });
  it('shows approvals count from checklist (1/2)', () => {
    render(<ContractWorkspaceIdentityCard contract={contract} />);
    expect(screen.getByText('1/2')).toBeInTheDocument();
  });
  it('shows compliance percentage', () => {
    render(<ContractWorkspaceIdentityCard contract={contract} />);
    expect(screen.getByText('92%')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/ContractWorkspaceIdentityCard.test.tsx`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement the card**

```tsx
// src/frontend/src/features/contracts/workspace/components/ContractWorkspaceIdentityCard.tsx
import { useTranslation } from 'react-i18next';
import { Globe, Server, Zap, Cog, Database, Lock, FileSignature } from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { Badge } from '../../../../shared/ui';
import { LifecycleBadge } from '../../shared/components/LifecycleBadge';
import { IdentityMiniStat, IdentityMetaRow } from '../../shared/components/identityCardPrimitives';
import { PROTOCOL_COLORS } from '../../shared/constants';
import type { StudioContract } from '../studioTypes';

const TYPE_ICON: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  RestApi: Globe, Soap: Server, Event: Zap, BackgroundService: Cog, SharedSchema: Database,
};

/** Cartão de identidade sticky do workspace de contrato (padrão v5). Apresentacional. */
export function ContractWorkspaceIdentityCard({ contract }: { contract: StudioContract }) {
  const { t } = useTranslation();
  const Icon = TYPE_ICON[contract.serviceType] ?? Globe;
  const approved = contract.approvalChecklist.filter((a) => a.state === 'Approved').length;
  const totalApprovals = contract.approvalChecklist.length;
  const passedPolicies = contract.policyChecks.filter((p) => p.passed).length;
  const totalPolicies = contract.policyChecks.length;
  const compliance = contract.complianceScore;

  return (
    <div className="rounded-2xl border border-edge bg-card overflow-hidden shadow-sm">
      <div className="bg-gradient-to-b from-accent/10 to-transparent p-4">
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-accent text-white shrink-0">
            <Icon size={20} />
          </div>
          <div className="min-w-0 flex-1">
            <p className="font-mono text-sm font-semibold text-heading truncate">{contract.technicalName}</p>
            <p className="text-xs text-muted truncate mt-0.5">{contract.friendlyName || '—'}</p>
          </div>
          <LifecycleBadge state={contract.lifecycleState} />
        </div>
        <div className="flex flex-wrap gap-1.5 mt-3 items-center">
          <span className={cn('text-[10px] px-1.5 py-0.5 rounded font-medium', PROTOCOL_COLORS[contract.protocol] ?? 'bg-muted/15 text-muted border border-muted/25')}>{contract.protocol}</span>
          <Badge variant="primary" size="sm">{`v${contract.semVer}`}</Badge>
          {contract.isLocked && <Badge variant="default" size="sm"><Lock size={9} className="inline mr-0.5" />{t('contracts.locked', 'Locked')}</Badge>}
          {contract.signedBy && <Badge variant="success" size="sm"><FileSignature size={9} className="inline mr-0.5" />{t('contracts.signed', 'Signed')}</Badge>}
        </div>
      </div>

      <div className="grid grid-cols-3 gap-px bg-edge border-t border-b border-edge">
        <IdentityMiniStat value={totalApprovals ? `${approved}/${totalApprovals}` : '—'} label={t('contracts.workspace.approvals', 'Approvals')} />
        <IdentityMiniStat value={totalPolicies ? `${passedPolicies}/${totalPolicies}` : '—'} label={t('contracts.workspace.compliance', 'Policies')} />
        <IdentityMiniStat value={compliance == null ? '—' : `${compliance}%`} label={t('contracts.studio.rail.compliance', 'Compliance')} muted={compliance == null} />
      </div>

      <div className="px-4 py-2 divide-y divide-edge/60">
        <IdentityMetaRow label={t('contracts.studio.rail.owner', 'Owner')} value={contract.owner ? `@${contract.owner}` : '—'} />
        <IdentityMetaRow label={t('contracts.studio.rail.team', 'Team')} value={contract.team || '—'} />
        <IdentityMetaRow label={t('contracts.studio.rail.domain', 'Domain')} value={contract.domain || '—'} />
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/ContractWorkspaceIdentityCard.test.tsx`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/workspace/components/ContractWorkspaceIdentityCard.tsx src/frontend/src/__tests__/contracts/ContractWorkspaceIdentityCard.test.tsx
git commit -m "feat(contracts): ContractWorkspaceIdentityCard (padrao v5, StudioContract)"
```

---

## Task 3: `WorkspaceTabs` (two-tier group→section nav)

**Files:**
- Create: `src/frontend/src/features/contracts/workspace/components/WorkspaceTabs.tsx`
- Test: `src/frontend/src/__tests__/contracts/WorkspaceTabs.test.tsx`

Uses `WORKSPACE_SECTIONS` (each `{ id, labelKey, icon, group }`) and `WORKSPACE_SECTION_GROUPS` (each `{ key, labelKey }`) from `workspace/shared/constants` (re-exported at `../shared/constants`). `WorkspaceSectionId` type from `../types`. Controlled by parent: receives `activeSection` + `onSelect`.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/WorkspaceTabs.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { WorkspaceTabs } from '../../features/contracts/workspace/components/WorkspaceTabs';

describe('WorkspaceTabs', () => {
  it('renders the five group tabs', () => {
    render(<WorkspaceTabs activeSection="summary" onSelect={vi.fn()} />);
    // group labelKeys fall back to the group key under the i18n mock
    ['overview', 'contract', 'governance', 'relationships', 'ai'].forEach((g) => {
      expect(screen.getByRole('tab', { name: new RegExp(g, 'i') })).toBeInTheDocument();
    });
  });
  it('clicking a group selects that group\'s first section', () => {
    const onSelect = vi.fn();
    render(<WorkspaceTabs activeSection="summary" onSelect={onSelect} />);
    fireEvent.click(screen.getByRole('tab', { name: /governance/i }));
    expect(onSelect).toHaveBeenCalledWith('validation'); // first section in 'governance' group
  });
});
```

(Note: the governance group's first section per `WORKSPACE_SECTIONS` order is `validation`. Verify against `workspace/shared/constants.ts` when implementing; if the order differs, assert the actual first id.)

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/WorkspaceTabs.test.tsx`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement**

```tsx
// src/frontend/src/features/contracts/workspace/components/WorkspaceTabs.tsx
import { useTranslation } from 'react-i18next';
import { cn } from '../../../lib/cn';
import { WORKSPACE_SECTIONS, WORKSPACE_SECTION_GROUPS } from '../../shared/constants';
import type { WorkspaceSectionId } from '../../types';

interface WorkspaceTabsProps {
  activeSection: WorkspaceSectionId;
  onSelect: (section: WorkspaceSectionId) => void;
}

/** Navegação em dois níveis: grupos (primário) → secções do grupo (chips secundários). */
export function WorkspaceTabs({ activeSection, onSelect }: WorkspaceTabsProps) {
  const { t } = useTranslation();
  const activeGroup = WORKSPACE_SECTIONS.find((s) => s.id === activeSection)?.group
    ?? WORKSPACE_SECTION_GROUPS[0]!.key;
  const sectionsInGroup = WORKSPACE_SECTIONS.filter((s) => s.group === activeGroup);

  return (
    <div className="mb-5">
      {/* Primary: groups */}
      <div role="tablist" className="flex gap-1 border-b border-edge overflow-x-auto">
        {WORKSPACE_SECTION_GROUPS.map((group) => {
          const isActive = group.key === activeGroup;
          const first = WORKSPACE_SECTIONS.find((s) => s.group === group.key);
          return (
            <button
              key={group.key}
              role="tab"
              type="button"
              aria-selected={isActive}
              onClick={() => first && onSelect(first.id)}
              className={cn(
                'px-4 py-2.5 text-sm font-semibold whitespace-nowrap border-b-2 transition-colors',
                isActive ? 'text-accent border-accent' : 'text-muted border-transparent hover:text-heading',
              )}
            >
              {t(group.labelKey, group.key)}
            </button>
          );
        })}
      </div>
      {/* Secondary: section chips */}
      <div className="flex flex-wrap gap-1.5 mt-3">
        {sectionsInGroup.map((section) => {
          const isActive = section.id === activeSection;
          return (
            <button
              key={section.id}
              type="button"
              onClick={() => onSelect(section.id)}
              className={cn(
                'px-3 py-1 text-xs font-medium rounded-md border transition-colors',
                isActive ? 'bg-accent text-white border-accent' : 'bg-card text-muted border-edge hover:text-heading hover:border-edge-strong',
              )}
            >
              {t(section.labelKey, section.id)}
            </button>
          );
        })}
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/WorkspaceTabs.test.tsx`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/workspace/components/WorkspaceTabs.tsx src/frontend/src/__tests__/contracts/WorkspaceTabs.test.tsx
git commit -m "feat(contracts): WorkspaceTabs (navegacao grupo->seccao em 2 niveis)"
```

---

## Task 4: Slim the `StudioRail`

**Files:**
- Modify: `src/frontend/src/features/contracts/workspace/components/StudioRail.tsx`

- [ ] **Step 1: Edit the rail**

In `StudioRail.tsx`, REMOVE these `RailSection` blocks (their data now lives in the identity card / header):
- The **Status** block (lifecycle/approval/compliance/version).
- The **Owners** block (owner/team/domain).
- The **Approval Checklist** block.
- The **Policy Checks** block.
- The **Available Transitions** block (transitions now live in the PageHeader actions).

KEEP: the **Risks** block, the **Recent Activity** block, and the **Locked notice**. Remove now-unused locals (`passedPolicies`, `totalPolicies`, `approvedCount`, `totalApprovals`, `transitions`) and now-unused imports (e.g. `LifecycleBadge`, `LIFECYCLE_TRANSITIONS`, `CheckCircle2`, `Circle`, `Shield`, `Users`, `ChevronRight`, `ArrowRight` — keep only icons still referenced by Risks/Activity: `AlertTriangle`, `Activity`). Keep the `onTransition` prop removed from the interface IF nothing else uses it (the page will stop passing it). The `RailSection`/`RailRow`/`formatRelativeTime` helpers stay (RailRow may become unused — remove it if so).

Result: `StudioRail` renders only Risks + Recent Activity + Locked notice.

- [ ] **Step 2: Typecheck the file**

Run: `cd src/frontend && npx tsc --noEmit 2>&1 | grep -i "StudioRail" || echo "no StudioRail type errors"`
Expected: `no StudioRail type errors`.

- [ ] **Step 3: Lint the file**

Run: `cd src/frontend && npx eslint src/features/contracts/workspace/components/StudioRail.tsx`
Expected: 0 errors (no unused-var warnings for removed locals/imports — make sure you deleted them).

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/features/contracts/workspace/components/StudioRail.tsx
git commit -m "refactor(contracts): StudioRail slim (so Risks/Activity/Locked)"
```

---

## Task 5: `WorkspaceLayout` 3-column shell + DS lifecycle actions

**Files:**
- Rewrite: `src/frontend/src/features/contracts/workspace/WorkspaceLayout.tsx`
- Create: `src/frontend/src/features/contracts/workspace/components/ContractLifecycleActions.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractLifecycleActions.test.tsx`

`WorkspaceLayout` becomes a dumb shell (no `activeSection` state — the page owns it).

- [ ] **Step 1: Rewrite `WorkspaceLayout.tsx`**

```tsx
// src/frontend/src/features/contracts/workspace/WorkspaceLayout.tsx
import { cn } from '../../../lib/cn';

interface WorkspaceLayoutProps {
  header: React.ReactNode;
  identityCard: React.ReactNode;
  rail?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
}

/** Shell estrutural do workspace de contrato (padrão v5): PageHeader + 3 colunas. */
export function WorkspaceLayout({ header, identityCard, rail, children, className }: WorkspaceLayoutProps) {
  return (
    <div className={cn('flex flex-col h-full', className)}>
      {header}
      <div className="flex-1 min-h-0 overflow-y-auto p-6">
        <div className="grid grid-cols-1 lg:grid-cols-[300px_minmax(0,1fr)_240px] gap-6 items-start">
          <div className="lg:sticky lg:top-4">{identityCard}</div>
          <div className="min-w-0">{children}</div>
          {rail && <aside className="lg:sticky lg:top-4">{rail}</aside>}
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Write the failing test for `ContractLifecycleActions`**

```tsx
// src/frontend/src/__tests__/contracts/ContractLifecycleActions.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { ContractLifecycleActions } from '../../features/contracts/workspace/components/ContractLifecycleActions';

describe('ContractLifecycleActions', () => {
  it('renders an Export button and calls onExport', () => {
    const onExport = vi.fn();
    render(<ContractLifecycleActions lifecycleState="Draft" isLocked={false} onTransition={vi.fn()} onExport={onExport} />);
    fireEvent.click(screen.getByRole('button', { name: /export/i }));
    expect(onExport).toHaveBeenCalled();
  });
  it('renders a transition button for the current lifecycle state', () => {
    const onTransition = vi.fn();
    render(<ContractLifecycleActions lifecycleState="Draft" isLocked={false} onTransition={onTransition} onExport={vi.fn()} />);
    // Draft → InReview transition exists (LIFECYCLE_TRANSITIONS)
    const btn = screen.getByRole('button', { name: /submitForReview|InReview|review/i });
    fireEvent.click(btn);
    expect(onTransition).toHaveBeenCalledWith('InReview');
  });
});
```

- [ ] **Step 3: Implement `ContractLifecycleActions.tsx`**

```tsx
// src/frontend/src/features/contracts/workspace/components/ContractLifecycleActions.tsx
import { useTranslation } from 'react-i18next';
import { Download, Send, Check, Lock, AlertTriangle, Sunset, Archive, Undo2 } from 'lucide-react';
import { Button } from '../../../../shared/ui';
import { LIFECYCLE_TRANSITIONS } from '../../shared/constants';
import type { ContractLifecycleState } from '../../types';

const TRANSITION_ICONS: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  InReview: Send, Approved: Check, Locked: Lock, Deprecated: AlertTriangle, Sunset, Retired: Archive, Draft: Undo2,
};

interface ContractLifecycleActionsProps {
  lifecycleState: ContractLifecycleState;
  isLocked: boolean;
  onTransition: (state: ContractLifecycleState) => void;
  onExport: () => void;
}

/** Acções de lifecycle + export do workspace, em DS Button (para o PageHeader). */
export function ContractLifecycleActions({ lifecycleState, isLocked, onTransition, onExport }: ContractLifecycleActionsProps) {
  const { t } = useTranslation();
  const transitions = isLocked ? [] : (LIFECYCLE_TRANSITIONS[lifecycleState] ?? []);
  return (
    <div className="flex items-center gap-2 flex-wrap">
      <Button variant="outline" size="sm" icon={<Download size={14} />} onClick={onExport}>
        {t('contracts.export', 'Export')}
      </Button>
      {transitions.map(({ state, actionKey }) => {
        const Icon = TRANSITION_ICONS[state] ?? Send;
        return (
          <Button key={state} variant="primary" size="sm" icon={<Icon size={14} />} onClick={() => onTransition(state)}>
            {t(actionKey, state)}
          </Button>
        );
      })}
    </div>
  );
}
```

- [ ] **Step 4: Run the test**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/ContractLifecycleActions.test.tsx`
Expected: PASS (2 tests). (If the transition button name regex doesn't match the actual `actionKey` for Draft→InReview, open `shared/constants.ts` `LIFECYCLE_TRANSITIONS.Draft` and use the real `actionKey` in the test name regex.)

- [ ] **Step 5: Lint**

Run: `cd src/frontend && npx eslint src/features/contracts/workspace/WorkspaceLayout.tsx src/features/contracts/workspace/components/ContractLifecycleActions.tsx`
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/contracts/workspace/WorkspaceLayout.tsx src/frontend/src/features/contracts/workspace/components/ContractLifecycleActions.tsx src/frontend/src/__tests__/contracts/ContractLifecycleActions.test.tsx
git commit -m "feat(contracts): WorkspaceLayout shell 3 colunas + acoes DS de lifecycle"
```

---

## Task 6: Wire `ContractWorkspacePage` to the new layout

**Files:**
- Modify: `src/frontend/src/features/contracts/workspace/ContractWorkspacePage.tsx`
- Modify: `src/frontend/src/__tests__/pages/ContractWorkspacePage.test.tsx`

The `renderSection(section, onNavigate)` switch and all hooks/handlers stay. Replace the `WorkspaceLayout` usage (old render-prop API) with the new shell + owned `activeSection` state + `PageHeader` + identity card + `WorkspaceTabs` + slim rail.

- [ ] **Step 1: Update the page test first**

Replace the body of `__tests__/pages/ContractWorkspacePage.test.tsx` test cases (keep the mocks block, including the monaco + hooks mocks). Add an i18n mock `vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));`. Provide a populated `StudioContract`-shaped detail so the loaded view renders:

```tsx
function renderLoaded() {
  vi.mocked(useContractDetail).mockReturnValue({
    data: {
      id: 'c1', apiAssetId: 'a1', technicalName: 'payments-api', friendlyName: 'Payments API',
      semVer: '2.1.0', format: 'yaml', protocol: 'OpenApi', specContent: '', lifecycleState: 'Approved',
      isLocked: false, serviceType: 'RestApi', domain: 'payments', owner: 'ana', team: 'Payments',
      complianceScore: 92, approvalChecklist: [], policyChecks: [], risks: [], recentActivity: [],
      consumers: [], producers: [], dependencies: [], tags: [], externalLinks: [],
      // minimal fields used by toStudioContract / sections; cast as never
    },
    isLoading: false, isError: false, refetch: vi.fn(),
  } as never);
  vi.mocked(useContractViolations).mockReturnValue({ data: [], isLoading: false } as never);
  vi.mocked(useContractTransition).mockReturnValue({ mutate: vi.fn() } as never);
  vi.mocked(useContractExport).mockReturnValue({ exportVersion: vi.fn() } as never);
  // ...render as in renderPage()
}

it('renders the identity card and group tabs when loaded', () => {
  renderLoaded();
  expect(screen.getByText('payments-api')).toBeInTheDocument();
  expect(screen.getByRole('tab', { name: /overview/i })).toBeInTheDocument();
});
```

Note: `toStudioContract` maps the backend detail; the mock must satisfy what `toStudioContract` reads. If `toStudioContract` throws on the minimal mock, inspect `workspace/toStudioContract.ts` and add the fields it dereferences. Keep the existing "renders without crashing" + loading tests.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npx vitest run src/__tests__/pages/ContractWorkspacePage.test.tsx`
Expected: FAIL — identity card / tabs not present (old layout).

- [ ] **Step 3: Rewrite the page render**

Keep everything from the current file (hooks, `studioContract` memo, loading/error, `handleTransition`, `handleExport`, `renderSection`). Add `activeSection` state and replace the returned JSX:

```tsx
// imports to add:
import { useState } from 'react';
import { PageHeader } from '../../../components/PageHeader';
import { WorkspaceLayout } from './WorkspaceLayout';
import { WorkspaceTabs } from './components/WorkspaceTabs';
import { ContractWorkspaceIdentityCard } from './components/ContractWorkspaceIdentityCard';
import { ContractLifecycleActions } from './components/ContractLifecycleActions';
import { StudioRail } from './components/StudioRail';
import type { WorkspaceSectionId } from '../types';
// (remove the old ContractHeader/ContractQuickActions imports if no longer used)

// inside the component, after studioContract is known:
const [activeSection, setActiveSection] = useState<WorkspaceSectionId>('summary');

// returned JSX (replaces the old <WorkspaceLayout ...> render-prop block):
return (
  <WorkspaceLayout
    header={
      <PageHeader
        title={studioContract.technicalName}
        subtitle={`${detail.protocol} · v${detail.semVer}${studioContract.domain ? ` · ${studioContract.domain}` : ''}`}
        actions={
          <ContractLifecycleActions
            lifecycleState={detail.lifecycleState as ContractLifecycleState}
            isLocked={detail.isLocked}
            onTransition={handleTransition}
            onExport={handleExport}
          />
        }
      />
    }
    identityCard={<ContractWorkspaceIdentityCard contract={studioContract} />}
    rail={<StudioRail contract={studioContract} />}
  >
    <WorkspaceTabs activeSection={activeSection} onSelect={setActiveSection} />
    {renderSection(activeSection, setActiveSection)}
  </WorkspaceLayout>
);
```

Adjust the `StudioRail` props to its new (slimmed) signature from Task 4 (it no longer needs `onTransition`). Confirm `PageHeader` accepts `title`/`subtitle`/`actions` (it's used that way in `pages/ContractStudioPage.tsx`); if it requires an `icon`, pass a suitable lucide icon (e.g. `<FileText size={20} />`).

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npx vitest run src/__tests__/pages/ContractWorkspacePage.test.tsx`
Expected: PASS.

- [ ] **Step 5: Lint + typecheck**

Run: `cd src/frontend && npx eslint src/features/contracts/workspace/ContractWorkspacePage.tsx && npx tsc --noEmit 2>&1 | grep -i "ContractWorkspacePage" || echo "no page type errors"`
Expected: 0 eslint errors; `no page type errors`.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/contracts/workspace/ContractWorkspacePage.tsx src/frontend/src/__tests__/pages/ContractWorkspacePage.test.tsx
git commit -m "feat(contracts): ContractWorkspacePage no layout v5 (card+tabs+rail slim)"
```

---

## Task 7: Section polish sweep

Mechanical, behavior-preserving cleanup of the section components. **Do not change data wiring or logic.** For each file below, apply these RULES:
- Raw `<button>` used as an action → DS `Button` (`from '../../../../shared/ui'`), preserving onClick/disabled/label.
- Raw `<input>`/`<select>`/`<textarea>` form controls → DS `TextField`/`Select`/`TextArea`. (Leave interactive selection-cards and non-form buttons that have no DS equivalent, but add `type="button"`.)
- Hardcoded Tailwind palette colors (`bg-slate-*`, `text-gray-*`, `emerald/indigo/red-50` etc.) → semantic tokens (`bg-card`/`bg-elevated`, `text-heading`/`text-muted`, `text-mint`/`text-danger`/`text-warning`/`text-cyan`).
- Inline loading/error/empty → DS `Skeleton`/`ErrorState`/`EmptyState` from `shared/ui` where a section rolls its own.
- **Skip `ContractSection.tsx` internals** (Monaco/visual-builders — sub-projects C/D); only its outer chrome tokens may be tightened.

**Files to sweep** (from the raw-control scan; do the ones with raw controls/colors):
`workspace/sections/SummarySection.tsx`, `SecuritySection.tsx`, `OperationsSection.tsx`, `DefinitionSection.tsx`, `AiAgentsSection.tsx`, `VersioningSection.tsx`, `ValidationSection.tsx`, `SchemasSection.tsx`, `ComplianceSection.tsx`, `ApprovalsSection.tsx`, `ConsumersSection.tsx`, `DependenciesSection.tsx`, `ChangelogSection.tsx`, `ScorecardSection.tsx`, `DeploymentsSection.tsx`.

- [ ] **Step 1: Inventory before**

Run: `cd src/frontend/src/features/contracts && grep -rcE "<input|<select|<textarea|bg-(slate|gray|zinc)-[0-9]|text-(slate|gray|zinc)-[0-9]" workspace/sections | grep -v ":0"`
Note the files/counts.

- [ ] **Step 2: Apply the rules per file**

Go file by file applying the RULES above. Keep each section's props, data hooks, and conditional logic identical. Commit per-file or in small groups.

- [ ] **Step 3: Verify no raw form controls remain in swept sections**

Run: `cd src/frontend/src/features/contracts && grep -rnE "<input|<select|<textarea" workspace/sections | grep -v "ContractSection" || echo "no raw form controls in sections"`
Expected: `no raw form controls in sections` (or only justified non-DS cases you note).

- [ ] **Step 4: Run section + workspace tests**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts src/__tests__/pages/ContractWorkspacePage.test.tsx`
Expected: all PASS.

- [ ] **Step 5: Lint**

Run: `cd src/frontend && npx eslint src/features/contracts/workspace/sections`
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/contracts/workspace/sections
git commit -m "polish(contracts): sweep DS + tokens nas seccoes do workspace"
```

---

## Task 8: Full verification

- [ ] **Step 1: Lint**

Run: `cd src/frontend && npm run lint`
Expected: 0 errors.

- [ ] **Step 2: Contract + page suites**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts src/__tests__/pages/ContractWorkspacePage.test.tsx`
Expected: all PASS.

- [ ] **Step 3: Full unit suite**

Run: `cd src/frontend && npx vitest run`
Expected: green (no regressions vs the 2337 baseline; new tests added).

- [ ] **Step 4: Build**

Run: `cd src/frontend && npm run build`
Expected: success.

- [ ] **Step 5: Confirm out-of-scope untouched**

Run: `cd "C:/Users/dlima/Documents/GitHub/NexTraceOne" && git diff --name-only main...HEAD | grep -E 'workspace/builders/|workspace/editor/|studio/|BuilderPage|DraftStudio' || echo "clean — no C/D/B files touched"`
Expected: `clean — no C/D/B files touched`. (Also confirm `ContractSection.tsx` logic unchanged — only token/chrome edits if any.)

- [ ] **Step 6: Confirm branch scope sane**

Run: `cd "C:/Users/dlima/Documents/GitHub/NexTraceOne" && git diff --name-only main...HEAD | wc -l && git diff --name-only main...HEAD | grep -vE 'contracts/|docs/superpowers' || echo "only contracts + docs touched"`
Expected: only contracts files + the spec/plan docs.

---

## Self-review notes (for the implementer)

- **`toStudioContract` mock:** Task 6 Step 1 — the loaded-view test depends on `toStudioContract(detail)` not throwing. Read `workspace/toStudioContract.ts` and ensure the mock detail carries every field it dereferences; expand the mock if needed.
- **Group-first-section ids:** Tasks 3 & assertions assume `WORKSPACE_SECTIONS` order puts `summary` first in `overview` and `validation` first in `governance`. Verify against `workspace/shared/constants.ts`; adjust test expectations to the real first id if different.
- **`PageHeader` props:** confirm `title/subtitle/actions` (and whether `icon` is required) against `components/PageHeader.tsx`; it's used in `pages/ContractStudioPage.tsx`.
- **No behavior change:** rail slimming and section sweep are presentation-only. If any removed rail data is genuinely not shown anywhere else, that's acceptable (Status/Owners → card; Approvals/Policies detail → their sections).
- **Out-of-scope guard:** Task 8 Step 5 is the protection for Monaco/visual-builders.

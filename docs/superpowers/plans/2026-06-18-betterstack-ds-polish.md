# Betterstack DS Polish — 4 Pages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply minimal Design System polish to four NexTraceOne pages, replacing raw HTML controls and hardcoded Tailwind colors with DS tokens/components, without touching data-fetching, mutations, query keys, permissions, validation, or i18n keys.

**Architecture:** View-layer-only surgical edits. Each page file is one task with its own build+test gate and atomic commit. Sub-components inside the page file (FourEyesForm, WasteSignalCard, etc.) are part of the same task. No new files created; no API/hook changes.

**Tech Stack:** React 19, TypeScript 5.9, Tailwind CSS 4, DS components from `src/frontend/src/components/` and `src/frontend/src/shared/ui/index.ts`. Build: `npm run build` from `src/frontend`. Tests: `npm run test -- run --reporter=verbose <testfile>` from `src/frontend`.

**Branch:** `redesign/betterstack-foundation`

---

## DS Component Reference (read before touching any file)

| DS Component | Import path | Key props |
|---|---|---|
| `Select` | `../../../components/Select` | `options: {value,label}[]`, `size="sm"`, `value`, `onChange`, `label` |
| `TextField` | `../../../components/TextField` | `size="sm"`, `value`, `onChange`, `placeholder`, `type` |
| `Button` | `../../../components/Button` | `variant="primary"\|"ghost"\|"outline"`, `size="sm"`, `icon={<Node/>}`, `loading` |
| `Toggle` | `../../../components/Toggle` | `checked`, `onChange(v:boolean)`, `label` |
| `PageContainer` | `../../../components/shell` | wraps page with standard padding |
| `PageHeader` | `../../../components/PageHeader` | `title`, `subtitle`, `icon`, `actions` |
| `PageLoadingState` | `../../../components/PageLoadingState` | zero props needed |
| `PageErrorState` | `../../../components/PageErrorState` | `onRetry` |
| `EmptyState` | `../../../components/EmptyState` | `icon`, `title`, `description`, `action` |

**Select usage pattern** (the DS Select takes `options` array, NOT `<option>` children):
```tsx
<Select
  size="sm"
  value={action}
  onChange={e => setAction(e.target.value)}
  aria-label={t('governance.gates.fourEyes.action', 'Action')}
  options={[
    { value: 'production_deploy', label: 'production_deploy' },
    { value: 'security_config_change', label: 'security_config_change' },
  ]}
/>
```

**TextField usage pattern**:
```tsx
<TextField
  size="sm"
  value={requester}
  onChange={e => setRequester(e.target.value)}
  placeholder={t('governance.gates.fourEyes.requester', 'Requester')}
/>
```

**Button replacing raw `<button>` pattern**:
```tsx
// raw:   <button onClick={fn} className="btn btn-primary w-full" disabled={!requester}>Evaluate</button>
// DS:
<Button variant="primary" size="sm" onClick={fn} disabled={!requester} className="w-full">
  {t('governance.gates.evaluate', 'Evaluate')}
</Button>
```

**Toggle replacing raw switch pattern**:
```tsx
// raw:   <button role="switch" aria-checked={value} onClick={() => onChange(!value)} ...>
// DS:
<Toggle checked={value} onChange={onChange} label={label} />
```

---

## File Map

| File | Status | Changes |
|---|---|---|
| `src/frontend/src/features/governance/pages/GovernanceGatesPage.tsx` | Modify | Replace raw `<input>`, `<select>`, `<button>` in sub-form components |
| `src/frontend/src/features/governance/pages/NotebooksPage.tsx` | Modify | Wrap with `PageContainer`+`PageHeader`; remove `text-indigo-500` hardcoded colors |
| `src/frontend/src/features/governance/pages/WasteDetectionPage.tsx` | Modify | Add `PageContainer`+`PageHeader`+`PageLoadingState`+`PageErrorState`+`EmptyState`; replace raw button; replace hardcoded colors |
| `src/frontend/src/features/platform-admin/pages/GracefulShutdownPage.tsx` | Modify | Replace raw edit `<button>`, save/cancel `<button>` pair, raw `<input>` in NumericField, raw `<button role="switch">` in ToggleField |
| `src/frontend/src/__tests__/pages/WasteDetectionPage.test.tsx` | Modify | Update loading/error test selectors (text changes from raw string to DS component content) |

---

## Task 1: GovernanceGatesPage — Replace raw form controls

**Files:**
- Modify: `src/frontend/src/features/governance/pages/GovernanceGatesPage.tsx`
- Test: `src/frontend/src/__tests__/pages/GovernanceGatesPage.test.tsx`

### What changes
Three sub-form components (`FourEyesForm`, `CabForm`, `ErrorBudgetForm`) currently use raw `<input>`, `<select>`, `<button>` with `.input` and `.btn` utility classes. Replace with DS `TextField`, `Select`, `Button`.

The page-level structure (PageContainer, PageHeader, Card, PageErrorState) is already correct — do NOT touch it.

- [ ] **Step 1: Read the current file to confirm state**

```
C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend\src\features\governance\pages\GovernanceGatesPage.tsx
```
Confirm lines 164–270 contain the three form sub-components with raw controls.

- [ ] **Step 2: Update imports — add TextField, Select from DS**

Replace the existing import block at the top of the file. Current imports at lines 1–10:
```tsx
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import {
  Eye, ShieldCheck, AlertTriangle, CheckCircle2, XCircle, Users, Activity,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageErrorState } from '../../../components/PageErrorState';
```

Add `Button`, `TextField`, `Select` imports:
```tsx
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import {
  Eye, ShieldCheck, AlertTriangle, CheckCircle2, XCircle, Users, Activity,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { TextField } from '../../../components/TextField';
import { Select } from '../../../components/Select';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageErrorState } from '../../../components/PageErrorState';
```

- [ ] **Step 3: Replace FourEyesForm (lines 164–186)**

Replace:
```tsx
function FourEyesForm({ onEvaluate }: { onEvaluate: (a: string, r: string, ap?: string) => void }) {
  const { t } = useTranslation();
  const [action, setAction] = useState('production_deploy');
  const [requester, setRequester] = useState('');
  const [approver, setApprover] = useState('');

  return (
    <div className="space-y-2">
      <select value={action} onChange={e => setAction(e.target.value)} className="input w-full" aria-label={t('governance.gates.fourEyes.action', 'Action')}>
        <option value="production_deploy">production_deploy</option>
        <option value="security_config_change">security_config_change</option>
        <option value="privileged_access_grant">privileged_access_grant</option>
        <option value="compliance_waiver">compliance_waiver</option>
        <option value="break_glass">break_glass</option>
      </select>
      <input value={requester} onChange={e => setRequester(e.target.value)} placeholder={t('governance.gates.fourEyes.requester', 'Requester')} className="input w-full" />
      <input value={approver} onChange={e => setApprover(e.target.value)} placeholder={t('governance.gates.fourEyes.approver', 'Approver (optional)')} className="input w-full" />
      <button onClick={() => onEvaluate(action, requester, approver || undefined)} className="btn btn-primary w-full" disabled={!requester}>
        {t('governance.gates.evaluate', 'Evaluate')}
      </button>
    </div>
  );
}
```

With:
```tsx
function FourEyesForm({ onEvaluate }: { onEvaluate: (a: string, r: string, ap?: string) => void }) {
  const { t } = useTranslation();
  const [action, setAction] = useState('production_deploy');
  const [requester, setRequester] = useState('');
  const [approver, setApprover] = useState('');

  /* Opções de ação disponíveis para o gate Four Eyes */
  const actionOptions = [
    { value: 'production_deploy', label: 'production_deploy' },
    { value: 'security_config_change', label: 'security_config_change' },
    { value: 'privileged_access_grant', label: 'privileged_access_grant' },
    { value: 'compliance_waiver', label: 'compliance_waiver' },
    { value: 'break_glass', label: 'break_glass' },
  ];

  return (
    <div className="space-y-2">
      <Select
        size="sm"
        value={action}
        onChange={e => setAction(e.target.value)}
        aria-label={t('governance.gates.fourEyes.action', 'Action')}
        options={actionOptions}
      />
      <TextField
        size="sm"
        value={requester}
        onChange={e => setRequester(e.target.value)}
        placeholder={t('governance.gates.fourEyes.requester', 'Requester')}
      />
      <TextField
        size="sm"
        value={approver}
        onChange={e => setApprover(e.target.value)}
        placeholder={t('governance.gates.fourEyes.approver', 'Approver (optional)')}
      />
      <Button
        variant="primary"
        size="sm"
        onClick={() => onEvaluate(action, requester, approver || undefined)}
        disabled={!requester}
        className="w-full"
      >
        {t('governance.gates.evaluate', 'Evaluate')}
      </Button>
    </div>
  );
}
```

- [ ] **Step 4: Replace CabForm (lines 200–232)**

Replace:
```tsx
function CabForm({ onEvaluate }: { onEvaluate: (s: string, e: string, c: string, b: string) => void }) {
  const { t } = useTranslation();
  const [service, setService] = useState('');
  const [env, setEnv] = useState('production');
  const [crit, setCrit] = useState('High');
  const [blast, setBlast] = useState('Medium');

  return (
    <div className="space-y-2">
      <input value={service} onChange={e => setService(e.target.value)} placeholder={t('governance.gates.cab.serviceName', 'Service name')} className="input w-full" />
      <select value={env} onChange={e => setEnv(e.target.value)} className="input w-full" aria-label={t('governance.gates.cab.environment', 'Environment')}>
        <option value="production">{t('environment.profile.production', 'Production')}</option>
        <option value="staging">{t('environment.profile.staging', 'Staging')}</option>
        <option value="development">{t('environment.profile.development', 'Development')}</option>
      </select>
      <select value={crit} onChange={e => setCrit(e.target.value)} className="input w-full" aria-label={t('governance.gates.cab.criticality', 'Criticality')}>
        <option value="Low">Low</option>
        <option value="Medium">Medium</option>
        <option value="High">High</option>
        <option value="Critical">Critical</option>
      </select>
      <select value={blast} onChange={e => setBlast(e.target.value)} className="input w-full" aria-label={t('governance.gates.cab.blastRadius', 'Blast Radius')}>
        <option value="None">None</option>
        <option value="Low">Low</option>
        <option value="Medium">Medium</option>
        <option value="High">High</option>
      </select>
      <button onClick={() => onEvaluate(service, env, crit, blast)} className="btn btn-primary w-full" disabled={!service}>
        {t('governance.gates.evaluate', 'Evaluate')}
      </button>
    </div>
  );
}
```

With:
```tsx
function CabForm({ onEvaluate }: { onEvaluate: (s: string, e: string, c: string, b: string) => void }) {
  const { t } = useTranslation();
  const [service, setService] = useState('');
  const [env, setEnv] = useState('production');
  const [crit, setCrit] = useState('High');
  const [blast, setBlast] = useState('Medium');

  /* Opções de ambiente para o gate CAB */
  const envOptions = [
    { value: 'production', label: t('environment.profile.production', 'Production') },
    { value: 'staging', label: t('environment.profile.staging', 'Staging') },
    { value: 'development', label: t('environment.profile.development', 'Development') },
  ];
  const critOptions = [
    { value: 'Low', label: 'Low' },
    { value: 'Medium', label: 'Medium' },
    { value: 'High', label: 'High' },
    { value: 'Critical', label: 'Critical' },
  ];
  const blastOptions = [
    { value: 'None', label: 'None' },
    { value: 'Low', label: 'Low' },
    { value: 'Medium', label: 'Medium' },
    { value: 'High', label: 'High' },
  ];

  return (
    <div className="space-y-2">
      <TextField
        size="sm"
        value={service}
        onChange={e => setService(e.target.value)}
        placeholder={t('governance.gates.cab.serviceName', 'Service name')}
      />
      <Select
        size="sm"
        value={env}
        onChange={e => setEnv(e.target.value)}
        aria-label={t('governance.gates.cab.environment', 'Environment')}
        options={envOptions}
      />
      <Select
        size="sm"
        value={crit}
        onChange={e => setCrit(e.target.value)}
        aria-label={t('governance.gates.cab.criticality', 'Criticality')}
        options={critOptions}
      />
      <Select
        size="sm"
        value={blast}
        onChange={e => setBlast(e.target.value)}
        aria-label={t('governance.gates.cab.blastRadius', 'Blast Radius')}
        options={blastOptions}
      />
      <Button
        variant="primary"
        size="sm"
        onClick={() => onEvaluate(service, env, crit, blast)}
        disabled={!service}
        className="w-full"
      >
        {t('governance.gates.evaluate', 'Evaluate')}
      </Button>
    </div>
  );
}
```

- [ ] **Step 5: Replace ErrorBudgetForm (lines 251–270)**

Replace:
```tsx
function ErrorBudgetForm({ onEvaluate }: { onEvaluate: (s: string, e: string, b: string) => void }) {
  const { t } = useTranslation();
  const [service, setService] = useState('');
  const [env, setEnv] = useState('production');
  const [budget, setBudget] = useState('50');

  return (
    <div className="space-y-2">
      <input value={service} onChange={e => setService(e.target.value)} placeholder={t('governance.gates.errorBudget.serviceName', 'Service name')} className="input w-full" />
      <select value={env} onChange={e => setEnv(e.target.value)} className="input w-full" aria-label={t('governance.gates.errorBudget.environment', 'Environment')}>
        <option value="production">{t('environment.profile.production', 'Production')}</option>
        <option value="staging">{t('environment.profile.staging', 'Staging')}</option>
      </select>
      <input type="number" value={budget} onChange={e => setBudget(e.target.value)} placeholder={t('governance.gates.errorBudget.remainingPct', 'Budget remaining %')} className="input w-full" min="0" max="100" />
      <button onClick={() => onEvaluate(service, env, budget)} className="btn btn-primary w-full" disabled={!service}>
        {t('governance.gates.evaluate', 'Evaluate')}
      </button>
    </div>
  );
}
```

With:
```tsx
function ErrorBudgetForm({ onEvaluate }: { onEvaluate: (s: string, e: string, b: string) => void }) {
  const { t } = useTranslation();
  const [service, setService] = useState('');
  const [env, setEnv] = useState('production');
  const [budget, setBudget] = useState('50');

  /* Opções de ambiente para o gate de Error Budget */
  const envOptions = [
    { value: 'production', label: t('environment.profile.production', 'Production') },
    { value: 'staging', label: t('environment.profile.staging', 'Staging') },
  ];

  return (
    <div className="space-y-2">
      <TextField
        size="sm"
        value={service}
        onChange={e => setService(e.target.value)}
        placeholder={t('governance.gates.errorBudget.serviceName', 'Service name')}
      />
      <Select
        size="sm"
        value={env}
        onChange={e => setEnv(e.target.value)}
        aria-label={t('governance.gates.errorBudget.environment', 'Environment')}
        options={envOptions}
      />
      <TextField
        size="sm"
        type="number"
        value={budget}
        onChange={e => setBudget(e.target.value)}
        placeholder={t('governance.gates.errorBudget.remainingPct', 'Budget remaining %')}
        min={0}
        max={100}
      />
      <Button
        variant="primary"
        size="sm"
        onClick={() => onEvaluate(service, env, budget)}
        disabled={!service}
        className="w-full"
      >
        {t('governance.gates.evaluate', 'Evaluate')}
      </Button>
    </div>
  );
}
```

- [ ] **Step 6: Run build**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm run build
```

Expected: exit 0, no TypeScript errors.

- [ ] **Step 7: Run tests**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm run test -- run --reporter=verbose src/frontend/src/__tests__/pages/GovernanceGatesPage.test.tsx
```

Expected: 2 tests pass. The test only checks `screen.getByText('Governance Gates')` and container defined — no selector changes needed.

- [ ] **Step 8: Commit**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne
git add src/frontend/src/features/governance/pages/GovernanceGatesPage.tsx
git commit -m "feat(governance): jornada Betterstack na GovernanceGatesPage (DS controls)"
```

---

## Task 2: NotebooksPage — PageContainer + PageHeader + remove hardcoded indigo

**Files:**
- Modify: `src/frontend/src/features/governance/pages/NotebooksPage.tsx`
- Test: none (no test file found for NotebooksPage)

### What changes
1. Wrap outermost `<div className="space-y-6">` with `PageContainer` from shell.
2. Replace the raw header `<div>` (lines 43–71) with `PageHeader` + standard pattern.
3. Replace `text-indigo-500` on `BookOpen` icon (line 47) and persona span (line 117) with `text-accent`.
4. The two `Button` CTA slots (`AI Compose` and `New Notebook`) are already using the DS Button — just move them into `PageHeader actions`. No functional change.

- [ ] **Step 1: Read the current file**

```
C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend\src\features\governance\pages\NotebooksPage.tsx
```

Confirm line 42 is `<div className="space-y-6">` and line 44 is the raw header div.

- [ ] **Step 2: Update imports**

Current imports (lines 1–16):
```tsx
import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { BookOpen, Plus, Bot, Clock, Layers } from 'lucide-react';
import { notebooksApi } from '../api/notebooks';
import { useAuth } from '../../../contexts/AuthContext';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { Card } from '../../../components/Card';
import { FormattedTimestamp } from '../../../components/FormattedTimestamp';
import { AiComposeDashboardModal } from '../components/AiComposeDashboardModal';
```

Replace with (add `PageContainer` and `PageHeader`, remove `BookOpen` since it moves to PageHeader icon):
```tsx
import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { BookOpen, Plus, Bot, Clock, Layers } from 'lucide-react';
import { notebooksApi } from '../api/notebooks';
import { useAuth } from '../../../contexts/AuthContext';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { Card } from '../../../components/Card';
import { FormattedTimestamp } from '../../../components/FormattedTimestamp';
import { AiComposeDashboardModal } from '../components/AiComposeDashboardModal';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
```

- [ ] **Step 3: Replace outermost wrapper and header block**

The current JSX return (lines 41–134) starts with:
```tsx
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-heading flex items-center gap-2">
            <BookOpen className="h-6 w-6 text-indigo-500" />
            {t('notebook.title')}
          </h1>
          <p className="text-sm text-muted mt-1">
            {t('notebook.emptyHint')}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setComposeOpen(true)}
            className="flex items-center gap-2"
          >
            <Bot className="h-4 w-4" />
            {t('aiCompose.title')}
          </Button>
          <Button asChild size="sm" className="flex items-center gap-2">
            <Link to="/governance/notebooks/new">
              <Plus className="h-4 w-4" />
              {t('notebook.new')}
            </Link>
          </Button>
        </div>
      </div>
```

Replace with:
```tsx
  return (
    <PageContainer>
      <PageHeader
        title={t('notebook.title')}
        subtitle={t('notebook.emptyHint')}
        icon={<BookOpen size={24} />}
        actions={
          <>
            {/* Botão para compor notebook via IA */}
            <Button
              variant="outline"
              size="sm"
              icon={<Bot size={14} />}
              onClick={() => setComposeOpen(true)}
            >
              {t('aiCompose.title')}
            </Button>
            {/* Botão primário para criar novo notebook */}
            <Button
              variant="primary"
              size="sm"
              icon={<Plus size={14} />}
              onClick={() => { window.location.href = '/governance/notebooks/new'; }}
            >
              {t('notebook.new')}
            </Button>
          </>
        }
      />
```

> Note: `Button` in the DS does not support `asChild` as a true polymorphic slot (it renders as `<button>` always). The `asChild` usage on line 64 was non-functional. Use `onClick` with `window.location.href` or `Link` wrapping the whole button. Since this is view-only and avoids changing routing logic, use `Link` outside the Button:

Correction — wrap in Link instead:
```tsx
            {/* Botão primário para criar novo notebook */}
            <Link to="/governance/notebooks/new">
              <Button
                variant="primary"
                size="sm"
                icon={<Plus size={14} />}
              >
                {t('notebook.new')}
              </Button>
            </Link>
```

- [ ] **Step 4: Close PageContainer at end of return**

The current closing `</div>` at line 133 should become `</PageContainer>`. Also close the `space-y-6` wrapper — since `PageContainer` provides its own spacing, wrap the notebooks grid section in a `<div className="space-y-6">` inside `PageContainer` after the PageHeader:

Final structure:
```tsx
  return (
    <PageContainer>
      <PageHeader ... />

      <div className="space-y-6">
        {/* Notebooks list */}
        {notebooks.length === 0 ? (
          <EmptyState ... />
        ) : (
          <div className="grid ...">
            ...
          </div>
        )}

        {/* AI Compose modal */}
        <AiComposeDashboardModal ... />
      </div>
    </PageContainer>
  );
```

- [ ] **Step 5: Replace text-indigo-500 on persona span (line 117)**

Replace:
```tsx
                  <span className="capitalize text-indigo-500">{nb.persona}</span>
```

With:
```tsx
                  {/* Persona do notebook — usa token de cor accent */}
                  <span className="capitalize text-accent">{nb.persona}</span>
```

- [ ] **Step 6: Run build**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm run build
```

Expected: exit 0.

- [ ] **Step 7: Commit**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne
git add src/frontend/src/features/governance/pages/NotebooksPage.tsx
git commit -m "feat(governance): jornada Betterstack na NotebooksPage (DS controls)"
```

---

## Task 3: WasteDetectionPage — Full DS structure + semantic tokens

**Files:**
- Modify: `src/frontend/src/features/governance/pages/WasteDetectionPage.tsx`
- Test: `src/frontend/src/__tests__/pages/WasteDetectionPage.test.tsx` — update loading/error selectors

### What changes
This page is the most work. Currently: no `PageContainer`/`PageHeader`, raw `<button>` for refresh, raw inline loading/error/empty states, hardcoded `text-slate-*`/`bg-white`/`bg-red-50`/`bg-emerald-50` colors, hardcoded severity badge classes in `WasteSignalCard`.

Changes:
1. Add `PageContainer`, `PageHeader` with refresh as `actions` button.
2. Replace raw loading `<div>` with `PageLoadingState`.
3. Replace raw error `<div>` with `PageErrorState`.
4. Replace raw empty state `<div>` with `EmptyState`.
5. Remove outer `<div className="p-6 space-y-6">` (PageContainer handles padding).
6. Replace summary card `bg-white border border-slate-200` with `bg-card border border-edge`.
7. Replace `text-slate-*` with semantic tokens (`text-muted`, `text-heading`, `text-body`).
8. In `WasteSignalCard`, replace hardcoded severity color maps with `Badge` component variants.

- [ ] **Step 1: Read the current file**

```
C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend\src\features\governance\pages\WasteDetectionPage.tsx
```

Confirm the structure matches what was read during planning.

- [ ] **Step 2: Update imports**

Current imports (lines 1–13):
```tsx
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import {
  AlertTriangle,
  TrendingDown,
  Trash2,
  XCircle,
  CheckCircle2,
  Server,
  Clock,
} from 'lucide-react';
import { finOpsApi, type WasteSignalDetail } from '../api/finOps';
```

Replace with:
```tsx
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import {
  AlertTriangle,
  TrendingDown,
  Trash2,
  Server,
  Clock,
  RefreshCw,
} from 'lucide-react';
import { finOpsApi, type WasteSignalDetail } from '../api/finOps';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
```

Note: `XCircle` and `CheckCircle2` can be removed since loading/error/empty are now handled by DS components.

- [ ] **Step 3: Rewrite WasteDetectionPage component body**

Replace the entire `WasteDetectionPage` function (lines 15–108) with:

```tsx
export function WasteDetectionPage() {
  const { t } = useTranslation('wasteDetection');
  const { activeEnvironmentId } = useEnvironment();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['waste-signals', activeEnvironmentId],
    queryFn: () => finOpsApi.getWasteSignals(),
  });

  /* Estado de carregamento inicial */
  if (isLoading) return <PageLoadingState />;

  /* Estado de erro com retry */
  if (isError) return <PageErrorState onRetry={() => refetch()} />;

  return (
    <PageContainer>
      {/* Cabeçalho com ação de refresh */}
      <PageHeader
        title={t('title')}
        subtitle={t('subtitle')}
        icon={<AlertTriangle size={24} />}
        actions={
          <Button variant="ghost" size="sm" icon={<RefreshCw size={14} />} onClick={() => refetch()}>
            {t('refresh')}
          </Button>
        }
      />

      {data && (
        <div className="space-y-6">
          {/* Cards de resumo */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="bg-card border border-edge rounded-lg p-4">
              <p className="text-xs text-muted uppercase tracking-wide">{t('totalWaste')}</p>
              <p className="text-2xl font-bold text-warning mt-1">
                {Number(data.totalWaste).toLocaleString(undefined, { maximumFractionDigits: 2 })}
              </p>
            </div>
            <div className="bg-card border border-edge rounded-lg p-4">
              <p className="text-xs text-muted uppercase tracking-wide">{t('signalCount')}</p>
              <p className="text-2xl font-bold text-heading mt-1">{data.signalCount}</p>
            </div>
            <div className="bg-card border border-edge rounded-lg p-4">
              <p className="text-xs text-muted uppercase tracking-wide">{t('byTypeTitle')}</p>
              <div className="mt-2 space-y-1">
                {data.byType.slice(0, 3).map((bt) => (
                  <div key={bt.type} className="flex items-center justify-between text-xs">
                    <span className="text-body">{bt.type}</span>
                    <span className="font-medium text-heading">{bt.count}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Lista de sinais ou estado vazio */}
          {data.signals.length === 0 ? (
            <EmptyState
              title={t('noWaste')}
              description={t('noWasteDescription', 'Nenhum sinal de desperdício detectado.')}
            />
          ) : (
            <section>
              <h2 className="text-base font-medium text-heading mb-3">{t('signalsTitle')}</h2>
              <div className="space-y-3">
                {data.signals.map((s) => (
                  <WasteSignalCard key={s.signalId} signal={s} t={t} />
                ))}
              </div>
            </section>
          )}

          {data.isSimulated && (
            <p className="text-xs text-muted italic">{t('simulatedNote')}</p>
          )}

          <p className="text-xs text-muted">
            {t('generatedAt')}: {new Date(data.generatedAt).toLocaleString()}
          </p>
        </div>
      )}
    </PageContainer>
  );
}
```

- [ ] **Step 4: Rewrite WasteSignalCard (lines 110–172)**

The severity config currently uses hardcoded Tailwind color classes for border and badge. Replace with semantic tokens and use `Badge` component for severity/type labels.

Map severity to Badge variant:
- `Critical` → `critical`
- `High` → `warning`  
- `Medium` → `warning`
- `Low` → `neutral`

Replace the entire `WasteSignalCard` function:

```tsx
/* Mapa de severidade para variante de Badge DS */
const SEVERITY_BADGE_VARIANT: Record<string, 'critical' | 'warning' | 'neutral'> = {
  Critical: 'critical',
  High: 'warning',
  Medium: 'warning',
  Low: 'neutral',
};

/* Borda do card por severidade — tokens semânticos */
const SEVERITY_BORDER: Record<string, string> = {
  Critical: 'border-critical/40 bg-critical/5',
  High: 'border-warning/40 bg-warning/5',
  Medium: 'border-warning/30 bg-warning/5',
  Low: 'border-edge bg-card',
};

function WasteSignalCard({
  signal,
  t,
}: {
  signal: WasteSignalDetail;
  t: (key: string) => string;
}) {
  const badgeVariant = SEVERITY_BADGE_VARIANT[signal.severity] ?? 'neutral';
  const borderCls = SEVERITY_BORDER[signal.severity] ?? 'border-edge bg-card';

  /* Ícone contextual por tipo de sinal */
  const typeIcon = signal.type.includes('Idle')
    ? <Clock size={14} className="text-muted" />
    : signal.type.includes('Cpu') || signal.type.includes('Memory')
    ? <Server size={14} className="text-muted" />
    : <Trash2 size={14} className="text-muted" />;

  return (
    <div className={`border rounded-lg p-4 ${borderCls}`}>
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3">
          <div className="mt-0.5">
            <AlertTriangle size={16} className="text-warning" />
          </div>
          <div>
            <div className="flex items-center gap-2 flex-wrap">
              <span className="font-medium text-sm text-heading">{signal.serviceName}</span>
              <Badge variant={badgeVariant} size="sm">
                {typeIcon}
                {signal.type}
              </Badge>
              <Badge variant={badgeVariant} size="sm">{signal.severity}</Badge>
            </div>
            <p className="text-xs text-body mt-1">{signal.description}</p>
            <p className="text-xs text-muted mt-1">{signal.pattern}</p>
            {signal.correlatedCause && (
              <p className="text-xs text-muted mt-1">
                {t('correlatedCause')}: {signal.correlatedCause}
              </p>
            )}
            <div className="flex items-center gap-3 mt-2 text-xs text-muted">
              <span>{t('team')}: {signal.team}</span>
              <span>·</span>
              <span>{t('domain')}: {signal.domain}</span>
            </div>
          </div>
        </div>
        <div className="text-right flex-shrink-0">
          <div className="flex items-center gap-1 text-warning text-sm font-semibold">
            <TrendingDown size={14} />
            {Number(signal.estimatedWaste).toLocaleString(undefined, { maximumFractionDigits: 2 })}
          </div>
          <p className="text-xs text-muted">{t('estimatedWaste')}</p>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 5: Update test — loading and error state selectors**

The test currently looks for `screen.getByText('loading')` and `screen.getByText('error')` because the i18n mock returns the key as text and the raw divs rendered the key directly.

After replacing with `PageLoadingState` (which uses `t('common.loading')`) and `PageErrorState` (which renders a title from `t('common.error')` and description from `t('common.errorDescription')`), the text keys change.

Since the test mock is `t: (key: string) => key`, the PageLoadingState will render `'common.loading'` and PageErrorState renders `'common.error'`.

Update `src/frontend/src/__tests__/pages/WasteDetectionPage.test.tsx`:

Test at line 97:
```tsx
  it('shows loading state', () => {
    vi.mocked(finOpsApi.getWasteSignals).mockImplementation(
      () => new Promise(() => {})
    );
    render(<WasteDetectionPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });
```
Replace with:
```tsx
  it('shows loading state', () => {
    vi.mocked(finOpsApi.getWasteSignals).mockImplementation(
      () => new Promise(() => {})
    );
    render(<WasteDetectionPage />, { wrapper: Wrapper });
    // PageLoadingState renderiza t('common.loading') — mock retorna a chave
    expect(screen.getByText('common.loading')).toBeDefined();
  });
```

Test at line 131:
```tsx
  it('shows error state on API failure', async () => {
    vi.mocked(finOpsApi.getWasteSignals).mockRejectedValue(new Error('Network error'));
    render(<WasteDetectionPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });
```
Replace with:
```tsx
  it('shows error state on API failure', async () => {
    vi.mocked(finOpsApi.getWasteSignals).mockRejectedValue(new Error('Network error'));
    render(<WasteDetectionPage />, { wrapper: Wrapper });
    // PageErrorState renderiza t('common.error') como título — mock retorna a chave
    await waitFor(() => expect(screen.getByText('common.error')).toBeDefined());
  });
```

Also note: the `mockNoWaste` test at line 104 uses `screen.getByText('noWaste')` — this tested the raw `<div>` text. After the change it goes through `EmptyState` which renders `t('noWaste')` as the `title` prop, which the i18n mock returns as `'noWaste'`. No change needed for that selector.

- [ ] **Step 6: Check Badge variants available**

Read `src/frontend/src/components/Badge.tsx` to confirm `neutral`, `warning`, `critical` are valid variant values. If `neutral` doesn't exist, use `gray` instead.

- [ ] **Step 7: Run build**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm run build
```

Expected: exit 0.

- [ ] **Step 8: Run tests**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm run test -- run --reporter=verbose src/frontend/src/__tests__/pages/WasteDetectionPage.test.tsx
```

Expected: all 6 tests pass.

- [ ] **Step 9: Commit**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne
git add src/frontend/src/features/governance/pages/WasteDetectionPage.tsx
git add src/frontend/src/__tests__/pages/WasteDetectionPage.test.tsx
git commit -m "feat(governance): jornada Betterstack na WasteDetectionPage (DS controls)"
```

---

## Task 4: GracefulShutdownPage — Replace raw edit/save/cancel buttons + NumericField input + ToggleField

**Files:**
- Modify: `src/frontend/src/features/platform-admin/pages/GracefulShutdownPage.tsx`
- Test: `src/frontend/src/__tests__/pages/GracefulShutdownPage.test.tsx` — verify selectors still work

### What changes
Three areas:
1. Raw `<button>` "Edit config" (line 88) → DS `Button variant="outline" size="sm"`.
2. Raw `<button>` save (line 128) and cancel (line 134) → DS `Button`.
3. `NumericField` raw `<input type="number">` (line 213) → DS `TextField size="sm" type="number"`.
4. `ToggleField` raw `<button role="switch">` (line 241) → DS `Toggle`.

The page already uses `PageContainer`, `PageHeader`, `Button` (for refresh) — those are correct. The `isLoading` and `isError` inline divs (lines 62–72) use semantic token classes (`text-faded`, `bg-critical/10`, `border-critical/20`, `text-critical`) — these are already correct semantic tokens, leave them.

- [ ] **Step 1: Read the current file**

```
C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend\src\features\platform-admin\pages\GracefulShutdownPage.tsx
```

Confirm the raw controls at lines 86–93, 126–140, 212–220, 240–257.

- [ ] **Step 2: Update imports — add TextField and Toggle**

Current imports (lines 1–8):
```tsx
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Power, CheckCircle2, XCircle, RefreshCw, Settings } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type GracefulShutdownConfigUpdate } from '../api/platformAdmin';
```

Replace with:
```tsx
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Power, CheckCircle2, XCircle, RefreshCw, Settings } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { TextField } from '../../../components/TextField';
import { Toggle } from '../../../components/Toggle';
import { platformAdminApi, type GracefulShutdownConfigUpdate } from '../api/platformAdmin';
```

- [ ] **Step 3: Replace raw edit button (lines 85–93)**

Current:
```tsx
              {!editing && (
                  <button
                    onClick={startEdit}
                    className="flex items-center gap-2 px-3 py-1.5 text-sm text-accent border border-accent/20 rounded hover:bg-accent/10"
                  >
                    <Settings size={14} />
                    {t('editBtn')}
                  </button>
                )}
```

Replace with:
```tsx
              {!editing && (
                  /* Botão de edição usa variante outline do DS */
                  <Button
                    variant="outline"
                    size="sm"
                    icon={<Settings size={14} />}
                    onClick={startEdit}
                  >
                    {t('editBtn')}
                  </Button>
                )}
```

- [ ] **Step 4: Replace raw save/cancel buttons (lines 126–140)**

Current:
```tsx
                  <div className="flex gap-3 pt-2">
                    <button
                      onClick={() => mutation.mutate(form)}
                      disabled={mutation.isPending}
                      className="px-4 py-2 bg-accent text-white text-sm rounded-lg hover:bg-accent/90 disabled:opacity-50"
                    >
                      {mutation.isPending ? t('saving') : t('save')}
                    </button>
                    <button
                      onClick={() => setEditing(false)}
                      className="px-4 py-2 text-sm border border-edge rounded-lg hover:bg-elevated text-muted"
                    >
                      {t('cancel')}
                    </button>
                  </div>
```

Replace with:
```tsx
                  <div className="flex gap-3 pt-2">
                    {/* Salvar configuração de shutdown */}
                    <Button
                      variant="primary"
                      size="sm"
                      loading={mutation.isPending}
                      onClick={() => mutation.mutate(form)}
                      disabled={mutation.isPending}
                    >
                      {mutation.isPending ? t('saving') : t('save')}
                    </Button>
                    {/* Cancelar edição sem salvar */}
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setEditing(false)}
                    >
                      {t('cancel')}
                    </Button>
                  </div>
```

- [ ] **Step 5: Replace NumericField raw input (lines 195–225)**

Current `NumericField` function:
```tsx
function NumericField({
  label,
  hint,
  value,
  onChange,
  min,
  max,
}: {
  label: string;
  hint: string;
  value: number;
  onChange: (v: number) => void;
  min: number;
  max: number;
}) {
  return (
    <div className="space-y-1">
      <label className="block text-sm font-medium text-body">{label}</label>
      <input
        type="number"
        value={value}
        min={min}
        max={max}
        onChange={(e) => onChange(Number(e.target.value))}
        className="w-40 px-3 py-2 border border-edge rounded-lg bg-canvas text-body text-sm focus:ring-1 focus:ring-accent/50 focus:border-accent/50"
        aria-label={label}
      />
      <p className="text-xs text-muted">{hint}</p>
    </div>
  );
}
```

Replace with:
```tsx
function NumericField({
  label,
  hint,
  value,
  onChange,
  min,
  max,
}: {
  label: string;
  hint: string;
  value: number;
  onChange: (v: number) => void;
  min: number;
  max: number;
}) {
  return (
    /* Campo numérico usa TextField DS com size sm */
    <TextField
      size="sm"
      label={label}
      type="number"
      value={value}
      min={min}
      max={max}
      onChange={(e) => onChange(Number(e.target.value))}
      helperText={hint}
      className="w-40"
    />
  );
}
```

- [ ] **Step 6: Replace ToggleField raw switch (lines 227–258)**

Current `ToggleField` function:
```tsx
function ToggleField({
  label,
  hint,
  value,
  onChange,
}: {
  label: string;
  hint: string;
  value: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <div className="flex items-start gap-4">
      <button
        type="button"
        role="switch"
        aria-checked={value}
        onClick={() => onChange(!value)}
        className={`mt-0.5 relative inline-flex h-5 w-9 items-center rounded-full transition-colors shrink-0 ${value ? 'bg-accent' : 'bg-elevated'}`}
        aria-label={label}
      >
        <span
          className={`inline-block h-3.5 w-3.5 transform rounded-full bg-white transition-transform ${value ? 'translate-x-4' : 'translate-x-1'}`}
        />
      </button>
      <div>
        <p className="text-sm font-medium text-body">{label}</p>
        <p className="text-xs text-muted">{hint}</p>
      </div>
    </div>
  );
}
```

Replace with:
```tsx
function ToggleField({
  label,
  hint,
  value,
  onChange,
}: {
  label: string;
  hint: string;
  value: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    /* Toggle DS com label e hint separados */
    <div className="space-y-1">
      <Toggle checked={value} onChange={onChange} label={label} />
      <p className="text-xs text-muted ml-14">{hint}</p>
    </div>
  );
}
```

- [ ] **Step 7: Run build**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm run build
```

Expected: exit 0.

- [ ] **Step 8: Run tests**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm run test -- run --reporter=verbose src/frontend/src/__tests__/pages/GracefulShutdownPage.test.tsx
```

The test uses `screen.getByText('editBtn')`, `screen.getByText('save')`, `screen.getByText('cancel')` — these query the rendered text of the buttons. Since DS `Button` renders its children directly, these text selectors continue to work. Expected: all 7 tests pass.

- [ ] **Step 9: Commit**

```powershell
cd C:\Users\dlima\Documents\GitHub\NexTraceOne
git add src/frontend/src/features/platform-admin/pages/GracefulShutdownPage.tsx
git commit -m "feat(platform-admin): jornada Betterstack na GracefulShutdownPage (DS controls)"
```

---

## Self-Review

### Spec coverage check

| Requirement | Task |
|---|---|
| PageHeader for title/subtitle; primary CTA → PageHeader actions | Task 2 (NotebooksPage), Task 3 (WasteDetectionPage) |
| Raw `<input>`/`<select>`/`<button>` → DS controls | Tasks 1, 3, 4 |
| `size="sm"` for toolbar/filter controls | All tasks use `size="sm"` |
| Hardcoded Tailwind colors → semantic tokens | Tasks 2, 3 (main offenders) |
| Raw loading/error/empty → DS states | Task 3 (WasteDetectionPage) |
| No `text-cyan` interactive → `text-accent` | Task 2 (indigo-500 → accent) |
| No `bg-surface`/`text-primary`/`bg-primary` | Verified none present in any file |
| View-layer only | All tasks — no data/fetch changes |
| Each file: npm run build must pass | Steps 6/7 per task |
| Run page tests if they exist | GovernanceGates (Task 1 Step 7), WasteDetection (Task 3 Step 8), GracefulShutdown (Task 4 Step 8) |
| Commit per file, explicit paths | Each task has single-file commit |
| Comments in Portuguese | All new comments in PT |

### Placeholder scan
No TBD, TODO, or "similar to" references.

### Type consistency check
- `Select` component takes `options: SelectOption[]` — all usages provide `{ value: string, label: string }[]` ✓
- `TextField` takes `size?: 'sm' | 'md'` — all usages pass `size="sm"` ✓
- `Button` takes `icon?: ReactNode` — all usages pass JSX node ✓
- `Toggle` takes `checked: boolean, onChange: (checked: boolean) => void` — ToggleField passes `value`/`onChange` correctly ✓
- `Badge` variant check: Task 3 Step 6 explicitly requires verifying Badge variants before coding ✓

### One concern flagged
The `NotebooksPage` currently uses `<Button asChild>` wrapping a `<Link>`. The DS `Button` component (as read) does not implement `asChild` as a true render-as-slot (it ignores the prop and renders `<button>`). Task 2 handles this by wrapping `<Link>` outside the `<Button>`. This is a pre-existing issue being corrected, not introduced.

---

## Execution sequence

Tasks 1 → 2 → 3 → 4 (sequential — each builds on the previous clean state).

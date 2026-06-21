# Draft Studio v5 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. UI tasks should also invoke `frontend-design:frontend-design`.

**Goal:** Restyle `DraftStudioPage` to the v5 2-column idiom — a sticky `DraftIdentityCard` first column + DS `Tabs`/content right + DS `PageHeader` with DS `Button` actions — keeping all behavior and leaving Monaco internals untouched.

**Architecture:** Presentation-only. A new presentational `DraftIdentityCard` (from `ContractDraft`, reusing the shared identity primitives) is added; `DraftStudioPage`'s render is rewritten to the 2-column layout (PageHeader + back link + identity card + DS Tabs). All hooks, mutations, state, `isEditable` logic, feedback banners (already token-clean), `ContractSection` (Monaco), and `DraftValidationPanel` are preserved. No backend/API/hook changes.

**Tech Stack:** React 19, TypeScript 5.9, TanStack Query 5, react-router-dom 7, Tailwind 4, DS primitives from `src/frontend/src/shared/ui` (`Button`, `TextField`, `TextArea`, `Select`, `Tabs`, `Badge`) + `components/PageHeader` + `components/PageLoadingState`/`PageErrorState`, Vitest + Testing Library, i18next.

---

## Conventions

- Paths relative to repo root. Frontend root: `src/frontend/`. Run tests from `src/frontend/`: `npx vitest run <path>`. Lint: `npm run lint` (errors fail; warnings OK).
- Branch `redesign/betterstack-draft-studio` is checked out. Commit after each task. **GIT HYGIENE:** `git add` only explicit paths (never `git add -A`).
- **Do not touch** `workspace/sections/ContractSection.tsx` or `workspace/sections/DraftValidationPanel.tsx` internals, or `workspace/editor/*` (Monaco). They are consumed as-is.
- DS imports from `studio/` files: `Button, TextField, TextArea, Select, Tabs, Badge` from `../../../shared/ui`; `PageHeader` from `../../../components/PageHeader`; `PageLoadingState`/`PageErrorState` from `../../../components/PageLoadingState` / `PageErrorState`. From `studio/components/`: add one `../` level.
- `ContractDraft` (from `contractStudio.ts`): `{ id, title, description, serviceId, contractType, protocol, specContent, format, proposedVersion, status, author, lastEditedAt?, lastEditedBy?, createdAt }`.

## File structure

| File | Responsibility |
|---|---|
| `features/contracts/studio/components/DraftIdentityCard.tsx` | **Create.** Sticky left card from `ContractDraft` + resolved service name. |
| `features/contracts/studio/DraftStudioPage.tsx` | **Modify (render rewrite).** 2-column v5 layout; preserve all logic/hooks/mutations. |
| `__tests__/pages/DraftStudioPage.test.tsx` | **Modify.** Loaded-view + tab-nav coverage. |

---

## Task 1: `DraftIdentityCard`

Sticky left card. Visual language matches `ContractWorkspaceIdentityCard`, but with draft data (no approvals/policies/compliance).

**Files:**
- Create: `src/frontend/src/features/contracts/studio/components/DraftIdentityCard.tsx`
- Test: `src/frontend/src/__tests__/contracts/DraftIdentityCard.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/DraftIdentityCard.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { DraftIdentityCard } from '../../features/contracts/studio/components/DraftIdentityCard';
import type { ContractDraft } from '../../features/contracts/types';

const draft = {
  id: 'd1', title: 'Orders API', description: '', serviceId: 'svc-1', contractType: 'RestApi',
  protocol: 'OpenApi', specContent: '', format: 'yaml', proposedVersion: '1.2.0', status: 'Editing',
  author: 'ana@x.io', createdAt: '2026-06-20T10:00:00Z',
} as unknown as ContractDraft;

describe('DraftIdentityCard', () => {
  it('shows title, version and a Draft status badge', () => {
    render(<DraftIdentityCard draft={draft} serviceName="Payments" />);
    expect(screen.getByText('Orders API')).toBeInTheDocument();
    expect(screen.getByText(/1\.2\.0/)).toBeInTheDocument();
    expect(screen.getByText('Editing')).toBeInTheDocument();
  });
  it('shows the resolved service name and author', () => {
    render(<DraftIdentityCard draft={draft} serviceName="Payments" />);
    expect(screen.getByText(/Payments/)).toBeInTheDocument();
    expect(screen.getByText(/ana@x\.io/)).toBeInTheDocument();
  });
  it('falls back to a dash when no service is linked', () => {
    render(<DraftIdentityCard draft={draft} serviceName={undefined} />);
    expect(screen.getByText('—')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/DraftIdentityCard.test.tsx`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement**

```tsx
// src/frontend/src/features/contracts/studio/components/DraftIdentityCard.tsx
import { useTranslation } from 'react-i18next';
import { Globe } from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { Badge } from '../../../../shared/ui';
import { TYPE_ICONS } from '../../create/contractCreateConstants';
import { IdentityMetaRow } from '../../shared/components/identityCardPrimitives';
import { PROTOCOL_COLORS } from '../../shared/constants';
import type { ContractDraft } from '../../types';

/** Cartão de identidade sticky do editor de draft (padrão v5). Apresentacional. */
export function DraftIdentityCard({ draft, serviceName }: { draft: ContractDraft; serviceName?: string }) {
  const { t } = useTranslation();
  const Icon = TYPE_ICONS[draft.contractType] ?? Globe;
  const created = draft.createdAt ? new Date(draft.createdAt).toLocaleString() : '—';
  const lastEdited = draft.lastEditedAt
    ? `${new Date(draft.lastEditedAt).toLocaleString()}${draft.lastEditedBy ? ` · ${draft.lastEditedBy}` : ''}`
    : '—';

  return (
    <div className="rounded-2xl border border-edge bg-card overflow-hidden shadow-sm">
      <div className="bg-gradient-to-b from-accent/10 to-transparent p-4">
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-accent text-white shrink-0">
            <Icon size={20} />
          </div>
          <div className="min-w-0 flex-1">
            <p className="font-mono text-sm font-semibold text-heading truncate">{draft.title || t('contracts.studio.untitledDraft', 'untitled-draft')}</p>
            <p className="text-xs text-muted truncate mt-0.5">{t(`contracts.contractTypes.${draft.contractType}`, draft.contractType)}</p>
          </div>
          <Badge variant="warning" size="sm">{t(`contracts.draftStatus.${draft.status}`, draft.status)}</Badge>
        </div>
        <div className="flex flex-wrap gap-1.5 mt-3 items-center">
          <span className={cn('text-[10px] px-1.5 py-0.5 rounded font-medium', PROTOCOL_COLORS[draft.protocol] ?? 'bg-muted/15 text-muted border border-muted/25')}>{draft.protocol}</span>
          <Badge variant="primary" size="sm">{`v${draft.proposedVersion}`}</Badge>
        </div>
      </div>

      <div className="px-4 py-2 divide-y divide-edge/60">
        <IdentityMetaRow label={t('contracts.studio.linkedService', 'Service')} value={serviceName || '—'} />
        <IdentityMetaRow label={t('contracts.studio.author', 'Author')} value={draft.author || '—'} />
        <IdentityMetaRow label={t('contracts.studio.createdAt', 'Created')} value={created} />
        <IdentityMetaRow label={t('contracts.studio.lastEdited', 'Last edited')} value={lastEdited} />
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/DraftIdentityCard.test.tsx`
Expected: PASS (3 tests). Then `cd src/frontend && npx eslint src/features/contracts/studio/components/DraftIdentityCard.tsx` → 0 errors, and `npx tsc --noEmit 2>&1 | grep -i DraftIdentityCard || echo "no type errors"` → `no type errors`. (Verify `ContractDraft` is exported from `features/contracts/types`; if it lives at `../api/contractStudio` types instead, import it from the correct path — read `features/contracts/types/index.ts`.)

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/studio/components/DraftIdentityCard.tsx src/frontend/src/__tests__/contracts/DraftIdentityCard.test.tsx
git commit -m "feat(contracts): DraftIdentityCard (padrao v5, ContractDraft)"
```

---

## Task 2: Rewrite `DraftStudioPage` to the 2-column v5 layout

KEEP all logic above the `return`: the `useParams`/hooks, `draftQuery`/`servicesQuery`, all `useState` override fields, the `resetOverrides`, the three mutations (`saveContentMutation`/`saveMetadataMutation`/`submitMutation`), `handleRunValidation`, the derived `specContent`/`format`/`title`/`description`/`proposedVersion`/`serviceId`/`services`, `isEditable`/`isSaving`/`validationIssueCount`. Replace the loading/error early-returns + the whole `return (...)` block.

**Files:**
- Modify: `src/frontend/src/features/contracts/studio/DraftStudioPage.tsx`
- Modify: `src/frontend/src/__tests__/pages/DraftStudioPage.test.tsx`

- [ ] **Step 1: Update the page test first**

Edit `__tests__/pages/DraftStudioPage.test.tsx`. KEEP the existing mocks block (monaco, AuthContext, contractStudioApi, serviceCatalog, useDraftExport, api/client). ADD an i18n mock at top: `vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));`. ADD a `useDraftValidation` mock (the page calls it):
```tsx
vi.mock('../../features/contracts/hooks/useDraftValidation', () => ({
  useDraftValidation: vi.fn(() => ({ state: { summary: { totalIssues: 0 } }, isRunning: false, validateAll: vi.fn() })),
}));
```
Add a loaded-view helper + test (the getDraft mock must return a `ContractDraft`-shaped object):
```tsx
function renderLoaded() {
  vi.mocked(contractStudioApi.getDraft).mockResolvedValue({
    id: 'd1', title: 'Orders API', description: '', serviceId: '', contractType: 'RestApi',
    protocol: 'OpenApi', specContent: 'openapi: 3.1.0', format: 'yaml', proposedVersion: '1.2.0',
    status: 'Editing', author: 'ana@x.io', createdAt: '2026-06-20T10:00:00Z',
  } as never);
  vi.mocked(serviceCatalogApi.listServices).mockResolvedValue({ items: [] } as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/contracts/studio/d1']}>
        <Routes><Route path="/contracts/studio/:draftId" element={<DraftStudioPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

it('renders the identity card and DS tabs when loaded', async () => {
  renderLoaded();
  expect(await screen.findByText('Orders API')).toBeInTheDocument();
  expect(screen.getByRole('tab', { name: /spec/i })).toBeInTheDocument();
  expect(screen.getByRole('tab', { name: /metadata/i })).toBeInTheDocument();
});
```
Keep the existing "renders without crashing" + loading tests.

Run: `cd src/frontend && npx vitest run src/__tests__/pages/DraftStudioPage.test.tsx` → the loaded test FAILS (old layout has no role=tab / identity card).

- [ ] **Step 2: Rewrite the loading/error returns + main return**

Replace the loading early-return with DS state:
```tsx
if (draftQuery.isLoading) {
  return <PageContainer><PageLoadingState size="lg" /></PageContainer>;
}
if (draftQuery.isError || !draft) {
  return (
    <PageContainer>
      <PageErrorState
        message={t('contracts.studio.draftNotFound', 'Draft not found or failed to load.')}
        action={<Link to="/contracts" className="text-sm text-accent hover:underline">{t('contracts.studio.backToCatalog', 'Back to Contracts')}</Link>}
      />
    </PageContainer>
  );
}
```

Add a derived service name (for the card) after `services` is known:
```tsx
const linkedServiceName = services.find((s) => s.serviceId === serviceId)?.displayName;
```

Build the DS tab items (Validation label carries the issue count when > 0):
```tsx
const tabItems = [
  { id: 'spec', label: t('contracts.studio.tabSpec', 'Spec'), icon: <Code size={13} /> },
  { id: 'metadata', label: t('contracts.studio.tabMetadata', 'Metadata'), icon: <Settings size={13} /> },
  { id: 'validation', label: `${t('contracts.draftValidation.tabValidation', 'Validation')}${validationIssueCount ? ` (${validationIssueCount > 99 ? '99+' : validationIssueCount})` : ''}`, icon: <ScanSearch size={13} /> },
];
```

Replace the whole `return ( <div className="flex flex-col h-full"> ... )` with:
```tsx
return (
  <PageContainer className="animate-fade-in">
    <div className="mb-4">
      <Link to="/contracts" className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-heading transition-colors">
        <ChevronLeft size={14} /> {t('contracts.title', 'Contracts')}
      </Link>
    </div>

    <PageHeader
      title={draft.title}
      subtitle={`${draft.protocol} · v${draft.proposedVersion}`}
      actions={
        <div className="flex items-center gap-2">
          {draftId && specContent.trim() && (
            <Button variant="outline" size="sm" icon={<Download size={14} />} loading={isExporting} onClick={() => exportDraft(draftId)}>
              {t('contracts.studio.exportDraft', 'Export')}
            </Button>
          )}
          {isEditable && (
            <>
              <Button variant="outline" size="sm" icon={<Save size={14} />} loading={isSaving}
                onClick={() => (activeTab === 'metadata' ? saveMetadataMutation.mutate() : saveContentMutation.mutate())}>
                {t('common.save', 'Save')}
              </Button>
              <Button variant="primary" size="sm" icon={<Send size={14} />} loading={submitMutation.isPending}
                disabled={!specContent.trim()} onClick={() => submitMutation.mutate()}>
                {t('contracts.studio.submitForReview', 'Submit for Review')}
              </Button>
            </>
          )}
        </div>
      }
    />

    {/* feedback banners — already token-clean; keep them, rendered above the grid */}
    <div className="mb-4 space-y-2">
      {(saveContentMutation.isError || saveMetadataMutation.isError) && (
        <div className="text-xs text-critical bg-critical/15 border border-critical/25 rounded-md px-3 py-2">{t('contracts.studio.saveFailed', 'Failed to save changes.')}</div>
      )}
      {(saveContentMutation.isSuccess || saveMetadataMutation.isSuccess) && !isSaving && (
        <div className="text-xs text-success bg-success/15 border border-success/25 rounded-md px-3 py-2">{t('contracts.studio.saveSuccess', 'Changes saved successfully.')}</div>
      )}
      {submitMutation.isError && (
        <div className="text-xs text-critical bg-critical/15 border border-critical/25 rounded-md px-3 py-2">{t('contracts.studio.submitFailed', 'Failed to submit for review.')}</div>
      )}
      {submitMutation.isSuccess && (
        <div className="text-xs text-success bg-success/15 border border-success/25 rounded-md px-3 py-2">{t('contracts.studio.submitSuccess', 'Draft submitted for review successfully.')}</div>
      )}
      {exportError && (
        <div className="text-xs text-critical bg-critical/15 border border-critical/25 rounded-md px-3 py-2">{t('contracts.studio.exportDraftFailed', 'Failed to export draft.')}</div>
      )}
    </div>

    <div className="grid grid-cols-1 lg:grid-cols-[300px_minmax(0,1fr)] gap-6 items-start">
      <div className="lg:sticky lg:top-4">
        <DraftIdentityCard draft={draft} serviceName={linkedServiceName} />
      </div>

      <div className="min-w-0">
        <Tabs
          items={tabItems}
          activeId={activeTab}
          onChange={(id) => setActiveTab(id as DraftTab)}
          className="mb-5"
        />

        {/* Tab: Spec */}
        {activeTab === 'spec' && (
          <div className="flex flex-col gap-3">
            <div className="flex items-center justify-between">
              <label className="text-xs font-medium text-heading">{t('contracts.studio.specContent', 'Specification Content')}</label>
              <Select
                value={format}
                onChange={(e) => setDraftFormat(e.target.value)}
                disabled={!isEditable}
                options={[{ value: 'yaml', label: 'YAML' }, { value: 'json', label: 'JSON' }, { value: 'xml', label: 'XML' }]}
                className="w-32"
              />
            </div>
            <ContractSection
              key={saveKey}
              specContent={specContent}
              format={format}
              protocol={draft.protocol}
              contractType={draft.contractType}
              isReadOnly={!isEditable}
              onContentChange={setDraftSpecContent}
              className="border border-edge rounded-lg overflow-hidden h-[60vh] min-h-[420px]"
            />
          </div>
        )}

        {/* Tab: Validation */}
        {activeTab === 'validation' && (
          <DraftValidationPanel
            state={draftValidation.state}
            isRunning={draftValidation.isRunning}
            protocol={draft.protocol as ContractProtocol}
            onRunValidation={handleRunValidation}
          />
        )}

        {/* Tab: Metadata */}
        {activeTab === 'metadata' && (
          <Card>
            <CardBody className="space-y-4 max-w-2xl">
              <TextField label={t('contracts.studio.draftTitle', 'Title')} value={title} onChange={(e) => setDraftTitle(e.target.value)} disabled={!isEditable} />
              <TextArea label={t('contracts.studio.draftDescription', 'Description')} value={description} onChange={(e) => setDraftDescription(e.target.value)} disabled={!isEditable} rows={3} />
              <TextField label={t('contracts.studio.proposedVersion', 'Proposed Version')} value={proposedVersion} onChange={(e) => setDraftProposedVersion(e.target.value)} disabled={!isEditable} />
              <Select
                label={t('contracts.studio.linkedService', 'Linked Service')}
                value={serviceId}
                onChange={(e) => setDraftServiceId(e.target.value)}
                disabled={!isEditable}
                options={[
                  { value: '', label: t('contracts.studio.linkedServiceOptional', 'No linked service yet') },
                  ...services.map((s) => ({ value: s.serviceId, label: `${s.displayName} · ${s.domain} · ${s.teamName}` })),
                ]}
                helperText={t('contracts.studio.linkedServiceHint', 'Publishing requires a real catalog link. Select a service to let Contracts create or reuse the correct API asset.')}
              />
            </CardBody>
          </Card>
        )}
      </div>
    </div>
  </PageContainer>
);
```

Update imports: ADD `Link`, `ChevronLeft` (lucide), `PageHeader`, `Button`, `TextField`, `TextArea`, `Select`, `Tabs` (from `shared/ui`), `PageLoadingState`, `PageErrorState`, `DraftIdentityCard` (from `./components/DraftIdentityCard`). REMOVE now-unused: `ArrowLeft`, `Loader2`, `Card`/`CardBody` stay (still used in metadata), the raw markup helpers. Keep `Code`, `Settings`, `ScanSearch`, `Save`, `Send`, `Download`. Verify each lucide import is used; remove any that aren't.

NOTE on DS APIs (verify against the real components before finalizing): `Select` takes `options: {value,label}[]`, `value`, `onChange`, `label?`, `helperText?`, `disabled?`, `className?` (confirmed in the create flow's DetailsTab usage). `TextField`/`TextArea` take `label?`, `value`, `onChange`, `disabled?`, `rows?` (TextArea). `Tabs` takes `items: {id,label,icon?}[]`, `activeId`, `onChange:(id)=>void`, `className?` (confirmed in WorkspaceTabs/ServiceDetailPage). `PageHeader` takes `title`, `subtitle?`, `actions?`, `icon?`. If any prop differs, adapt to the real signature.

Behavior to preserve EXACTLY: the Save button calls `saveMetadataMutation` on the metadata tab else `saveContentMutation`; Submit disabled without specContent; Export only when specContent present; `setDraftFormat`/`setDraftTitle`/etc. override setters unchanged; `key={saveKey}` on ContractSection; `isEditable` gating (note: original used `readOnly` for inputs and `disabled` for selects — using DS `disabled` for all is acceptable and equivalent for these draft fields).

- [ ] **Step 3: Run the page test**

Run: `cd src/frontend && npx vitest run src/__tests__/pages/DraftStudioPage.test.tsx`
Expected: PASS.

- [ ] **Step 4: Lint + typecheck**

Run: `cd src/frontend && npx eslint src/features/contracts/studio/DraftStudioPage.tsx && npx tsc --noEmit 2>&1 | grep -i "DraftStudioPage" || echo "no page type errors"`
Expected: 0 eslint errors (no unused imports — remove `ArrowLeft`/`Loader2` if now unused); `no page type errors`.

- [ ] **Step 5: Verify no raw form controls remain**

Run: `cd src/frontend/src/features/contracts && grep -nE "<input|<select|<textarea" studio/DraftStudioPage.tsx || echo "no raw form controls in DraftStudioPage"`
Expected: `no raw form controls in DraftStudioPage`.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/contracts/studio/DraftStudioPage.tsx src/frontend/src/__tests__/pages/DraftStudioPage.test.tsx
git commit -m "feat(contracts): DraftStudioPage no layout v5 (card + DS tabs + controlos DS)"
```

---

## Task 3: Full verification

- [ ] **Step 1: Lint**

Run: `cd src/frontend && npm run lint`
Expected: 0 errors.

- [ ] **Step 2: Contract + page suites**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts src/__tests__/pages/DraftStudioPage.test.tsx`
Expected: all PASS.

- [ ] **Step 3: Full unit suite**

Run: `cd src/frontend && npx vitest run`
Expected: green (no regressions vs the 2348 baseline; new tests added).

- [ ] **Step 4: Build**

Run: `cd src/frontend && npm run build`
Expected: success.

- [ ] **Step 5: Confirm out-of-scope untouched**

Run: `cd "C:/Users/dlima/Documents/GitHub/NexTraceOne" && git diff --name-only main...HEAD | grep -E 'sections/ContractSection|sections/DraftValidationPanel|workspace/editor/|workspace/builders/|BuilderPage' || echo "clean — no Monaco/builder files touched"`
Expected: `clean — no Monaco/builder files touched`.

- [ ] **Step 6: Confirm branch scope sane**

Run: `cd "C:/Users/dlima/Documents/GitHub/NexTraceOne" && git diff --name-only main...HEAD | wc -l && git diff --name-only main...HEAD | grep -vE 'features/contracts/|__tests__/contracts/|__tests__/pages/Draft|docs/superpowers' || echo "only contracts + docs touched"`
Expected: only contracts files + the spec/plan docs.

---

## Self-review notes (for the implementer)

- **`ContractDraft` import path:** Task 1 imports it from `../../types`. Verify `features/contracts/types` re-exports `ContractDraft`; if not, import from where `contractStudio.ts` declares it (`../types` per its own import). Read `features/contracts/types/index.ts` first.
- **`Select`/`TextField`/`TextArea`/`Tabs`/`PageHeader` prop signatures:** confirm against the real components (`components/Select.tsx`, `TextField.tsx`, `TextArea.tsx`, `Tabs.tsx`, `PageHeader.tsx`) — the plan's usage mirrors the create-flow DetailsTab + WorkspaceTabs, but adapt if any prop name differs (e.g. `disabled` vs `readOnly`).
- **`useDraftValidation` mock:** the page reads `draftValidation.state.summary.totalIssues` — the test mock must provide that shape (done in Task 2 Step 1).
- **Monaco untouched:** `ContractSection` is rendered with the same props (now in a fixed-height container instead of `flex-1`); its internals are not modified. Task 3 Step 5 guards this.
- **Feedback banners:** intentionally kept (already token-clean) rather than swapped to toast/Alert — avoids introducing a new feedback pattern the contracts module doesn't use (YAGNI; documented deviation from the spec's "Alert/toast" suggestion).

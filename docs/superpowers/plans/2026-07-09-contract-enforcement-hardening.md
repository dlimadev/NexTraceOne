# Contract Enforcement & Publication Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Endurecer o loop de enforcement/publicação de contratos — entradas da Publication ligam ao contrato, withdraw usa o DS `Modal`, e eliminar um ruleset Spectral exige confirmação.

**Architecture:** Alterações cirúrgicas de UX em 2 páginas de gestão do produtor. Um `Link` na tabela de publicação; conversão de um modal cru para o DS `Modal`; um modal de confirmação de eliminação. Zero backend novo, honest-null.

**Tech Stack:** React 19, TypeScript 5.9, react-router-dom 7 (`Link`), TanStack Query 5 (mutations existentes), DS `../../../shared/ui` (`Modal`, `Button`, `TextField`, `IconButton`), lucide-react, i18next (4 locales), Vitest + Testing Library, Playwright.

## Global Constraints

- DS de `../../../shared/ui` (`Modal`, `Button`, `TextField`, `IconButton`); ícones `lucide-react`; `Link` de `react-router-dom`.
- DS `Modal` API: `<Modal open onClose title? size? footer?>{children}</Modal>` (focus-trap/escape/aria; render `role="dialog"`).
- Honest-null: link do título só com `contractVersionId`; nunca fabricar.
- i18n: nenhuma string de UI hardcoded; `t('key','fallback inglês')`; chaves nos 4 locales `en, es, pt-BR, pt-PT` (NÃO existe `fr`); ficheiros FLAT `src/frontend/src/locales/<l>.json`.
- Mudanças cirúrgicas: não alterar a lógica das mutations; preservar `withdrawTarget`/`withdrawReason`/`handleWithdraw` e o toggle/create do Spectral.
- Testes centralizados em `src/frontend/src/__tests__/**`; e2e em `src/frontend/e2e/**` (globs de URL Playwright usam `**`).
- Tooling: `npm run test` (NÃO `npx vitest`); gate final `npm run build` (`tsc -b`); `npm run validate:i18n`.
- Rota verbatim: workspace `/contracts/:contractVersionId`.

---

### Task 1: Publication — entrada liga ao contrato + withdraw via DS `Modal`

**Files:**
- Modify: `src/frontend/src/features/contracts/publication/PublicationCenterPage.tsx`
- Test: `src/frontend/src/__tests__/contracts/PublicationCenterPage.hardening.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/PublicationCenterPage.hardening.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PublicationCenterPage } from '../../features/contracts/publication/PublicationCenterPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
const entry = {
  publicationEntryId: 'pe-1', contractVersionId: 'cv-1', apiAssetId: 'a-1',
  contractTitle: 'orders-api', semVer: '1.0.0', status: 'Published', visibility: 'Public', publishedBy: 'me',
};
vi.mock('../../features/contracts/hooks/usePublicationCenter', () => ({
  usePublicationCenterEntries: () => ({ data: { items: [entry], totalCount: 1 }, isLoading: false, isError: false, refetch: vi.fn() }),
  useWithdrawContractFromPortal: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={qc}><MemoryRouter><PublicationCenterPage /></MemoryRouter></QueryClientProvider>);
}

describe('PublicationCenterPage hardening', () => {
  it('links the contract title to the workspace', () => {
    wrap();
    const link = screen.getByRole('link', { name: 'orders-api' });
    expect(link.getAttribute('href')).toBe('/contracts/cv-1');
  });

  it('opens the withdraw confirmation as a dialog', () => {
    wrap();
    fireEvent.click(screen.getByRole('button', { name: /withdraw/i }));
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/PublicationCenterPage.hardening.test.tsx --run`
Expected: FAIL — o título não é link e o modal cru não expõe `role="dialog"`.

- [ ] **Step 3: Write minimal implementation**

Adicionar `Link` ao import de `react-router-dom` (o ficheiro já importa `useNavigate`) e `Modal` ao import do DS:

```tsx
import { useNavigate, Link } from 'react-router-dom';
```
```tsx
import { Button, TextField, Modal } from '../../../shared/ui';
```

Tornar o `contractTitle` um link (substituir a célula existente):

```tsx
                    <td className="px-4 py-3 font-medium">
                      {entry.contractVersionId ? (
                        <Link to={`/contracts/${entry.contractVersionId}`} className="text-accent hover:underline">
                          {entry.contractTitle}
                        </Link>
                      ) : (
                        <span className="text-accent">{entry.contractTitle}</span>
                      )}
                    </td>
```

Substituir o modal cru (todo o bloco `{withdrawTarget && (<div className="fixed inset-0 ...">…</div>)}`) por um DS `Modal`:

```tsx
      <Modal
        open={!!withdrawTarget}
        onClose={() => { setWithdrawTarget(null); setWithdrawReason(''); }}
        title={t('contracts.publication.withdrawModal.title', 'Withdraw Publication')}
        size="sm"
        footer={
          <>
            <Button variant="outline" size="sm" onClick={() => { setWithdrawTarget(null); setWithdrawReason(''); }}>
              {t('common.cancel', 'Cancel')}
            </Button>
            <Button
              variant="danger"
              size="sm"
              loading={withdrawMutation.isPending}
              disabled={withdrawMutation.isPending}
              onClick={() => { if (withdrawTarget) handleWithdraw(withdrawTarget); }}
            >
              {t('contracts.publication.withdrawModal.confirm', 'Withdraw')}
            </Button>
          </>
        }
      >
        {withdrawTarget && (
          <>
            <p className="text-xs text-muted mb-4">
              {t(
                'contracts.publication.withdrawModal.description',
                'Contract {{title}} (v{{version}}) will be removed from the Developer Portal.',
                { title: withdrawTarget.contractTitle, version: withdrawTarget.semVer },
              )}
            </p>
            <TextField
              label={t('contracts.publication.withdrawModal.reason', 'Reason (optional)')}
              value={withdrawReason}
              onChange={(e) => setWithdrawReason(e.target.value)}
              placeholder={t('contracts.publication.withdrawModal.reasonPlaceholder', 'e.g. Replaced by v2.0.0')}
              size="sm"
            />
          </>
        )}
      </Modal>
```

Remover os imports de `AlertTriangle`/`XCircle`/`CheckCircle2`/`Clock`/`EyeOff` que deixem de ser usados após remover o markup cru — **verificar** quais permanecem: `EyeOff` (badge + botão withdraw), `AlertTriangle`/`Clock`/`CheckCircle2`/`XCircle` (usados em `PublicationStatusBadge`). Portanto só remover o que o markup cru usava e mais nada usa (nenhum, neste caso — todos continuam no `PublicationStatusBadge`). Não remover imports ainda usados.

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/PublicationCenterPage.hardening.test.tsx --run`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/publication/PublicationCenterPage.tsx src/frontend/src/__tests__/contracts/PublicationCenterPage.hardening.test.tsx
git commit -m "feat(contracts): publication entries link to the contract; withdraw uses DS Modal

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Spectral — confirmação de eliminação via DS `Modal`

**Files:**
- Modify: `src/frontend/src/features/contracts/spectral/SpectralRulesetManagerPage.tsx`
- Test: `src/frontend/src/__tests__/contracts/SpectralRulesetManagerPage.deleteConfirm.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/SpectralRulesetManagerPage.deleteConfirm.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { SpectralRulesetManagerPage } from '../../features/contracts/spectral/SpectralRulesetManagerPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
const ruleset = { id: 'rs-1', name: 'core-rules', description: 'd', rulesetType: 'Custom', isDefault: false, isActive: true, createdAt: '2026-01-01T00:00:00Z', content: '' };
const deleteMutate = vi.fn();
vi.mock('../../features/contracts/hooks', () => ({
  useSpectralRulesets: () => ({ data: { items: [ruleset] }, isLoading: false, isError: false, refetch: vi.fn() }),
  useToggleSpectralRuleset: () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteSpectralRuleset: () => ({ mutate: deleteMutate, isPending: false }),
  useCreateSpectralRuleset: () => ({ mutate: vi.fn(), isPending: false }),
}));

describe('SpectralRulesetManagerPage delete confirmation', () => {
  it('requires confirmation before deleting a ruleset', () => {
    render(<SpectralRulesetManagerPage />);
    fireEvent.click(screen.getByRole('button', { name: /delete/i }));
    // Não elimina imediatamente — abre confirmação.
    expect(deleteMutate).not.toHaveBeenCalled();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    // Confirmar dentro do diálogo dispara a mutation.
    const dialog = screen.getByRole('dialog');
    const confirm = within(dialog).getByRole('button', { name: /delete/i });
    fireEvent.click(confirm);
    expect(deleteMutate).toHaveBeenCalledWith('rs-1', expect.anything());
  });
});
```

(Adicionar o import `import { within } from '@testing-library/react';` no topo do teste.)

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/SpectralRulesetManagerPage.deleteConfirm.test.tsx --run`
Expected: FAIL — o delete chama a mutation imediatamente, sem diálogo.

- [ ] **Step 3: Write minimal implementation**

Adicionar `Modal` ao import do DS:

```tsx
import { Button, IconButton, Modal } from '../../../shared/ui';
```

Adicionar estado de alvo de eliminação (junto aos outros `useState`):

```tsx
  const [deleteTarget, setDeleteTarget] = useState<SpectralRuleset | null>(null);
```

O `IconButton` de delete passa a abrir a confirmação (substituir o `onClick`):

```tsx
                    onClick={() => setDeleteTarget(ruleset)}
```

Remover a função `handleDelete` antiga (deixou de ser usada) e adicionar o modal de
confirmação imediatamente antes do `<CreateRulesetModal ... />`:

```tsx
      <Modal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title={t('contracts.spectral.manager.deleteTitle', 'Delete ruleset')}
        size="sm"
        footer={
          <>
            <Button variant="outline" size="sm" onClick={() => setDeleteTarget(null)}>
              {t('common.cancel', 'Cancel')}
            </Button>
            <Button
              variant="danger"
              size="sm"
              loading={deleteMutation.isPending}
              disabled={deleteMutation.isPending}
              onClick={() => {
                if (deleteTarget) {
                  deleteMutation.mutate(deleteTarget.id, { onSuccess: () => setDeleteTarget(null) });
                }
              }}
            >
              {t('common.delete', 'Delete')}
            </Button>
          </>
        }
      >
        <p className="text-xs text-muted">
          {t('contracts.spectral.manager.deleteConfirm', 'Delete ruleset "{{name}}"? This cannot be undone.', { name: deleteTarget?.name ?? '' })}
        </p>
      </Modal>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/SpectralRulesetManagerPage.deleteConfirm.test.tsx --run`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/spectral/SpectralRulesetManagerPage.tsx src/frontend/src/__tests__/contracts/SpectralRulesetManagerPage.deleteConfirm.test.tsx
git commit -m "feat(contracts): confirm before deleting a Spectral ruleset (DS Modal)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Chaves i18n (4 locales)

**Files:**
- Modify: `src/frontend/src/locales/en.json`
- Modify: `src/frontend/src/locales/es.json`
- Modify: `src/frontend/src/locales/pt-BR.json`
- Modify: `src/frontend/src/locales/pt-PT.json`

Chaves a adicionar (valores por locale):

- `contracts.spectral.manager.deleteTitle` — en `Delete ruleset` · es `Eliminar ruleset` · pt-BR `Excluir ruleset` · pt-PT `Eliminar ruleset`
- `contracts.spectral.manager.deleteConfirm` — en `Delete ruleset "{{name}}"? This cannot be undone.` · es `¿Eliminar el ruleset "{{name}}"? Esta acción no se puede deshacer.` · pt-BR `Excluir o ruleset "{{name}}"? Esta ação não pode ser desfeita.` · pt-PT `Eliminar o ruleset "{{name}}"? Esta ação não pode ser anulada.`

- [ ] **Step 1: Adicionar as chaves aos 4 locales** (deep-merge sob `contracts.spectral.manager`, preservando chaves existentes; reescrever com `JSON.stringify(obj, null, 2)`).

- [ ] **Step 2: Validar i18n**

Run: `cd src/frontend && npm run validate:i18n`
Expected: PASS — 4 locales completos e em paridade.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/locales/en.json src/frontend/src/locales/es.json src/frontend/src/locales/pt-BR.json src/frontend/src/locales/pt-PT.json
git commit -m "i18n(contracts): spectral delete-confirm keys (4 locales)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: e2e + gates finais

**Files:**
- Create: `src/frontend/e2e/contract-enforcement.spec.ts`

- [ ] **Step 1: Escrever o e2e**

```ts
// src/frontend/e2e/contract-enforcement.spec.ts
import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/** E2E — a Publication liga cada entrada ao contrato. */
test.describe('Contract enforcement hardening', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/publication-center**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [{ publicationEntryId: 'pe-1', contractVersionId: 'cv-1', apiAssetId: 'a-1', contractTitle: 'orders-api', semVer: '1.0.0', status: 'Published', visibility: 'Public', publishedBy: 'me' }],
          totalCount: 1,
        }),
      }));
  });

  test('publication entry title links to the contract', async ({ page }) => {
    await page.goto('/contracts/publication');
    const link = page.getByRole('link', { name: 'orders-api' });
    await expect(link).toBeVisible({ timeout: 5_000 });
    await expect(link).toHaveAttribute('href', '/contracts/cv-1');
  });
});
```

- [ ] **Step 2: Correr o e2e**

Run (PowerShell): `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; $env:CI=""; npx playwright test --project=chromium e2e/contract-enforcement.spec.ts`
Expected: 1 passed.

- [ ] **Step 3: Gates finais**

Run: `cd src/frontend && npm run test -- --run 2>&1 | tail -5` → suite completa verde.
Run: `cd src/frontend && npm run validate:i18n` → PASS.
Run: `cd src/frontend && npm run build 2>&1 | tail -3` → exit 0.
Run: `cd src/frontend && npx eslint src/features/contracts/publication/PublicationCenterPage.tsx src/features/contracts/spectral/SpectralRulesetManagerPage.tsx` → 0 erros.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/e2e/contract-enforcement.spec.ts
git commit -m "test(contracts): e2e — publication entry links to the contract

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Self-Review

**1. Spec coverage:**
- §4.1 publication → contrato link → Task 1. ✓
- §4.2 withdraw via DS Modal → Task 1. ✓
- §4.3 spectral delete confirm → Task 2. ✓
- §7 i18n (deleteTitle, deleteConfirm; withdraw keys já existem) → Task 3. ✓
- §8 testes (publication link + dialog, spectral confirm, e2e) → Tasks 1, 2, 4. ✓

**2. Placeholder scan:** Sem TBD/TODO. Código completo em cada step. Task 1 Step 3 dá a instrução concreta sobre quais imports NÃO remover (todos ainda usados no `PublicationStatusBadge`).

**3. Type consistency:** `SpectralRuleset` (já importado no ficheiro) usado para `deleteTarget`; `deleteMutation.mutate(id, { onSuccess })` confere com a assinatura da mutation existente. `withdrawTarget`/`withdrawReason`/`handleWithdraw` preservados. Rota `/contracts/${entry.contractVersionId}` verbatim. `Modal` props (`open`/`onClose`/`title`/`size`/`footer`) conferem com a API real do DS.

# AI-Scaffold Creates Service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Inline execution (não subagent).

**Goal:** Fechar a jornada do AI-Scaffold — no passo de review, permitir registar o serviço scaffoldado no catálogo e continuar o setup no seu detalhe.

**Architecture:** Uma fatia sobre `AiScaffoldWizardPage`. No Step 4 (review), adicionar uma ação primária "Criar serviço no catálogo" que chama `serviceCatalogApi.registerService` (a mesma API do onboarding) com os dados do passo intent + defaults do template, e navega para `/services/:id`. Download ZIP passa a secundário.

**Tech Stack:** React 19, TypeScript 5.9, react-router-dom 7 (`useNavigate`, `useParams`), TanStack Query 5 (`useMutation`), Vitest + Testing Library, i18next (4 locales).

## Global Constraints

- Idioma de UI: **chaves i18n**, nunca strings hardcoded. Usar `t('key', 'English fallback')`.
- Novas chaves nos 4 locales (`en`, `es`, `pt-BR`, `pt-PT`) via script deep-merge; `npm run validate:i18n` **tem de passar**.
- Comandos npm a partir de `src/frontend`: `npm run test`, `npm run build`, `npm run validate:i18n`. Bash tool: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend` antes (o cwd volta à raiz entre chamadas).
- Testes centralizados em `src/frontend/src/__tests__/**`.
- **Armadilha do mock `t`:** `t:(k,f)=>typeof f==='string'?f:k` devolve o **fallback** quando há default string; devolve a **chave** quando não há. Para labels sem fallback (ex. `t('templates.scaffold.fields.serviceName')`) o texto é a própria chave. Preferir `getByPlaceholderText` com fallback (ex. `'payment-api'`) e `getByRole('button',{name:'Create service in catalog'})`.
- `serviceCatalogApi.registerService({ name, domain, team, description?, serviceType? }) => Promise<{ id: string }>` — importar de `../api/serviceCatalog` (não do barrel).
- `template` (de `templatesApi.getById`) é `TemplateDetail` com `serviceType`, `defaultDomain`, `defaultTeam`, `language`, `displayName`, `slug`, `version`, `description`, `hasBaseContract`, `hasScaffoldingManifest`.
- Cada commit termina com `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

### Task 1: F1 — "Criar serviço no catálogo" no passo de review

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/AiScaffoldWizardPage.tsx` (import `serviceCatalogApi`; nova mutation; ações do Step review)
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` (chaves `templates.scaffold.review.createService`, `templates.scaffold.review.createServiceError`)
- Test: `src/frontend/src/__tests__/catalog/AiScaffoldWizardPage.createService.test.tsx` (novo)

**Interfaces:**
- Consumes: `serviceCatalogApi.registerService(payload) => Promise<{ id: string }>`; `templatesApi.getById`, `templatesApi.generateWithAi`; estado local `serviceName`/`serviceDescription`/`teamName`/`domain`; `template.serviceType`/`defaultDomain`/`defaultTeam`.
- Produces: navegação `/services/${id}` após criação.

- [ ] **Step 1: Escrever o teste (falha)**

Criar `src/frontend/src/__tests__/catalog/AiScaffoldWizardPage.createService.test.tsx`:

```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { AiScaffoldWizardPage } from '../../features/catalog/pages/AiScaffoldWizardPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: unknown) => (typeof f === 'string' ? f : k) }) }));

const navigate = vi.fn();
vi.mock('react-router-dom', async (orig) => ({ ...(await orig() as object), useNavigate: () => navigate }));

const template = {
  id: 'tpl-1', slug: 'rest-dotnet', displayName: 'REST .NET', version: '1.0.0',
  description: 'A REST template', serviceType: 'RestApi', language: 'DotNet',
  defaultDomain: 'Payments', defaultTeam: 'Platform',
  hasBaseContract: true, hasScaffoldingManifest: true,
};
const scaffoldResult = {
  serviceName: 'payment-api', language: 'DotNet', isFallback: false,
  files: [{ path: 'src/Program.cs', content: 'class Program {}' }],
};
const getById = vi.fn().mockResolvedValue(template);
const generateWithAi = vi.fn().mockResolvedValue(scaffoldResult);
vi.mock('../../features/catalog/api/templates', () => ({
  templatesApi: {
    getById: (...a: unknown[]) => getById(...a),
    generateWithAi: (...a: unknown[]) => generateWithAi(...a),
  },
}));

const registerService = vi.fn().mockResolvedValue({ id: 'new-svc-id' });
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: { registerService: (...a: unknown[]) => registerService(...a) },
}));

function renderWizard() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/catalog/templates/tpl-1/scaffold']}>
        <Routes><Route path="/catalog/templates/:id/scaffold" element={<AiScaffoldWizardPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

/** Conduz o wizard até ao passo review. */
async function driveToReview() {
  renderWizard();
  // Step 1 (template) → Next
  fireEvent.click(await screen.findByRole('button', { name: 'templates.scaffold.next' }));
  // Step 2 (intent) → preencher obrigatórios
  fireEvent.change(screen.getByPlaceholderText('payment-api'), { target: { value: 'payment-api' } });
  fireEvent.change(screen.getByPlaceholderText('templates.scaffold.placeholders.serviceDescription'), { target: { value: 'Handles payments' } });
  // Generate
  fireEvent.click(screen.getByRole('button', { name: 'templates.scaffold.generate' }));
  // Step 4 (review) — botão de criar serviço aparece
  return screen.findByRole('button', { name: 'Create service in catalog' });
}

describe('AiScaffoldWizardPage — criar serviço no catálogo', () => {
  beforeEach(() => { navigate.mockClear(); registerService.mockClear(); generateWithAi.mockClear(); });

  it('mostra a ação de criar serviço no passo de review', async () => {
    const btn = await driveToReview();
    expect(btn).toBeInTheDocument();
  });

  it('cria o serviço com o payload mapeado e navega para o detalhe', async () => {
    const btn = await driveToReview();
    fireEvent.click(btn);
    await waitFor(() => expect(registerService).toHaveBeenCalledWith({
      name: 'payment-api',
      domain: 'Payments',
      team: 'Platform',
      description: 'Handles payments',
      serviceType: 'RestApi',
    }));
    await waitFor(() => expect(navigate).toHaveBeenCalledWith('/services/new-svc-id'));
  });
});
```

- [ ] **Step 2: Correr — falha**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- AiScaffoldWizardPage.createService --run`
Expected: FAIL (botão "Create service in catalog" não existe). Se falhar antes (ex. placeholder da descrição não encontrado), ajustar o seletor ao texto real do placeholder e confirmar que depois falha por ausência do botão.

- [ ] **Step 3: Adicionar o import de `serviceCatalogApi`**

No topo de `AiScaffoldWizardPage.tsx`, junto do import de `templatesApi`:
```tsx
import { templatesApi, type AiScaffoldResult, type ScaffoldedFile } from '../api/templates';
```
adicionar imediatamente a seguir:
```tsx
import { serviceCatalogApi } from '../api/serviceCatalog';
```

- [ ] **Step 4: Adicionar a mutation de criação de serviço**

Dentro de `AiScaffoldWizardPage`, a seguir a `generateMutation` (linhas ~120-127), adicionar:
```tsx
  const createServiceMutation = useMutation({
    mutationFn: () =>
      serviceCatalogApi.registerService({
        name: serviceName,
        domain: domain || template!.defaultDomain,
        team: teamName || template!.defaultTeam,
        description: serviceDescription,
        serviceType: template!.serviceType,
      }),
    onSuccess: (res) => {
      if (res?.id) navigate(`/services/${res.id}`);
    },
  });
```
(`template` está garantido não-nulo no passo review — o botão só renderiza aí; ainda assim, o `!` é seguro porque o review exige `result`, que só existe após `template` ter carregado.)

- [ ] **Step 5: Reescrever a zona de ações do Step review**

No Step 4 (`step === 'review' && result`), o bloco de ações atual é:
```tsx
          {/* Actions */}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <Button
              variant="outline"
              icon={<ArrowLeft className="h-4 w-4" />}
              onClick={() => setStep('intent')}
            >
              {t('templates.scaffold.review.regenerate')}
            </Button>

            <div className="flex gap-2">
              <Button
                variant="primary"
                className="bg-success hover:bg-success/90"
                icon={<Download className="h-4 w-4" />}
                onClick={handleDownloadZip}
              >
                {t('templates.scaffold.review.downloadZip')}
              </Button>
            </div>
          </div>
```
Substituir por (Download passa a secundário `outline`; Criar serviço primário; erro honesto):
```tsx
          {/* Actions */}
          <div className="flex flex-col gap-2">
            {createServiceMutation.isError && (
              <p className="text-xs text-critical">
                {t('templates.scaffold.review.createServiceError', 'Could not create the service in the catalog.')}
              </p>
            )}
            <div className="flex flex-wrap items-center justify-between gap-3">
              <Button
                variant="outline"
                icon={<ArrowLeft className="h-4 w-4" />}
                onClick={() => setStep('intent')}
              >
                {t('templates.scaffold.review.regenerate')}
              </Button>

              <div className="flex gap-2">
                <Button
                  variant="outline"
                  icon={<Download className="h-4 w-4" />}
                  onClick={handleDownloadZip}
                >
                  {t('templates.scaffold.review.downloadZip')}
                </Button>
                <Button
                  variant="primary"
                  icon={<Server className="h-4 w-4" />}
                  loading={createServiceMutation.isPending}
                  disabled={createServiceMutation.isPending || !serviceName}
                  onClick={() => createServiceMutation.mutate()}
                >
                  {t('templates.scaffold.review.createService', 'Create service in catalog')}
                </Button>
              </div>
            </div>
          </div>
```
(`Server` já está importado de `lucide-react`; `useMutation` já está importado.)

- [ ] **Step 6: Correr — passa**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- AiScaffoldWizardPage.createService --run`
Expected: PASS (2 testes).

- [ ] **Step 7: i18n (4 locales)**

Script Node (scratchpad):
```js
import { readFileSync, writeFileSync } from 'node:fs';
const dir = 'C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend/src/locales';
const byLoc = {
  en:      { createService: 'Create service in catalog', createServiceError: 'Could not create the service in the catalog.' },
  es:      { createService: 'Crear servicio en el catálogo', createServiceError: 'No se pudo crear el servicio en el catálogo.' },
  'pt-BR': { createService: 'Criar serviço no catálogo', createServiceError: 'Não foi possível criar o serviço no catálogo.' },
  'pt-PT': { createService: 'Criar serviço no catálogo', createServiceError: 'Não foi possível criar o serviço no catálogo.' },
};
function deepMerge(t, p) { for (const [k, v] of Object.entries(p)) { t[k] = (v && typeof v === 'object' && !Array.isArray(v)) ? deepMerge(t[k] && typeof t[k] === 'object' ? t[k] : {}, v) : v; } return t; }
for (const [loc, v] of Object.entries(byLoc)) {
  const path = `${dir}/${loc}.json`;
  const json = JSON.parse(readFileSync(path, 'utf8'));
  deepMerge(json, { templates: { scaffold: { review: { createService: v.createService, createServiceError: v.createServiceError } } } });
  writeFileSync(path, JSON.stringify(json, null, 2) + '\n', 'utf8');
}
```
Run: `node <script>.mjs` depois `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run validate:i18n`
Expected: PASS.

- [ ] **Step 8: Build + commit**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run build`
Expected: sem erros.
```bash
git add src/frontend/src/features/catalog/pages/AiScaffoldWizardPage.tsx src/frontend/src/locales/*.json "src/frontend/src/__tests__/catalog/AiScaffoldWizardPage.createService.test.tsx"
git commit -m "feat(catalog): AI-scaffold cria o serviço no catálogo (P5)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Revisão final e merge

- [ ] **Step 1: Suite completa + build + i18n**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- --run && npm run build && npm run validate:i18n`
Expected: tudo PASS.

- [ ] **Step 2: Revisão opus de todo o branch**

Dispatch do code-reviewer sobre o diff da P5 (desde o commit pré-P5 até HEAD). Corrigir Critical/Important com um único fix.

- [ ] **Step 3: Push em `main`** (sem PR, conforme instrução do owner)

Após a revisão retornar 0 Critical/0 Important:
```bash
git push origin main
```

---

## Self-Review

**1. Spec coverage:**
- F1 (botão "Criar serviço no catálogo" no review; payload mapeado; navigate para detalhe; Download secundário; erro honesto; guard duplo-clique) → Task 1 ✓
- i18n 4 locales + validate → Step 7 ✓
- Não-objetivos (sem repo git; sem tocar DevPortal/templates redesign; sem criar interface/contrato automático) → nenhuma task os viola ✓

**2. Placeholder scan:** sem TBD/TODO; código completo. A única nota de incerteza (seletor de placeholder da descrição no teste) tem instrução explícita de ajuste ao texto real. ✓

**3. Type consistency:** `registerService` payload (`name/domain/team/description/serviceType`) idêntico entre implementação, teste e assinatura da API; `template.defaultDomain/defaultTeam/serviceType` conforme `TemplateDetail`; navegação `/services/${id}` com o `id` de `{ id }`. Fallbacks i18n idênticos entre impl e teste ('Create service in catalog' / 'Could not create...'). ✓

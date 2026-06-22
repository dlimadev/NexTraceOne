# Long-tail de Contratos — passe de jornada v5 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Aplicar o padrão de jornada v5 às 4 sub-páginas long-tail de contratos (CDCT, Playground, Portal, PublicationCenter): `PageContainer`+`PageHeader`, controlos crus→DS, cores de status→tokens, estados loading/error/empty — sem alterar comportamento.

**Architecture:** Uma tarefa por página. Cada uma substitui o shell e os controlos pelo padrão DS já usado em todo o app, preservando queries/mutations/onChange/i18n. Nenhuma lógica de negócio muda.

**Tech Stack:** React 19, TypeScript 5.9, TanStack Query, Tailwind v4 (tokens `@theme`), DS `shared/ui`, Vitest.

## Global Constraints

- `npm run lint` + `npm run build` 0 erros; suíte total verde.
- Comportamento idêntico: preservar `useQuery`/`useMutation`, `value`/`onChange`/`placeholder`/`disabled` exatos, e **chaves i18n existentes** (não renomear; adicionar só se um label novo for mesmo introduzido).
- Sem `<button>`/`<input>`/`<select>`/`<textarea>` crus nas 4 páginas no fim — todos via DS de `shared/ui`.
- Cores de status cruas → tokens: `text-green-400`→`text-success`, `text-red-400`→`text-critical`, `text-orange-400`→`text-warning`, `text-white` (em botão accent)→`text-on-accent` (ou simplesmente `<Button variant="primary">`, que já usa `text-on-accent`).
- `bg-panel`, `bg-elevated`, `bg-canvas`, `border-edge`, `text-heading/body/muted`, `text-accent` são tokens válidos — **não** mexer.
- Mudança cirúrgica (lição do ciclo 9): `git add` só dos paths explícitos; `git diff --name-only main...HEAD` no fim.
- **Sem teste a renderizar estas páginas** → rede de segurança = `tsc`/build/lint + suíte + smoke visual (Task 5). Implementadores: meticulosos na preservação de `onChange`/lógica.

## Conversion Reference (usar em todas as tarefas)

**Imports DS** (caminho a partir de `features/contracts/<sub>/`):
```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField, TextArea, Select } from '../../../shared/ui';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
```

**APIs DS (confirmadas):**
- `TextField`: `label?`, `helperText?`, `error?`, `size?: 'sm'|'md'` + `...rest` (`value`, `onChange`, `placeholder`, `disabled`, `type`). Label embutido.
- `TextArea`: `label?`, `helperText?`, `error?` + `...rest` (`value`, `onChange`, `rows`, `placeholder`).
- `Select`: `label?`, `options: {value:string;label:string}[]`, `placeholder?`, `size?` + `...rest` (`value`, `onChange`, `disabled`).
- `Button`: `variant?: 'primary'|'secondary'|'outline'|'ghost'|'danger'|...`, `size?: 'xs'|'sm'|'md'|'lg'`, `loading?`, `icon?: ReactNode` + `...rest` (`onClick`, `disabled`, `type`). Renderiza ícone antes do texto.

**Shell:** root custom (`<div className="min-h-screen ...">`) → `<PageContainer>...</PageContainer>`.

**Header:** bloco custom (ícone + `<h1>` + `<p>` subtítulo) →
```tsx
<PageHeader
  title={t('<existing title key>', '<fallback>')}
  subtitle={t('<existing subtitle key>', '<fallback>')}
  actions={/* DS Button da CTA primária, OU omitir se a ação for contextual no card */}
/>
```

**Input (label separado + `<input>`):**
```tsx
// ANTES
<label className="...">{t('k','L')}</label>
<input type="text" value={x} onChange={(e)=>setX(e.target.value)} placeholder={t('p','P')} className="..." />
// DEPOIS
<TextField label={t('k','L')} value={x} onChange={(e)=>setX(e.target.value)} placeholder={t('p','P')} size="sm" />
```

**Textarea:** `<TextArea label={...} value={...} onChange={...} rows={6} />`.

**Botão CTA accent (`bg-accent text-white`):** `<Button variant="primary" size="sm" icon={<Icon size={14}/>} onClick={...} disabled={...}>{t(...)}</Button>`.
**Botão secundário/cancel (`text-muted hover:text-body`):** `<Button variant="ghost" size="sm" onClick={...}>{t(...)}</Button>`.
**Botão accent-text inline (`text-accent hover:text-accent/80`):** `<Button variant="ghost" size="sm" icon={<Plus size={12}/>} onClick={...} disabled={...}>{t(...)}</Button>`.

**Estados:**
```tsx
if (isLoading) return <PageContainer><PageLoadingState size="lg" /></PageContainer>;
if (isError) return <PageContainer><PageErrorState onRetry={() => refetch()} /></PageContainer>;
// vazio (inline, dentro do card): <EmptyState .../> ou o bloco vazio existente com tokens.
```

---

### Task 1: CDCT — `ConsumerDrivenContractPage.tsx`

**Files:**
- Modify: `src/frontend/src/features/contracts/cdct/ConsumerDrivenContractPage.tsx`

**Interfaces:** Consumes a Conversion Reference acima. Página independente; nada produzido para outras tasks.

Conversões exatas (a página já foi mapeada — aplicar todas):
- **Shell:** root `<div className="min-h-screen bg-canvas px-6 py-6 text-body">` → `<PageContainer>`. O `text-body` cai (PageContainer/tema já o dá).
- **Header** (linhas ~94-108): bloco ícone+`<h1>`+`<p>` → `<PageHeader title={t('contracts.cdct.title','Consumer-Driven Contract Testing')} subtitle={t('contracts.cdct.subtitle', '...')} />` (sem CTA — "Register Expectation" é contextual no card).
- **Seletor de contrato** (linhas ~110-148): os 2 `<input>` (apiAssetId, versionId) → `TextField` (label embutido = as keys `apiAssetId`/`versionId`); o botão "Verify" → `<Button variant="primary" size="sm" icon={<RefreshCw size={12} className={loadingCdct ? 'animate-spin' : ''} />} onClick={() => runCdct()} disabled={!apiAssetId || !versionId || loadingCdct}>{t('contracts.cdct.verify','Verify')}</Button>`. Manter o layout: versionId + Verify ficam num `flex gap-2` (TextField cresce, Button ao lado — como o Button traz altura própria, alinhar com `items-end` se o TextField tiver label por cima).
- **Stat cards** (linhas ~159-171): `text-green-400`→`text-success`, `text-red-400`→`text-critical`.
- **Resultados** (linhas ~175-202): `text-green-400`→`text-success`, `text-red-400`→`text-critical`, `text-orange-400`→`text-warning`.
- **Botão "Register Expectation"** (linhas ~217-225, `text-accent hover:text-accent/80`): `<Button variant="ghost" size="sm" icon={<Plus size={12}/>} onClick={() => setShowNewForm(true)} disabled={!apiAssetId}>{t('contracts.cdct.addExpectation','Register Expectation')}</Button>`.
- **Form de nova expectativa** (linhas ~255-327): os 2 `<input>` (consumerServiceName, consumerDomain) → `TextField`; o `<textarea>` (expectedSubsetJson) → `<TextArea label={t('contracts.cdct.expectedSubset','Expected Subset (JSON)')} value={newExpectation.expectedSubsetJson} onChange={(e)=>setNewExpectation((p)=>({...p, expectedSubsetJson: e.target.value}))} rows={6} className="font-mono" />`; o `<input>` notes → `TextField`. Botão "Register" → `<Button variant="primary" size="sm" icon={<ShieldAlert size={12}/>} onClick={() => registerMutation.mutate()} disabled={!newExpectation.consumerServiceName || registerMutation.isPending}>{t('contracts.cdct.register','Register')}</Button>`; botão "Cancel" → `<Button variant="ghost" size="sm" onClick={() => setShowNewForm(false)}>{t('common.cancel','Cancel')}</Button>`.
- **Empty/loading inline** (linhas ~228-236): manter, já usa tokens (`text-muted`); opcional trocar o bloco vazio por `<EmptyState>` — manter o existente é aceitável (já token-clean).
- O `PageErrorState` no topo (linhas ~84-90) já está correto — manter.
- **Limpeza:** remover do import `lucide-react` os ícones que deixarem de ser usados (verificar com grep no ficheiro; `ShieldCheck` era do header — se o PageHeader não o usar, removê-lo).

- [ ] **Step 1: Aplicar as conversões acima**

Preservar todo o `value`/`onChange`/`disabled` e as queries/mutations. Não tocar em `formatJson`, tipos, ou lógica.

- [ ] **Step 2: Lint + build**

Run: `cd src/frontend && npm run lint && npm run build`
Expected: 0 erros (sem imports não usados, sem controlos crus restantes).

- [ ] **Step 3: Confirmar ausência de controlos crus e cores de status**

Run: `cd src/frontend && grep -nE "<(button|input|select|textarea)\b|text-(green|red|orange)-400|text-white" src/features/contracts/cdct/ConsumerDrivenContractPage.tsx`
Expected: sem resultados (ou só ocorrências legítimas dentro de strings, não JSX).

- [ ] **Step 4: Suíte de contratos**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts`
Expected: PASS.

- [ ] **Step 5: Higiene do diff + commit**

Run: `git diff --name-only main...HEAD`
Expected: só este ficheiro (+ docs).
```bash
git add src/frontend/src/features/contracts/cdct/ConsumerDrivenContractPage.tsx
git commit -m "feat(contracts): CDCT page no padrao de jornada v5

PageContainer+PageHeader, controlos crus->DS, cores status->tokens.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: PublicationCenter — `PublicationCenterPage.tsx`

**Files:**
- Modify: `src/frontend/src/features/contracts/publication/PublicationCenterPage.tsx`

**Interfaces:** Consumes a Conversion Reference. Já usa `PageContainer` + `Card` + estados (`PageErrorState`/`EmptyState`/`isLoading`).

- [ ] **Step 1: Ler a página inteira**

Run: ler `src/frontend/src/features/contracts/publication/PublicationCenterPage.tsx` (243 linhas) por completo antes de editar.

- [ ] **Step 2: Aplicar o padrão v5**

- Adicionar `<PageHeader title subtitle [actions]>` logo dentro do `PageContainer` (reutilizar a key de título/subtítulo já existente; se houver uma CTA de página clara — ex. "Publish"/"Export" no topo — passá-la como `actions` em DS `Button`; senão, só título+subtítulo).
- Converter os 4 `<button>` crus → DS `Button` (variante por papel: primary p/ CTA, ghost p/ secundária).
- Converter o 1 `<input>` → `TextField` (label embutido).
- Cores de status cruas (se houver `text-green/red/orange-400` ou `text-white` em botão) → tokens (`text-success`/`text-critical`/`text-warning`/`text-on-accent`). Confirmar com grep.
- Preservar queries/mutations/onChange/i18n; não refatorar lógica.
- Remover imports `lucide-react`/`<button>` agora não usados.

- [ ] **Step 3: Lint + build**

Run: `cd src/frontend && npm run lint && npm run build`
Expected: 0 erros.

- [ ] **Step 4: Confirmar ausência de controlos crus**

Run: `cd src/frontend && grep -nE "<(button|input|select|textarea)\b|text-(green|red|orange)-400|text-white" src/features/contracts/publication/PublicationCenterPage.tsx`
Expected: sem resultados em JSX.

- [ ] **Step 5: Suíte + commit**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts`
Expected: PASS.
```bash
git add src/frontend/src/features/contracts/publication/PublicationCenterPage.tsx
git commit -m "feat(contracts): PublicationCenter page no padrao de jornada v5

PageHeader + botoes/input crus->DS + cores status->tokens.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Playground — `ContractPlaygroundPage.tsx`

**Files:**
- Modify: `src/frontend/src/features/contracts/playground/ContractPlaygroundPage.tsx`

**Interfaces:** Consumes a Conversion Reference. Inventário conhecido: 4 `<button>`, 5 `<input>/<select>`, tem `isLoading` mas **falta** tratamento de erro/empty.

- [ ] **Step 1: Ler a página inteira** (357 linhas) antes de editar.

- [ ] **Step 2: Aplicar o padrão v5**

- Shell → `PageContainer`; header custom → `<PageHeader title subtitle [actions]>` (CTA só se houver ação de página clara; senão título+subtítulo).
- 5 `<input>/<select>` → `TextField`/`Select` (label embutido; `Select` precisa de `options:{value,label}[]`).
- 4 `<button>` → DS `Button` (variante por papel).
- **Adicionar estados em falta:** após o `useQuery`, `if (isError) return <PageContainer><PageErrorState onRetry={() => refetch()} /></PageContainer>;` (usar o `refetch` da query relevante) e um empty (`EmptyState` ou bloco vazio com tokens) quando não há dados.
- Cores de status cruas → tokens (confirmar com grep).
- Preservar queries/mutations/onChange/i18n; remover imports não usados.

- [ ] **Step 3: Lint + build**

Run: `cd src/frontend && npm run lint && npm run build`
Expected: 0 erros.

- [ ] **Step 4: Confirmar ausência de controlos crus**

Run: `cd src/frontend && grep -nE "<(button|input|select|textarea)\b|text-(green|red|orange)-400|text-white" src/features/contracts/playground/ContractPlaygroundPage.tsx`
Expected: sem resultados em JSX.

- [ ] **Step 5: Suíte + commit**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts`
Expected: PASS.
```bash
git add src/frontend/src/features/contracts/playground/ContractPlaygroundPage.tsx
git commit -m "feat(contracts): Playground page no padrao de jornada v5

PageContainer+PageHeader, controlos crus->DS, estados error/empty, cores->tokens.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Portal — `ContractPortalPage.tsx`

**Files:**
- Modify: `src/frontend/src/features/contracts/portal/ContractPortalPage.tsx`

**Interfaces:** Consumes a Conversion Reference. Já usa `PageContainer`+`Card`+estados (`EmptyState`/`isError`/`isLoading`). Ficheiro grande (716 linhas) → mudança **cirúrgica** só no header + 3 botões + cores status; **não** refatorar o resto.

- [ ] **Step 1: Localizar os pontos a mudar**

Run: `cd src/frontend && grep -nE "<button|text-(green|red|orange)-400|text-white|<h1" src/features/contracts/portal/ContractPortalPage.tsx`
Ler as zonas em torno de cada resultado (não o ficheiro inteiro).

- [ ] **Step 2: Aplicar o padrão v5 (cirúrgico)**

- Adicionar `<PageHeader title subtitle [actions]>` logo dentro do `PageContainer` (reutilizar a key de título já existente — provavelmente um `<h1>` atual; converter esse `<h1>`/subtítulo no PageHeader). CTA só se houver ação de página clara.
- 3 `<button>` crus → DS `Button`.
- Quaisquer `text-green/red/orange-400`/`text-white` → tokens.
- **Não** tocar no resto da página (cards, listas, lógica).
- Remover imports não usados resultantes.

- [ ] **Step 3: Lint + build**

Run: `cd src/frontend && npm run lint && npm run build`
Expected: 0 erros.

- [ ] **Step 4: Confirmar ausência de controlos crus + cores status**

Run: `cd src/frontend && grep -nE "<button\b|text-(green|red|orange)-400|text-white" src/features/contracts/portal/ContractPortalPage.tsx`
Expected: sem resultados em JSX.

- [ ] **Step 5: Suíte + commit**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts`
Expected: PASS.
```bash
git add src/frontend/src/features/contracts/portal/ContractPortalPage.tsx
git commit -m "feat(contracts): Portal page ganha PageHeader + botoes DS (jornada v5)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Smoke visual + suíte total (checkpoint manual)

**Files:** nenhum (verificação).

- [ ] **Step 1: Suíte total**

Run: `cd src/frontend && npm run test`
Expected: suíte total verde.

- [ ] **Step 2: Smoke visual**

Run: `cd src/frontend && npm run dev`. Abrir as 4 rotas e confirmar:
- Header v5 (PageHeader) no topo de cada página.
- Inputs/selects/botões com aspeto DS consistente; forms de CDCT/Playground funcionais (escrever, submeter).
- Stat/resultados com cores semânticas (sucesso=verde-token, erro=vermelho-token, aviso=âmbar-token) — não cores cruas.
- Estados de loading/erro/vazio renderizam.

- [ ] **Step 3: Registar resultado**

Sem código se OK. Se algo regredir, correção cirúrgica no ficheiro afetado.

---

## Self-Review

**Spec coverage:**
- Shell→PageContainer + header→PageHeader → Tasks 1–4 (Step shell/header). ✅
- Controlos crus→DS → Tasks 1–4 + Conversion Reference. ✅
- Cores status→tokens → Tasks 1–4 (grep de confirmação). ✅
- Estados loading/error/empty (CDCT/Playground em falta) → Task 1 (mantém PageErrorState), Task 3 (adiciona error/empty). ✅
- Preservar queries/mutations/onChange/i18n → Global Constraints + cada task. ✅
- Lint/build/suíte + diff hygiene → cada task Steps + Task 5. ✅
- bg-panel/elevated/etc. intocados → Global Constraints. ✅

**Placeholder scan:** as Tasks 2–4 instruem "ler a página e aplicar a referência" porque só a CDCT foi mapeada linha-a-linha; a Conversion Reference dá o código concreto de cada transformação (inputs/botões/estados/header), por isso não há lacuna de "como". Task 1 (CDCT) tem as conversões enumeradas com linhas. Sem TBD/TODO. ✅

**Type consistency:** APIs DS (`TextField`/`TextArea`/`Select`/`Button` props) confirmadas do código e usadas de forma consistente; mapa de cores status idêntico em todas as tasks; imports DS idênticos. ✅

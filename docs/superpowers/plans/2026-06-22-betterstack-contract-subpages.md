# Sub-páginas de Contratos (lote 2) — passe de jornada v5 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Converter os controlos HTML crus de 7 sub-páginas de contratos para componentes DS, e fazer o sweep de cores legacy→tokens na ContractMigrationPage — sem alterar comportamento.

**Architecture:** Uma tarefa por página. O `PageHeader` já existe em todas (header v5 feito), por isso o trabalho é controlos `<button>/<input>/<select>/<textarea>` → DS `shared/ui`, preservando queries/mutations/onChange/i18n. A ContractMigrationPage leva ainda um sweep de cores cruas + `dark:` manual → tokens semânticos.

**Tech Stack:** React 19, TypeScript 5.9, TanStack Query, Tailwind v4 (tokens `@theme`), DS `shared/ui`, Vitest.

## Global Constraints

- `npm run lint` + `npm run build` 0 erros; suíte total verde.
- Comportamento idêntico: preservar `useQuery`/`useMutation`, `value`/`onChange`/`disabled`, e chaves i18n existentes (reutilizar; adicionar só se mesmo necessário).
- Zero `<button>/<input>/<select>/<textarea>` crus nas 7 páginas — todos via DS de `shared/ui`.
- **Link-como-botão:** usar `useNavigate()` + `<Button onClick={() => navigate(path)}>`. NUNCA `<Link><Button>` (o `Button.asChild` está declarado mas NÃO implementado). Um `<Link>` de texto normal pode ficar.
- **Preservar testes:** 4 páginas têm teste (ver por-tarefa). Após converter, correr o teste dessa página; manter texto/papéis acessíveis/`data-testid`. Se um selector partir, preferir preservar o nome; só atualizar o teste se a mudança for legítima e mínima (e justificar no report).
- Tokens válidos: `bg-panel/elevated/canvas`, `border-edge`, `text-heading/body/muted/faded`, `text-accent`, `text-on-accent`, `bg-accent`, e os pares semânticos `text-{success,warning,critical,info}` + `bg-{success,warning,critical,info}-muted`. NÃO usar cores cruas Tailwind.
- Mudança cirúrgica (lição ciclo 9): `git add` só dos paths explícitos; `git diff --name-only main...HEAD` no fim.
- 3 páginas sem teste (ContractGovernance, ContractHealthTimeline, CanonicalEntityImpactCascade) → rede de segurança = tsc/build/lint + smoke (Task 8).

## Conversion Reference (usar em todas as tarefas)

**Imports DS** (caminho a partir de `features/contracts/<sub>/` = 3 níveis até `src/`):
```tsx
import { Button, TextField, TextArea, Select } from '../../../shared/ui';
import { useNavigate } from 'react-router-dom'; // só se houver link-nav a converter
```
(PageContainer/PageHeader/estados já estão importados nestas páginas — manter.)

**APIs DS:**
- `Button`: `variant?: 'primary'|'secondary'|'outline'|'ghost'|'danger'`, `size?: 'xs'|'sm'|'md'|'lg'`, `loading?`, `icon?: ReactNode` + ...rest(`onClick`,`disabled`,`type`,`title`,`data-testid`). Ícone antes do texto.
- `TextField`: `label?`,`helperText?`,`error?`,`size?:'sm'|'md'` + ...rest(`value`,`onChange`,`placeholder`,`disabled`,`type`). `className` vai para o input.
- `TextArea`: `label?`,`error?` + ...rest(`value`,`onChange`,`rows`,`placeholder`). Estilo do inner via `textareaClassName` (NÃO `className`).
- `Select`: `label?`,`options:{value:string;label:string}[]`,`placeholder?`,`size?` + ...rest(`value`,`onChange`,`disabled`).

**Patterns:**
- `<button onClick className="...accent-filled...">{x}</button>` → `<Button variant="primary" size="sm" icon={<Icon size={14}/>} onClick disabled>{x}</Button>`
- text-only button (`text-muted hover:...`) → `<Button variant="ghost" size="sm" ...>`; bordered → `variant="outline"`; destructive → `variant="danger"`.
- `<button onClick={() => navigate(p)}>` / `<Link to=p>` styled as button → `useNavigate` + `<Button onClick={() => navigate(p)}>`.
- `<label>+<input>` → `<TextField label value onChange placeholder size="sm" />`; `<select>` → `<Select label options value onChange size="sm" />`; `<textarea>` → `<TextArea label value onChange rows />`.
- Preservar `{!isReadOnly && (...)}`/guards e `e.stopPropagation()` exatos.
- Remover imports `lucide-react`/`Link` que deixarem de ser usados (grep antes).

---

### Task 1: ContractGovernancePage (`governance/ContractGovernancePage.tsx`)

**Files:** Modify `src/frontend/src/features/contracts/governance/ContractGovernancePage.tsx` (120 linhas, 2 controlos). Sem teste dedicado.

- [ ] **Step 1: Ler a página inteira.**
- [ ] **Step 2: Aplicar a Conversion Reference** — converter os 2 `<button>` crus para DS `Button` (variante por papel; `useNavigate` se for link-nav). Preservar onClick/disabled/i18n. Remover imports órfãos.
- [ ] **Step 3: Verificar.** `cd src/frontend && npm run lint && npm run build` → 0 erros. `grep -nE "<(button|input|select|textarea)\b" src/features/contracts/governance/ContractGovernancePage.tsx` → sem JSX.
- [ ] **Step 4: Suíte de contratos + pages.** `npx vitest run src/__tests__/contracts src/__tests__/pages` → pass.
- [ ] **Step 5: Diff hygiene + commit.** `git diff --name-only main...HEAD` (só este ficheiro + docs).
```bash
git add src/frontend/src/features/contracts/governance/ContractGovernancePage.tsx
git commit -m "feat(contracts): ContractGovernance controlos crus->DS (jornada v5)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: ContractHealthTimelinePage (`governance/ContractHealthTimelinePage.tsx`)

**Files:** Modify `src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx` (189, 3 controlos). Sem teste dedicado.

- [ ] **Step 1:** Ler a página inteira.
- [ ] **Step 2:** Converter os 3 controlos crus → DS (Conversion Reference). Preservar comportamento/i18n; remover imports órfãos.
- [ ] **Step 3:** `npm run lint && npm run build` → 0; `grep -nE "<(button|input|select|textarea)\b" .../ContractHealthTimelinePage.tsx` → sem JSX.
- [ ] **Step 4:** `npx vitest run src/__tests__/contracts src/__tests__/pages` → pass.
- [ ] **Step 5:** Diff hygiene + commit.
```bash
git add src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx
git commit -m "feat(contracts): ContractHealthTimeline controlos crus->DS (jornada v5)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: CanonicalEntityImpactCascadePage (`canonical/CanonicalEntityImpactCascadePage.tsx`)

**Files:** Modify `src/frontend/src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx` (243, 5 controlos). Sem teste dedicado.

- [ ] **Step 1:** Ler a página inteira.
- [ ] **Step 2:** Converter os 5 controlos crus → DS. Preservar; remover órfãos.
- [ ] **Step 3:** `npm run lint && npm run build` → 0; grep → sem JSX.
- [ ] **Step 4:** `npx vitest run src/__tests__/contracts src/__tests__/pages` → pass.
- [ ] **Step 5:** Diff hygiene + commit.
```bash
git add src/frontend/src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx
git commit -m "feat(contracts): CanonicalEntityImpactCascade controlos crus->DS (jornada v5)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: SpectralRulesetManagerPage (`spectral/SpectralRulesetManagerPage.tsx`) — TEM TESTE

**Files:** Modify `src/frontend/src/features/contracts/spectral/SpectralRulesetManagerPage.tsx` (214, 4 controlos). Teste: `src/frontend/src/__tests__/pages/SpectralRulesetManagerPage.test.tsx`.

- [ ] **Step 1:** Ler a página inteira E o teste (para saber que selectors preservar).
- [ ] **Step 2:** Converter os 4 controlos crus → DS, preservando texto/role/`data-testid` que o teste usa.
- [ ] **Step 3:** `npm run lint && npm run build` → 0; grep → sem JSX.
- [ ] **Step 4:** Correr o teste da página: `npx vitest run src/__tests__/pages/SpectralRulesetManagerPage.test.tsx` → PASS (atualizar o teste só se um selector partir por razão legítima, mínima, e justificar). Depois `npx vitest run src/__tests__/contracts src/__tests__/pages` → pass.
- [ ] **Step 5:** Diff hygiene + commit (incluir o teste só se foi tocado).
```bash
git add src/frontend/src/features/contracts/spectral/SpectralRulesetManagerPage.tsx
git commit -m "feat(contracts): SpectralRulesetManager controlos crus->DS (jornada v5)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: CanonicalEntityCatalogPage (`canonical/CanonicalEntityCatalogPage.tsx`) — TEM TESTE

**Files:** Modify `src/frontend/src/features/contracts/canonical/CanonicalEntityCatalogPage.tsx` (288, 5 controlos). Teste: `src/frontend/src/__tests__/pages/CanonicalEntityCatalogPage.test.tsx`.

- [ ] **Step 1:** Ler a página inteira E o teste.
- [ ] **Step 2:** Converter os 5 controlos crus → DS, preservando selectors do teste.
- [ ] **Step 3:** `npm run lint && npm run build` → 0; grep → sem JSX.
- [ ] **Step 4:** `npx vitest run src/__tests__/pages/CanonicalEntityCatalogPage.test.tsx` → PASS; depois `npx vitest run src/__tests__/contracts src/__tests__/pages` → pass.
- [ ] **Step 5:** Diff hygiene + commit.
```bash
git add src/frontend/src/features/contracts/canonical/CanonicalEntityCatalogPage.tsx
git commit -m "feat(contracts): CanonicalEntityCatalog controlos crus->DS (jornada v5)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: ContractStudioPage (`pages/ContractStudioPage.tsx`) — TEM TESTE

**Files:** Modify `src/frontend/src/features/contracts/pages/ContractStudioPage.tsx` (376, 4 botões de ação). Teste: `src/frontend/src/__tests__/contracts/ContractStudioPage.test.tsx`.

Os 4 botões crus: Resume (linha ~131, `onResume`), Design (~224, `onDesign` com `stopPropagation`), Import (~231, `onImport` com `stopPropagation`), New (~264, `navigate('/contracts/new')`). Converter para DS `Button` (Design/Import provavelmente `ghost`/`outline` por serem ações de card; New → `primary` com `useNavigate` já existente ou nova; Resume → por papel). Preservar `e.stopPropagation()` e os onClick exatos.

- [ ] **Step 1:** Ler a página inteira E o teste.
- [ ] **Step 2:** Converter os 4 botões → DS `Button`, preservando handlers (incl. stopPropagation) e os selectors do teste.
- [ ] **Step 3:** `npm run lint && npm run build` → 0; `grep -nE "<button\b" src/features/contracts/pages/ContractStudioPage.tsx` → sem JSX.
- [ ] **Step 4:** `npx vitest run src/__tests__/contracts/ContractStudioPage.test.tsx` → PASS; depois `npx vitest run src/__tests__/contracts src/__tests__/pages` → pass.
- [ ] **Step 5:** Diff hygiene + commit.
```bash
git add src/frontend/src/features/contracts/pages/ContractStudioPage.tsx
git commit -m "feat(contracts): ContractStudio botoes de acao->DS Button (jornada v5)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: ContractMigrationPage (`governance/ContractMigrationPage.tsx`) — TEM TESTE + SWEEP DE CORES

**Files:** Modify `src/frontend/src/features/contracts/governance/ContractMigrationPage.tsx` (311, 7 controlos + 13 cores cruas + `dark:` manual). Teste: `src/frontend/src/__tests__/pages/ContractMigrationPage.test.tsx`. É a página mais pesada — fazer com cuidado.

**Mapa de cores (substituir os pares light/dark manuais pelo token único, que já é theme-aware):**
- `text-red-500` → `text-critical`; `text-yellow-500` → `text-warning`; `text-green-500` / `text-green-300` → `text-success`.
- badge `cn(base, 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300')` → `cn(base, 'bg-critical-muted text-critical')`; idem `yellow`→`bg-warning-muted text-warning`; `green`→`bg-success-muted text-success`.
- link `text-blue-600 hover:text-blue-700 dark:text-blue-400` → `text-accent hover:text-accent/80`.
- botão/tab `bg-blue-600 text-white` (e `hover:bg-blue-700`) → converter o `<button>` para `<Button variant="primary">` (que já dá `bg-accent text-on-accent hover:bg-accent-hover`); onde for um estado ativo não-botão, usar `bg-accent text-on-accent`.
- banner de erro `bg-red-50 border border-red-200 ... text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300` → `bg-critical-muted border border-critical/25 text-critical`.

**Controlos:** converter os 7 `<button>/<input>/<select>/<textarea>` → DS conforme a Reference (alguns são as tabs `bg-blue-600`; converter a tabs para `Button` com variante ativa/inativa, OU manter a estrutura e aplicar tokens — preferir DS `Button` por papel).

- [ ] **Step 1:** Ler a página inteira E o teste.
- [ ] **Step 2:** Aplicar o mapa de cores (remover todos os pares `dark:` manuais migrados) E converter os 7 controlos → DS. Preservar onClick/disabled/i18n/selectors do teste.
- [ ] **Step 3:** `npm run lint && npm run build` → 0. Confirmar limpeza:
  `grep -nE "<(button|input|select|textarea)\b|(red|green|blue|yellow|orange|purple|pink|indigo|emerald|amber|teal|cyan|violet|sky|rose|lime|slate|gray|zinc)-(50|100|200|300|400|500|600|700|800|900)|text-white|dark:" src/features/contracts/governance/ContractMigrationPage.tsx`
  Expected: sem resultados em JSX (nem cores cruas nem `dark:` manual).
- [ ] **Step 4:** `npx vitest run src/__tests__/pages/ContractMigrationPage.test.tsx` → PASS; depois `npx vitest run src/__tests__/contracts src/__tests__/pages` → pass.
- [ ] **Step 5:** Diff hygiene + commit.
```bash
git add src/frontend/src/features/contracts/governance/ContractMigrationPage.tsx
git commit -m "feat(contracts): ContractMigration controlos->DS + cores legacy/dark->tokens (jornada v5)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 8: Smoke visual + suíte total (checkpoint manual)

- [ ] **Step 1:** `cd src/frontend && npm run test` → suíte total verde.
- [ ] **Step 2:** `npm run dev`; abrir as 7 rotas. Confirmar: controlos com aspeto DS; ContractMigration correta em **dark E light** (o sweep removeu o `dark:` manual — verificar ambos os temas); badges de severidade com cores semânticas; estados loading/error/empty.
- [ ] **Step 3:** Registar. Sem código se OK; senão correção cirúrgica no ficheiro afetado.

---

## Self-Review

**Spec coverage:**
- Controlos crus→DS nas 7 páginas → Tasks 1–7. ✅
- ContractMigration sweep de cores + dark→tokens → Task 7 (mapa + grep). ✅
- useNavigate, nunca `<Link><Button>` → Global Constraints + Reference. ✅
- Preservar testes das 4 páginas com teste → Tasks 4,5,6,7 (Step 4 corre o teste da página). ✅
- Estados loading/error/empty → verificados nas tasks + Task 8 smoke. ✅
- Preservar queries/mutations/onChange/i18n → Global Constraints + cada task. ✅
- Diff hygiene → cada task Step 5. ✅

**Placeholder scan:** Tasks 1–6 são recipe-driven ("ler a página, aplicar a Reference") porque os controlos variam por página e a Reference dá o código concreto de cada transformação; Task 7 (a única com trabalho não-óbvio: cores) tem o mapa explícito. Sem TBD. ✅

**Type consistency:** APIs DS idênticas em todas as tasks; mapa de cores usa tokens confirmados (`text-{critical,warning,success}`, `bg-{critical,warning,success}-muted`, `text-accent`, `text-on-accent`, `bg-accent`); caminhos de teste corretos por página. ✅

# Finalização do Módulo Catálogo (Serviços) + Contratos — Plano

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development. Steps usam checkbox (`- [ ]`).

**Contexto:** Auditoria factual (2026-07-06) mostra que o módulo catálogo+contratos está ~98% concluído — tema/tokens/DS app-wide (ciclos 1-22), descoberta browse-first entregue para serviços e contratos (ciclo 24). Este plano fecha o **long-tail residual verificado**, não refaz o que está feito.

**Estado real (auditoria):**
- Controlos HTML crus restantes (excl. testes): **6 ficheiros**, todos em superfícies de editor/builder de contratos — mas a maioria são **forms standard** embebidos, não Monaco → convertíveis.
- Zero TODO/FIXME/placeholder reais (matches eram a palavra "Todo/Todos" em comentários).
- 1 componente órfão (`ServiceRegistrationWizard`, só o tipo é reusado).
- Health-strip do detalhe de serviço com placeholders (falta wiring de dados).
- Minors diferidos das superfícies browse (ciclo 24).

## Global Constraints
- Design system apenas (`components/*`) + tokens semânticos. Zero controlos HTML crus (exceto exceções deliberadas identificadas), zero cores hardcoded.
- Texto de UI só por i18n `t('...')`; paridade nos 4 locales (`en/es/pt-BR/pt-PT`); `validate:i18n` PASS.
- `npx tsc --noEmit`, `npx eslint`, `npx vitest run` verdes ao fim de cada tarefa (de `src/frontend`). Suite COMPLETA verde no fim.
- Commits atómicos por tarefa. Branch nova `redesign/betterstack-catalog-finalization` (de `main`).
- **PRESERVAR exceções deliberadas** (não são dívida): seletores de tipo **color-coded** do `SchemaPropertyEditor` (object=purple/$ref=pink/oneOf=orange — cor = significado), editores Monaco (`ContractSection`, `MonacoEditorWrapper`), color-pickers, radios sem equivalente DS, taxonomias de cor (status/data-viz).

---

### Task 1: Forms standard das secções do Contract Workspace → DS

**Files:**
- Modify: `src/frontend/src/features/contracts/workspace/sections/SecuritySection.tsx` (input/textarea/select em field-helpers, ~L259/268/277)
- Modify: `src/frontend/src/features/contracts/workspace/sections/DefinitionSection.tsx` (idem, ~L234/243/252)

**Contexto:** os passes do ciclo 10 (workspace) deixaram os forms internos destas secções intocados por serem "dentro do editor". Mas são field-helpers de form standard (input text, textarea, select) com classes token — convertíveis sem risco de partir Monaco.

- [ ] **Step 1:** Ler os field-helpers internos. Substituir `<input>`→`TextField`, `<textarea>`→`TextArea`, `<select>`→`Select` (de `components/*`), preservando `value/onChange/placeholder/disabled/rows` e o comportamento. Manter a densidade (`size="sm"`/`text-xs`) equivalente.
- [ ] **Step 2:** Se houver teste da secção, mantê-lo verde (ajustar seletores role se necessário). Se não houver, adicionar um teste mínimo de render + onChange de um campo.
- [ ] **Step 3:** `tsc`+`eslint`+`vitest` (dos ficheiros) verdes. Commit — `refactor(contracts): DS form controls in workspace Security/Definition sections`

---

### Task 2: Forms standard dos builder panels → DS (preservando seletores color-coded)

**Files:**
- Modify: `src/frontend/src/features/contracts/workspace/builders/ParameterConstraintsPanel.tsx` (8 controlos: number/text/select de constraints)
- Modify: `src/frontend/src/features/contracts/workspace/builders/shared/SchemaPropertyEditor.tsx` (13 controlos: text/number/checkbox/select standard **+ preservar** os seletores de tipo color-coded)

- [ ] **Step 1:** `ParameterConstraintsPanel` — todos os controlos são form standard (min/maxLength, pattern, min/max, format select, default, enum). Converter para `TextField` (com `type="number"` onde aplicável, preservando a coerção `Number(...)`), `Select` (format), preservando o `update({...})`.
- [ ] **Step 2:** `SchemaPropertyEditor` — converter os inputs de texto/número → `TextField`, os `<input type="checkbox">` (exclusiveMin/Max, nullable) → DS `Checkbox`, o `<select format>` → `Select`. **NÃO tocar** nos seletores de tipo color-coded (`TYPE_COLORS`, object/$ref/oneOf) — são semântica por cor. Documentar no report o que foi preservado.
- [ ] **Step 3:** Manter testes verdes / adicionar teste mínimo. `tsc`+`eslint`+`vitest` verdes. Commit — `refactor(contracts): DS form controls in builder constraint/schema panels (color-coded selectors preserved)`

---

### Task 3: SoapWsdlBuilderPage — triagem + conversão se standard

**Files:**
- Read/Modify: `src/frontend/src/features/contracts/pages/SoapWsdlBuilderPage.tsx` (4 controlos crus)

- [ ] **Step 1:** Inspecionar os 4 controlos. Se forem form standard (nome/namespace/opções) → converter para DS. Se forem internos ao editor/preview de WSDL (parte da experiência de edição, análogo aos outros builders deferidos) → **deixar e documentar como exceção deliberada** no report (com a razão). Não fazer swap cego que parta o preview.
- [ ] **Step 2:** Se convertido: manter teste verde. `tsc`+`eslint`+`vitest`. Commit — `refactor(contracts): DS controls in SoapWsdlBuilderPage` (ou, se deferido, sem commit de código — só nota no report).

---

### Task 4: Remover o componente órfão `ServiceRegistrationWizard`

**Files:**
- Move: o tipo `ServiceFormData` (único símbolo ainda reusado — importado por `ServiceDetailPage.tsx:52`) para um módulo de tipos partilhado (ex.: `features/catalog/pages/ServiceDetailPage.tsx` local, ou `features/catalog/types`).
- Delete: `src/frontend/src/features/catalog/components/ServiceRegistrationWizard.tsx` + o seu teste (`ServiceRegistrationWizard.test.tsx`).

**Contexto:** superseded pelo Service Workspace v5 (ciclo 8). Só o *tipo* `ServiceFormData` sobrevive; o componente é código morto.

- [ ] **Step 1:** Confirmar com grep que, além do `import type { ServiceFormData }`, não há uso do componente `ServiceRegistrationWizard` em lado nenhum (rotas, páginas). Mover o tipo, atualizar o import em `ServiceDetailPage`.
- [ ] **Step 2:** Apagar o componente + teste órfãos. `tsc`+`eslint`+`vitest` (suite) verdes (sem referências penduradas). Commit — `chore(catalog): remove orphan ServiceRegistrationWizard (superseded by workspace v5)`

---

### Task 5: Wiring de dados reais do health-strip (ou defer honesto)

**Files:**
- Read: `src/frontend/src/features/catalog/api/serviceCatalog.ts` (existe query de maturity/SLO/incidents por serviço?), `ServiceDetailPage.tsx` (~L743 `// placeholders em criação`).

- [ ] **Step 1:** Verificar se `serviceCatalogApi` (ou outro) já expõe endpoints para Maturity/SLO/Incidents de um serviço. 
  - **Se SIM:** adicionar o(s) `useQuery` e ligar o health-strip aos dados reais, com **honest-null** quando indisponível (nunca inventar). Skeleton enquanto carrega.
  - **Se NÃO (backend não expõe):** **NÃO inventar dados**. Manter honest-null (esconder os sinais sem dado) e registar como "needs-backend" — trocar o comentário `placeholders em criação` por uma renderização honest-null explícita e documentar a dependência de backend. (Esta tarefa pode ficar como no-op de código se for puramente backend — reportar claramente.)
- [ ] **Step 2:** `tsc`+`eslint`+`vitest`. Commit — `feat(catalog): wire real health-strip data (honest-null)` (ou nota de defer se needs-backend).

---

### Task 6: Resolver minors diferidos das superfícies browse (ciclo 24)

**Files:**
- Modify: `src/frontend/src/features/contracts/catalog/browse/ContractFacetBar.tsx` (+ surface), `ContractResultCard`/`ServiceResultCard` conforme.

- [ ] **Step 1 — Sort inútil em vista Tabela (contratos):** o `Select` de ordenação da `ContractFacetBar` não faz nada em `viewMode==='table'` (a `CatalogTable` ordena por cabeçalhos). Esconder o `Select` de ordenação quando `viewMode==='table'` (passar `viewMode` à facet bar já disponível). Manter em `cards`.
- [ ] **Step 2 — Extrair `isFiltersActive(filters)`:** o predicado `hasActiveFilters` está duplicado em `ContractFacetBar` + `ContractBrowseSurface` (e análogo no service browse). Extrair uma função pura para o adapter respetivo e reutilizar nos dois sítios (contratos; opcionalmente serviços).
- [ ] **Step 3 — Polish:** `resultCount` com label i18n interpolado (`t('...resultCount', { count })` "N resultados") em ambas as facet bars (adicionar chave nos 4 locales); `ServiceResultCard` separador `<hr>` → DS `Divider`.
- [ ] **Step 4:** Manter testes verdes (ajustar se um label mudou). `validate:i18n` PASS. `tsc`+`eslint`+`vitest`. Commit — `polish(catalog,contracts): browse discovery deferred minors (sort-in-table, shared active-filter helper, resultCount label, Divider)`

---

### Task 7: Smoke visual (browser) + verificação final

- [ ] **Step 1:** Playwright (chromium headless, auth+endpoints mockados — usar `e2e/helpers/auth.ts` como modelo; dark real via `colorScheme:'dark'`+`localStorage nto-theme=dark`). Capturar em **light e dark**: `/services` (segmento Browse | Explorar, vista Serviços e APIs, estado sem-resultados) e `/contracts` (toggle Tabela | Cartões, facetas). Confirmar **0 PAGEERROR** e contraste OK.
- [ ] **Step 2:** `npx vitest run` (suite COMPLETA) + `npx tsc --noEmit` + `npx eslint` + `npm run validate:i18n` — tudo verde.
- [ ] **Step 3:** Commit dos artefactos de smoke (se aplicável) + relatório final.

---

## Fora de escopo (deliberado — NÃO fazer neste plano)
- Redesenhar Monaco/`ContractSection`/`MonacoEditorWrapper` (editores intencionais).
- Reformular a jornada de **criação/onboarding** (J2) — já entregue v5 (ciclos 7-9); só abrir se o utilizador pedir um redesign de experiência específico.
- Seletores color-coded, color-pickers, radios sem DS, taxonomias de cor (status/data-viz) — exceções documentadas.
- `TraceExplorerPage`/`RequestExplorerPage` (não são deste módulo; [[project_trace_refactor]]).

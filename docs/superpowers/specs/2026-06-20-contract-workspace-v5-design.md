# Contract Workspace v5 — Betterstack redesign (sub-projeto A)

- **Data:** 2026-06-20
- **Ciclo:** Betterstack redesign — telas de edição/governança de contrato (deferidas no ciclo 9)
- **Sub-projeto:** A de 4 (A=Contract Workspace, B=Draft Studio, C=Code Builders, D=Visual Builders)
- **Referência de padrão:** Service Workspace v5 (`features/catalog/pages/ServiceDetailPage.tsx`) + `ContractIdentityCard` do fluxo de criação (`features/contracts/create/ContractIdentityCard.tsx`)

## Objetivo

Restilizar `ContractWorkspacePage` (a superfície de consulta/governança de uma versão de
contrato publicada, em `/contracts/:contractVersionId`) ao idioma v5, mantendo a IA de 16 secções
e todo o comportamento existente. Mudança de apresentação — sem alterações de backend, API ou
schema.

## Decisões travadas (brainstorming + visual companion)

- **Intenção:** A + B — alinhar com o idioma v5 do service workspace **e** fazer o passe de polish
  (DS components, PageHeader, estados Loading/Error/Empty, tokens), **sem** o teardown da IA (C).
- **Layout (Opção 2):** `ContractIdentityCard` passa a ser a **1ª coluna (esquerda, sticky)**; a nav
  de 16 secções vira **tab-strip horizontal**; o `StudioRail` fica **slim** à direita.
- **Tabs (dois níveis):** primário = os 5 grupos (`WORKSPACE_SECTION_GROUPS`:
  Overview/Contract/Governance/Relationships/AI); secundário = as secções do grupo activo como chips
  (≤6 visíveis). Mantêm-se as 16 secções.

## Estado atual (resumo)

- `workspace/ContractWorkspacePage.tsx` (226 linhas) — orquestra: `useContractDetail` →
  `toStudioContract` → `WorkspaceLayout` com `ContractHeader`+`ContractQuickActions` (topo),
  nav de 16 secções (esq, agrupada em 5), `renderSection` (centro), `StudioRail` (dir).
- `workspace/WorkspaceLayout.tsx` (127) — 3 colunas: nav-esq `w-56` agrupada, `main` central,
  `aside` rail-dir `w-72`. Já usa tokens (`border-edge`, `bg-panel`, `text-accent`).
- `workspace/components/StudioRail.tsx` (298) — Status, Owners, Approval Checklist, Policy Checks,
  Risks, Recent Activity, Available Transitions, Locked notice. Driven por `StudioContract`.
- 16 secções em `workspace/sections/*` — já maioritariamente DS (1-4 controlos crus cada).
- Hooks: `useContractDetail`, `useContractViolations`, `useContractTransition`, `useContractExport`.

## Componentes a entregar

### 1. Card de identidade do workspace — `workspace/components/ContractWorkspaceIdentityCard.tsx` (novo)

Mesma linguagem visual do `ContractIdentityCard` do fluxo de criação (header com gradiente, tile de
ícone do tipo, nome em mono, chips, mini-strip, meta rows), mas alimentado por `StudioContract`:

- Header: tile de ícone (por protocolo/serviceType) + `technicalName` (mono) + `friendlyName` +
  `LifecycleBadge`(state) + ícones lock/signed quando aplicável.
- Chips: protocolo, approval state (mint/cyan/danger conforme actual), compliance.
- Mini-strip 3 colunas: **Approvals `x/y` · Policies `x/y` · Compliance `%`** (dados reais de
  `StudioContract.approvalChecklist`/`.policyChecks`/`.complianceScore`).
- Meta rows: Owner, Team, Domain (de `StudioContract`), Version `v{semVer}`.
- Absorve os blocos Status + Owners que hoje vivem no topo do `StudioRail`.

**Reuso:** extrair o "shell" visual partilhado (header gradiente + mini-strip + meta-row helpers)
para `features/contracts/shared/components/` ou um helper local, para que o card de criação e o de
workspace partilhem o esqueleto sem forçar uma única forma de dados. Manter mínimo (YAGNI) — não
abstrair além do shell.

### 2. Navegação em dois níveis — `workspace/components/WorkspaceTabs.tsx` (novo) + refactor de `WorkspaceLayout`

- `WorkspaceLayout` reescrita: `PageHeader` (topo) + **3 colunas**
  `[300px_minmax(0,1fr)_240px]` — 1ª sticky (identity card), 2ª = tabs + conteúdo da secção, 3ª = rail
  slim. Mantém 3 colunas no total (decisão fixa, não deixar em aberto).
- `WorkspaceTabs`: tab-strip primário = 5 grupos (role=tab, DS-style underline accent), + linha
  secundária de chips das secções do grupo activo. Estado: grupo activo + secção activa derivada.
  Ao trocar de grupo, selecciona a 1ª secção do grupo. Mantém `data-testid` para testes.

### 3. PageHeader + ações — usar o `PageHeader` do DS

- Substituir o `ContractHeader` custom no topo pelo `PageHeader` padrão (back para `/contracts`,
  título = identidade do contrato, subtítulo, acção primária à direita).
- `ContractQuickActions` reconstruído com DS `Button` (transições de lifecycle + Export). Preservar
  exactamente as mesmas acções/handlers (`onTransition`, `onExport`).

### 4. Rail slim — `StudioRail` reduzido

- Manter no rail slim: **Risks, Recent Activity, Available Transitions, Locked notice**.
- Remover do rail (movido para o identity card): blocos **Status** e **Owners**.
- **Decisão fixa:** as listas detalhadas de **Approval Checklist** e **Policy Checks** NÃO ficam no
  rail slim — só os contadores (`x/y`) no mini-strip do identity card; as listas completas continuam
  nas suas secções (Approvals/Compliance). Evita duplicação.

### 5. Passe de polish nas 16 secções

Manter comportamento e wiring de dados de cada secção. Varrer controlos crus restantes
(`<button>`/`<input>`/`<select>`) → DS; rotear loading/error/empty pelos componentes DS de estado;
afinar tokens/spacing Betterstack. **`ContractSection` (Monaco) e os Visual*Builder embebidos são
chrome-only / internamente intocados** (sub-projetos C/D). É um sweep de polish, não reescrita.

### 6. Fluxo de dados

Inalterado. `useContractDetail`/`useContractViolations`/`useContractTransition`/`useContractExport`
e `toStudioContract`. Card e rail são views derivadas de `StudioContract` — só apresentação.

## Testes

- Atualizar `__tests__/pages/ContractWorkspacePage.test.tsx` para a nova estrutura: identity card
  presente, navegação grupo→secção, rail com Risks/Activity.
- Adicionar cobertura: troca de tabs em dois níveis (clicar grupo activa 1ª secção; clicar chip
  activa secção); identity card reflecte `StudioContract` (lifecycle/approvals/compliance/owner).
- Suíte deve permanecer verde.

## i18n

Reusar chaves existentes (`contracts.workspace.*`, `contracts.studio.rail.*`). Novas chaves só se
introduzir labels novos (ex.: títulos de grupo já existem em `WORKSPACE_SECTION_GROUPS`). 4 locales
reais: en/es/pt-BR/pt-PT (NÃO há fr).

## Fora de escopo

- A IA de 16 secções (mantida tal como está).
- Internos do Monaco `ContractSection` e dos `workspace/builders/Visual*Builder` (sub-projetos C/D).
- `DraftStudioPage` (sub-projeto B), `*BuilderPage`/`ContractBuilderLayout` (sub-projeto C).
- Backend, contratos de API, schema.

## Critérios de verificação

1. `/contracts/:id` renderiza: PageHeader (DS) + identity card sticky à esquerda + tab-strip 2 níveis
   + conteúdo da secção + rail slim. → verificar render.
2. Clicar num grupo activa a 1ª secção desse grupo; clicar num chip activa a secção. → verificar.
3. Identity card reflecte dados reais do `StudioContract` (lifecycle, approvals x/y, compliance, owner).
4. Acções de lifecycle/export continuam a funcionar (mesmos handlers). → verificar mock.
5. Secções mantêm comportamento; sem controlos crus restantes nas secções tocadas. → verificar.
6. `npm run lint` 0 erros; `npm run test` suíte verde; build OK.
7. Monaco/visual-builders internamente intocados (diff não altera `ContractSection` lógica nem `builders/`).

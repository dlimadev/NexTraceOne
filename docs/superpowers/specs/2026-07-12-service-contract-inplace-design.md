# Navegação Contextual In-Place (Serviços ↔ Contratos) — Design

**Data:** 2026-07-12
**Estado:** Aprovado (brainstorming) — pronto para plano de implementação

## Problema

A queixa do utilizador: ao trabalhar com um serviço, as ações relacionadas
**direcionam-no para outro item do menu** em vez de o manter no contexto. O
exemplo central: ver os contratos de um serviço ou **criar um novo contrato**
deveria acontecer na **tela de detalhes do serviço**, não redirecionar para o
módulo de contratos. Este princípio — renderizar as telas relacionadas no mesmo
contexto/tela — deve aplicar-se a todas as funcionalidades relacionadas.

Além disso, o ciclo anterior (redesign da navegação) reduziu a sidebar de ~19
para 5 roots + sub-nav de área. O utilizador não percebeu a remoção de itens e
pediu para **restaurar os itens removidos** — a redução foi o mecanismo errado.

### Diagnóstico no código atual

`ServiceDetailPage` **já mostra os contratos do serviço** contextualmente (aba
"Contratos" + secção que os lista). O que redireciona para fora são as **ações
e ligações**, não a visualização:

- "Add Contract" / checklist de setup → `navigate('/contracts/new?serviceId=…')`
- "View contract" → `navigate('/source-of-truth/contracts/:id')`

Logo, o trabalho é mais cirúrgico do que reconstruir a navegação: **manter as
ações no contexto** (criar/ver/editar um contrato sem sair do serviço) e
estabelecer um padrão reutilizável para os restantes relacionamentos.

## Decisões (validadas com o utilizador)

1. **Sidebar:** restaurar os itens removidos (reverter a restruturação do ciclo
   anterior). O modelo in-place entra por cima, sem tirar acesso direto às
   áreas globais.
2. **Modelo de interação:** **drawer/overlay lateral**. Um painel desliza por
   cima do detalhe do serviço com o editor/vista do contrato; o URL e o
   contexto do serviço ficam intactos por trás; fechar (Esc/clique/botão)
   volta exatamente ao mesmo ponto. Escolhido por lidar bem com os editores
   pesados (Monaco/multi-step).
3. **Âmbito/faseamento:** **flagship service↔contratos primeiro** — construir o
   padrão de drawer reutilizável e aplicá-lo ao caso do exemplo. Os outros
   relacionamentos entram em ciclos seguintes reutilizando o padrão.
4. **Profundidade:** **completa** — ver o contrato (resumo + spec) e criar/editar
   (serviço pré-preenchido, tipo, modo, metadados **e edição do spec Monaco**),
   tudo no drawer, sem nunca sair do serviço.

## Abordagem escolhida (das 3 avaliadas)

**A — Extrair & hospedar (escolhida).** Extrair os *cores* de editor/vista das
páginas roteadas existentes para componentes que recebem IDs como **props**; as
páginas roteadas passam a ser wrappers finos à volta dos mesmos cores; um novo
drawer hospeda-os. Reutiliza toda a lógica real do contrato (Monaco, validação,
save/submit), sem duplicação, e as rotas de deep-link continuam a funcionar.

- **B — Rota-no-drawer** (renderizar as páginas via router aninhado): frágil —
  as páginas detêm o seu `useParams`/`useNavigate`/shell. Rejeitada.
- **C — Componentes leves novos no drawer:** duplicaria a lógica de contrato e
  divergiria dos editores reais; contradiz a decisão de profundidade completa.
  Rejeitada.

## Parte 0 — Restaurar a sidebar (reverter o ciclo 36)

O utilizador quer os itens de volta. O 5-root + sub-nav de área foi o mecanismo
que os removeu; manter ambos (itens na sidebar **e** sub-nav) reintroduziria o
duplo-destaque. Portanto:

- **Reverter os commits de navegação do ciclo 36** via `git revert` (preserva
  histórico; nada de force-push): restaura os ~19 itens do catálogo na sidebar
  e as rotas planas; remove a sub-nav de área e o aninhamento de rotas.
- **Commits a reverter (na main):** `5cea777f` (navegação em 2 níveis: sidebar
  5 roots + rotas aninhadas + strays renomeados) e o commit do AreaSubNav/i18n.
  O commit do plano/spec de docs pode ficar (histórico) ou ser revertido — sem
  impacto funcional.
- **Resultado:** sidebar exatamente como antes do ciclo 36 (19 itens planos,
  `/catalog/developer-experience-score` e `/catalog/contracts/pipeline` de volta
  aos paths originais), **mais** o comportamento in-place da Parte 1.
- **Verificação:** `git diff` da main pós-revert contra o commit pré-ciclo-36
  (`985c8e37`) nos ficheiros de navegação (`AppSidebar.tsx`, `catalogRoutes.tsx`,
  `contractsRoutes.tsx`, `ServiceScoreTab.tsx`) = vazio; ficheiros novos do
  ciclo 36 (AreaSubNav, layouts, testes) removidos.

## Parte 1 — Flagship: contratos in-place no detalhe do serviço

### 1. `Drawer` — nova largura `xl`

`components/Drawer.tsx` ganha um tamanho `xl` (`w-[min(1100px,92vw)]`) para dar
espaço ao editor Monaco. Adição cirúrgica ao mapa `sizeClasses`; API inalterada.

### 2. `ContractDraftEditor` — core de edição extraído

Extrair o core de edição de `DraftStudioPage` (as tabs Spec/Metadata/Validation,
`ContractSection` Monaco, ações Save/Submit/Export, mutations
`saveContent`/`saveMetadata`/`submitForReview`, hooks `useDraftValidation`/
`useDraftExport`) para um componente que recebe `draftId` como **prop** (em vez
de `useParams`). Localização: `features/contracts/studio/components/ContractDraftEditor.tsx`.

- `DraftStudioPage` passa a ser um wrapper fino: lê `draftId` da rota, mantém o
  shell (PageContainer + back-link + PageHeader + DraftIdentityCard sticky) e
  renderiza `<ContractDraftEditor draftId={draftId} />` no lugar do bloco de
  tabs. Comportamento e testes existentes preservados.
- O `ContractDraftEditor` **não** inclui o `PageContainer`/back-link (isso é do
  wrapper); expõe uma prop opcional `variant?: 'page' | 'drawer'` apenas para
  ajustes de espaçamento (ex.: altura do Monaco `h-[60vh]` vs `h-[70vh]`), sem
  ramificar lógica.

### 3. `ContractViewPanel` — vista read-only

Novo `features/contracts/components/ContractViewPanel.tsx`. Recebe
`contractVersionId` como prop; carrega o detalhe do contrato (a mesma query que
alimenta a vista de detalhe — `/contracts/:id/detail`) e renderiza:

- Cabeçalho de resumo: protocolo, versão (semVer), estado do ciclo de vida,
  locked, serviço/apiName — usando os `Badge` do DS.
- Spec **read-only**: `ContractSection` com `isReadOnly` a mostrar o spec.
- Ligação secundária "Abrir fonte de verdade" (deep-link `/source-of-truth/...`)
  para quem quiser a vista completa — o drawer é a via primária.

Estados: `PageLoadingState`/`PageErrorState` (variante compacta) para
loading/erro; honest-null se o spec não vier.

### 4. `ServiceContractDrawer` — orquestrador

Novo `features/catalog/components/ServiceContractDrawer.tsx`. Hospeda o `Drawer`
(`size="xl"`) e orquestra três modos via estado interno:

```ts
type ContractDrawerState =
  | { mode: 'view'; contractVersionId: string }
  | { mode: 'create' }
  | { mode: 'edit'; draftId: string }
  | { mode: 'closed' };
```

- **Props:** `state: ContractDrawerState`, `onClose: () => void`,
  `serviceId: string`, `serviceName: string`, `serviceType: string`.
- **`view`** → renderiza `ContractViewPanel`.
- **`create`** → renderiza o formulário de criação (serviço pré-preenchido,
  tipo, modo, metadados) usando o hook existente `useContractDraftForm` com a
  **navegação suprimida**: em vez de `navigate('/contracts/studio/{draftId}')`,
  invoca um callback `onDraftCreated(draftId)` que transiciona o drawer
  **in-place** para `{ mode: 'edit', draftId }`. Sem navegação de rota.
- **`edit`** → renderiza `ContractDraftEditor` (Monaco + validação + submit)
  diretamente, keyed por `draftId`.
- Título do drawer muda por modo (`Ver contrato` / `Novo contrato` /
  `Editar rascunho`).

#### Alteração ao `useContractDraftForm`

Hoje o hook cria o draft e navega para `/contracts/studio/{draftId}`. Torná-lo
**agnóstico à navegação**: aceitar um callback `onCreated?(draftId: string)` (ou
retornar o `draftId` da mutation para o chamador decidir). O `CreateContractPage`
existente passa um `onCreated` que faz `navigate(...)` (comportamento atual,
preservado); o drawer passa um `onCreated` que transiciona de modo. Nenhuma
mudança de comportamento para o fluxo roteado existente.

### 5. Wiring em `ServiceDetailPage`

Substituir as navegações que saem da página por abertura do drawer, mantendo o
serviço montado por trás:

- `onAddContract` do `ServiceSetupChecklist` e o botão "Add Contract" da aba
  contratos → `setDrawerState({ mode: 'create' })` (era
  `navigate('/contracts/new?serviceId=…')`).
- Ligação "View contract" na tabela de contratos → `setDrawerState({ mode:
  'view', contractVersionId })` (era `navigate('/source-of-truth/contracts/:id')`).
- Ao fechar o drawer, **invalidar** `['catalog-service-contracts', serviceId]`
  para a lista de contratos do serviço refletir um contrato recém-criado.
- O `ServiceContractDrawer` é montado uma vez no fundo da `ServiceDetailPage`,
  controlado por `drawerState`.

### Fora do âmbito do flagship (ciclos seguintes, mesmo padrão de drawer)

- Editar um contrato **publicado** (é, na prática, "propor nova versão"/
  versionamento) — o flagship cobre criar/editar **rascunhos** e ver publicados.
- Os relacionamentos espelho: fonte de verdade, scorecard, interfaces, changes,
  e o lado dos contratos (contrato→consumidores/serviço). Todos passam a
  drawers in-place em iterações futuras reutilizando `Drawer` + o padrão de
  orquestrador.

## Ficheiros afetados

**Parte 0 (revert):**
- `src/components/shell/AppSidebar.tsx`, `src/routes/catalogRoutes.tsx`,
  `src/routes/contractsRoutes.tsx`, `src/features/catalog/components/ServiceScoreTab.tsx`
  — restaurados ao estado pré-ciclo-36.
- Removidos: `src/features/catalog/components/AreaSubNav.tsx`,
  `src/features/catalog/layouts/{ServicesAreaLayout,ContractsAreaLayout}.tsx`,
  testes do ciclo 36, chaves i18n `catalogAreaNav`/`contractsAreaNav`.

**Parte 1 (novo):**
- `src/components/Drawer.tsx` — tamanho `xl`.
- `src/features/contracts/studio/components/ContractDraftEditor.tsx` — **novo** (core extraído).
- `src/features/contracts/studio/DraftStudioPage.tsx` — passa a wrapper fino.
- `src/features/contracts/components/ContractViewPanel.tsx` — **novo**.
- `src/features/contracts/hooks/useContractDraftForm.ts` — callback `onCreated`.
- `src/features/contracts/create/CreateContractPage.tsx` — passa `onCreated` (nav preservada).
- `src/features/catalog/components/ServiceContractDrawer.tsx` — **novo** orquestrador.
- `src/features/catalog/pages/ServiceDetailPage.tsx` — wiring do drawer.
- `src/locales/{en,es,pt-BR,pt-PT}.json` — chaves do drawer (títulos por modo, ações).

## Testes

- **`ServiceContractDrawer`:** transições de modo (view / create→editor in-place /
  edit); o create chama `onCreated` e o drawer transiciona sem navegação.
- **`ServiceDetailPage`:** "Add Contract" e "View contract" **abrem o drawer**
  (não navegam) — asserir que o `navigate` não é chamado e o conteúdo do drawer
  aparece; fechar invalida a query de contratos.
- **`ContractDraftEditor`:** render com `draftId` prop; save/submit chamam as
  mutations (mocks) — paridade com o comportamento do DraftStudioPage.
- **`DraftStudioPage` (regressão):** continua a renderizar o editor via wrapper.
- **`ContractViewPanel`:** render read-only com `contractVersionId` (spec
  read-only, resumo).
- **Sidebar (Parte 0):** os itens restaurados aparecem; rotas antigas resolvem.
- **Browser (stub):** abrir detalhe de serviço → "Add Contract" abre drawer →
  criar rascunho → editor Monaco no drawer → fechar volta ao serviço com a lista
  atualizada; "View contract" abre a vista read-only; sidebar com os itens de
  volta; 0 erros de consola.

## Critérios de sucesso

1. No detalhe de um serviço, **criar um novo contrato** (até editar o spec no
   Monaco) acontece num drawer, **sem sair** da página do serviço.
2. **Ver um contrato** do serviço abre num drawer read-only, sem navegar para o
   módulo de contratos/fonte de verdade.
3. Fechar o drawer volta exatamente ao mesmo ponto; um contrato recém-criado
   aparece na lista do serviço.
4. A sidebar volta a ter os itens removidos no ciclo 36; deep links antigos
   funcionam.
5. `tsc`, `eslint`, `build` e a suíte de testes a passar; sem regressões na
   varredura em browser.
6. `useContractDraftForm` e o fluxo roteado `CreateContractPage`/`DraftStudioPage`
   mantêm o comportamento atual (deep-links preservados) — a extração é sem
   regressão.

# Betterstack — Sub-projeto C: Code Builders (Monaco) — Design

**Data:** 2026-06-22
**Branch:** `redesign/betterstack-code-builders`
**Memória de contexto:** `project_betterstack_redesign` (ciclos 9–11), `project_contract_studio_redesign`
**Precedente de idioma:** `DraftStudioPage` (ciclo 11, sub-projeto B)

## Contexto

Continuação do redesign Betterstack. As telas de edição/builders de contrato foram
decompostas em 4 sub-projetos: A=Contract Workspace (feito, ciclo 10), B=Draft Studio
(feito, ciclo 11), **C=Code Builders (este)**, D=Visual Builders.

Sub-projeto C abrange os editores de código baseados em Monaco:

- `src/frontend/src/features/contracts/studio/ContractBuilderLayout.tsx` — o shell real:
  split-pane (Monaco à esquerda, preview à direita) + `BuilderHeader`, com parse/debounce/format.
- `src/frontend/src/features/contracts/studio/components/BuilderHeader.tsx` — breadcrumb +
  badges de validação + ações (Format/Save/Publish). Já usa DS (Button/Badge) e tokens semânticos.
- 5 páginas wrapper finas (~50–70 linhas cada) que só fornecem template + componente de preview:
  `RestOpenApiBuilderPage`, `AsyncApiBuilderPage`, `GraphQLBuilderPage`, `ProtobufBuilderPage`,
  `SoapWsdlBuilderPage` (em `features/contracts/pages/`).

**Routing:** as 5 páginas estão roteadas em `/contracts/studio/{rest|async|soap|graphql|protobuf}`
(`contractsRoutes.tsx`). Montam o `ContractBuilderLayout` **sem** `onSave`/`onPublish` — hoje os
botões Save/Publish não renderizam; é um playground de template com preview ao vivo, sem
persistência ligada.

**Observação (não bloqueia):** desde o ciclo 9, o hub (`ContractStudioPage`) aponta os cards de
tipo para `/contracts/new`. Estas rotas `/contracts/studio/{type}` continuam roteadas mas podem já
não ser alcançáveis pela navegação da UI (só por URL direto). Mantêm-se no escopo porque estão
roteadas e o objetivo é alinhá-las ao idioma v5.

## Intenção e alcance (travado com o utilizador)

- **Profundidade:** "Moldura v5 + polish" — alinhar o idioma visual v5 e corrigir defeitos, **sem
  teardown da arquitetura de informação do editor**. Monaco, parse/debounce/format e os componentes
  de preview ficam **intocados**.
- **Moldura (Abordagem B):** replicar exatamente o padrão do `DraftStudioPage` —
  `PageContainer` + back-link + `<PageHeader>` literal + o split-pane do editor dentro de um
  caixote limitado (`h-[60vh] min-h-[420px]`, `border border-edge rounded-lg overflow-hidden`).
  Decisão do utilizador: consistência máxima com o Draft Studio acima da ergonomia de viewport cheio.
- **Ações (Variante 4a — só visual):** Save/Publish continuam condicionais e, como as páginas não
  passam handlers, **não renderizam**. Zero lógica nova / zero rewire de persistência. A criação real
  de contratos continua a viver em `/contracts/new`.

## Mudanças de design

### 1. `ContractBuilderLayout` → moldura DraftStudio (Abordagem B)

Reescrever a shell do layout para o padrão do `DraftStudioPage`:

- Envolver em `PageContainer className="animate-fade-in"`.
- Back-link discreto no topo: `Link` para `/contracts/studio` com `ChevronLeft` +
  `text-muted hover:text-heading` (mesmo idioma do DraftStudio, que usa `/contracts`).
- `<PageHeader>` com:
  - `title` = `contractName` (ex.: `t('restBuilder.title')`).
  - `subtitle` = `protocol` (ex.: `OpenAPI 3.1`).
  - `actions` = cluster DS Button:
    - chip de validação (válido/erros + linha) à esquerda das ações, usando o `Badge` DS
      (`variant="success"` quando válido, `variant="danger"` quando erros — variantes canónicas;
      `destructive` é alias válido mas usamos as canónicas).
    - `Format` como `Button variant="outline" size="sm"` (`data-testid="btn-format"`).
    - Save/Publish condicionais (`onSave`/`onPublish`), DS Button, preservando
      `data-testid="btn-save"` / `data-testid="btn-publish"`. Em 4a não renderizam (sem handlers).
- O split-pane (`SimpleSplitPane` com `MonacoEditorWrapper` à esquerda e preview à direita) passa a
  viver dentro de um caixote limitado: `h-[60vh] min-h-[420px] border border-edge rounded-lg overflow-hidden`.
- Preservar `data-testid="contract-builder-layout"` no contentor raiz.

### 2. `BuilderHeader.tsx` → removido (órfão)

Com o `<PageHeader>` literal + back-link a assumir o papel do header, o `BuilderHeader.tsx` fica
órfão. Removê-lo (é órfão criado por esta mudança) e:

- Relocar o tipo `BuilderValidationStatus` para o `ContractBuilderLayout` (ou um módulo de tipos
  partilhado do studio), mantendo a API pública do layout inalterada.
- Migrar a lógica do `chipLabel`/`chipVariant` para dentro do `ContractBuilderLayout`.
- Confirmar (grep) que nada além do `ContractBuilderLayout` importa `BuilderHeader` antes de remover.

### 3. Correção de defeitos (polish)

- **Chave i18n literal:** `ContractBuilderLayout` linha ~118 renderiza a string
  `contractBuilder.preview.parseError` **literal** (não passa por `t()`). Corrigir para
  `t('contractBuilder.preview.parseError')` e garantir a chave nos 4 locales
  (`en`, `es`, `pt-BR`, `pt-PT`) — ficheiros FLAT em `src/frontend/src/locales/<l>.json`.
- **Token partido no banner de parse-error:** as utilities `text-destructive` / `bg-destructive/10`
  / `border-destructive/20` (linhas ~113–115) não têm `--color-destructive` no `@theme` (só existe
  `critical`) → renderizam no-op. Trocar para `text-critical` / `bg-critical/10` (ou
  `bg-critical-muted`) / `border-critical/25`, alinhado com o padrão de banners do `DraftStudioPage`.

### 4. As 5 páginas builder

Permanecem wrappers finos. Sem mudança estrutural — continuam a passar `contractName`, `protocol`,
`language`, `initialContent`, `renderPreview` ao `ContractBuilderLayout`. Apenas sweep de tokens se
houver alguma cor crua/token legado (à partida estão limpas).

### 5. Sweep de tokens

Varrer os 2 ficheiros centrais (e confirmar nas 5 páginas) por tokens legados/cores cruas contra o
`@theme`. Pontos conhecidos: `destructive` utilities (ponto 3), `bg-elevated` (válido), `border-edge`
(válido). Garantir alinhamento com os tokens v5 reais.

### 6. Testes

`src/frontend/src/__tests__/contracts/ContractBuilderLayout.test.tsx` (4 casos) depende dos
`data-testid`: `contract-builder-layout`, `parse-error-banner`, `btn-save`. Todos preservados pelo
design. Atualizar o teste apenas se algum testid/texto mudar (não deve). O caso "calls onSave"
passa `onSave` explicitamente, pelo que o `btn-save` continua a renderizar quando há handler.
Objetivo: suíte total verde no fim.

## Fora de escopo (explícito)

- Monaco / `MonacoEditorWrapper`, lógica de parse/debounce/format.
- Componentes de preview (`RestOperationsPreview`, `GraphQlTypesPreview`, etc.).
- Templates de conteúdo das 5 páginas.
- Qualquer rewire de persistência / ligação de `onSave`/`onPublish` (Variante 4a = só visual).
- Sub-projeto D (Visual Builders).
- Reativar/alterar a navegação do hub para estas rotas (observação registada, não acionada).

## Critérios de sucesso

1. Os 5 builders renderizam no idioma v5 idêntico ao `DraftStudioPage` (PageContainer + back-link +
   PageHeader + editor em caixote), via `npm run dev` / smoke visual.
2. A chave i18n `parseError` aparece traduzida (não literal) e o banner de erro usa tokens `critical`.
3. `BuilderHeader.tsx` removido sem imports órfãos; `npm run build` e `npm run lint` limpos.
4. Suíte de testes verde (incl. `ContractBuilderLayout.test.tsx`).
5. `git diff --name-only main...HEAD` contém apenas ficheiros de contratos/builders + locales + spec
   (lição do ciclo 9: nada de varrer ficheiros não relacionados).

## Riscos

- **Altura do editor:** o caixote `h-[60vh]` encolhe o Monaco face ao full-bleed atual — aceite
  explicitamente pelo utilizador (Abordagem B).
- **Remoção do `BuilderHeader`:** confirmar todos os importadores antes de apagar; relocar o tipo
  exportado.
- **`PageHeader` actions com chip:** garantir que o `Badge` de validação assenta bem no slot de
  `actions` do `PageHeader` (alinhamento vertical com os botões).

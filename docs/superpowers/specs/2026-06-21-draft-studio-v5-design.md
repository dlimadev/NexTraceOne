# Draft Studio v5 — Betterstack redesign (sub-projeto B)

- **Data:** 2026-06-21
- **Ciclo:** Betterstack redesign — telas de edição de contrato (sub-projeto B de 4)
- **Sub-projeto:** B = Draft Studio (`DraftStudioPage`). (A=Contract Workspace ✅ ciclo 10; C=Code Builders; D=Visual Builders.)
- **Referência de padrão:** Contract Workspace v5 (`ContractWorkspacePage` + `ContractWorkspaceIdentityCard`) e os primitives `shared/components/identityCardPrimitives.tsx`.
- **Decisão do utilizador:** **Plano B** (o mais completo) — layout v5 de 2 colunas com cartão de identidade.

## Objetivo

Alinhar o `DraftStudioPage` (editor de draft de contrato, rota `/contracts/studio/:draftId`, destino
pós-criação do fluxo de criação) ao idioma v5: cartão de identidade do draft fixo à esquerda + tabs
DS/conteúdo à direita, mantendo todo o comportamento existente. Mudança de apresentação — sem
alterações de backend, API ou schema. **Monaco (`ContractSection`) e `DraftValidationPanel`
intocados internamente.**

## Estado atual

`studio/DraftStudioPage.tsx` (~408 linhas): editor full-height (`flex flex-col h-full`) com:
- Header custom: voltar + título + chips protocolo/estado/versão (esq); Export/Save/Submit (dir, botões crus).
- Tab bar custom: Spec · Metadata · Validation (botões crus, `border-b-2`, badge de issues na Validation).
- Conteúdo: **Spec** = seletor de formato (`<select>` cru) + `ContractSection` (Monaco); **Metadata** =
  Card com título/descrição/versão/serviço (`<input>/<textarea>/<select>` crus) + bloco read-only
  (tipo/protocolo/autor/datas); **Validation** = `DraftValidationPanel`.
- Banners inline de feedback (save/submit/export success/error).
- Hooks/estado: `contractStudioApi.{getDraft,updateContent,updateMetadata,submitForReview}`,
  `useDraftExport`, `useDraftValidation`, `serviceCatalogApi.listServices`. `isEditable = status === 'Editing'`.
- `ContractDraft` (de `contractStudio.ts`): `id, title, description, serviceId, contractType, protocol,
  specContent, format, proposedVersion, status, author, lastEditedAt, lastEditedBy, createdAt, ...`.

## Componentes a entregar

### 1. `DraftIdentityCard` — `studio/components/DraftIdentityCard.tsx` (novo)

Cartão de identidade sticky à esquerda, mesma linguagem visual do `ContractWorkspaceIdentityCard`
(header com gradiente, tile de ícone do tipo, nome em mono, chips, meta-rows), mas com dados de
DRAFT (sem approvals/policies/compliance — não se aplicam a um draft):
- Header: tile de ícone (por `contractType`/protocolo) + `title` (mono) + badge **Draft**
  (estado via `t('contracts.draftStatus.<status>')`).
- Chips: protocolo (via `PROTOCOL_COLORS`) + `v{proposedVersion}`.
- Meta-rows (via `IdentityMetaRow`): Tipo (`contractType`), Serviço vinculado (nome do serviço ou "—"),
  Autor (`author`), Criado (`createdAt`), Última edição (`lastEditedAt`/`lastEditedBy`).
- Reusa os primitives partilhados (`IdentityMetaRow`; `IdentityMiniStat` apenas se houver uma
  mini-strip útil — caso contrário não usar, YAGNI). Apresentacional, sem hooks além de `useTranslation`.

### 2. Layout v5 de 2 colunas + `PageHeader`

- Acima do grid, um link compacto de **voltar para `/contracts`** (padrão do create workspace:
  `‹ Contracts`). A seguir, o **`PageHeader`** do DS (título = `title` do draft; subtítulo =
  `protocolo · v{proposedVersion}`), com as ações à direita em **DS `Button`**: Export (quando há
  specContent), Save, Submit for Review — preservando
  `isEditable`, `isExporting`, `isSaving`, `submitMutation.isPending`, e os mesmos handlers
  (Save chama metadata-save na tab metadata, content-save caso contrário — comportamento atual).
- Abaixo, **grid 2 colunas** `[300px_minmax(0,1fr)]`: 1ª sticky = `DraftIdentityCard`; 2ª = tabs + conteúdo.
  (Nota: ao contrário do workspace, NÃO há 3ª coluna/rail — o draft não tem rail.)

### 3. Tabs DS

- Substituir a tab bar custom por **DS `Tabs`** (Spec · Metadata · Validation), preservando o badge de
  contagem de issues na Validation (`validationIssueCount`) — passar como parte do label/ícone se o
  `Tabs` suportar; caso contrário, manter um indicador compacto ao lado.
- A tab **Spec** mantém `ContractSection` (Monaco) ocupando a altura/largura da coluna direita
  (`flex-1 min-h-0`). Editor intocado.

### 4. Controlos DS

- Seletor de formato (Spec) `<select>` → **DS `Select`** (YAML/JSON/XML), preservando `value`/`onChange`/`disabled`.
- Form da Metadata: `<input>`→`TextField`, `<textarea>`→`TextArea`, `<select>` de serviço→`Select`
  (opções = serviços; opção vazia "sem serviço" selecionável), preservando `readOnly`/`disabled` por
  `isEditable` e os mesmos `value`/`onChange`. O bloco read-only de info mantém-se (tokens afinados);
  evitar duplicar no form o que já aparece no card (tipo/protocolo/autor/datas).

### 5. Feedback de estado

- Os banners inline de save/submit/export (success/error) → padrão DS (`Alert` ou toast `Sonner`,
  conforme o que o app usa para feedback de mutação). Manter as mesmas chaves i18n.
- Loading/error de `getDraft` → componentes DS de estado (`PageLoadingState`/`PageErrorState` ou
  `Loader`/`ErrorState` do DS) em vez do markup custom atual.

## Fluxo de dados

Inalterado. Mesmos hooks, mutations e `isEditable`. O `DraftIdentityCard` é uma view derivada do
`ContractDraft` — só apresentação.

## Testes

- Atualizar `__tests__/pages/DraftStudioPage.test.tsx` para a nova estrutura: `DraftIdentityCard`
  presente; DS Tabs (role=tab) navegáveis (Spec/Metadata/Validation); o card reflete `title`/`protocol`/`status`.
- Manter os testes de loading/no-crash. A suíte deve permanecer verde.

## i18n

Reusar chaves existentes (`contracts.studio.*`, `contracts.draftStatus.*`, `contracts.draftValidation.*`).
Novas chaves só se introduzir labels novos (ex.: labels de meta-row do card que ainda não existam).
4 locales reais: en/es/pt-BR/pt-PT.

## Fora de escopo

- Internos do Monaco `ContractSection` e do `DraftValidationPanel`.
- `ContractBuilderLayout`/`*BuilderPage` (sub-projeto C), `workspace/builders/Visual*Builder` (sub-projeto D).
- Backend, contratos de API, schema.

## Critérios de verificação

1. `/contracts/studio/:draftId` renderiza: `PageHeader` (DS) + `DraftIdentityCard` sticky à esquerda
   + DS Tabs + conteúdo da tab. → verificar render.
2. Trocar de tab (Spec/Metadata/Validation) funciona; badge de issues na Validation preservado. → verificar.
3. `DraftIdentityCard` reflete `title`/`protocol`/`status`/`proposedVersion`/serviço/autor. → verificar.
4. Save/Submit/Export continuam a funcionar (mesmos handlers e regras de `isEditable`). → verificar mock.
5. Form da Metadata usa controlos DS; sem `<input>/<select>/<textarea>` crus restantes na página
   (exceto o que não tenha equivalente DS, justificado). → verificar.
6. `npm run lint` 0 erros; `npm run test` suíte verde; build OK.
7. `ContractSection`/`DraftValidationPanel` internamente intocados (diff não altera a sua lógica).

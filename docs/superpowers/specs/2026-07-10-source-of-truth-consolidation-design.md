# P4 — Consolidação Source-of-Truth + Global Search (design)

**Data:** 2026-07-10
**Autor:** Claude Opus 4.8 (autonomia total concedida pelo owner)
**Persona:** engenheiro/owner que quer, a partir de onde já está (um serviço, um
contrato, um resultado de busca), chegar à "fonte da verdade" consolidada
cross-module dessa entidade.

## Contexto auditado — o que já existe e funciona

- **Global search maduro e ligado:** `CommandPalette` (⌘K via
  `AppShell` `metaKey/ctrlKey + 'k'`, montado + `AppTopbarSearch`) e
  `GlobalSearchPage` (`/search`) consomem `/source-of-truth/global-search`
  (`globalSearchApi.search`). Têm facets, scope pills, navegação por teclado,
  "see all", dedup com knowledge search. **Não rebuildar.**
- **Vistas SoT consolidadas ricas:** `ServiceSourceOfTruthPage`
  (`/source-of-truth/services/:serviceId`) e `ContractSourceOfTruthPage`
  (`/source-of-truth/contracts/:contractVersionId`) — overview, ownership, APIs,
  contratos, referências, anel de cobertura (7 indicadores), quick links. A
  `ServiceSoT` já liga de volta a `/services/:serviceId` e ao Explorer.
- **SoT Explorer** (`/source-of-truth`, `SourceOfTruthExplorerPage`) →
  `/source-of-truth/search` (`sourceOfTruthApi.search`, agrupado
  services/contracts/references) → vistas SoT.

## Problema (fragmentação real)

1. A `ServiceDetailPage` primária **não liga à sua vista SoT consolidada**. A
   página `/source-of-truth/services/:serviceId` só é alcançável pela busca do
   Explorer — ilha a partir do serviço em foco. Assimétrico: a vista SoT liga ao
   detalhe, mas o detalhe não liga à vista SoT.
2. Simétrico para contratos: o `ContractWorkspacePage` não liga a
   `/source-of-truth/contracts/:contractVersionId`.
3. Duas caixas de busca (Explorer + Global) com destinos diferentes (vistas SoT
   vs. páginas primárias). Não são duplicados puros; fundir os endpoints é
   trabalho de backend (fora de escopo).

## Arquitetura

Mesma filosofia da P3: **a vista consolidada acessível de onde já estás.** As
páginas primárias (serviço, contrato) e os resultados de busca ganham um
caminho honesto para a respetiva vista SoT consolidada. Os motores de busca não
mudam.

## Fatias

### F1 — Serviço → sua vista SoT consolidada

- `ServiceDetailPage`: adicionar link **"Ver fonte da verdade"** →
  `/source-of-truth/services/${serviceId}`, junto dos cross-links já existentes
  no separador overview secundário (onde ficam "Ver mudanças" e "Ver scorecard",
  com `service.name`/`serviceId` em scope). Usa o `serviceId` de
  `useParams` (inequívoco).
- **Verificação:** teste que o `ServiceDetailPage` (serviço `Active`, scaffold de
  `ServiceDetailPage.setup.test.tsx`) renderiza um link com
  `href="/source-of-truth/services/<id>"`.

### F2 — Contrato → sua vista SoT consolidada

- `ContractWorkspacePage`: adicionar link **"Ver fonte da verdade"** →
  `/source-of-truth/contracts/${contractVersionId}` no mesmo wrapper de ações do
  header onde a P2/P3 colocou "Health timeline" e "Consumer portal" (gated em
  `useParams().contractVersionId`, honest-null).
- Reverso já existe: `ContractSourceOfTruthPage` liga a
  `/contracts/:contractVersionId` (não precisa de mudança).
- **Verificação:** teste que o header do workspace (com
  `<Route path="/contracts/:contractVersionId">`) renderiza o link SoT com o
  `href` correcto; teste do reverso se adicionado.

### F3 — Ponte busca global → vista SoT (apenas serviços)

- `GlobalSearchPage` `SearchResultCard`: para `item.entityType === 'service'`
  (onde `item.entityId === serviceId` é inequívoco), adicionar um link
  secundário **"Fonte da verdade"** → `/source-of-truth/services/${item.entityId}`,
  distinto do clique principal no card (que continua a ir para `item.route`, a
  página primária). Liga honestamente as duas superfícies de busca sem tocar no
  backend.
- **Honest-null:** só para `entityType === 'service'`. **Não** se cria a ponte
  para contratos dentro da busca global — o `entityId` de contrato aí não é
  confirmadamente o `contractVersionId`; não se fabrica esse mapeamento.
- **Verificação:** teste que um resultado `entityType:'service'` mostra a ponte
  com `href="/source-of-truth/services/<entityId>"` e que um resultado
  `entityType:'contract'` **não** a mostra.

## i18n

Novas chaves nos 4 locales (`en`, `es`, `pt-BR`, `pt-PT`) via script deep-merge;
`npm run validate:i18n` tem de passar. Chaves previstas:
- `serviceDetail.viewSourceOfTruth`
- `contracts.workspace.viewSourceOfTruth` (namespace do header do workspace,
  junto de `healthTimeline`/`consumerPortal`)
- `commandPalette.globalSearch.sourceOfTruthLink`

## Testes

Vitest + Testing Library, centralizados em `src/frontend/src/__tests__/**`.
Forward-only (falha antes, passa depois). Revisão final opus de todo o branch
antes do merge direto em `main` (sem PR).

## Não-objetivos

- Não rebuildar `CommandPalette` / `GlobalSearchPage` / motores de busca.
- Não fundir os endpoints `/source-of-truth/search` e
  `/source-of-truth/global-search` (backend).
- Não criar ponte SoT para contratos dentro da busca global (identidade não
  confirmada).
- Não redesenhar as vistas SoT nem o Explorer.

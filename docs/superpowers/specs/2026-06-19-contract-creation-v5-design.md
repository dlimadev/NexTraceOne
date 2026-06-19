# Contract Creation v5 — Hub + Create Workspace (Betterstack redesign)

- **Data:** 2026-06-19
- **Ciclo:** Betterstack redesign — par do flagship "Registrar Serviço" para contratos
- **Branch:** `redesign/betterstack-contract-creation`
- **Referência de padrão:** Service Workspace v5 (`features/catalog/pages/ServiceDetailPage.tsx`)
- **Plano relacionado:** `project_contract_studio_redesign` (memória)

## Objetivo

Espelhar o flagship de criação de serviço ("Registrar Serviço" / Service Workspace v5) para
**contratos**: repaginar o hub (Contract Studio) e reescrever o fluxo de criação como um
**workspace de 2 colunas com cartão de identidade ao vivo**. Mudança de apresentação — sem
alterações de backend nem de API.

## Recorte (escopo)

Decisões travadas no brainstorming:

- **Escopo A** — só a *entrada* de criação (hub + wizard). Editores Monaco/builders NÃO são tocados.
- **Fluxo A** — hub-launcher → create workspace v5 pré-semeado. Tabs do form:
  `Serviço · Tipo & Modo · Detalhes · Confirmar`.
- **Alvo dos cards de tipo** — abrem o create workspace (tipo pré-semeado). Os builders visuais
  (`/contracts/studio/{type}`) ficam acessíveis pelas suas rotas mas **deixam de ser a entrada do hub**
  (demovidos, intocados).

### Fora de escopo (não tocar)

- `studio/DraftStudioPage.tsx` (editor de draft — destino pós-criação)
- `workspace/ContractWorkspacePage.tsx` (workspace completo de edição)
- `pages/{RestOpenApi,AsyncApi,GraphQL,Protobuf,SoapWsdl}BuilderPage.tsx` (builders visuais)
- Qualquer Monaco / `workspace/builders/*` / `workspace/sections/*`
- Backend, contratos de API, schema

## Estado atual (5 superfícies)

| Rota | Página | Papel |
|---|---|---|
| `/contracts/studio/new` | `ContractStudioPage` | Hub: cards de tipo + drafts + banner workflow |
| `/contracts/new` | `CreateContractPage` | Wizard 3 passos (serviço→tipo+modo→detalhes) |
| `/contracts/studio/:draftId` | `DraftStudioPage` | Editor de draft (spec Monaco / metadata / validação) |
| `/contracts/studio/{rest,async,soap,graphql,protobuf}` | `*BuilderPage` | Builders visuais por tipo |
| `/contracts/:contractVersionId` | `ContractWorkspacePage` | Workspace completo |

Fragmentação análoga à que o v5 de serviço resolveu. Este ciclo trata só as 2 primeiras linhas.

## Componentes a entregar

### 1. Hub — `pages/ContractStudioPage.tsx` (toque leve)

- Manter a strip "Em progresso" (drafts) e o banner de workflow (já limpos em Betterstack).
- Cards de tipo ganham:
  - linha **"Best for…"** (guidance do plano Contract Studio):
    - REST/OpenAPI → "Best for HTTP request/response APIs between microservices"
    - AsyncAPI → "Best for Kafka, AMQP, SNS, WebSocket event-driven services"
    - SOAP/WSDL → "Best for legacy SOAP services and enterprise integrations"
    - GraphQL → "Best for flexible data APIs with complex querying needs"
    - Protobuf/gRPC → "Best for high-performance binary RPC between services"
    - Shared Schema → "Reusable types referenced across multiple contracts"
  - deep-links no rodapé: **[Design]** → `/contracts/new?type={type}&mode=visual`;
    **[Import]** → `/contracts/new?type={type}&mode=import`.
  - clique principal do card → `/contracts/new?type={type}`.
- Deixar de navegar para `/contracts/studio/{type}` (builders demovidos).
- Polish Betterstack onde necessário (já usa tokens semânticos).

### 2. Create workspace — reescrita de `create/CreateContractPage.tsx`

Layout 2 colunas copiando o idioma do v5 (`grid lg:grid-cols-[300px_minmax(0,1fr)]`, coluna
esquerda `lg:sticky lg:top-4`).

**`ContractIdentityCard`** (novo sub-componente, espelha `ServiceIdentityCard`):
- topo com gradiente: tile de ícone do tipo + título (mono) + serviço vinculado + badge `Draft`;
- chips: tipo, protocolo, modo (visual/import/AI);
- mini strip 3 colunas: **Version · Operations · Validation** (placeholders na criação, como o
  strip Maturity/SLO/Incidents do v5);
- meta rows: Serviço, Protocolo, Author, Created;
- nota "Resumo atualiza ao vivo".
- Reflete o estado do formulário ao vivo via `summary` derivado (`useMemo`), igual ao v5.

**Form tabs** `Serviço · Tipo & Modo · Detalhes · Confirmar` com stepper numerado + Anterior/Próximo
(mesmo markup do `EditTabsContent` do v5), usando componentes DS
(`Button`, `TextField`, `TextArea`, `Select` de `shared/ui`, `SearchInput`):

- **Serviço** — busca + cards de serviço selecionáveis. Reusa a política existente
  (`supportsContracts`, `allowedContractTypes` de `contracts/shared/serviceContractPolicy`).
  Serviço pré-preenchido (`?serviceId=`) pula esta tab.
- **Tipo & Modo** — galeria de tipos filtrada pela política do serviço (pré-semeada por `?type=`),
  com a linha "Best for"; + modo visual/import/AI (pré-semeado por `?mode=`).
- **Detalhes** — título, descrição, protocolo (quando há múltiplos via `PROTOCOL_BY_TYPE`),
  textarea de import / prompt AI, + metadata específica de tipo:
  - SOAP: serviceName, targetNamespace, soapVersion, endpointUrl;
  - Event/AsyncAPI: asyncApiVersion, defaultContentType;
  - BackgroundService: serviceName, category, triggerType, scheduleExpression.
- **Confirmar** — recap read-only + botão "Criar draft".

**Lógica de criação preservada literalmente** (apenas movida para a estrutura de tabs):
`contractStudioApi.{createDraft, createSoapDraft, createEventDraft, createBackgroundServiceDraft,
generateFromAi, updateContent}`, seguida de `navigate('/contracts/studio/{draftId}')` para o
editor existente. O preview ao vivo é estado derivado — não altera persistência.

### 3. Rotas

- `/contracts/new` permanece a rota de criação; passa a ler `?type=` e `?mode=` (já lê `?serviceId=`).
- `/contracts/studio/new` (hub) e as rotas `*BuilderPage` ficam inalteradas.

### 4. Fluxo de dados

Sem mudanças de API. Mesmas queries (`serviceCatalogApi.listServices`, mutations de draft).
O cartão de preview é puramente derivado do estado do formulário.

## Testes

- Atualizar `__tests__/pages/CreateContractPage.test.tsx` — estrutura mudou (tabs + identity card).
- Atualizar `__tests__/contracts/ContractStudioPage.test.tsx` — linhas "Best for" + novos alvos
  dos cards (`/contracts/new?type=`).
- Acrescentar cobertura: cartão de identidade refletindo input do formulário ao vivo; pré-semeio
  por `?type=`/`?mode=`.
- Suíte deve permanecer verde (atualmente 2323/2323).

## i18n

Novas chaves em **4 locales (pt, en, es, fr)**: linhas "Best for" por tipo, labels das tabs
(`Serviço/Tipo & Modo/Detalhes/Confirmar`), hint "atualiza ao vivo", recap de confirmação.
Nunca strings hardcoded em UI (Parte 20 do CLAUDE.md).

## Critérios de verificação

1. Hub: cada card de tipo mostra "Best for" e navega para `/contracts/new?type=…` → verificar render + href.
2. Create workspace: 2 colunas, cartão à esquerda reflete título/serviço/tipo/modo ao digitar → verificar live update.
3. Tabs `Serviço·Tipo&Modo·Detalhes·Confirmar` navegáveis; `?type`/`?mode`/`?serviceId` pré-semeiam → verificar.
4. "Criar draft" chama a mesma mutation por tipo e navega para `/contracts/studio/{draftId}` → verificar mock de API.
5. `npm run lint` 0 erros; `npm run test` suíte verde; build OK.
6. Editores e builders intocados (diff não toca `studio/`, `workspace/`, `*BuilderPage`).

# Criação de contrato ancorada ao serviço + consolidação APIs/Interfaces — Design

**Data:** 2026-07-14
**Contexto:** Continuação do redesign Betterstack (ciclo 38). Sucede o ciclo 37 (drawer in-place serviço↔contratos).

---

## Motivação

Dois problemas de arquitetura de informação levantados pelo utilizador:

1. **"Não existe contrato sem API"** — há um item/CTA de criação de contrato *solto* no menu (Contract Catalog → "New contract" → `/contracts/new`, e o hub `/contracts/studio/new`) que começa por obrigar o utilizador a *escolher um serviço*. A ordem está invertida: o contrato deve nascer **a partir** do serviço/interface, não de um menu global.

2. **APIs vs Interfaces** — a tela de detalhe do serviço tem **duas abas** ("APIs" e "Interfaces") com listas quase idênticas para serviços REST, gerando confusão sobre a diferença entre os conceitos.

### Fundamentação de domínio (o "porquê")

Verificado no backend (`src/modules/catalog/.../Graph/Entities/`):

- **`ServiceInterface`** = *como* o serviço se expõe/conecta. Conceito largo — 13 tipos (`RestApi`, `SoapService`, `KafkaProducer/Consumer`, `GrpcService`, `GraphqlApi`, `BackgroundWorker`, `ScheduledJob`, `WebhookProducer/Consumer`, `ZosConnectApi`, `MqQueue`, `IntegrationBridge`). Tem `requiresContract`.
- **`ApiAsset`** (aggregate root, *"API publicada no grafo de engenharia"*) = subconjunto **publicado, versionado e consumível** (`RoutePattern`, `Version`, `Visibility`, `ConsumerRelationships`).
- **`ContractBinding`** liga `ServiceInterfaceId` ↔ `ContractVersionId` por ambiente.

Conclusão: **toda API é exposta por uma interface; nem toda interface é uma API.** O contrato liga-se à **interface**. "API" = a interface publicada e consumível. Isto valida ambas as mudanças: criação ancorada ao serviço/interface, e interface como conceito raiz com a API como subconjunto destacado.

---

## Peça A — Criação de contrato só a partir do serviço

### Comportamento

- **Contract Catalog (`/contracts`)** passa a ser só browse/governança: remover o botão "New contract".
- **Rotas redirecionam** (padrão já usado por `/contracts/legacy`):
  - `/contracts/new` → `<Navigate to="/contracts" replace />`
  - `/contracts/studio/new` → `<Navigate to="/contracts" replace />`
  - `/contracts/studio/:draftId` **mantém-se** (edição de rascunho existente, usada pelo drawer in-place). React-router ranqueia rotas estáticas acima de dinâmicas, portanto `/contracts/studio/new` (estática) tem precedência sobre `:draftId` sem depender da ordem de declaração.
- **CTAs soltos redirecionados** para o caminho sancionado from-scratch, o **onboarding wizard** (`/services/onboard`, que regista serviço + interface + contrato):
  - `SelfServicePortalPage.tsx:78,84` — tiles `/contracts/new?type=...` → `/services/onboard`
  - `PublicationCenterPage.tsx:64` — botão "New" `/contracts/studio/new` → `/services/onboard`
- **Caminhos sancionados de criação** ficam: (1) drawer in-place na tela do serviço (ciclo 37); (2) onboarding wizard.

### Componentes preservados

- `ContractCreateForm` (core reutilizado pelo drawer) — **inalterado**.
- `ContractDraftEditor` (edição) e `DraftStudioPage` — **inalterados**.
- `CreateContractPage` e `ContractStudioPage` — tornam-se **wrappers órfãos** (rota atrás de redirect). **Não** são apagados (mudança cirúrgica; código morto pré-existente não é removido sem pedido). Documentado como órfão para follow-up futuro.

### i18n

- A chave `contracts.catalog.actions.newContract` deixa de ser usada. **Não** é removida (pode ter outras referências; remoção arriscada por pouco ganho). Sem novas chaves nesta peça.

---

## Peça B — Consolidar "APIs" + "Interfaces" numa só aba

### Restrição de honestidade

Não há join fiável `ApiAsset ↔ ServiceInterface` exposto no frontend (contratos ligam via `ServiceInterfaceId`; `ApiAsset` junta-se por `RoutePattern`/consumidores). **Não** se inventa um merge record-a-record frágil.

### Comportamento

- Remover as abas separadas `apis` e `interfaces` do `viewTabItems` e do tipo `ServiceTab`.
- Adicionar **uma aba unificada** com id `'interfaces'` (reutilizado) e label `serviceDetail.tabInterfacesApis` = "Interfaces & APIs".
- O render de `activeViewTab === 'interfaces'` empilha, por ordem:
  1. **Secção "APIs publicadas"** (topo, destaque) — a tabela `service.apis` atual (o subconjunto consumível/versionado com contagem de consumidores). Extraída do bloco inline `activeViewTab === 'apis'` (linhas 1110–1162) para um componente `ServiceApisSection` (props: `apis: ServiceApiSummary[]`), em `features/catalog/components/ServiceApisSection.tsx`.
  2. **Secção "Todas as interfaces"** — o `<ServiceInterfacesTab serviceId={serviceId} />` atual, com um **badge "API"** adicionado aos tipos consumíveis (`RestApi`, `GraphqlApi`, `GrpcService`, `SoapService`) na coluna de tipo, materializando "API = interface publicada e consumível".
- Remover o bloco inline `activeViewTab === 'apis'`.

### i18n (4 locales: en, es, pt-BR, pt-PT)

- Nova chave `serviceDetail.tabInterfacesApis` = "Interfaces & APIs" (traduzida).
- Nova chave para o cabeçalho da secção de APIs publicadas: `serviceDetail.publishedApis` = "Published APIs" (traduzida).
- Nova chave de badge: `serviceInterfaces.apiBadge` = "API" (igual nos 4 locales).

### Componentes

- `ServiceApisSection.tsx` — **novo**. Move a tabela de APIs inline (sem alterar colunas/lógica). Recebe `apis` e o tradutor via `useTranslation`.
- `ServiceInterfacesTab.tsx` — **modificado**. Badge "API" nos tipos consumíveis. Sem outras alterações.
- `ServiceDetailPage.tsx` — **modificado**. Remove tab/tipo `apis`, remove bloco inline, renderiza `ServiceApisSection` + `ServiceInterfacesTab` sob a aba unificada.

---

## Testes

- **Peça A:**
  - Teste de rota: `/contracts/new` e `/contracts/studio/new` renderizam redirect para `/contracts` (não a página de criação).
  - `ContractCatalogPage` já **não** renderiza o botão "New contract".
  - Ajustar/confirmar testes existentes de `ContractCatalogPage.browse` e `CreateContractPage` (este último pode passar a testar o redirect ou ser removido se a página deixar de ser alvo — decisão no plano, preferindo ajustar sobre apagar).
- **Peça B:**
  - `ServiceApisSection` renderiza linhas a partir de `apis` e estado vazio.
  - `ServiceDetailPage`: a aba unificada "Interfaces & APIs" existe; as abas `apis` e `interfaces` separadas já **não** existem.
  - `ServiceInterfacesTab`: badge "API" aparece para tipo `RestApi` e não para `KafkaProducer`.
- Todos os testes em `src/__tests__/**` (Vitest só descobre aí).
- Gates: `tsc`, `eslint`, `build`, suíte completa.

---

## Fora de âmbito

- Merge record-a-record `ApiAsset ↔ ServiceInterface` (requer join no backend) — follow-up.
- Remoção do código órfão `CreateContractPage`/`ContractStudioPage` — follow-up.
- Alterações no onboarding wizard em si.
- Edição de contrato publicado / relacionamentos espelho (backlog do ciclo 37).

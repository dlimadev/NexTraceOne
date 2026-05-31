# Contract Studio Redesign — Design Spec

**Data:** 2026-05-31
**Autor:** Diogo Lima
**Escopo:** Feature — Contracts (ContractStudioPage hub + todos os builders individuais)
**Status:** Aprovado para implementação

---

## 1. Objetivo

Redesenhar o `ContractStudioPage` de uma página estática de escolha de tipo para um **Hub APIM-style** com métricas, rascunhos em progresso e picker por categoria. Simultaneamente, substituir os builders individuais (REST, AsyncAPI, GraphQL, Protobuf, SOAP) por um layout partilhado **Code + Visual split** — editor Monaco à esquerda, preview visual sincronizado à direita — inspirado no modelo Stoplight Studio.

---

## 2. Contexto — Estado Actual

| Componente | Implementação actual | Problemas |
|---|---|---|
| `ContractStudioPage` | Página estática com 6 cards de tipo | Sem métricas, sem acesso rápido a rascunhos, sem contexto por categoria |
| `RestOpenApiBuilderPage` | Formulário com lista de endpoints expansíveis | Sem editor de spec raw, sem preview de operações sincronizado |
| `AsyncApiBuilderPage` | Formulário com lista de canais + detail panel | Sem editor Monaco, sem preview visual |
| `GraphQLBuilderPage` | Formulário básico | Sem SDL editor com highlighting, sem type explorer |
| `ProtobufBuilderPage` | Formulário básico | Sem editor .proto com syntax, sem services tree |
| `SoapWsdlBuilderPage` | Formulário básico | Sem editor WSDL/XML, sem operations list |

---

## 3. Decisões de Design

| Questão | Decisão |
|---|---|
| Modelo de interacção dos builders | Code + Visual split (Monaco esquerda, preview direita) — estilo Stoplight Studio |
| Landing page | Hub APIM-style: stats + rascunhos em progresso + picker por categoria |
| Layout base | `ContractBuilderLayout` partilhado por todos os builders |
| Preview | Read-only, sincronizado com debounce 400ms, mantém último estado válido em caso de parse error |
| SplitPane | Divider arrastável, default 45%/55%, posição memorizada em `localStorage` por `language` key |
| Rotas | Sem alteração — builders continuam em `/contracts/studio/rest`, `/contracts/studio/async`, etc. |
| Backend | Nenhuma alteração a endpoints ou DTOs |
| i18n | 100% via `t()` — sem strings hardcoded |

---

## 4. Secção 1 — ContractStudioPage (Hub)

### 4.1 Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│  Contract Studio                              [+ New Contract]       │
│  Design, version and publish API contracts                          │
├─────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │  32          │  │  18          │  │  5           │              │
│  │  Contracts   │  │  Published   │  │  In Draft    │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
├─────────────────────────────────────────────────────────────────────┤
│  In Progress  (5)                                              [→]  │
│  ┌─────────────────────┐  ┌─────────────────────┐  ┌──────────...  │
│  │  OpenAPI  DRAFT     │  │  AsyncAPI  DRAFT    │  │              │
│  │  Payments API v2    │  │  Order Events       │  │              │
│  │  Modified 2h ago    │  │  Modified 1d ago    │  │              │
│  │            [Resume] │  │            [Resume] │  │              │
│  └─────────────────────┘  └─────────────────────┘  └──────────...  │
├─────────────────────────────────────────────────────────────────────┤
│  New Contract                                                       │
│                                                                     │
│  REST & HTTP                                                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                │
│  │ REST/OpenAPI│  │ SOAP / WSDL │  │  GraphQL    │                │
│  │ Best for:   │  │ Best for:   │  │  Best for:  │                │
│  │ Public APIs │  │ Enterprise  │  │  Flexible   │                │
│  │ microservices│ │ mainframe   │  │  queries    │                │
│  └─────────────┘  └─────────────┘  └─────────────┘                │
│                                                                     │
│  Event-Driven                                                       │
│  ┌─────────────┐  ┌─────────────┐                                  │
│  │  AsyncAPI   │  │  Protobuf   │                                  │
│  │ Best for:   │  │ Best for:   │                                  │
│  │ Kafka, SNS  │  │  gRPC, IoT  │                                  │
│  └─────────────┘  └─────────────┘                                  │
│                                                                     │
│  Shared / Cross-cutting                                             │
│  ┌─────────────┐                                                    │
│  │ Shared Schema│                                                   │
│  └─────────────┘                                                    │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.2 Comportamento

- **Stats** — 3 `StatCard`s com dados vindos de `useContractStats()`. Se a query falhar ou carregar, os cards mostram `—` sem bloquear a página.
- **In Progress** — scroll horizontal de draft cards. Cada card: protocol badge + nome + "Modified X ago" + botão "Resume" que navega para o builder correspondente à rota do contrato. Query: `useContractDrafts()`, limitado a 10 resultados. Se vazio: mensagem `contractStudio.inProgress.empty`.
- **New Contract** — tipo picker estático organizado em 3 categorias. Cada card tem label, ícone Lucide, badge de protocolo e 2–3 bullets "Best for".
- **[+ New Contract]** no header faz `scrollIntoView` na secção "New Contract".
- **"→" (Ver todos)** na secção In Progress navega para `/contracts/catalog`.

### 4.3 Categorias e tipos

| Categoria | Tipos |
|---|---|
| REST & HTTP | REST/OpenAPI → `/contracts/studio/rest`, SOAP/WSDL → `/contracts/studio/soap`, GraphQL → `/contracts/studio/graphql` |
| Event-Driven | AsyncAPI → `/contracts/studio/async`, Protobuf/gRPC → `/contracts/studio/protobuf` |
| Shared / Cross-cutting | Shared Schema → `/contracts/studio/shared-schema` |

### 4.4 Hooks necessários

```ts
// useContractStats() — retorna { total, published, inDraft }
// useContractDrafts() — retorna DraftContractSummary[] (limitado a 10)
// DraftContractSummary: { id, name, protocol, lastModified, builderRoute }
```

Estes hooks são **criados neste sprint**. `useContractStats` agrega contagens a partir do endpoint de listagem existente (filtra por `lifecycleState`). `useContractDrafts` chama o mesmo endpoint com filtro `state=Draft&limit=10`. Se o endpoint não existir ainda, os hooks retornam dados estáticos vazios sem quebrar a página.

---

## 5. Secção 2 — ContractBuilderLayout (layout partilhado)

### 5.1 Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│ ← Studio  /  Payments API v2    [OpenAPI 3.1]  [● Valid]            │
│                                         [Format] [Save] [Publish]   │
├─────────────────────────┬───────────────────────────────────────────┤
│  Editor Monaco          │  Visual Preview                           │
│                         │                                           │
│  openapi: 3.1.0        ││  ▼ /users                                │
│  info:                 ││    GET    List users                      │
│    title: Payments API ││    POST   Create user                     │
│    version: 1.0.0      ││                                           │
│  paths:                ││  ▼ /users/{id}                           │
│    /users:             ││    GET    Get user by ID                  │
│      get:              ││    PUT    Update user                     │
│        summary: List   ││    DELETE Delete user                     │
│                        ││                                           │
│                     ──◆──│  3 paths · 7 operations                 │
└─────────────────────────┴───────────────────────────────────────────┘
```

### 5.2 Interface do componente

```tsx
interface ContractBuilderLayoutProps {
  contractName: string;          // ex: "Payments API v2"
  protocol: string;              // ex: "OpenAPI 3.1"
  language: 'yaml' | 'json' | 'graphql' | 'proto' | 'xml';
  initialContent: string;        // template inicial (string estática por builder)
  renderPreview: (content: string) => React.ReactNode;
  onSave?: (content: string) => void;     // botão "Save Draft" oculto se undefined
  onPublish?: (content: string) => void;  // botão "Publish" oculto se undefined
}
```

### 5.3 BuilderHeader

- Breadcrumb: "← Studio" (link para `/contracts/studio`) + "/" + `contractName`
- Badge de protocolo: `protocol` prop — ex: `[OpenAPI 3.1]`
- Chip de validação:
  - Verde "● Valid" — parse OK, zero erros
  - Âmbar "▲ N warnings" — parse OK com avisos
  - Vermelho "✕ N errors" — parse falhou ou erros de schema
- Botões: **Format** (prettify do editor), **Save Draft** (chama `onSave`), **Publish** (chama `onPublish`)

### 5.4 SplitPane

- Dois painéis com divider arrastável (`mousedown` + `mousemove` no `document`)
- Default: esquerda 45%, direita 55%
- Mínimo 25% por painel
- Posição memorizada em `localStorage` com key `builder-split-${language}`
- O divider tem `cursor: col-resize` e uma linha visual subtil com handle central `◆`

### 5.5 Fluxo editor → preview

1. Monaco emite `onChange(value)`
2. `useDebounce(value, 400)` aguarda 400ms
3. `parseContent(language, debouncedValue)` tenta parse:
   - `yaml` → `js-yaml.load()`
   - `json` → `JSON.parse()`
   - `proto` / `graphql` / `xml` → extracção por regex lightweight
4. Parse OK → `setLastValidContent(parsed)` + actualiza chip para Valid/warnings
5. Parse error → mantém `lastValidContent` + mostra banner "Parse error at line N" no topo do preview
6. `renderPreview(debouncedValue)` recebe sempre o conteúdo raw (string); o preview faz o seu próprio parse internamente

### 5.6 Nota sobre `js-yaml`

`js-yaml` já está disponível como dependência transitiva no projecto (via swagger-parser ou similar). Verificar antes de adicionar dependência nova.

---

## 6. Secção 3 — Previews por tipo

### Interface base

Todos os previews recebem `content: string` (raw) e retornam JSX ou `null` em caso de erro — nunca lançam excepção.

### 6.1 RestOperationsPreview

Extrai `paths` do YAML/JSON OpenAPI. Para cada path, lista os métodos HTTP com badge colorido + summary. Agrupa por path. Footer: "N paths · M operations".

```
▼ /users
  GET    List users
  POST   Create user
▼ /users/{id}
  GET    Get user by ID
  PUT    Update user
  DELETE Delete user
```

### 6.2 AsyncApiChannelsPreview

Extrai `channels` do YAML AsyncAPI. Para cada canal: address em `font-mono` + badge de protocol + badge de operation (publish/subscribe).

```
● user.registered     kafka   publish
● order.created       kafka   publish
● inventory.updated   amqp    subscribe
```

### 6.3 GraphQlTypesPreview

Extrai blocos `type`, `input`, `enum`, `interface` via regex. Agrupa por kind. Lista fields por tipo (expandível).

```
▼ Types (3)
  User  · id, name, email
  Order · id, total, status
▼ Inputs (1)
  CreateUserInput · name, email
▼ Enums (1)
  OrderStatus · PENDING, PAID, SHIPPED
```

### 6.4 ProtobufServicesPreview

Extrai `service` e `message` via regex no `.proto`. Lista services com os seus RPCs + messages definidas.

```
▼ Services (1)
  UserService
    rpc GetUser (GetUserRequest) returns (User)
    rpc ListUsers (ListUsersRequest) returns (ListUsersResponse)
▼ Messages (4)
  User, GetUserRequest, ListUsersRequest, ListUsersResponse
```

### 6.5 SoapOperationsPreview

Extrai `<operation>` via regex no WSDL XML. Lista operações flat com nome.

```
Operations (3)
  GetUser
  CreateUser
  DeleteUser
```

---

## 7. Ficheiros — Resumo de alterações

### Novos ficheiros

```
src/frontend/src/features/contracts/studio/
├── ContractBuilderLayout.tsx
├── components/
│   ├── BuilderHeader.tsx
│   ├── SplitPane.tsx
│   └── previews/
│       ├── RestOperationsPreview.tsx
│       ├── AsyncApiChannelsPreview.tsx
│       ├── GraphQlTypesPreview.tsx
│       ├── ProtobufServicesPreview.tsx
│       └── SoapOperationsPreview.tsx
```

### Ficheiros reescritos

```
src/frontend/src/features/contracts/pages/
├── ContractStudioPage.tsx        ← Hub APIM-style (secção 4)
├── RestOpenApiBuilderPage.tsx    ← usa ContractBuilderLayout
├── AsyncApiBuilderPage.tsx       ← usa ContractBuilderLayout
├── GraphQLBuilderPage.tsx        ← usa ContractBuilderLayout
├── ProtobufBuilderPage.tsx       ← usa ContractBuilderLayout
└── SoapWsdlBuilderPage.tsx       ← usa ContractBuilderLayout
```

### Ficheiros não alterados

- `ContractWorkspacePage.tsx` e toda a pasta `workspace/` — visualização de contratos existentes
- `DraftStudioPage.tsx` — caso de uso diferente
- `ContractCatalogPage.tsx`, `ContractGovernancePage.tsx` e restantes páginas do módulo
- Backend — nenhum endpoint, DTO ou migration

---

## 8. i18n — Novas chaves

Adicionar em `en.json`, `pt-BR.json`, `es.json`, `pt-PT.json` (inferir `fr.json`):

```
contractStudio.stats.total
contractStudio.stats.published
contractStudio.stats.inDraft
contractStudio.inProgress.title
contractStudio.inProgress.resume
contractStudio.inProgress.viewAll
contractStudio.inProgress.empty
contractStudio.newContract.title
contractStudio.newContract.categories.rest
contractStudio.newContract.categories.events
contractStudio.newContract.categories.shared
contractStudio.type.bestFor.rest
contractStudio.type.bestFor.soap
contractStudio.type.bestFor.graphql
contractStudio.type.bestFor.asyncapi
contractStudio.type.bestFor.protobuf
contractStudio.type.bestFor.sharedSchema
contractBuilder.header.format
contractBuilder.header.saveDraft
contractBuilder.header.publish
contractBuilder.validation.valid
contractBuilder.validation.warnings
contractBuilder.validation.errors
contractBuilder.preview.parseError
contractBuilder.preview.empty
```

---

## 9. Testes

| Ficheiro | Casos principais |
|---|---|
| `ContractStudioPage.test.tsx` | Renderiza 3 stat cards; mostra secção "In Progress" com draft cards; clicar num type card navega para rota correcta; botão "Resume" navega para builder do contrato |
| `ContractBuilderLayout.test.tsx` | Renderiza Monaco + preview; `onChange` com debounce actualiza preview; parse error mantém último preview válido + mostra banner; botão Format chama formatter; Save chama `onSave` com conteúdo actual |
| `SplitPane.test.tsx` | Renderiza dois painéis; divider existe no DOM; proporção default 45/55 respeitada |
| `RestOperationsPreview.test.tsx` | YAML válido com 2 paths → renderiza todos os métodos; YAML inválido → retorna null sem throw |
| `AsyncApiChannelsPreview.test.tsx` | YAML com 2 channels → renderiza address + operation badge |

Os previews GraphQL, Protobuf e SOAP não têm testes unitários isolados neste sprint — cobertos pelo teste do `ContractBuilderLayout`.

---

## 10. Fora de escopo

- Persistência do conteúdo do editor (Save chama `onSave`, mas o backend store não é implementado neste sprint)
- Testes E2E
- `SharedSchemaBuilderPage` — recebe o layout novo mas sem preview especializado (área de preview mostra placeholder)
- `DraftStudioPage.tsx` — não alterada
- Remoção de código legado nos builders antigos — os ficheiros são reescritos in-place, sem PR separado de cleanup

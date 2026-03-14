# Developer Portal — Workspace de Descoberta, Consumo e Evolução de APIs

> **Bounded Context:** Catalog · **Camadas:** Domain / Application / Infrastructure
> **Namespace raiz:** `NexTraceOne.Catalog.Domain.Portal`, `NexTraceOne.Catalog.Application.Portal`, `NexTraceOne.Catalog.Infrastructure.Portal`

---

## Índice

1. [Visão Geral](#1-visão-geral)
2. [Arquitetura](#2-arquitetura)
3. [Entidades de Domínio](#3-entidades-de-domínio)
4. [Enums](#4-enums)
5. [Erros de Domínio](#5-erros-de-domínio)
6. [Features / Use Cases](#6-features--use-cases)
7. [API Endpoints](#7-api-endpoints)
8. [Integrações Cross-Module](#8-integrações-cross-module)
9. [Segurança e Permissões](#9-segurança-e-permissões)
10. [Frontend](#10-frontend)
11. [SQL Seeds para Desenvolvimento](#11-sql-seeds-para-desenvolvimento)
12. [Testes](#12-testes)
13. [Roadmap / Preparação para Evolução](#13-roadmap--preparação-para-evolução)

---

## 1. Visão Geral

O **Developer Portal** é o workspace central de **descoberta, consumo, criação e evolução** de APIs, serviços e eventos na plataforma NexTraceOne. Enquanto o Engineering Graph modela a topologia do ecossistema e o módulo Contracts gere os contratos técnicos, o Developer Portal é a camada voltada para o desenvolvedor — o ponto de entrada único para interagir com todo o catálogo tecnológico da organização.

### O que o módulo resolve

| Pergunta do desenvolvedor | Capacidade correspondente |
|---|---|
| *Que APIs existem na minha organização?* | Pesquisa com full-text e filtros facetados no catálogo |
| *Quais APIs eu consumo? Quais são minhas?* | Vistas personalizadas "My APIs" e "APIs I Consume" |
| *Como integrar com esta API rapidamente?* | Playground interativo com execução sandbox |
| *Como gerar o SDK client para esta API?* | Geração de código em 5 linguagens (C#, Java, Python, TypeScript, Go) |
| *Como ser notificado de breaking changes?* | Subscrições com notificações por e-mail ou webhook |
| *Qual é a saúde e o histórico desta API?* | Health indicators, timeline de eventos e lista de consumidores |
| *Quais APIs são mais utilizadas no portal?* | Analytics de pesquisas, visualizações e execuções |

### Características-chave

- **Pesquisa facetada** — busca por nome, equipa, descrição, tipo, status e dono
- **Subscrições configuráveis** — 4 níveis de notificação × 2 canais de entrega
- **Playground sandbox** — execução segura de chamadas HTTP com histórico completo
- **Geração de código** — SDK clients, exemplos de integração, testes de contrato e modelos de dados
- **Analytics de adoção** — métricas de utilização, top pesquisas e lacunas no catálogo
- **Pesquisas salvas** — critérios persistidos com filtros JSON e rastreamento de uso
- **i18n completo** — interface em 4 idiomas (en, pt-BR, pt-PT, es)
- **Auditoria total** — cada geração de código, execução de playground e subscrição é registada

---

## 2. Arquitetura

O Developer Portal pertence ao bounded context **Catalog**, que consolida três subdomínios: **EngineeringGraph**, **Contracts** e **DeveloperPortal**. Cada subdomínio mantém isolamento interno via namespaces distintos, mas partilha o mesmo contexto de deployment.

```
src/modules/catalog/
├── NexTraceOne.Catalog.Domain/Portal/          ← Entidades, enums, erros, contratos
├── NexTraceOne.Catalog.Application/Portal/     ← 16 features CQRS (VSA)
└── NexTraceOne.Catalog.Infrastructure/Portal/  ← Repositórios, DbContext, endpoints, migrações
```

### Diagrama de camadas

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Endpoints                            │
│           (Minimal API via DeveloperPortalEndpointModule)        │
├─────────────────────────────────────────────────────────────────┤
│                     Application Layer                            │
│          Commands / Queries / Validators / Handlers              │
│                   (MediatR + FluentValidation)                   │
├─────────────────────────────────────────────────────────────────┤
│                       Domain Layer                               │
│      Entities / Enums / Errors / SharedContracts                 │
│            (Aggregate Roots + Result Pattern)                    │
├─────────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                           │
│       DeveloperPortalDbContext / Repositories / EF Core          │
│             (PostgreSQL + Row-Level Security)                    │
└─────────────────────────────────────────────────────────────────┘
```

### Dependências do módulo

```
DeveloperPortal.Domain    → BuildingBlocks.Domain (AggregateRoot, Result, TypedIdBase)
DeveloperPortal.Application → DeveloperPortal.Domain
                            → BuildingBlocks.Application (MediatR, FluentValidation, IUnitOfWork)
DeveloperPortal.Infrastructure → DeveloperPortal.Domain
                               → DeveloperPortal.Application
                               → BuildingBlocks.Infrastructure (NexTraceDbContextBase, RLS)
```

### Comunicação cross-module

- **Nunca** acessa o `DbContext` de EngineeringGraph ou Contracts diretamente.
- Consultas cross-module são feitas via `ServiceInterfaces` (`IEngineeringGraphModule`, `IContractsModule`).
- Módulos externos consomem dados do portal via `IDeveloperPortalModule`.
- Mutations são exclusivamente via MediatR handlers — nenhuma operação de escrita é exposta nas `ServiceInterfaces`.

---

## 3. Entidades de Domínio

O módulo possui **5 entidades**, 4 delas Aggregate Roots, todas com Strongly Typed IDs.

### 3.1 Subscription (Aggregate Root)

Representa a subscrição de um desenvolvedor a uma API do catálogo para receber notificações de alterações.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `SubscriptionId` | Identificador fortemente tipado |
| `ApiAssetId` | `Guid` | Referência ao ativo de API no EngineeringGraph |
| `ApiName` | `string` (max 200) | Nome legível da API subscrita |
| `SubscriberId` | `Guid` | Referência ao utilizador no IdentityAccess |
| `SubscriberEmail` | `string` (max 320) | E-mail para notificações por e-mail |
| `ConsumerServiceName` | `string` (max 200) | Nome do serviço consumidor |
| `ConsumerServiceVersion` | `string` (max 50) | Versão do serviço no momento da subscrição |
| `Level` | `SubscriptionLevel` | Nível de notificação desejado |
| `Channel` | `NotificationChannel` | Canal de entrega (Email ou Webhook) |
| `WebhookUrl` | `string?` (max 2000) | URL do webhook — obrigatório quando canal = Webhook |
| `IsActive` | `bool` | Indica se a subscrição está ativa |
| `CreatedAt` | `DateTimeOffset` | Data/hora UTC de criação |
| `LastNotifiedAt` | `DateTimeOffset?` | Última notificação enviada com sucesso |

**Factory Method:** `Create(...)` → `Result<Subscription>` — valida que canal Webhook tem URL válida.

**Invariantes:**
- Combinação `(ApiAssetId, SubscriberId)` é única — impedida por índice no banco.
- Canal `Webhook` exige `WebhookUrl` não vazia.
- Desativar uma subscrição já inativa retorna `DeveloperPortalErrors.SubscriptionAlreadyInactive`.
- Reativar uma subscrição já ativa retorna `DeveloperPortalErrors.SubscriptionAlreadyActive`.

**Operações de domínio:**
- `Deactivate()` → `Result<Unit>` — desativa a subscrição.
- `Reactivate()` → `Result<Unit>` — reativa a subscrição.
- `UpdatePreferences(level, channel, webhookUrl)` → `Result<Unit>` — atualiza preferências.
- `MarkNotified(timestamp)` → `void` — regista a última notificação enviada.

---

### 3.2 PlaygroundSession (Aggregate Root)

Registo imutável de uma execução de chamada HTTP no playground sandbox.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `PlaygroundSessionId` | Identificador fortemente tipado |
| `ApiAssetId` | `Guid` | Referência à API testada |
| `ApiName` | `string` (max 200) | Nome legível da API |
| `UserId` | `Guid` | Referência ao utilizador executor |
| `HttpMethod` | `string` (max 10) | Método HTTP (GET, POST, PUT, PATCH, DELETE) |
| `RequestPath` | `string` (max 2000) | Caminho completo da requisição |
| `RequestBody` | `string?` (max 50KB) | Corpo da requisição em JSON |
| `RequestHeaders` | `string?` | Headers serializados em JSON |
| `ResponseStatusCode` | `int` | Código de resposta HTTP |
| `ResponseBody` | `string?` | Corpo da resposta |
| `DurationMs` | `long` | Tempo de execução em milissegundos |
| `Environment` | `string` | Sempre `"sandbox"` — nunca produção |
| `ExecutedAt` | `DateTimeOffset` | Timestamp UTC da execução |

**Factory Method:** `Create(...)` → `PlaygroundSession` com validação de campos obrigatórios.

**Decisão de design:** A entidade é imutável após criação — sessões de playground são registos de auditoria, não entidades mutáveis. No MVP1, a execução retorna mock responses (200 OK); a integração real com gateways está preparada como seam futura.

---

### 3.3 CodeGenerationRecord (Aggregate Root)

Trilha de auditoria completa de cada geração de código a partir de contratos OpenAPI.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `CodeGenerationRecordId` | Identificador fortemente tipado |
| `ApiAssetId` | `Guid` | Referência à API de origem |
| `ApiName` | `string` (max 200) | Nome legível da API |
| `ContractVersion` | `string` (max 50) | Versão semântica do contrato |
| `RequestedById` | `Guid` | Referência ao utilizador solicitante |
| `Language` | `string` (max 50) | Linguagem alvo (CSharp, Java, Python, TypeScript, Go) |
| `GenerationType` | `string` | Tipo de artefacto gerado |
| `GeneratedCode` | `string` | Código-fonte completo gerado |
| `IsAiGenerated` | `bool` | Indica se IA assistiu na geração |
| `TemplateId` | `string?` | Template utilizado (null se gerado por IA) |
| `GeneratedAt` | `DateTimeOffset` | Timestamp UTC da geração |

**Factory Method:** `Create(...)` → `CodeGenerationRecord` com validação completa.

**Linguagens suportadas:** `CSharp`, `Java`, `Python`, `TypeScript`, `Go`

**Tipos de geração:**
| Tipo | Descrição |
|---|---|
| `SdkClient` | Cliente SDK completo para consumir a API |
| `IntegrationExample` | Exemplo de integração pronto a usar |
| `ContractTest` | Testes de contrato gerados automaticamente |
| `DataModels` | Modelos de dados (DTOs, records) derivados do contrato |

---

### 3.4 PortalAnalyticsEvent (Entity)

Evento de atividade do utilizador no portal — base para métricas de adoção e descoberta de lacunas no catálogo.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `PortalAnalyticsEventId` | Identificador fortemente tipado |
| `UserId` | `Guid?` | Referência ao utilizador (null para eventos anónimos) |
| `EventType` | `string` | Tipo do evento (ver enum `PortalEventType`) |
| `EntityId` | `string?` | ID da entidade relacionada |
| `EntityType` | `string?` | Tipo da entidade (ex: `"ApiAsset"`, `"Contract"`) |
| `SearchQuery` | `string?` | Query de pesquisa (apenas eventos de tipo Search) |
| `ZeroResults` | `bool?` | Indica pesquisa sem resultados — sinaliza lacuna no catálogo |
| `DurationMs` | `long?` | Duração da ação em milissegundos |
| `Metadata` | `string?` | Dados contextuais em JSON (filtros, user-agent, etc.) |
| `OccurredAt` | `DateTimeOffset` | Timestamp UTC do evento |

**Factory Method:** `Create(...)` → `PortalAnalyticsEvent` — apenas `EventType` é obrigatório.

**Decisão de design:** `ZeroResults = true` permite identificar termos de pesquisa sem correspondência, revelando APIs que os desenvolvedores procuram mas que ainda não existem no catálogo.

---

### 3.5 SavedSearch (Aggregate Root)

Critérios de pesquisa persistidos pelo utilizador para reutilização frequente.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `SavedSearchId` | Identificador fortemente tipado |
| `UserId` | `Guid` | Referência ao utilizador |
| `Name` | `string` | Nome dado pelo utilizador à pesquisa |
| `SearchQuery` | `string` | Query de pesquisa full-text |
| `Filters` | `string?` | Filtros serializados em JSON (tags, domínios, status, etc.) |
| `CreatedAt` | `DateTimeOffset` | Timestamp UTC de criação |
| `LastUsedAt` | `DateTimeOffset` | Última utilização — usado para ranking de relevância |

**Factory Method:** `Create(...)` → `SavedSearch` com `LastUsedAt = CreatedAt`.

**Operações:**
- `MarkUsed(timestamp)` → `void` — atualiza timestamp de última utilização.
- `UpdateQuery(name, searchQuery, filters)` → `Result<Unit>` — atualiza critérios com validação.

---

## 4. Enums

### SubscriptionLevel

Nível de detalhe das notificações recebidas pelo subscritor.

| Valor | Código | Descrição |
|---|---|---|
| `BreakingChangesOnly` | `0` | Apenas mudanças breaking (MAJOR version) |
| `AllChanges` | `1` | Todas as mudanças (breaking, aditivas, não-breaking) |
| `DeprecationNotices` | `2` | Avisos de depreciação de endpoints ou campos |
| `SecurityAdvisories` | `3` | Alertas de segurança e vulnerabilidades |

### NotificationChannel

Canal de entrega das notificações.

| Valor | Código | Descrição |
|---|---|---|
| `Email` | `0` | Notificação por e-mail |
| `Webhook` | `1` | Notificação por webhook HTTP |

### GenerationType

Tipo de artefacto gerado a partir de um contrato OpenAPI.

| Valor | Código | Descrição |
|---|---|---|
| `SdkClient` | `0` | Cliente SDK completo |
| `IntegrationExample` | `1` | Exemplo de integração |
| `ContractTest` | `2` | Testes de contrato automáticos |
| `DataModels` | `3` | Modelos de dados (DTOs, records) |

### PortalEventType

Tipo de evento de analytics registado pelo portal.

| Valor | Código | Descrição |
|---|---|---|
| `Search` | `0` | Pesquisa executada no catálogo |
| `ApiView` | `1` | Visualização de detalhes de uma API |
| `PlaygroundExecution` | `2` | Execução no playground |
| `CodeGeneration` | `3` | Geração de código |
| `SubscriptionCreated` | `4` | Criação de subscrição |
| `DocumentViewed` | `5` | Visualização de documentação |
| `OnboardingStarted` | `6` | Início do onboarding |
| `OnboardingCompleted` | `7` | Conclusão do onboarding |

---

## 5. Erros de Domínio

Todos os erros seguem o padrão `DeveloperPortal.{Entidade}.{Descrição}` com códigos i18n estáveis para rastreabilidade.

| Código i18n | Tipo | Descrição |
|---|---|---|
| `DeveloperPortal.Subscription.NotFound` | `NotFound` | Subscrição não encontrada |
| `DeveloperPortal.Subscription.AlreadyExists` | `Conflict` | Subscrição duplicada para API + subscritor |
| `DeveloperPortal.Subscription.AlreadyActive` | `Conflict` | Subscrição já está ativa |
| `DeveloperPortal.Subscription.AlreadyInactive` | `Conflict` | Subscrição já está inativa |
| `DeveloperPortal.Subscription.InvalidWebhookUrl` | `Validation` | URL de webhook inválida ou ausente |
| `DeveloperPortal.PlaygroundSession.NotFound` | `NotFound` | Sessão de playground não encontrada |
| `DeveloperPortal.PlaygroundSession.DisabledForApi` | `Business` | Playground desativado para esta API |
| `DeveloperPortal.CodeGeneration.NotAllowed` | `Forbidden` | Geração de código não permitida |
| `DeveloperPortal.CodeGeneration.InvalidContract` | `Validation` | Contrato inválido para geração |
| `DeveloperPortal.SavedSearch.NotFound` | `NotFound` | Pesquisa salva não encontrada |
| `DeveloperPortal.Api.NotFound` | `NotFound` | API não encontrada no catálogo |

---

## 6. Features / Use Cases

O módulo possui **16 features** organizadas em 5 grupos funcionais, todas seguindo o padrão Vertical Slice Architecture (VSA): cada feature contém Command/Query + Validator + Handler + Response num único ficheiro.

### 6.1 Catálogo (8 queries)

#### SearchCatalog

Pesquisa full-text no catálogo com filtros facetados.

- **Tipo:** `Query`
- **Input:** `SearchTerm`, `TypeFilter?`, `StatusFilter?`, `OwnerFilter?`, `Page`, `PageSize`
- **Output:** Lista de `SearchResultItem` com `RelevanceScore`, `MatchReason`, facetas (`TypeCounts`, `StatusCounts`) e paginação
- **Nota MVP1:** Retorna resultados filtrados por nome — full-text search com PostgreSQL FTS está preparado como seam futura.

#### GetMyApis

Lista as APIs que o utilizador é dono/responsável.

- **Tipo:** `Query`
- **Input:** `OwnerId`, `Page`, `PageSize`
- **Output:** Lista de `OwnedApiDto` com contagem total

#### GetApisIConsume

Lista as APIs que o utilizador consome como dependência.

- **Tipo:** `Query`
- **Input:** `UserId`, `Page`, `PageSize`
- **Output:** Lista de `ConsumedApiDto` com métricas de consumo

#### GetApiDetail

Detalhe completo de uma API incluindo trust signals.

- **Tipo:** `Query`
- **Input:** `ApiAssetId`
- **Output:** `ApiDetail` com metadados, versões, trust signals (estabilidade, cobertura de testes, etc.)

#### GetApiHealth

Indicadores de saúde de uma API específica.

- **Tipo:** `Query`
- **Input:** `ApiAssetId`
- **Output:** Health indicators (uptime, latência, taxa de erros)

#### GetAssetTimeline

Timeline cronológica de eventos relevantes de uma API.

- **Tipo:** `Query`
- **Input:** `ApiAssetId`, `Page`, `PageSize`
- **Output:** Lista paginada de `TimelineEventDto` (versões, deploys, incidentes, etc.)

#### GetApiConsumers

Lista de serviços que consomem uma API.

- **Tipo:** `Query`
- **Input:** `ApiAssetId`
- **Output:** Lista de `ConsumerDto` com contagem total

#### RenderOpenApiContract

Renderização do contrato OpenAPI de uma API com trust signals.

- **Tipo:** `Query`
- **Input:** `ApiAssetId`, `Version?`
- **Output:** Contrato OpenAPI renderizado + trust signals da versão

---

### 6.2 Subscrições (3 features)

#### CreateSubscription

Cria nova subscrição para notificações de alterações numa API.

- **Tipo:** `Command`
- **Input:** `ApiAssetId`, `ApiName`, `SubscriberId`, `SubscriberEmail`, `ConsumerServiceName`, `ConsumerServiceVersion`, `Level`, `Channel`, `WebhookUrl?`
- **Output:** `SubscriptionId`, detalhes da subscrição criada
- **Validações:** E-mail válido, URL de webhook obrigatória para canal Webhook, unicidade por (API + Subscritor)
- **Invariante:** Rejeita duplicados via `ISubscriptionRepository.GetByApiAndSubscriberAsync()`

#### DeleteSubscription

Remove uma subscrição existente.

- **Tipo:** `Command`
- **Input:** `SubscriptionId`, `RequesterId`
- **Output:** Sem payload (fire-and-forget)
- **Validação:** O `RequesterId` deve corresponder ao `SubscriberId` da subscrição

#### GetSubscriptions

Lista todas as subscrições de um utilizador.

- **Tipo:** `Query`
- **Input:** `SubscriberId`
- **Output:** Lista de `SubscriptionDto` com detalhes de cada subscrição

---

### 6.3 Playground (2 features)

#### ExecutePlayground

Executa uma chamada HTTP sandbox contra uma API.

- **Tipo:** `Command`
- **Input:** `ApiAssetId`, `ApiName`, `UserId`, `HttpMethod`, `RequestPath`, `RequestBody?`, `RequestHeaders?`
- **Output:** `SessionId`, `ResponseStatusCode`, `ResponseBody`, `DurationMs`, `ExecutedAt`
- **Nota MVP1:** Retorna mock response (200 OK com payload estático). Integração real com gateways é seam futura.
- **Segurança:** O ambiente é sempre `"sandbox"` — nunca permite execução contra produção.

#### GetPlaygroundHistory

Histórico de execuções do playground por utilizador.

- **Tipo:** `Query`
- **Input:** `UserId`, `Page`, `PageSize`
- **Output:** Lista paginada de `PlaygroundSessionDto` ordenada por `ExecutedAt DESC`

---

### 6.4 Geração de Código (1 feature)

#### GenerateCode

Gera código-fonte a partir de um contrato OpenAPI.

- **Tipo:** `Command`
- **Input:** `ApiAssetId`, `ApiName`, `ContractVersion`, `Language`, `GenerationType`
- **Output:** Código gerado, metadados (linguagem, tipo, template, flag IA)
- **Linguagens:** `CSharp`, `Java`, `Python`, `TypeScript`, `Go`
- **Tipos:** `SdkClient`, `IntegrationExample`, `ContractTest`, `DataModels`
- **Nota MVP1:** Gera templates estáticos por combinação (linguagem × tipo). Integração com IA generativa é seam futura.
- **Auditoria:** Cada geração cria um `CodeGenerationRecord` completo com o código gerado, linguagem, tipo e flag `IsAiGenerated`.

**Templates gerados (exemplos):**

| Linguagem | SdkClient | IntegrationExample |
|---|---|---|
| CSharp | HttpClient-based | Console app com chamada HTTP |
| TypeScript | Axios-based | Fetch com async/await |
| Python | Requests library | Script com tratamento de erros |
| Java | HttpClient (JDK 11+) | Main class com request/response |
| Go | net/http package | Função com marshal/unmarshal |

---

### 6.5 Analytics (2 features)

#### RecordAnalyticsEvent

Regista um evento de analytics no portal.

- **Tipo:** `Command` (fire-and-forget — sem response)
- **Input:** `UserId?`, `EventType`, `EntityId?`, `EntityType?`, `SearchQuery?`, `ZeroResults?`, `DurationMs?`, `Metadata?`
- **Nota:** Apenas `EventType` é obrigatório. Eventos anónimos (sem `UserId`) são suportados.

#### GetPortalAnalytics

Obtém métricas agregadas de utilização do portal.

- **Tipo:** `Query`
- **Input:** `DaysBack` (padrão: 30)
- **Output:** Contagens por tipo de evento + lista de top pesquisas (`Term`, `Count`)

**Métricas disponíveis:**

| Métrica | Descrição |
|---|---|
| `SearchCount` | Total de pesquisas no período |
| `ApiViewCount` | Total de visualizações de APIs |
| `PlaygroundExecutionCount` | Total de execuções no playground |
| `CodeGenerationCount` | Total de gerações de código |
| `SubscriptionCreatedCount` | Total de subscrições criadas |
| `TopSearchTerms` | Termos mais pesquisados com contagem |

---

## 7. API Endpoints

Todos os endpoints estão registados em `DeveloperPortalEndpointModule` sob o prefixo `/api/v1/developerportal`.

### Catálogo

| Método | Rota | Feature | Descrição |
|---|---|---|---|
| `GET` | `/catalog/search` | SearchCatalog | Pesquisa no catálogo de APIs |
| `GET` | `/catalog/my-apis` | GetMyApis | APIs que o utilizador é dono |
| `GET` | `/catalog/consuming` | GetApisIConsume | APIs que o utilizador consome |
| `GET` | `/catalog/{apiAssetId}` | GetApiDetail | Detalhe completo de uma API |
| `GET` | `/catalog/{apiAssetId}/health` | GetApiHealth | Indicadores de saúde |
| `GET` | `/catalog/{apiAssetId}/timeline` | GetAssetTimeline | Timeline de eventos |
| `GET` | `/catalog/{apiAssetId}/consumers` | GetApiConsumers | Serviços consumidores |
| `GET` | `/catalog/{apiAssetId}/contract` | RenderOpenApiContract | Contrato OpenAPI renderizado |

### Subscrições

| Método | Rota | Feature | Descrição |
|---|---|---|---|
| `POST` | `/subscriptions` | CreateSubscription | Criar nova subscrição |
| `GET` | `/subscriptions` | GetSubscriptions | Listar subscrições do utilizador |
| `DELETE` | `/subscriptions/{subscriptionId}` | DeleteSubscription | Remover subscrição |

### Playground

| Método | Rota | Feature | Descrição |
|---|---|---|---|
| `POST` | `/playground/execute` | ExecutePlayground | Executar chamada sandbox |
| `GET` | `/playground/history` | GetPlaygroundHistory | Histórico de execuções |

### Geração de Código

| Método | Rota | Feature | Descrição |
|---|---|---|---|
| `POST` | `/codegen` | GenerateCode | Gerar código a partir de contrato |

### Analytics

| Método | Rota | Feature | Descrição |
|---|---|---|---|
| `POST` | `/analytics/events` | RecordAnalyticsEvent | Registar evento de analytics |
| `GET` | `/analytics` | GetPortalAnalytics | Obter métricas de analytics |

**Total: 16 endpoints** (11 GET + 3 POST + 1 DELETE + 1 POST analytics)

### Padrão de resposta

Todos os endpoints seguem o padrão:

```
Request → MediatR Send → Handler → Result<T> → ToHttpResult(localizer)
```

Respostas de erro seguem a estrutura padronizada com `code` (chave i18n), `messageKey`, `params`, `correlationId` e `details` — permitindo ao frontend resolver a mensagem via i18n.

---

## 8. Integrações Cross-Module

### 8.1 IDeveloperPortalModule (Contrato Público)

Interface pública que outros módulos utilizam para consultar dados do Developer Portal. Definida em `SharedContracts/ServiceInterfaces/`.

```csharp
public interface IDeveloperPortalModule
{
    // Verifica se existem subscritores ativos para uma API
    Task<bool> HasActiveSubscriptionsAsync(Guid apiAssetId, CancellationToken ct);

    // Contagem de subscrições ativas para indicadores de popularidade
    Task<int> GetActiveSubscriptionCountAsync(Guid apiAssetId, CancellationToken ct);

    // IDs dos subscritores para envio de notificações
    Task<IReadOnlyList<Guid>> GetSubscriberIdsAsync(Guid apiAssetId, CancellationToken ct);
}
```

**Consumidores típicos:**
- **ChangeGovernance** — determina se há consumidores registados a notificar quando uma breaking change é detetada.
- **EngineeringGraph** — exibe indicadores de popularidade de uma API no catálogo.
- **Contracts** — consulta se deve enviar alertas de depreciação ou segurança.

### 8.2 Integração com Engineering Graph

O Developer Portal consome dados do Engineering Graph para:
- Pesquisa no catálogo — APIs, serviços e suas relações
- Detalhes de API — metadados, dono, equipa, domínio
- Consumidores — grafo de dependências diretas e transitivas
- Health — overlays de saúde do Engineering Graph

A comunicação é feita via `IEngineeringGraphModule` (ServiceInterface do Engineering Graph).

### 8.3 Integração com Contracts

O Developer Portal consome dados do módulo Contracts para:
- Renderização de contratos OpenAPI
- Versões disponíveis de uma API
- Trust signals por versão (estabilidade, cobertura de testes)
- Geração de código a partir de contratos específicos

A comunicação é feita via `IContractsModule` (ServiceInterface do Contracts).

### Diagrama de integrações

```
┌──────────────────────┐
│   ChangeGovernance    │─── IDeveloperPortalModule ──┐
│ (notificar breaking)  │                              │
└──────────────────────┘                              │
                                                       ▼
┌──────────────────────┐         ┌──────────────────────────────┐
│  Engineering Graph    │◄────── │      Developer Portal         │
│ (IEngineeringGraph    │        │                              │
│  Module)              │        │  Subscription, Playground,    │
└──────────────────────┘        │  CodeGen, Analytics           │
                                 │                              │
┌──────────────────────┐        └──────────────────────────────┘
│     Contracts         │◄──────          │
│ (IContractsModule)    │                 │
└──────────────────────┘    IDeveloperPortalModule
                                          │
                                          ▼
                               ┌──────────────────────┐
                               │  Outros módulos       │
                               │ (popularidade, alertas)│
                               └──────────────────────┘
```

---

## 9. Segurança e Permissões

### Tenant scoping

- Todos os dados do portal são isolados por tenant via **Row-Level Security (RLS)** herdada do `NexTraceDbContextBase`.
- Subscrições, sessões de playground, gerações de código e pesquisas salvas são sempre filtradas pelo tenant do utilizador autenticado.
- Analytics events são registados no contexto do tenant atual.

### User-level scoping

| Operação | Scoping |
|---|---|
| Criar subscrição | Utilizador autenticado = subscritor |
| Eliminar subscrição | Apenas o próprio subscritor (`RequesterId = SubscriberId`) |
| Listar subscrições | Apenas subscrições do utilizador autenticado |
| Executar playground | Utilizador autenticado = executor |
| Histórico playground | Apenas sessões do utilizador autenticado |
| Gerar código | Utilizador autenticado = solicitante |
| Pesquisas salvas | Apenas pesquisas do utilizador autenticado |

### Playground: execução segura

- O ambiente é **sempre** `"sandbox"` — o playground nunca executa contra produção.
- No MVP1, a execução retorna mock responses; não há chamada real a APIs externas.
- Quando a integração real for implementada, a execução será mediada por um gateway controlado com rate limiting, timeouts e isolamento de rede.

### Rastreamento de analytics

- Cada evento de analytics regista `UserId` (quando autenticado) e `OccurredAt`.
- Eventos anónimos são suportados mas sem contexto de utilizador.
- `Metadata` em JSON permite capturar contexto adicional sem expor PII.
- Pesquisas com `ZeroResults = true` são sinalizadas para análise de lacunas — nenhum dado pessoal é incluído.

### Auditoria de geração de código

- Toda geração de código cria um `CodeGenerationRecord` imutável com:
  - Quem solicitou (`RequestedById`)
  - Qual API e versão do contrato
  - Qual linguagem e tipo
  - Se foi assistida por IA
  - O código completo gerado
- Esta trilha permite auditoria completa de artefactos gerados a partir de contratos.

---

## 10. Frontend

### Componente principal: `DeveloperPortalPage.tsx`

Localização: `src/frontend/src/features/catalog/pages/DeveloperPortalPage.tsx` (732 linhas)

A página é organizada em **4 tabs** seguindo o padrão visual das outras páginas do módulo Catalog (EngineeringGraphPage, ContractsPage).

### Tabs

| Tab | Ícone | Funcionalidade |
|---|---|---|
| **Catalog** | `Search` | Pesquisa de APIs com filtros e cards responsivos |
| **Subscriptions** | `Bell` | Gestão de subscrições com formulário e tabela |
| **Playground** | `Play` | Execução sandbox com formulário e histórico |
| **Analytics** | `BarChart3` | Métricas de adoção e top pesquisas |

### Tab: Catalog

- Input de pesquisa com debounce
- Grid responsivo de cards (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3`)
- Cada card exibe: nome da API, badge de saúde, descrição, dono, versão
- Estados: loading, erro, sem resultados — todos com mensagens i18n

### Tab: Subscriptions

- Formulário de criação com campos: API, e-mail, serviço consumidor, nível, canal, webhook URL
- A URL de webhook só aparece quando o canal selecionado é `Webhook`
- Tabela de subscrições ativas/inativas com botão de eliminar por linha
- Mutations invalidam o cache de queries automaticamente via React Query

### Tab: Playground

- Formulário de execução: API, método HTTP, path, body, headers, ambiente
- Card de resultado com: body da resposta (monospace), status code (badge colorido), duração, timestamp
- Tabela de histórico com colunas: API, Método, Path, Status, Duração
- Ordenação por execução mais recente

### Tab: Analytics

- Cards de estatísticas em grid responsivo (`grid-cols-2 md:grid-cols-4`):
  - Total de pesquisas, API views, execuções no playground, gerações de código
- Tabela de top pesquisas (query × contagem)
- Botão de refresh que invalida o cache
- Estado vazio com mensagem i18n

### API Client: `developerPortal.ts`

Localização: `src/frontend/src/features/catalog/api/developerPortal.ts` (139 linhas)

Todas as chamadas usam o `client` centralizado (Axios com interceptors de autenticação). Padrão `.then((r) => r.data)` para extrair o payload.

**16 métodos** organizados por grupo funcional:

```typescript
// Catálogo
searchCatalog(query, page, pageSize)
getMyApis(page, pageSize)
getConsuming(page, pageSize)
getApiDetail(apiAssetId)
getApiHealth(apiAssetId)
getApiTimeline(apiAssetId)
getApiConsumers(apiAssetId)
getApiContract(apiAssetId)

// Subscrições
createSubscription(data)
listSubscriptions()
deleteSubscription(subscriptionId)

// Playground
executePlayground(data)
getPlaygroundHistory(page, pageSize)

// Geração de Código
generateCode(data)

// Analytics
trackEvent(data)
getAnalytics(since)
```

### i18n — 4 idiomas

Localização: `src/frontend/src/locales/{en,pt-BR,pt-PT,es}.json`

Todas as strings visíveis ao utilizador usam `t('developerPortal.*')` via `react-i18next`. A estrutura de chaves cobre:

```
developerPortal.title
developerPortal.description
developerPortal.tabs.{catalog,subscriptions,playground,analytics}
developerPortal.catalog.{title,searchPlaceholder,noResults,owner,version,...}
developerPortal.subscriptions.{title,create,form.*,levels.*,channels.*,...}
developerPortal.playground.{title,execute,form.*,result.*,...}
developerPortal.analytics.{title,totalSearches,topSearches,...}
```

**Idiomas disponíveis:**

| Idioma | Ficheiro | Descrição |
|---|---|---|
| Inglês | `en.json` | Idioma base |
| Português (Brasil) | `pt-BR.json` | Tradução completa |
| Português (Portugal) | `pt-PT.json` | Tradução completa |
| Espanhol | `es.json` | Tradução completa |

### Stack frontend

| Tecnologia | Utilização |
|---|---|
| React 18 | Componentes e hooks |
| TanStack Query | Server state, cache, invalidation |
| react-i18next | Internacionalização |
| Tailwind CSS | Estilização responsiva |
| Lucide React | Ícones (BookOpen, Search, Bell, Play, BarChart3, etc.) |

---

## 11. SQL Seeds para Desenvolvimento

Localização: `database/seeds/developer-portal/`

### Ficheiros disponíveis

| Ficheiro | Registos | Descrição |
|---|---|---|
| `00-reset-developer-portal-test-data.sql` | — | Limpa todos os dados das 5 tabelas (respeita ordem de FKs) |
| `01-seed-subscriptions.sql` | 8 | Subscrições com variações de nível, canal, tenant e estado |
| `02-seed-playground-sessions.sql` | 10 | Sessões com todos os métodos HTTP, status codes e durações |
| `03-seed-code-generation.sql` | 6 | Gerações em 5 linguagens, templates e IA |
| `04-seed-analytics-events.sql` | 15 | Todos os 8 tipos de evento cobertos |
| `05-seed-saved-searches.sql` | 4 | Pesquisas salvas com filtros JSON variados |

**Total: 43 registos de teste**

### Dependências cross-módulo

Os seeds referenciam IDs de outros módulos:
- **IdentityAccess** — utilizadores e tenants (UUIDs determinísticos para dev/test)
- **Engineering Graph** — APIs e serviços (UUIDs determinísticos)

### Convenção de IDs

| Prefixo | Entidade |
|---|---|
| `d1xxxxxx-...` | Subscriptions |
| `d2xxxxxx-...` | Playground Sessions |
| `d3xxxxxx-...` | Code Generation Records |
| `d4xxxxxx-...` | Portal Analytics Events |
| `d5xxxxxx-...` | Saved Searches |

### Execução

```bash
# Limpar dados existentes
psql -f database/seeds/developer-portal/00-reset-developer-portal-test-data.sql

# Seed completo (ordem obrigatória)
psql -f database/seeds/developer-portal/01-seed-subscriptions.sql
psql -f database/seeds/developer-portal/02-seed-playground-sessions.sql
psql -f database/seeds/developer-portal/03-seed-code-generation.sql
psql -f database/seeds/developer-portal/04-seed-analytics-events.sql
psql -f database/seeds/developer-portal/05-seed-saved-searches.sql
```

> ⚠️ **Segurança:** Os seeds utilizam IDs determinísticos e dados fictícios — **apenas para dev/test**, nunca para produção.

---

## 12. Testes

### Estrutura de testes

```
tests/modules/catalog/
└── NexTraceOne.Catalog.Tests/
    └── Portal/
        └── DeveloperPortalApplicationTests.cs
```

### Cobertura

Os testes unitários cobrem os handlers de Application layer com mocks dos repositórios:

| Feature | Cenários testados |
|---|---|
| CreateSubscription | Criação válida, subscrição duplicada, webhook sem URL |
| DeleteSubscription | Remoção válida, subscrição não encontrada, requesterId inválido |
| GetSubscriptions | Lista com resultados, lista vazia |
| ExecutePlayground | Execução válida com mock response |
| GetPlaygroundHistory | Histórico paginado |
| GenerateCode | Geração em cada linguagem, tipo inválido |
| RecordAnalyticsEvent | Registo de evento válido |
| GetPortalAnalytics | Métricas agregadas no período |
| SearchCatalog | Pesquisa com resultados, pesquisa vazia |

### Execução

```bash
dotnet test tests/modules/catalog/NexTraceOne.Catalog.Tests/ --filter "Portal"
```

---

## 13. Roadmap / Preparação para Evolução

O Developer Portal foi desenhado com seams preparadas para evolução futura sem breaking changes nos contratos públicos.

### 🟡 AI-Assisted Contract Design (Preparado)

- O `CodeGenerationRecord` já possui a flag `IsAiGenerated` e campo `TemplateId`.
- O handler `GenerateCode` está preparado para delegar a um `ICodeGenerationStrategy` injetável.
- **Seam futura:** Integrar com o bounded context `AIKnowledge` para geração assistida por IA generativa.
- **Timeline estimada:** Fase 5 (Semanas 17–20).

### 🟡 Event/Kafka Workspace (Preparado)

- O modelo de pesquisa (`SearchCatalog`) suporta `TypeFilter` — permite filtrar por tipo de ativo (API REST, Event, gRPC, etc.).
- O Engineering Graph já modela eventos como nós do grafo.
- **Seam futura:** Adicionar tab dedicada para visualização e subscrição de event streams (Kafka, RabbitMQ, etc.).
- **Timeline estimada:** Pós-MVP1.

### 🟡 SOAP/WSDL Support (Preparado)

- O `GenerationType` e `Language` são strings, não enums fixos no handler — permitem extensão.
- A feature `RenderOpenApiContract` pode ser complementada com `RenderWsdlContract`.
- **Seam futura:** Adicionar parser WSDL no módulo Contracts e templates SOAP no Developer Portal.
- **Timeline estimada:** Pós-MVP1.

### 🟡 Full-Text Search com PostgreSQL FTS (Preparado)

- O `SearchCatalog` já retorna facetas (`TypeCounts`, `StatusCounts`) e `RelevanceScore`.
- A estrutura de resposta está preparada para ranking por relevância.
- **Seam futura:** Implementar `tsvector`/`tsquery` no PostgreSQL para pesquisa full-text real.
- **Timeline estimada:** Fase 6 (Semanas 21–24).

### 🟡 Playground com Execução Real (Preparado)

- O `PlaygroundSession` já persiste todos os campos necessários para execuções reais.
- O ambiente é forçado a `"sandbox"` — o contrato não precisa mudar.
- **Seam futura:** Integrar com API gateway sandbox (com rate limiting, timeouts e isolamento de rede).
- **Timeline estimada:** Pós-MVP1.

### 🟡 Workflow Integration (Preparado)

- As subscrições (`IDeveloperPortalModule.GetSubscriberIdsAsync`) permitem ao `ChangeGovernance` notificar consumidores automaticamente quando uma breaking change passa pelo workflow de aprovação.
- **Seam futura:** Notificações automáticas no workflow de aprovação de mudanças.
- **Timeline estimada:** Fase 4 (Semanas 13–16).

### 🟡 Migration Timeline (Preparado)

- O catálogo suporta `StatusFilter` que pode incluir estados como `Deprecated`, `Decommissioned`, `Migrating`.
- **Seam futura:** Timelines de migração com planeamento de sunset e guias de migração por API.
- **Timeline estimada:** Pós-MVP1.

---

## Referências

- **Engineering Graph:** `docs/engineering-graph/README.md`
- **Contracts:** `docs/contracts/README.md`
- **Licensing:** `docs/licensing/README.md`
- **Identity:** `docs/identity/README.md`
- **Arquitetura geral:** `docs/ARCHITECTURE.md`
- **Convenções:** `docs/CONVENTIONS.md`
- **Roadmap:** `docs/ROADMAP.md`

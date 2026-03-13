# Developer Portal — API de Integração Externa

## Visão Geral

O módulo Developer Portal do NexTraceOne expõe uma API REST para acesso ao
catálogo de APIs, subscrições, playground interactivo, geração de código e
analytics do portal. Permite integração com developer tools, CI/CD pipelines
e dashboards externos.

Todas as APIs seguem o padrão REST sob `/api/v1/developerportal/` e utilizam
JWT Bearer ou API Key para autenticação.

## Autenticação e Autorização

### Mecanismos Suportados

| Mecanismo | Header | Descrição |
|-----------|--------|-----------|
| JWT Bearer | `Authorization: Bearer <token>` | Autenticação via token JWT |
| API Key | `X-Api-Key: <key>` | Autenticação sistema-a-sistema |

### Tenant Scoping

Todas as operações são automaticamente escopadas ao tenant do token.

## Endpoints

### Catálogo de APIs (`/catalog`)

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/catalog/search?searchTerm=&typeFilter=&statusFilter=&ownerFilter=&page=1&pageSize=20` | Pesquisar APIs no catálogo |
| GET | `/catalog/my-apis?ownerId=UUID&page=1&pageSize=20` | Listar APIs que o utilizador é owner |
| GET | `/catalog/consuming?userId=UUID&page=1&pageSize=20` | Listar APIs que o utilizador consome |
| GET | `/catalog/{apiAssetId}` | Obter detalhe de uma API |
| GET | `/catalog/{apiAssetId}/health` | Obter estado de health da API |
| GET | `/catalog/{apiAssetId}/consumers` | Listar consumidores da API |
| GET | `/catalog/{apiAssetId}/contract?version=latest` | Renderizar contrato OpenAPI |
| GET | `/catalog/{apiAssetId}/timeline?page=1&pageSize=20` | Timeline de alterações da API |

### Subscrições (`/subscriptions`)

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/subscriptions` | Criar subscrição a uma API |
| GET | `/subscriptions?subscriberId=UUID` | Listar subscrições do utilizador |
| DELETE | `/subscriptions/{subscriptionId}?requesterId=UUID` | Remover subscrição |

### Playground (`/playground`)

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/playground/execute` | Executar chamada de API no playground |
| GET | `/playground/history?userId=UUID&page=1&pageSize=20` | Histórico de execuções |

### Code Generation (`/codegen`)

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/codegen` | Gerar código de cliente a partir de contrato OpenAPI |

### Analytics (`/analytics`)

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/analytics/events` | Registar evento de analytics |
| GET | `/analytics?daysBack=30` | Obter métricas do portal |

## Detalhes dos Endpoints

### GET `/catalog/search`

Pesquisa no catálogo de APIs com filtros.

**Query Parameters:**
| Parâmetro | Tipo | Obrigatório | Descrição |
|-----------|------|-------------|-----------|
| `searchTerm` | string | Não | Texto de pesquisa livre |
| `typeFilter` | string | Não | Filtro por tipo de API (REST, gRPC, GraphQL) |
| `statusFilter` | string | Não | Filtro por estado (Active, Deprecated, Decommissioned) |
| `ownerFilter` | string | Não | Filtro por owner da API |
| `page` | int | Não | Página (default: 1) |
| `pageSize` | int | Não | Tamanho da página (default: 20, máx: 100) |

**Response (200 OK):**
```json
{
  "items": [
    {
      "apiAssetId": "UUID",
      "name": "Payment API",
      "description": "API de pagamentos",
      "apiType": "REST",
      "status": "Active",
      "ownerTeam": "payments-team",
      "version": "2.1.0",
      "consumerCount": 5,
      "healthStatus": "Healthy",
      "lastUpdated": "2026-03-13T10:00:00Z"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "hasNextPage": true
}
```

### GET `/catalog/{apiAssetId}`

Retorna detalhe completo de uma API.

**Response (200 OK):**
```json
{
  "apiAssetId": "UUID",
  "name": "Payment API",
  "description": "API de processamento de pagamentos",
  "apiType": "REST",
  "status": "Active",
  "ownerTeam": "payments-team",
  "version": "2.1.0",
  "consumerCount": 5,
  "healthStatus": "Healthy",
  "trustScore": 0.92,
  "freshnessScore": 0.85,
  "tags": ["payments", "core"],
  "metadata": { "repository": "https://github.com/org/payment-api" },
  "createdAt": "2025-01-15T08:00:00Z",
  "lastUpdated": "2026-03-13T10:00:00Z"
}
```

### POST `/subscriptions`

Cria subscrição para receber notificações sobre uma API.

**Request:**
```json
{
  "apiAssetId": "UUID",
  "subscriberId": "UUID",
  "level": "Breaking",
  "notificationChannel": "Email"
}
```

**Response (200 OK):**
```json
{
  "subscriptionId": "UUID",
  "apiAssetId": "UUID",
  "subscriberId": "UUID",
  "level": "Breaking",
  "notificationChannel": "Email",
  "createdAt": "2026-03-13T10:00:00Z"
}
```

### POST `/playground/execute`

Executa uma chamada de API no playground sandbox.

**Request:**
```json
{
  "apiAssetId": "UUID",
  "userId": "UUID",
  "method": "GET",
  "path": "/v1/payments",
  "headers": { "Accept": "application/json" },
  "body": null
}
```

**Response (200 OK):**
```json
{
  "sessionId": "UUID",
  "statusCode": 200,
  "responseHeaders": { "Content-Type": "application/json" },
  "responseBody": "{\"payments\": [...]}",
  "executionTimeMs": 145,
  "executedAt": "2026-03-13T10:00:00Z"
}
```

### POST `/codegen`

Gera código de cliente para a API.

**Request:**
```json
{
  "apiAssetId": "UUID",
  "language": "TypeScript",
  "requestedBy": "UUID"
}
```

**Linguagens suportadas:** C#, TypeScript, Python, Java, Go

**Response (200 OK):**
```json
{
  "code": "// Generated TypeScript client...\nexport class PaymentApiClient { ... }",
  "language": "TypeScript",
  "generatedAt": "2026-03-13T10:00:00Z",
  "apiVersion": "2.1.0"
}
```

## Contratos Públicos

### Interface Cross-Module

```csharp
public interface IDeveloperPortalModule
{
    Task<bool> HasActiveSubscriptionsAsync(Guid apiAssetId, CancellationToken ct);
    Task<int> GetActiveSubscriptionCountAsync(Guid apiAssetId, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetSubscriberIdsAsync(Guid apiAssetId, CancellationToken ct);
}
```

Esta interface é usada por outros módulos (ex: ChangeIntelligence) para verificar
se uma API tem subscritores antes de notificar sobre breaking changes.

### Níveis de Subscrição

| Nível | Descrição |
|-------|-----------|
| `All` | Todas as alterações (major, minor, patch) |
| `Breaking` | Apenas breaking changes |
| `Major` | Alterações major e breaking |
| `Security` | Apenas alertas de segurança |

### Canais de Notificação

| Canal | Descrição |
|-------|-----------|
| `Email` | Notificação via email |
| `Webhook` | Notificação via webhook HTTP |
| `InApp` | Notificação in-app |
| `Slack` | Integração Slack (futuro) |

## Códigos de Erro (i18n)

| Código | Descrição |
|--------|-----------|
| `DeveloperPortal.Subscription.NotFound` | Subscrição não encontrada |
| `DeveloperPortal.Subscription.Duplicate` | Subscrição duplicada |
| `DeveloperPortal.Subscription.InvalidLevel` | Nível de subscrição inválido |
| `DeveloperPortal.Playground.NotFound` | Sessão não encontrada |
| `DeveloperPortal.Playground.ExecutionFailed` | Execução no playground falhou |
| `DeveloperPortal.CodeGeneration.UnsupportedLanguage` | Linguagem não suportada |
| `DeveloperPortal.CodeGeneration.NotFound` | Registo de geração não encontrado |
| `DeveloperPortal.Analytics.NotFound` | Evento de analytics não encontrado |
| `DeveloperPortal.SavedSearch.NotFound` | Pesquisa guardada não encontrada |
| `DeveloperPortal.Catalog.ApiNotFound` | API não encontrada no catálogo |
| `DeveloperPortal.Catalog.ApiDecommissioned` | API foi descomissionada |

## Códigos HTTP

| Código | Descrição |
|--------|-----------|
| 200 | Operação bem sucedida |
| 400 | Payload inválido (validação FluentValidation) |
| 401 | Não autenticado |
| 403 | Sem permissão |
| 404 | Recurso não encontrado |
| 409 | Conflito (ex: subscrição duplicada) |
| 429 | Rate limit excedido (100 req/min por IP) |

## Exemplos de Integração

### Pesquisar APIs e Subscrever (curl)

```bash
# 1. Pesquisar APIs
curl -s "http://localhost:5000/api/v1/developerportal/catalog/search?searchTerm=payment&page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN" | jq .

# 2. Obter detalhe de uma API
curl -s "http://localhost:5000/api/v1/developerportal/catalog/API_UUID" \
  -H "Authorization: Bearer $TOKEN" | jq .

# 3. Criar subscrição
curl -X POST "http://localhost:5000/api/v1/developerportal/subscriptions" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"apiAssetId": "API_UUID", "subscriberId": "USER_UUID", "level": "Breaking", "notificationChannel": "Email"}'

# 4. Gerar código TypeScript
curl -X POST "http://localhost:5000/api/v1/developerportal/codegen" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"apiAssetId": "API_UUID", "language": "TypeScript", "requestedBy": "USER_UUID"}'
```

### Integração CI/CD (Python)

```python
import requests

BASE_URL = "http://localhost:5000/api/v1/developerportal"
HEADERS = {
    "Content-Type": "application/json",
    "X-Api-Key": "<ci-system-api-key>",
}

# Verificar se API tem subscritores (antes de deploy com breaking change)
response = requests.get(
    f"{BASE_URL}/catalog/{api_asset_id}/consumers",
    headers=HEADERS,
)
consumers = response.json()
if consumers:
    print(f"⚠️ {len(consumers)} consumers affected by this change")
    for consumer in consumers:
        print(f"  - {consumer['name']}")

# Obter contrato OpenAPI para geração de documentação
response = requests.get(
    f"{BASE_URL}/catalog/{api_asset_id}/contract",
    headers=HEADERS,
)
contract = response.json()
# Processar contrato OpenAPI...
```

## Dependências Cross-Module

O Developer Portal integra com outros módulos:

| Módulo | Interface | Utilização |
|--------|-----------|-----------|
| Engineering Graph | `IEngineeringGraphModule` | Dados de APIs, serviços e relações para catálogo |
| Contracts | `IContractsModule` | Specs OpenAPI para renderização e geração de código |
| Change Intelligence | Consumo de Integration Events | Timeline de alterações por API |

## Auditabilidade

- Subscrições criadas/removidas registadas pelo AuditInterceptor
- Execuções de playground registadas com dados do request/response
- Gerações de código rastreáveis por utilizador e API
- Analytics do portal disponíveis via endpoint dedicado
- Tenant isolation em todas as operações

## Observabilidade

- Logs em inglês com structured logging (Serilog)
- Tracing OpenTelemetry para execuções de playground
- Métricas de utilização do portal (analytics endpoint)
- Health status por API via endpoint dedicado

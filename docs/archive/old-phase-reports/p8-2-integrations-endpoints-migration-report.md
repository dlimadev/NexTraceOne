# P8.2 — Integrations Endpoints Migration Report

## Objetivo

Completar a separação do domínio de integrações na camada HTTP, garantindo que:
- os endpoints de integrações são servidos pelo módulo Integrations dedicado;
- o frontend não depende estruturalmente de `/api/governance/integrations`;
- os testes do domínio de integrações residem no projecto correcto;
- Governance deixa de ter qualquer ownership residual sobre o domínio de integrações.

---

## Estado anterior (antes de P8.2)

A execução do P8.1 já havia concretizado a maior parte da separação estrutural:

| Componente | Estado pós-P8.1 |
|---|---|
| `Integrations.API` (endpoints) | ✅ Criado — `IntegrationHubEndpointModule` com 8 endpoints |
| `Integrations.Application` (handlers) | ✅ Completo — 8 CQRS handlers migrados |
| `Integrations.Infrastructure` (DbContext) | ✅ Existente — 3 entidades, 3 repositórios |
| `AddIntegrationsModule()` em Program.cs | ✅ Registado |
| Frontend API client | ✅ Já consumia `/integrations/*` e `/ingestion/*` |
| Governance.API sem endpoints de integração | ✅ Limpo |
| Governance.Application sem handlers de integração | ✅ Limpo |
| **Testes de integração** | ⚠️ Ainda em `Governance.Tests` |
| **Governance.Tests.csproj** | ⚠️ Ainda referenciava `Integrations.Domain` e `Integrations.Application` |

---

## Rotas servidas pelo módulo Integrations (verificação final)

| Método | Rota | Permissão | Handler |
|---|---|---|---|
| GET | `/api/v1/integrations/connectors` | `integrations:read` | `ListIntegrationConnectors` |
| GET | `/api/v1/integrations/connectors/{id}` | `integrations:read` | `GetIntegrationConnector` |
| GET | `/api/v1/ingestion/sources` | `integrations:read` | `ListIngestionSources` |
| GET | `/api/v1/ingestion/executions` | `integrations:read` | `ListIngestionExecutions` |
| GET | `/api/v1/integrations/health` | `integrations:read` | `GetIngestionHealth` |
| GET | `/api/v1/ingestion/freshness` | `integrations:read` | `GetIngestionFreshness` |
| POST | `/api/v1/integrations/connectors/{id}/retry` | `integrations:write` | `RetryConnector` |
| POST | `/api/v1/ingestion/executions/{id}/reprocess` | `integrations:write` | `ReprocessExecution` |

Todas as rotas mantêm-se idênticas ao estado funcional anterior — sem breaking changes.

---

## Frontend — verificação de consumo

O cliente API do frontend (`src/frontend/src/features/integrations/api/integrations.ts`) consome:

```typescript
client.get('/integrations/connectors', ...)    // → GET /api/v1/integrations/connectors
client.get('/integrations/connectors/${id}')   // → GET /api/v1/integrations/connectors/{id}
client.post('/integrations/connectors/${id}/retry') // → POST /api/v1/integrations/connectors/{id}/retry
client.get('/ingestion/sources', ...)          // → GET /api/v1/ingestion/sources
client.get('/ingestion/executions', ...)       // → GET /api/v1/ingestion/executions
client.post('/ingestion/executions/${id}/reprocess') // → POST /api/v1/ingestion/executions/{id}/reprocess
client.get('/integrations/health', ...)        // → GET /api/v1/integrations/health
client.get('/ingestion/freshness', ...)        // → GET /api/v1/ingestion/freshness
```

O `apiClient` (axios) tem `baseURL: '/api/v1'`, o que resolve correctamente todas as rotas.

**Verificação**: Zero referências a `/api/governance/integrations` em todo o frontend.

---

## Ficheiros alterados/criados nesta fase

### Criados

| Ficheiro | Descrição |
|---|---|
| `tests/modules/integrations/NexTraceOne.Integrations.Tests/NexTraceOne.Integrations.Tests.csproj` | Projecto de testes dedicado |
| `tests/modules/integrations/NexTraceOne.Integrations.Tests/GlobalUsings.cs` | Usings globais para testes |
| `tests/modules/integrations/NexTraceOne.Integrations.Tests/Application/Features/IntegrationHubFeatureTests.cs` | 15 testes (movidos de Governance.Tests) |
| `tests/modules/integrations/NexTraceOne.Integrations.Tests/Application/Features/IngestionSourceDomainTests.cs` | 2 testes de domínio (movidos de Phase3GovernanceFeatureTests) |

### Alterados

| Ficheiro | Alteração |
|---|---|
| `NexTraceOne.sln` | Adicionado projecto `Integrations.Tests` |
| `Governance.Tests.csproj` | Removidas referências a `Integrations.Domain` e `Integrations.Application` |
| `Phase3GovernanceFeatureTests.cs` | Removidos 2 testes de `IngestionSource` e import de `Integrations.Domain.Entities` |

### Removidos de Governance.Tests

| Ficheiro | Motivo |
|---|---|
| `IntegrationHubFeatureTests.cs` | Movido para `Integrations.Tests` |

---

## Handlers/DTOs/permissões

Todos os 8 handlers residem em `NexTraceOne.Integrations.Application.Features/`:

- `ListIntegrationConnectors` — Query + Handler
- `GetIntegrationConnector` — Query + Handler
- `ListIngestionSources` — Query + Handler
- `ListIngestionExecutions` — Query + Handler
- `GetIngestionHealth` — Query + Handler
- `GetIngestionFreshness` — Query + Handler
- `RetryConnector` — Command + Handler + Validator
- `ReprocessExecution` — Command + Handler + Validator

**Permissões**: `integrations:read` (GET) e `integrations:write` (POST) — residem semanticamente no módulo Integrations, não em Governance.

**DTOs**: Response records são definidos inline nos handlers (padrão do projecto). Tipos TypeScript correspondentes em `src/frontend/src/types/index.ts`.

---

## Compatibilidade transitória

**Nenhuma compatibilidade transitória foi necessária.**

- As rotas HTTP são idênticas ao estado anterior
- O frontend já consumia os paths correctos (`/integrations/*`, `/ingestion/*`)
- Não existe nenhum redirect ou façade

---

## Validação funcional/compilação

| Verificação | Resultado |
|---|---|
| Build completo (`dotnet build`) | ✅ 0 erros |
| Testes Integrations (`Integrations.Tests`) | ✅ 17 passed |
| Testes Governance (`Governance.Tests`) | ✅ 146 passed (163 - 17 movidos) |
| Frontend API client correctamente configurado | ✅ Verificado |
| Zero referências a `/api/governance/integrations` | ✅ Verificado |
| Governance.Tests sem dependência de Integrations | ✅ Referências removidas |

---

## Resumo da separação completa (P8.1 + P8.2)

| Antes (P2.4) | Depois (P8.2) |
|---|---|
| Handlers em `Governance.Application` | Handlers em `Integrations.Application` |
| Endpoints em `Governance.API` | Endpoints em `Integrations.API` |
| Infra wired via `Governance.Infrastructure` | Infra wired via `Integrations.API` → `AddIntegrationsModule()` |
| Testes em `Governance.Tests` | Testes em `Integrations.Tests` |
| `Governance.Tests` referenciava `Integrations.*` | Referências removidas |
| Governance como catch-all | Governance limpo — apenas domínio Governance puro |

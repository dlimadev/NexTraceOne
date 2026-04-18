# Integrations Module — NexTraceOne

> **Bounded Context:** Integrations
> **Responsabilidade:** Gestão de conectores de integração com sistemas externos, ingestão de eventos de deploy/CI/CD, subscriptions de webhook e resolução de contexto de integração por tenant/ambiente.

---

## Propósito

O módulo Integrations é o ponto de entrada governado para **tudo o que vem de fora** do NexTraceOne. Ele abstrai a complexidade de múltiplos sistemas externos (GitLab, Jenkins, GitHub, Azure DevOps, etc.) num modelo canónico consistente, permitindo que os demais módulos — especialmente ChangeGovernance e OperationalIntelligence — consumam eventos de deploy e pipeline sem conhecer o sistema de origem.

Sem este módulo, eventos de deploy chegariam ao produto de forma ad-hoc, sem rastreabilidade, sem associação a tenant/ambiente e sem possibilidade de reprocessamento ou auditoria.

## Bounded Context

| Aspecto | Detalhe |
|---------|---------|
| **Assemblies** | `NexTraceOne.Integrations.Domain`, `.Application`, `.Infrastructure`, `.API`, `.Contracts` |
| **DbContext** | `IntegrationsDbContext` — banco `IntegrationsDatabase` |
| **Outbox table** | `integrations_outbox_messages` |
| **Base URL de API** | `/api/v1/integrations` |

---

## Entidades Principais

### IntegrationConnector

Representa um conector com um sistema externo configurado pelo tenant.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Id` | `IntegrationConnectorId` | Identificador fortemente tipado |
| `TenantId` | `Guid` | Isolamento multi-tenant |
| `Name` | `string` | Nome legível do conector |
| `ConnectorType` | `string` | Tipo: `GitLab`, `Jenkins`, `GitHub`, `AzureDevOps`, etc. |
| `Status` | `ConnectorStatus` | `Inactive`, `Active`, `Error`, `Deprecated` |
| `Health` | `ConnectorHealth` | `Unknown`, `Healthy`, `Degraded`, `Unhealthy` |
| `BaseUrl` | `string?` | URL base do sistema externo |

O conector é o "passaporte" de um sistema externo para o NexTraceOne — cada ingestão de evento referencia um conector activo.

### IngestionSource

Representa uma fonte de dados configurada dentro de um conector, com regras de mapeamento e trust level.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Id` | `IngestionSourceId` | Identificador fortemente tipado |
| `ConnectorId` | `IntegrationConnectorId` | Conector pai |
| `EnvironmentId` | `Guid` | Ambiente associado à ingestão |
| `TrustLevel` | `SourceTrustLevel` | `Trusted`, `Verified`, `Unverified` |
| `Status` | `SourceStatus` | Estado operacional da fonte |

### IngestionExecution

Registo imutável de cada execução de ingestão — o log de auditoria operacional.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Id` | `IngestionExecutionId` | Identificador fortemente tipado |
| `SourceId` | `IngestionSourceId` | Fonte que originou a execução |
| `Result` | `ExecutionResult` | `Success`, `PartialSuccess`, `Failed`, `Skipped` |
| `ProcessingStatus` | `ProcessingStatus` | Estado do processamento do payload |
| `FreshnessStatus` | `FreshnessStatus` | Se o evento foi recebido dentro da janela esperada |
| `OccurredAt` | `DateTimeOffset` | Quando o evento ocorreu na fonte |
| `ReceivedAt` | `DateTimeOffset` | Quando o NexTraceOne recebeu o evento |

### WebhookSubscription

Assinatura de webhook de saída — permite que o NexTraceOne notifique sistemas externos sobre eventos internos.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Id` | `WebhookSubscriptionId` | Identificador fortemente tipado |
| `Name` | `string` | Nome descritivo |
| `TargetUrl` | `string` | URL de destino dos eventos |
| `EventTypes` | `IReadOnlyList<string>` | Tipos de evento subscritos |
| `HasSecret` | `bool` | Se usa HMAC para verificação de payload |
| `IsActive` | `bool` | Estado operacional |

---

## Como as Integrações Funcionam

O fluxo de ingestão de um evento externo (ex.: deploy concluído no GitLab) percorre:

```
Sistema Externo (GitLab CI)
    │
    ▼ POST /api/ingestion/events (NexTraceOne.Ingestion.Api)
    │  Headers: Authorization: ApiKey <key>
    │  Body: payload de deploy (JSON genérico ou formato canónico)
    │
    ▼ IngestionController
    │  - Valida API Key e resolve conector
    │  - Cria IngestionExecution
    │
    ▼ ProcessIngestionPayloadCommand (MediatR)
    │  - Mapeia payload para modelo canónico interno
    │  - Associa a serviço, ambiente e tenante
    │  - Persiste IngestionExecution com resultado
    │
    ▼ IngestionPayloadProcessedDomainEvent
    │  → Outbox → EventBus
    │
    ▼ ChangeGovernance module
       - Cria/atualiza Change baseado no evento de deploy
       - Associa blast radius, risco e evidências
```

---

## IIntegrationContextResolver

A interface `IIntegrationContextResolver` é usada por outros módulos para resolver o contexto de integração activo para um determinado tenant, ambiente e tipo de conector, sem precisar conhecer os detalhes de configuração:

```csharp
public interface IIntegrationContextResolver
{
    Task<IntegrationContextDescriptor?> ResolveAsync(
        string connectorType,
        Guid tenantId,
        Guid environmentId,
        CancellationToken cancellationToken);
}
```

---

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/integrations/connectors` | Lista conectores com filtros |
| `POST` | `/integrations/connectors` | Cria novo conector |
| `GET` | `/integrations/connectors/{id}` | Detalhe de um conector |
| `PUT` | `/integrations/connectors/{id}/activate` | Activa um conector |
| `GET` | `/integrations/sources` | Lista fontes de ingestão |
| `GET` | `/integrations/executions` | Histórico de execuções de ingestão |
| `GET` | `/integrations/freshness` | Estado de frescura dos dados por fonte |
| `GET` | `/integrations/webhooks` | Lista subscrições de webhook |
| `POST` | `/integrations/webhooks` | Regista nova subscrição de webhook |
| `DELETE` | `/integrations/webhooks/{id}` | Remove subscrição de webhook |

---

## Frontend

O módulo Integrations tem as seguintes páginas em `src/frontend/src/features/integrations/`:

| Página | Rota | Descrição |
|--------|------|-----------|
| `IntegrationHubPage` | `/integrations` | Visão geral dos conectores e fontes activas |
| `ConnectorDetailPage` | `/integrations/connectors/:id` | Detalhe de um conector com histórico |
| `IngestionExecutionsPage` | `/integrations/executions` | Log de execuções de ingestão |
| `IngestionFreshnessPage` | `/integrations/freshness` | Estado de frescura das fontes |
| `WebhookSubscriptionsPage` | `/integrations/webhooks` | Gestão de webhooks de saída |

---

## Dependências Cross-Module

| Módulo | Direção | Natureza |
|--------|---------|---------|
| ChangeGovernance | Integrations → Change | `IngestionPayloadProcessedIntegrationEvent` |
| IdentityAccess | Integrations ← Identity | Validação de API Keys e tenants |
| AuditCompliance | Integrations → Audit | Eventos de criação/modificação de conectores |

---

## Registro no DI

```csharp
// Program.cs do ApiHost
builder.Services.AddIntegrationsInfrastructure(builder.Configuration);

// appsettings.json
"ConnectionStrings": {
  "IntegrationsDatabase": "Host=localhost;Database=nextraceone_integrations;..."
}
```

---

*Última atualização: Março 2026.*

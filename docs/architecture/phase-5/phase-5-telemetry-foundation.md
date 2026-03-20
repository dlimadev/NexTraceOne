# Fase 5 — Fundação de Telemetria com Contexto Operacional

## Objetivo

Garantir que todos os traces, spans e logs gerados pelo NexTraceOne carregem contexto operacional (TenantId, EnvironmentId, CorrelationId) como atributos padrão — permitindo filtragem, correlação e análise por tenant/ambiente em qualquer ferramenta de observabilidade.

---

## TelemetryContextEnricher

**Localização:** `BuildingBlocks.Observability.Telemetry`

O `TelemetryContextEnricher` é o ponto central de enriquecimento de Activities OpenTelemetry com contexto NexTraceOne.

### Responsabilidade

- Adicionar atributos de contexto operacional em Activities existentes ou novas
- Padronizar a nomenclatura de atributos com prefixo `nexttrace.`
- Não criar spans — apenas enriquecer spans existentes

### API

```csharp
// Enriquece a Activity corrente (Activity.Current)
TelemetryContextEnricher.EnrichCurrentActivity(
    tenantId: guid,
    environmentId: guid,
    isProductionLike: bool,
    correlationId: string,
    serviceOrigin: string,
    userId: string);

// Cria e enriquece uma Activity nova
using var activity = TelemetryContextEnricher.StartEnrichedActivity(
    source: NexTraceActivitySources.Integrations,
    operationName: "kafka.publish",
    tenantId: guid,
    environmentId: guid,
    correlationId: correlationId);
```

### Atributos Gerados

| Atributo OpenTelemetry | Origem | Exemplo |
|---|---|---|
| `nexttrace.tenant_id` | `ICurrentTenant.Id` | `"a1b2c3d4-..."` |
| `nexttrace.environment_id` | `ICurrentEnvironment.EnvironmentId` | `"e5f6g7h8-..."` |
| `nexttrace.environment.is_production_like` | `ICurrentEnvironment.IsProductionLike` | `"true"` |
| `nexttrace.correlation_id` | Header `X-Correlation-Id` | `"abc123..."` |
| `nexttrace.service_origin` | Módulo/serviço de origem | `"ChangeGovernance"` |
| `nexttrace.user_id` | `ICurrentUser.Id` | `"user-123"` |

---

## Activity Sources Registrados

O `NexTraceActivitySources` centraliza todos os `ActivitySource` da plataforma:

| Source Name | Constante | Uso |
|---|---|---|
| `NexTraceOne.Commands` | `NexTraceActivitySources.Commands` | Handlers de command (CQRS escrita) |
| `NexTraceOne.Queries` | `NexTraceActivitySources.Queries` | Handlers de query (CQRS leitura) |
| `NexTraceOne.Events` | `NexTraceActivitySources.Events` | Publicação/consumo de eventos |
| `NexTraceOne.ExternalHttp` | `NexTraceActivitySources.ExternalHttp` | Chamadas HTTP externas (adapters) |
| `NexTraceOne.TelemetryPipeline` | `NexTraceActivitySources.TelemetryPipeline` | Jobs de consolidação/cleanup de telemetria |
| `NexTraceOne.Integrations` | `NexTraceActivitySources.Integrations` | **Novo Fase 5** — integrações externas |

Todos estão registrados em `AddBuildingBlocksObservability()` via `AddSource(...)`.

---

## ContextualLoggingBehavior — Enriquecimento de Logs

O `ContextualLoggingBehavior<TRequest, TResponse>` é um pipeline behavior MediatR que:

1. Abre um `ILogger.BeginScope` com TenantId, EnvironmentId e IsProductionLike
2. Enriquece `Activity.Current` com os mesmos atributos via `SetTag`
3. Fica ativo durante toda a execução do pipeline

### Por que ILogger.BeginScope e não Serilog.Context.LogContext?

`BuildingBlocks.Application` não tem referência ao Serilog (que está em `BuildingBlocks.Observability`). Usar `ILogger.BeginScope` é a abordagem provider-agnostic — funciona com:

- Serilog (via `Serilog.Extensions.Logging`)
- Application Insights
- Console structured logging
- Qualquer `ILoggerProvider` configurado no host

O Serilog, quando configurado como provider do `ILoggerFactory`, processa automaticamente os scopes do `ILogger` e os inclui nas propriedades estruturadas.

### Propriedades adicionadas ao scope

```json
{
  "TenantId": "a1b2c3d4-...",
  "EnvironmentId": "e5f6g7h8-...",
  "IsProductionLike": true
}
```

Todos os logs emitidos dentro do pipeline (incluindo LoggingBehavior, PerformanceBehavior, handlers e repositories) herdam estas propriedades automaticamente.

---

## Padrão de Nomenclatura de Atributos

### OpenTelemetry (Semantic Conventions)

O NexTraceOne segue as Semantic Conventions do OpenTelemetry onde aplicável:

- `http.method`, `http.status_code` — instrumentação ASP.NET Core
- `db.system`, `db.statement` — instrumentação EF Core
- `messaging.system`, `messaging.destination` — futuramente para Kafka

### Atributos Custom NexTraceOne

Prefixo `nexttrace.` para todos os atributos proprietários:

```
nexttrace.tenant_id
nexttrace.environment_id
nexttrace.environment.is_production_like
nexttrace.correlation_id
nexttrace.service_origin
nexttrace.user_id
```

Este prefixo garante que não haverá colisão com convenções OpenTelemetry atuais ou futuras.

---

## Configuração OpenTelemetry

O pipeline OTLP configurado em `AddBuildingBlocksObservability()`:

```
Application
    │  ActivitySource.StartActivity(...)
    │  + SetTag("nexttrace.tenant_id", ...)
    ▼
OTLP Exporter ──► OpenTelemetry Collector
                        │
                        ├── Tempo (traces)
                        ├── Loki (logs via tail sampling)
                        └── Prometheus (métricas via SpanMetrics connector)
```

Os atributos `nexttrace.*` são preservados na exportação OTLP e ficam disponíveis para:
- Filtragem de traces por tenant no Tempo/Jaeger
- Alertas por ambiente no Grafana
- Dashboards de confiança por release por tenant

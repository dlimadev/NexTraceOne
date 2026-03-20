# Fase 5 — Arquitetura de Contexto Distribuído

## Visão Geral

A Fase 5 estabelece os alicerces de contexto distribuído no NexTraceOne: propagação de TenantId e EnvironmentId em eventos de integração, enriquecimento automático de logs e traces com contexto operacional, contratos de binding de integrações e base para correlação de sinais distribuídos pela IA.

---

## Motivação

Nas Fases 1-4, o sistema tinha:

- `IntegrationEventBase` sem TenantId/EnvironmentId — eventos cruzando módulos perdiam contexto
- `LoggingBehavior` que registrava apenas o nome do request, sem tenant/ambiente
- `SerilogConfiguration` que enriquecia com MachineName/ThreadId, não com tenant/ambiente
- CORS permitia apenas `X-Tenant-Id`, não `X-Environment-Id` ou `X-Correlation-Id`
- Sem contrato padrão para headers de propagação de contexto
- Sem fundação para resolução de bindings por tenant/ambiente
- Sem contratos de correlação de sinais distribuídos para IA

A Fase 5 resolve esses gaps de forma incremental e retrocompatível.

---

## Componentes Implementados

### 1. IntegrationEventBase (BuildingBlocks.Core)

**Arquivo:** `src/building-blocks/NexTraceOne.BuildingBlocks.Core/Events/IntegrationEventBase.cs`

Adicionadas duas propriedades opcionais:

```csharp
public Guid? TenantId { get; init; }
public Guid? EnvironmentId { get; init; }
```

**Design:**
- `Guid?` (nullable) para compatibilidade retroativa com eventos anteriores à Fase 5
- Eventos de infraestrutura que não têm contexto de tenant continuam funcionando sem alteração
- Eventos operacionais devem preencher TenantId via `with` expression ou construtor

**Exemplo de uso:**
```csharp
// Antes (Fase 4) — ainda funciona
var evt = new UserCreatedIntegrationEvent(userId, email) with
{
    TenantId = currentTenant.Id,
    EnvironmentId = currentEnvironment.EnvironmentId
};
```

### 2. ContextPropagationHeaders (BuildingBlocks.Application)

**Arquivo:** `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Context/ContextPropagationHeaders.cs`

Constantes para todos os headers de propagação de contexto:

| Header | Constante | Propagado Downstream? |
|---|---|---|
| `X-Tenant-Id` | `TenantId` | ✅ |
| `X-Environment-Id` | `EnvironmentId` | ✅ |
| `X-Correlation-Id` | `CorrelationId` | ✅ |
| `X-Request-Id` | `RequestId` | ❌ |
| `X-Service-Origin` | `ServiceOrigin` | ✅ |

O array `PropagatedHeaders` lista os headers que devem ser forwarded em chamadas downstream.

**REGRA DE SEGURANÇA:** Estes headers propagam contexto, não autorização. TenantId é sempre validado contra o JWT — o header é apenas para conveniência de roteamento.

### 3. DistributedExecutionContext (BuildingBlocks.Application)

**Arquivo:** `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Context/DistributedExecutionContext.cs`

Snapshot imutável do contexto operacional para transporte em eventos, mensagens e jobs:

```csharp
var ctx = DistributedExecutionContext.From(currentTenant, currentEnvironment,
    correlationId: Request.Headers["X-Correlation-Id"],
    userId: currentUser.Id);
```

**Diferença de ICurrentTenant/ICurrentEnvironment:**
- `ICurrentTenant`/`ICurrentEnvironment` são scoped à requisição HTTP (live, mutável)
- `DistributedExecutionContext` é imutável, criado pontualmente para ser serializado/transportado

**Propriedade `IsOperational`:** `true` somente se TenantId presente e não vazio — permite guardar verificação.

### 4. TelemetryContextEnricher (BuildingBlocks.Observability)

**Arquivo:** `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Telemetry/TelemetryContextEnricher.cs`

Enriquece Activities OpenTelemetry com atributos de contexto:

```csharp
TelemetryContextEnricher.EnrichCurrentActivity(
    tenantId: currentTenant.Id,
    environmentId: currentEnvironment.EnvironmentId,
    isProductionLike: currentEnvironment.IsProductionLike,
    correlationId: correlationId);
```

**Nomenclatura de atributos (prefixo `nexttrace.`):**
- `nexttrace.tenant_id`
- `nexttrace.environment_id`
- `nexttrace.environment.is_production_like`
- `nexttrace.correlation_id`
- `nexttrace.service_origin`
- `nexttrace.user_id`

O prefixo `nexttrace.` evita colisão com atributos OpenTelemetry padrão (`http.*`, `db.*`, `cloud.*`).

### 5. ContextualLoggingBehavior (BuildingBlocks.Application)

**Arquivo:** `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Behaviors/ContextualLoggingBehavior.cs`

Pipeline behavior MediatR que enriquece o escopo de logging e a Activity OpenTelemetry:

```
Pipeline order (registration order):
1. ValidationBehavior         — valida o request
2. ContextualLoggingBehavior  — enriquece logs e traces (NOVO Fase 5)
3. LoggingBehavior            — loga entrada/saída
4. PerformanceBehavior        — performance tracking
5. TenantIsolationBehavior    — validação de tenant
6. TransactionBehavior        — gerenciamento de transação
```

**Por que antes do LoggingBehavior:** As propriedades de contexto ficam disponíveis quando LoggingBehavior emite seus logs de entrada/saída.

**Implementação com ILogger.BeginScope:** Usa o scope padrão do `Microsoft.Extensions.Logging` — compatível com Serilog, Application Insights e qualquer provider configurado.

### 6. NexTraceActivitySources (BuildingBlocks.Observability)

**Novo source adicionado:**
```csharp
public static readonly ActivitySource Integrations = new("NexTraceOne.Integrations");
```

Registrado automaticamente em `AddBuildingBlocksObservability()` via `AddSource(NexTraceActivitySources.Integrations.Name)`.

---

## Fluxo de Contexto em uma Requisição

```
HTTP Request
    │  X-Tenant-Id: {guid}
    │  X-Environment-Id: {guid}
    │  X-Correlation-Id: {guid}
    ▼
TenantResolutionMiddleware
    │  popula ICurrentTenant
    ▼
EnvironmentContextAccessor
    │  popula ICurrentEnvironment
    ▼
MediatR Pipeline
    │
    ├── ValidationBehavior
    ├── ContextualLoggingBehavior ◄── Fase 5
    │       │  ILogger.BeginScope({ TenantId, EnvironmentId, IsProductionLike })
    │       │  Activity.SetTag("nexttrace.tenant_id", ...)
    ├── LoggingBehavior (agora com contexto no scope)
    ├── PerformanceBehavior
    ├── TenantIsolationBehavior
    └── TransactionBehavior
            │
            ▼
        CommandHandler
            │  DistributedExecutionContext.From(tenant, environment)
            │  event with { TenantId = ..., EnvironmentId = ... }
            ▼
        IntegrationEvent publicado com contexto
```

---

## CORS — Headers Permitidos

Atualizado em `WebApplicationBuilderExtensions.cs`:

```csharp
.WithHeaders(
    "Content-Type", "Authorization",
    "X-Tenant-Id", "X-Environment-Id", "X-Correlation-Id",
    "X-Requested-With", "X-Csrf-Token")
```

Os novos headers (`X-Environment-Id`, `X-Correlation-Id`) são agora permitidos em requisições cross-origin.

---

## Compatibilidade Retroativa

Todas as mudanças são retrocompatíveis:

| Componente | Mudança | Impacto |
|---|---|---|
| `IntegrationEventBase.TenantId` | Adicionado como `Guid?` | Eventos existentes sem TenantId continuam compilando |
| `IntegrationEventBase.EnvironmentId` | Adicionado como `Guid?` | Idem |
| `ContextualLoggingBehavior` | Novo behavior | Requer ICurrentTenant e ICurrentEnvironment no DI — já registrados |
| `NullIntegrationContextResolver` | Novo, interno | Padrão seguro sem configuração |
| `NullDistributedSignalCorrelationService` | Novo, interno | Retorna dados vazios |
| `NullPromotionRiskSignalProvider` | Novo, interno | Retorna `PromotionRiskLevel.None` |

**Exceção:** Dois eventos no `IdentityAccess.Contracts` tinham parâmetros posicionais `Guid TenantId` que conflitavam com o tipo `Guid?` da base. Foram atualizados para `Guid? TenantId` — mudança não-breaking para chamadores existentes.

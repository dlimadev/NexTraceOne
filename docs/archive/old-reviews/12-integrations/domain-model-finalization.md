# Integrations — Domain Model Finalization

> **Module:** Integrations (12)  
> **Date:** 2026-03-25  
> **Status:** Domain model finalized

---

## 1. Aggregate Roots

### IntegrationConnector (Aggregate Root)

**Localização actual:** `src/modules/governance/NexTraceOne.Governance.Domain/Entities/IntegrationConnector.cs` (205 LOC)  
**Localização target:** `src/modules/integrations/NexTraceOne.Integrations.Domain/Entities/IntegrationConnector.cs`

| Propriedade | Tipo | Obrigatória | Notas |
|------------|------|-------------|-------|
| `Id` | `IntegrationConnectorId` (strongly typed) | ✅ | PK |
| `TenantId` | `Guid` | ✅ | Via `NexTraceDbContextBase` RLS |
| `Name` | `string` (max 200) | ✅ | Unique por tenant |
| `ConnectorType` | `string` (max 100) | ✅ | Ex: "CI/CD", "Telemetry", "Incidents" |
| `Description` | `string?` | ❌ | Descrição livre |
| `Provider` | `string` | ✅ | Ex: "GitHub", "Datadog", "PagerDuty" |
| `Endpoint` | `string?` | ❌ | URL do sistema externo |
| `Status` | `ConnectorStatus` enum | ✅ | Default: Pending |
| `Health` | `ConnectorHealth` enum | ✅ | Default: Unknown |
| `LastSuccessAt` | `DateTimeOffset?` | ❌ | Última execução com sucesso |
| `LastErrorAt` | `DateTimeOffset?` | ❌ | Último erro |
| `LastErrorMessage` | `string?` | ❌ | Mensagem do último erro |
| `FreshnessLagMinutes` | `int?` | ❌ | Lag em minutos |
| `TotalExecutions` | `long` | ✅ | Default: 0 |
| `SuccessfulExecutions` | `long` | ✅ | Default: 0 |
| `FailedExecutions` | `long` | ✅ | Default: 0 |
| `Environment` | `string` | ✅ | Default: "Production" |
| `AuthenticationMode` | `string` | ✅ | Default: "Not configured" |
| `PollingMode` | `string` | ✅ | Default: "Not configured" |
| `AllowedTeams` | `IReadOnlyList<string>` | ✅ | JSON, default: [] |
| `CreatedAt` | `DateTimeOffset` | ✅ | Auto |
| `UpdatedAt` | `DateTimeOffset?` | ❌ | Auto on change |

**Métodos de domínio:**
- `Create()` — Factory method
- `RecordSuccess()` — Regista sucesso, actualiza health para Healthy
- `RecordFailure(errorMessage)` — Regista falha, actualiza health
- `MarkDegraded()` — Marca como degradado
- `UpdateFreshnessLag(lag)` — Calcula lag, marca degraded se > 240 min
- `Disable()` / `Activate()` — Muda status operacional
- `UpdateEndpoint(endpoint)` — Actualiza URL
- `UpdateConfiguration(env, auth, polling, teams)` — Actualiza configuração

---

## 2. Entidades (Children)

### IngestionSource (Entity, child of IntegrationConnector)

**Localização actual:** `src/modules/governance/NexTraceOne.Governance.Domain/Entities/IngestionSource.cs` (177 LOC)

| Propriedade | Tipo | Obrigatória | Notas |
|------------|------|-------------|-------|
| `Id` | `IngestionSourceId` (strongly typed) | ✅ | PK |
| `ConnectorId` | `IntegrationConnectorId` | ✅ | FK → IntegrationConnector |
| `Name` | `string` (max 200) | ✅ | Nome da fonte |
| `SourceType` | `string` (max 100) | ✅ | "Webhook", "API Polling", etc. |
| `DataDomain` | `string` (max 100) | ✅ | "Changes", "Incidents", "Runtime" |
| `Description` | `string?` (max 2000) | ❌ | Descrição livre |
| `Endpoint` | `string?` (max 500) | ❌ | URL específica da fonte |
| `TrustLevel` | `SourceTrustLevel` enum | ✅ | Default: Unverified |
| `FreshnessStatus` | `FreshnessStatus` enum | ✅ | Default: Unknown |
| `Status` | `SourceStatus` enum | ✅ | Default: Pending |
| `LastDataReceivedAt` | `DateTimeOffset?` | ❌ | Último dado recebido |
| `LastProcessedAt` | `DateTimeOffset?` | ❌ | Última processamento |
| `DataItemsProcessed` | `long` | ✅ | Default: 0 |
| `ExpectedIntervalMinutes` | `int?` | ❌ | Intervalo esperado |
| `CreatedAt` | `DateTimeOffset` | ✅ | Auto |
| `UpdatedAt` | `DateTimeOffset?` | ❌ | Auto on change |

**Métodos de domínio:**
- `Create()` — Factory method
- `RecordDataReceived(itemCount)` — Regista recepção, incrementa counter
- `RecordProcessingCompleted()` — Marca processamento completo
- `PromoteTrustLevel()` — Promove nível de confiança
- `MarkError(message)` — Marca erro
- `Disable()` / `Activate()` — Muda status
- `UpdateDataDomain(domain)` — Actualiza domínio
- `UpdateFreshnessStatus(now)` — Calcula freshness baseado no lag vs expected interval

**Lógica de freshness:**
- Fresh: lag < expectedMinutes
- Stale: expectedMinutes ≤ lag < expectedMinutes × 4
- Outdated: expectedMinutes × 4 ≤ lag < expectedMinutes × 12
- Expired: lag ≥ expectedMinutes × 12

### IngestionExecution (Entity, child of IntegrationConnector + IngestionSource)

**Localização actual:** `src/modules/governance/NexTraceOne.Governance.Domain/Entities/IngestionExecution.cs` (126 LOC)

| Propriedade | Tipo | Obrigatória | Notas |
|------------|------|-------------|-------|
| `Id` | `IngestionExecutionId` (strongly typed) | ✅ | PK |
| `ConnectorId` | `IntegrationConnectorId` | ✅ | FK → IntegrationConnector |
| `SourceId` | `IngestionSourceId?` | ❌ | FK → IngestionSource (nullable) |
| `CorrelationId` | `string?` (max 100) | ❌ | Auto: `"exec-{Guid:N}"[..20]` |
| `StartedAt` | `DateTimeOffset` | ✅ | Início da execução |
| `CompletedAt` | `DateTimeOffset?` | ❌ | Fim da execução |
| `DurationMs` | `long?` | ❌ | Duração calculada |
| `Result` | `ExecutionResult` enum | ✅ | Default: Running |
| `ItemsProcessed` | `int` | ✅ | Default: 0 |
| `ItemsSucceeded` | `int` | ✅ | Default: 0 |
| `ItemsFailed` | `int` | ✅ | Default: 0 |
| `ErrorMessage` | `string?` (max 2000) | ❌ | Mensagem de erro |
| `ErrorCode` | `string?` (max 100) | ❌ | Código de erro |
| `RetryAttempt` | `int` | ✅ | Default: 0 |
| `CreatedAt` | `DateTimeOffset` | ✅ | Auto |

**Métodos de domínio:**
- `Start()` — Factory method, sets Result=Running
- `CompleteSuccess(itemsProcessed)` — Marca sucesso
- `CompletePartialSuccess(succeeded, failed)` — Marca sucesso parcial
- `CompleteFailed(errorMessage, errorCode)` — Marca falha

---

## 3. Enums persistidos

| Enum | Valores | Armazenamento actual |
|------|---------|---------------------|
| `ConnectorStatus` | Pending(0), Active(1), Paused(2), Disabled(3), Failed(4), Configuring(5) | STRING (max 50) |
| `ConnectorHealth` | Unknown(0), Healthy(1), Degraded(2), Unhealthy(3), Critical(4) | STRING (max 50) |
| `SourceStatus` | Pending(0), Active(1), Paused(2), Disabled(3), Error(4) | STRING (max 50) |
| `SourceTrustLevel` | Unverified(0), Basic(1), Verified(2), Trusted(3), Official(4) | STRING (max 50) |
| `FreshnessStatus` | Unknown(0), Fresh(1), Stale(2), Outdated(3), Expired(4) | STRING (max 50) |
| `ExecutionResult` | Running(0), Success(1), PartialSuccess(2), Failed(3), Cancelled(4), TimedOut(5) | STRING (max 50) |

---

## 4. Relações internas

```
IntegrationConnector (1) ──→ (N) IngestionSource     [cascade delete]
IntegrationConnector (1) ──→ (N) IngestionExecution   [cascade delete]
IngestionSource      (1) ──→ (N) IngestionExecution   [set null on delete]
```

---

## 5. Relações com outros módulos

| Módulo | Tipo | Mecanismo |
|--------|------|-----------|
| **Identity & Access** | Dependência | TenantId via RLS, JWT auth, permissões |
| **Governance** | Emissão → Consumo | Integrations publica eventos → Governance verifica policies |
| **Notifications** | Emissão | `ConnectorFailedEvent` → Notifications envia alerta |
| **Audit & Compliance** | Emissão | Acções sensíveis → Audit regista |
| **Operational Intelligence** | Emissão | Métricas de health → OI agrega |
| **Configuration** | Referência | Parâmetros globais de retry, timeout, etc. |

---

## 6. Gaps e problemas identificados

### Entidades anémicas
- ❌ **IntegrationConnector** não tem método `Delete()` com soft-delete ou cleanup de children
- ❌ **IntegrationConnector** não tem validação de `Endpoint` como URL válida
- ❌ **IngestionSource** não valida que `SourceType` é um valor conhecido

### Regras de negócio fora do lugar
- ❌ **Nenhuma regra de negócio de retry policy** — RetryAttempt existe mas sem max retries, backoff
- ❌ **Freshness threshold (240 min)** hardcoded em `UpdateFreshnessLag()` — deveria vir de Configuration

### Campos ausentes
| Entidade | Campo | Tipo | Razão |
|----------|-------|------|-------|
| `IntegrationConnector` | `RowVersion` | `uint` (xmin) | Concurrency control |
| `IntegrationConnector` | `IsDeleted` | `bool` | Soft delete |
| `IntegrationConnector` | `DeletedAt` | `DateTimeOffset?` | Rastreabilidade |
| `IntegrationConnector` | `MaxRetryAttempts` | `int` | Retry policy |
| `IntegrationConnector` | `RetryBackoffSeconds` | `int` | Retry policy |
| `IntegrationConnector` | `TimeoutSeconds` | `int` | Timeout de execução |
| `IntegrationConnector` | `CredentialEncrypted` | `string?` | Credenciais encriptadas |
| `IngestionSource` | `RowVersion` | `uint` (xmin) | Concurrency control |
| `IngestionSource` | `IsDeleted` | `bool` | Soft delete |
| `IngestionExecution` | `Metadata` | `JsonDocument?` | Dados adicionais de execução |

### Campos indevidos
- ⚠️ **`Environment` como string** — deveria ser `EnvironmentId` (Guid) referenciando Environment Management
- ⚠️ **`AuthenticationMode` como string genérica** — deveria ser enum tipado (None, ApiKey, OAuth2, Basic, Certificate)
- ⚠️ **`PollingMode` como string genérica** — deveria ser enum tipado (None, Scheduled, Webhook, Manual)

---

## 7. Domain events a criar

| Evento | Trigger | Consumidores |
|--------|---------|-------------|
| `ConnectorCreatedEvent` | `IntegrationConnector.Create()` | Governance (policy check), Audit |
| `ConnectorUpdatedEvent` | `UpdateConfiguration()`, `UpdateEndpoint()` | Audit |
| `ConnectorActivatedEvent` | `Activate()` | Governance, Notifications |
| `ConnectorDisabledEvent` | `Disable()` | Notifications, Audit |
| `ConnectorFailedEvent` | `RecordFailure()` | Notifications (alerta), OI (métrica) |
| `ConnectorHealthChangedEvent` | `MarkDegraded()`, health transitions | OI, Governance |
| `ExecutionCompletedEvent` | `CompleteSuccess/Partial/Failed()` | Audit (se sensível), OI |
| `ExecutionRetriedEvent` | `RetryConnector` command | Audit |
| `SourceTrustPromotedEvent` | `PromoteTrustLevel()` | Audit, Governance |

---

## 8. Modelo final do domínio (target)

O modelo actual é **sólido** na sua estrutura base. As correcções necessárias são:

1. **Adicionar RowVersion** a IntegrationConnector e IngestionSource
2. **Adicionar soft delete** (IsDeleted, DeletedAt) a IntegrationConnector e IngestionSource
3. **Tipar AuthenticationMode** como enum: `None, ApiKey, OAuth2, Basic, Certificate`
4. **Tipar PollingMode** como enum: `None, Scheduled, Webhook, Manual`
5. **Substituir Environment string** por `EnvironmentId` (Guid?) para referência formal
6. **Adicionar retry policy fields** a IntegrationConnector: `MaxRetryAttempts`, `RetryBackoffSeconds`, `TimeoutSeconds`
7. **Adicionar CredentialEncrypted** com encriptação via `EncryptionInterceptor`
8. **Publicar domain events** em todos os métodos de transição de estado
9. **Extrair entidades** de Governance para módulo Integrations independente

# P5.2 — Correlação Automática Trace → Release: Relatório de Execução

**Data:** 2026-03-26  
**Fase:** P5.2 — Change Intelligence: Correlação Automática Trace → Release  
**Estado:** CONCLUÍDO

---

## 1. Objetivo

Fechar o próximo elo do pipeline de Change Intelligence — correlação automática entre traces
distribuídos (OTel) e a entidade `Release` do módulo Change Governance.

Após o P5.1 ter fechado o pipeline deploy event → Release, o P5.2 fecha o elo
Release → telemetria, criando a estrutura analítica `chg_trace_release_mapping` no ClickHouse
e o pipeline mínimo que associa traces a releases de forma rastreável.

---

## 2. Estado Anterior

### Gap identificado antes do P5.2

| Gap | Descrição |
|-----|-----------|
| `chg_trace_release_mapping` | Não existia no schema ClickHouse |
| `IAnalyticsWriter.WriteTraceReleaseMappingAsync` | Não existia |
| Pipeline trace → release | Inexistente — correlação só possível via análise manual |
| `RecordTraceCorrelation` handler | Inexistente — nenhum endpoint para receber correlações |
| `GetTraceCorrelations` query | Inexistente — impossível consultar traces de uma release |
| `ITraceCorrelationWriter` | Abstração inexistente |
| `IChangeEventRepository.ListByReleaseIdAndEventTypeAsync` | Método inexistente |

---

## 3. Modelo de Correlação Adotado

### Chave de correlação trace → release

| Campo | Origem | Papel |
|-------|--------|-------|
| `TraceId` | OTel trace ID (hex string 32 chars) | Identificador único do trace |
| `ReleaseId` | `chg_releases.Id` | Release à qual o trace pertence |
| `ServiceName` | Identificação do serviço | Correlação por serviço |
| `Environment` | Ambiente onde o trace ocorreu | Correlação por ambiente |
| `CorrelationSource` | Origem da correlação (`deployment_event`, `manual`, etc.) | Rastreabilidade da origem |
| `TraceStartedAt` / `TraceEndedAt` | Janela temporal do trace | Análise de janela pós-deploy |

### Semântica

- **A correlação é registada** quando um deploy event → release cria uma nova release E existe um
  trace identificado para o mesmo serviço/ambiente.
- **Não é obrigatório** que o trace esteja armazenado no ClickHouse `nextraceone_obs` no momento
  da correlação — o mapping é independente do storage de traces raw.
- **A correlação é append-only** no ClickHouse — idempotência garantida pelo `Id` único por evento.

### Rastreabilidade

1. **PostgreSQL** — `ChangeEvent(trace_correlated)` com `Source = traceId` — auditável, consultável
   por release, mantém timeline operacional.
2. **ClickHouse** — `chg_trace_release_mapping` — analítico, append-only, consultas rápidas
   por release/serviço/ambiente/janela temporal.

---

## 4. Estrutura `chg_trace_release_mapping` (ClickHouse)

```sql
CREATE TABLE IF NOT EXISTS nextraceone_analytics.chg_trace_release_mapping
(
    id                  UUID,
    tenant_id           UUID,
    release_id          UUID,
    trace_id            String,
    service_name        LowCardinality(String),
    service_id          Nullable(UUID),
    environment         LowCardinality(String),
    environment_id      Nullable(UUID),
    correlation_source  LowCardinality(String)   DEFAULT 'deployment_event',
    trace_started_at    Nullable(DateTime64(3, 'UTC')),
    trace_ended_at      Nullable(DateTime64(3, 'UTC')),
    correlated_at       DateTime64(3, 'UTC')
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(correlated_at))
ORDER BY (tenant_id, release_id, correlated_at, trace_id)
TTL correlated_at + INTERVAL 1 YEAR
```

**Queries analíticas suportadas:**
- `WHERE tenant_id = ? AND release_id = ?` → todos os traces de uma release
- `WHERE tenant_id = ? AND service_name = ? AND correlated_at BETWEEN ? AND ?` → traces por serviço/janela
- `WHERE tenant_id = ? AND environment = ? AND release_id = ?` → traces por ambiente/release

---

## 5. Ficheiros Alterados

### ClickHouse schema

| Ficheiro | Alteração |
|----------|-----------|
| `build/clickhouse/analytics-schema.sql` | Adicionada secção `chg_*` + tabela `chg_trace_release_mapping` |

### Building Blocks — Observability

| Ficheiro | Alteração |
|----------|-----------|
| `Analytics/Events/AnalyticsRecords.cs` | Adicionado `TraceReleaseMappingRecord` (12 campos) |
| `Analytics/Abstractions/IAnalyticsWriter.cs` | Adicionado `WriteTraceReleaseMappingAsync()` |
| `Analytics/Writers/NullAnalyticsWriter.cs` | Implementação no-op do novo método |
| `Analytics/Writers/ClickHouseAnalyticsWriter.cs` | Implementação real via HTTP/JSONEachRow para `chg_trace_release_mapping` |

### Change Governance — Application

| Ficheiro | Alteração |
|----------|-----------|
| `Abstractions/IChangeEventRepository.cs` | Adicionado `ListByReleaseIdAndEventTypeAsync` |
| `Abstractions/ITraceCorrelationWriter.cs` | **Novo** — abstração de escrita analítica de correlações |
| `Features/RecordTraceCorrelation/RecordTraceCorrelation.cs` | **Novo** — Command + Validator + Handler |
| `Features/GetTraceCorrelations/GetTraceCorrelations.cs` | **Novo** — Query + Validator + Handler |
| `ChangeIntelligence/DependencyInjection.cs` | Registados validators de `RecordTraceCorrelation` e `GetTraceCorrelations` |

### Change Governance — Infrastructure

| Ficheiro | Alteração |
|----------|-----------|
| `NexTraceOne.ChangeGovernance.Infrastructure.csproj` | Adicionada referência a `BuildingBlocks.Observability` |
| `ChangeIntelligence/Analytics/TraceCorrelationAnalyticsWriter.cs` | **Novo** — adapter `ITraceCorrelationWriter → IAnalyticsWriter` |
| `ChangeIntelligence/DependencyInjection.cs` | `AddBuildingBlocksAnalytics` + `ITraceCorrelationWriter` registados |
| `Persistence/Repositories/ChangeEventRepository.cs` | Implementado `ListByReleaseIdAndEventTypeAsync` |

### Change Governance — API

| Ficheiro | Alteração |
|----------|-----------|
| `Endpoints/TraceCorrelationEndpoints.cs` | **Novo** — `POST /traces` e `GET /traces` por release |
| `Endpoints/ChangeIntelligenceEndpointModule.cs` | Registado `TraceCorrelationEndpoints.Map(group)` |

### Testes

| Ficheiro | Alteração |
|----------|-----------|
| `ChangeIntelligenceApplicationTests.cs` | 4 novos testes adicionados |

---

## 6. Pipeline Trace → Release (pós-P5.2)

```
Origem do trace (OTel pipeline / Ingestion API / operador)
    │
    └─► POST /api/v1/releases/{releaseId}/traces   (ChangeGovernance API)
            │
            └─► RecordTraceCorrelation.Command  (MediatR)
                    │
                    ├─ Verifica Release existe (PostgreSQL)
                    │
                    ├─ Cria ChangeEvent(trace_correlated, Source=traceId)   → PostgreSQL
                    │         auditável, timeline da release
                    │
                    └─► ITraceCorrelationWriter.WriteAsync()
                            │
                            └─► IAnalyticsWriter.WriteTraceReleaseMappingAsync()
                                    │
                                    └─► ClickHouse: chg_trace_release_mapping (append-only)
                                    │   ou NullAnalyticsWriter se Analytics:Enabled=false

GET /api/v1/releases/{releaseId}/traces
    └─► GetTraceCorrelations.Query
            └─► IChangeEventRepository.ListByReleaseIdAndEventTypeAsync("trace_correlated")
                    └─► PostgreSQL: chg_change_events WHERE EventType = "trace_correlated"
```

---

## 7. Novo Endpoint: Correlação de Traces

### `POST /api/v1/releases/{releaseId}/traces`

**Permissão:** `change-intelligence:write`  
**Body:**
```json
{
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "serviceName": "PaymentService",
  "environment": "production",
  "serviceId": null,
  "environmentId": null,
  "correlationSource": "deployment_event",
  "traceStartedAt": "2026-03-26T19:00:00Z",
  "traceEndedAt": null
}
```
**Resposta:** `{ correlationId, releaseId, traceId, serviceName, environment, correlationSource, correlatedAt }`

### `GET /api/v1/releases/{releaseId}/traces`

**Permissão:** `change-intelligence:read`  
**Resposta:**
```json
{
  "releaseId": "...",
  "serviceName": "PaymentService",
  "version": "3.1.0",
  "environment": "production",
  "correlations": [
    { "traceId": "...", "correlatedAt": "...", "description": "..." }
  ]
}
```

---

## 8. Validação Realizada

- ✅ Compilação sem erros: `BuildingBlocks.Observability`, `ChangeGovernance.Infrastructure`, `ChangeGovernance.API`
- ✅ 204/204 testes ChangeGovernance passam (incluindo 4 novos)
- ✅ Todos os testes unitários do projecto passam (2615+ testes)
- ✅ Testes Observability (96) passam — nova implementação `NullAnalyticsWriter` não quebra nada
- ✅ Graceful degradation: quando `Analytics:Enabled = false`, `NullAnalyticsWriter` é usado (no-op)

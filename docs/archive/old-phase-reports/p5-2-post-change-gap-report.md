# P5.2 — Post-Change Gap Report: Trace → Release Correlation

**Data:** 2026-03-26  
**Fase:** P5.2 — Change Intelligence: Correlação Automática Trace → Release

---

## 1. O que foi resolvido nesta fase

| Gap | Estado |
|-----|--------|
| Estrutura analítica `chg_trace_release_mapping` no ClickHouse | ✅ Criada |
| `IAnalyticsWriter.WriteTraceReleaseMappingAsync()` | ✅ Implementada |
| `NullAnalyticsWriter` e `ClickHouseAnalyticsWriter` actualizados | ✅ Implementados |
| `TraceReleaseMappingRecord` em `AnalyticsRecords.cs` | ✅ Criado |
| `ITraceCorrelationWriter` abstração no Application | ✅ Criada |
| `TraceCorrelationAnalyticsWriter` no Infrastructure | ✅ Criado |
| `RecordTraceCorrelation` handler (Command + Validator) | ✅ Implementado |
| `GetTraceCorrelations` handler (Query + Validator) | ✅ Implementado |
| `POST /api/v1/releases/{id}/traces` endpoint | ✅ Implementado |
| `GET  /api/v1/releases/{id}/traces` endpoint | ✅ Implementado |
| `ListByReleaseIdAndEventTypeAsync` no repositório | ✅ Implementado |
| DI wiring do `ITraceCorrelationWriter` na Infrastructure | ✅ Concluído |
| 4 novos testes unitários | ✅ Passam (204 total) |
| Código compila sem erros | ✅ Validado |

---

## 2. O que ainda fica pendente após P5.2

### Trigger automático de correlação ao criar Release

| Item pendente | Descrição |
|---------------|-----------|
| Trigger automático via `NotifyDeployment` | Ao criar uma release via deploy event, **ainda não há trigger automático** que correlacione traces existentes para o mesmo serviço/ambiente. O `RecordTraceCorrelation` existe mas requer chamada explícita. |
| Correlação retroativa de traces existentes | Traces gerados antes do deploy event chegou não são correlacionados automaticamente. |

### Integração com OTel Collector / nextraceone_obs

| Item pendente | Descrição |
|---------------|-----------|
| Pipeline OTel → ChangeGovernance | O OTel Collector (ou Ingestion API) ainda não dispatcha `RecordTraceCorrelation.Command` automaticamente quando ingere traces do serviço. |
| Lookup de traces por janela temporal em `nextraceone_obs` | Após um deploy, fazer lookup automático de traces no ClickHouse `nextraceone_obs` para correlacioná-los à nova release requer implementação futura. |
| `nextraceone_obs.otel_traces` schema | Ainda não activo/documentado; a correlação cruzada com `chg_trace_release_mapping` via JOIN ClickHouse fica para P5.3+. |

### Análise analítica avançada

| Item pendente | Descrição |
|---------------|-----------|
| Materialized view `chg_daily_trace_correlations` | Agregação diária de traces por release/serviço para dashboards — não criada nesta fase. |
| Consulta ClickHouse no `GetTraceCorrelations` | Actualmente o handler consulta PostgreSQL (ChangeEvents). A versão analítica que consulta directamente o ClickHouse `chg_trace_release_mapping` fica para fase futura. |

---

## 3. Limitações residuais após P5.2

1. **Correlação não é 100% automática end-to-end**: O endpoint `POST /traces` existe e funciona, mas
   requer que o chamador (pipeline ou operador) identifique o `traceId` e o envie. O trigger
   automático do pipeline OTel → ChangeGovernance não está fechado.

2. **`GetTraceCorrelations` consulta PostgreSQL, não ClickHouse**: Para consistência de arquitectura,
   a query de traces correlacionados lê `ChangeEvent(trace_correlated)` do PostgreSQL. Para analytics
   em escala (ex: "todos os traces da semana para este serviço"), a query directa ao ClickHouse
   `chg_trace_release_mapping` seria mais eficiente — isso fica para P5.3.

3. **`correlation_source` é passado pelo cliente**: A validação da origem da correlação é simples
   (string, max 100 chars). Uma enum ou set de valores válidos pode ser adicionada futuramente
   para melhorar a consistência dos dados analíticos.

---

## 4. O que fica explicitamente para P5.3

- **Pipeline OTel → ChangeGovernance**: quando o Ingestion API ingere traces, identificar
  automaticamente a release do serviço/ambiente e dispatchar `RecordTraceCorrelation.Command`.
- **Activação de `nextraceone_obs`**: definir e documentar o schema de `otel_traces` no ClickHouse
  para que os JOINs com `chg_trace_release_mapping` funcionem end-to-end.
- **Blast radius automático**: usar `chg_trace_release_mapping` + `otel_traces` para calcular
  blast radius a partir de traces reais (consumidores observados na telemetria).
- **Post-change verification automatizada**: usar `chg_trace_release_mapping` com janela temporal
  para comparar error rate / latência pré e pós-deploy a partir de traces correlacionados.
- **`GET /api/v1/releases/{id}/traces/analytics`**: endpoint que consulta directamente ClickHouse
  para retornar agregações analíticas (count, latency, errors) por release.

---

## 5. Próximos passos sugeridos

1. **P5.3 — Pipeline OTel → ChangeGovernance**: Ao ingerir traces no Ingestion API, resolver a
   release correspondente (via `GetByServiceNameVersionEnvironmentAsync`) e dispatchar
   `RecordTraceCorrelation.Command` automaticamente.
2. **P5.3 — `nextraceone_obs` schema**: Definir tabela `otel_traces` no ClickHouse e garantir
   que o OTel Collector exporta para ela, permitindo JOINs com `chg_trace_release_mapping`.
3. **P5.4 — Blast radius automático via telemetria**: Usar `chg_trace_release_mapping` +
   `otel_traces` para calcular blast radius real a partir de observações de dependência nos traces.

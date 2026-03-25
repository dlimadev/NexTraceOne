# Relatório de Observabilidade, Change Intelligence e Operação — NexTraceOne

**Data:** 25 de março de 2026

---

## 1. Objectivo

Auditar o estado de observabilidade, telemetria, change intelligence e operação: ingestão de traces, correlação com serviços/mudanças/incidentes, blast radius, evidence pack e post-change verification.

---

## 2. Stack de Observabilidade — Estado

### 2.1 Componentes Configurados

| Componente | Versão | Estado |
|-----------|--------|--------|
| OpenTelemetry Collector | 0.115.0 | READY |
| ClickHouse | 24.8 | READY |
| PostgreSQL | 16 | READY (main DB) |
| BuildingBlocks.Observability | .NET | PARTIAL |
| NexTraceOne.Ingestion.Api | Projecto dedicado | PARTIAL |

### 2.2 Pipeline OTel Configurado

**Ficheiro:** `build/otel-collector/otel-collector.yaml`

```yaml
receivers:
  otlp:          # gRPC 4317, HTTP 4318
  prometheus:    # Scraping de métricas

processors:
  batch:         # Processamento em lotes
  memory_limiter: # Limite de memória

exporters:
  clickhousetraces:   # ClickHouse para traces
  clickhouselogs:     # ClickHouse para logs
  clickhousemetrics:  # ClickHouse para métricas
  prometheus:         # Métricas para Prometheus
  debug:              # Debug logging

pipelines:
  traces:   [otlp] → [batch] → [clickhousetraces]
  metrics:  [otlp, prometheus] → [batch] → [clickhousemetrics, prometheus]
  logs:     [otlp] → [batch] → [clickhouselogs]
```

**Estado:** READY — pipeline completo e funcional

---

### 2.3 ClickHouse Schema

**Ficheiro:** `build/clickhouse/init-schema.sql`

**Database:** `nextraceone_obs`

| Tabela | Motor | TTL | Propósito |
|--------|-------|-----|-----------|
| `otel_logs` | MergeTree | 30 dias | Logs estruturados |
| `otel_traces` | MergeTree | 30 dias | Distributed traces |
| `otel_metrics` | MergeTree | 90 dias | Métricas de tempo |

**Qualidade:**
- Particionamento mensal
- Compressão Zstandard
- LowCardinality para campos de alta cardinalidade
- Nested arrays para eventos e links em traces
- TTL automático para purga

**Lacuna crítica:** Sem tabela de correlação `trace_release_mapping` ou similar para ligar trace IDs a releases/serviços.

---

### 2.4 BuildingBlocks.Observability

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/`

**Verificado:**
- OpenTelemetry setup com tracing, metrics e logging
- Serilog com sinks: Console, File, PostgreSQL, Grafana Loki
- Health check endpoints (/health, /ready, /live)
- Suporte a ClickHouse e Elastic como providers

**Estado:** PARTIAL — setup correcto; correlação com entidades de negócio não verificada

---

## 3. Change Intelligence — Estado

### 3.1 Modelo de Dados

**Ficheiro:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/ChangeIntelligence/Persistence/ChangeIntelligenceDbContext.cs`

**10 entidades:**

| Entidade | Propósito |
|----------|-----------|
| `Release` | Identidade de release com ambiente, versão, scoring |
| `BlastRadiusReport` | Análise de impacto de uma release |
| `ChangeIntelligenceScore` | Score de confiança (0-100) |
| `ChangeEvent` | Evento de mudança com correlação |
| `ExternalMarker` | Markers de ferramentas externas (CI/CD) |
| `FreezeWindow` | Janelas de congelamento de deploys |
| `ReleaseBaseline` | Baseline para comparação |
| `ObservationWindow` | Janela de observação pós-release |
| `PostReleaseReview` | Revisão pós-release |
| `RollbackAssessment` | Avaliação de rollback |

**Estado:** READY — schema completo e bem modelado

### 3.2 Promotion Governance

**PromotionDbContext (4 entidades):**
- `DeploymentEnvironment` — definição de ambientes
- `PromotionRequest` — pedido de promoção inter-ambiente
- `PromotionGate` — gates de aprovação por ambiente
- `GateEvaluation` — resultado de avaliação de gate

**Estado:** READY — schema para promotion governance com gates

### 3.3 Evidence Pack

**WorkflowDbContext (6 entidades):**
- `EvidencePack` — pacote de evidências para aprovação
- `WorkflowTemplate` — template de workflow de aprovação
- `WorkflowInstance` — instância de execução
- `ApprovalDecision` — decisão de aprovação com justificativa
- `SlaPolicy` — política de SLA por workflow

**Estado:** PARTIAL — entidades existem; fluxo end-to-end não verificado

---

## 4. Correlação Telemetria ↔ Negócio

### 4.1 O que existe

- `ChangeEvent` com campos: `serviceId`, `releaseId`, `environmentId`
- `IncidentRecord` com `correlatedChanges`
- `BlastRadiusReport` com análise de impacto
- OTel traces com campos de serviço no span

### 4.2 O que falta

- Correlação automática: trace → `releaseId` (não encontrado como fluxo automático)
- Mapeamento trace_id → release: sem tabela de correlação no ClickHouse
- Post-change verification automatizada com baseline comparison
- Alert baseado em degradação de métricas pós-release

**Lacuna:** O sistema tem as peças mas o "fio" que liga telemetria a mudanças de negócio não foi verificado como pipeline automático.

---

## 5. Ingestion.Api

**Estado:** PARTIAL

`NexTraceOne.Ingestion.Api` existe como projecto separado para receber telemetria. Não foi auditado em detalhe.

**Esperado:**
- Endpoint OTLP para receber dados das aplicações
- Normalização para modelo canónico
- Correlação com entidades de negócio (serviço, release, ambiente)
- Encaminhamento para ClickHouse e PostgreSQL

**Estado efectivo:** Estrutura existe; implementação não verificada

---

## 6. Operational Reliability

### 6.1 ReliabilityDbContext

**1 entidade:** `ReliabilitySnapshot`

**Lacuna:** Apenas 1 entidade para reliability é insuficiente para:
- SLO definitions (Service Level Objectives)
- SLA contracts
- Error budget tracking
- Burn rate alerts
- Reliability scoring por serviço

**Recomendação:** Expandir significativamente o `ReliabilityDbContext`

### 6.2 RuntimeIntelligenceDbContext

**4 entidades:** `RuntimeSnapshot`, `RuntimeBaseline`, `DriftFinding`, `ObservabilityProfile`

**Estado:** Modelo adequado para comparação de ambientes. Fluxo de detecção de drift não verificado.

---

## 7. Scripts de Verificação

**Scripts disponíveis:**
- `scripts/observability/verify-pipeline.sh` — verificação do pipeline de telemetria
- `scripts/deploy/smoke-check.sh` — smoke test pós-deploy

**Estado:** Scripts existem; conteúdo não verificado em detalhe

---

## 8. Blast Radius

**Estado: PARTIAL**

- `BlastRadiusReport` entidade real com análise de impacto
- Frontend com `serviceCatalogApi.getImpactPropagation(nodeId, depth)`
- Blast radius calculado no Service Catalog Graph

**Lacuna:** Blast radius manual via UI; não está automaticamente ligado ao `ChangeIntelligenceScore` no momento do deploy.

---

## 9. Regra Crítica do Produto

> "A análise de comportamento em ambientes não produtivos para prevenir falhas em produção é mandatória."

**Estado de conformidade:** PARTIAL

- `RuntimeBaseline` e `DriftFinding` existem para comparação de ambientes
- `PromotionGate` e `GateEvaluation` para gates de promoção
- Comparação automática de métricas baseline vs actual: **não verificada como pipeline automático**

---

## 10. Resumo de Achados

| Área | Estado | Lacunas Principais |
|------|--------|-------------------|
| OTel Collector | READY | — |
| ClickHouse Schema | READY | Sem tabela correlação trace↔release |
| Change Intelligence Entities | READY | — |
| Promotion Governance | READY | — |
| Evidence Pack | PARTIAL | Fluxo end-to-end não verificado |
| Correlação telemetria-negócio | PARTIAL | Pipeline automático não verificado |
| Reliability | INCOMPLETE | Apenas 1 entidade |
| Runtime Comparison | PARTIAL | Drift detection não verificada |
| Post-change Verification | PARTIAL | Entidade existe; automação não verificada |
| Ingestion.Api | PARTIAL | Não auditado em detalhe |

---

## 11. Recomendações

| Prioridade | Acção |
|-----------|-------|
| P1 | Adicionar tabela de correlação `trace_release_mapping` no ClickHouse |
| P1 | Implementar pipeline automático: deploy event → trace correlation |
| P1 | Completar fluxo de Evidence Pack end-to-end |
| P2 | Expandir ReliabilityDbContext com SLO/SLA/error budget entities |
| P2 | Implementar post-change verification automatizada com baseline |
| P2 | Auditar em detalhe NexTraceOne.Ingestion.Api |
| P2 | Ligar BlastRadiusReport ao ChangeIntelligenceScore automaticamente |
| P3 | Implementar alert baseado em degradação de métricas pós-release |
| P3 | Completar Runtime drift detection como fluxo automático |

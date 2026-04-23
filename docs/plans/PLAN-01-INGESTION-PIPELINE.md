# Plano 01 — Ingestion Pipeline (PIP-01..06)

> **Prioridade:** 🔴 Alta  
> **Esforço total:** 6–10 semanas  
> **ADR de referência:** [ADR-010](../adr/010-server-side-ingestion-pipeline.md)  
> **Spec técnica:** [INGESTION-PIPELINE-IMPLEMENTATION.md](../INGESTION-PIPELINE-IMPLEMENTATION.md)  
> **Tracking:** [HONEST-GAPS.md](../HONEST-GAPS.md) — secção Pipeline

---

## Contexto

O pipeline de ingestão atual é linear, global e sem configurabilidade por tenant. O objetivo é transformá-lo num motor de stream-processing equivalente ao Dynatrace OpenPipeline.

**Estado atual:**
```
Ingestion API → MediatR → PostgreSQL Outbox → handlers (5 retries, depois descarta)
```

**Estado alvo:**
```
Ingestion API / OTel Collector
  → TenantPipelineEngine (masking → filtering → enrichment → transform → routing)
  → StorageBucket (audit/debug/default com retenção configurável)
  → DeadLetterMessage (falhas auditáveis e reprocessáveis)
  → Métricas de ingestão (auto-observabilidade)
```

---

## PIP-01 — Dead Letter Queue

**Problema:** `ModuleOutboxProcessorJob` descarta silenciosamente após 5 retries. Dados perdem-se sem rastreio.

**Implementação:**
1. Criar `DeadLetterMessage` entity em `BuildingBlocks.Infrastructure/Outbox/`
   - Campos: `Id`, `TenantId`, `MessageType`, `Payload`, `FailureReason`, `LastException`, `AttemptCount`, `ExhaustedAt`, `ReprocessedAt`, `Status` (`DlqMessageStatus`: Pending/Reprocessing/Resolved/Discarded)
   - Métodos: `From(OutboxMessage, exception)`, `MarkReprocessing()`, `MarkResolved()`, `MarkDiscarded(reason)`
2. Migration: tabela `bb_dead_letter_messages` com índices por tenant + status + exhausted_at
3. Alterar `ModuleOutboxProcessorJob`: em vez de descartar após 5 retries → persistir `DeadLetterMessage`
4. Novo endpoint `GET /api/v1/platform/dead-letters` (Platform Admin) + `POST /api/v1/platform/dead-letters/{id}/reprocess`
5. Testes: 15+ unitários + 5 de integração

**Ficheiros a criar/modificar:**
- `src/building-blocks/.../Outbox/DeadLetterMessage.cs` (novo)
- `src/building-blocks/.../Outbox/DeadLetterMessageConfiguration.cs` (novo)
- `src/platform/NexTraceOne.BackgroundWorkers/Jobs/ModuleOutboxProcessorJob.cs` (modificar)
- Migration em `BuildingBlocks.Infrastructure`

**Esforço:** 2–3 dias

---

## PIP-02 — Ingestion Observability

**Problema:** Sem métricas de throughput/latência/falhas por tenant — pipeline é uma caixa negra.

**Implementação:**
1. Criar `IngestionMetricsCollector` service que emite (via OTel ActivitySource):
   - `ingestion.events.received` (counter por tenant + source)
   - `ingestion.events.processed` (counter por tenant + result: success/failure/dlq)
   - `ingestion.processing.duration` (histogram por tenant + pipeline stage)
   - `ingestion.dlq.count` (gauge por tenant)
2. Integrar no `ModuleOutboxProcessorJob` e `IngestionApi` endpoints
3. Dashboard widget "Ingestion Health" na `SystemHealthPage` existente
4. Config keys: `ingestion.metrics.enabled`, `ingestion.metrics.sampling_rate`

**Esforço:** 2–3 dias

---

## PIP-03 — TenantPipelineRule

**Problema:** Sem configuração de pipeline por tenant (masking, filtering, enrichment são globais).

**Implementação:**
1. Domain entity `TenantPipelineRule` em `Integrations.Domain`:
   - `RuleType`: Masking | Filtering | Enrichment | Transform
   - `SignalType`: Span | Log | Metric
   - `ConditionJson`, `ActionJson`, `Priority`, `IsEnabled`
2. `ITenantPipelineRuleRepository` + EF implementação
3. `TenantPipelineEngine` service: aplica regras ordenadas por priority com cache 60s (IMemoryCache)
4. Stages implementados:
   - **Masking**: redacção de campos via regex (ex: `$.body.email` → `[REDACTED]`)
   - **Filtering**: descarte de records por condição (ex: `level == "debug"` em produção)
   - **Enrichment**: injeção de atributos do Service Catalog (serviceOwner, tier, contracts)
5. CRUD endpoints: `POST/GET/PUT/DELETE /api/v1/integrations/pipeline-rules`
6. Migration: tabela `int_tenant_pipeline_rules`
7. Config keys: `pipeline.cache.ttl_seconds`, `pipeline.max_rules_per_tenant`
8. Testes: 20+ unitários

**Esforço:** 1–1.5 semanas

---

## PIP-04 — StorageBucket Routing

**Problema:** Sem routing condicional de dados para diferentes backends com retenção configurável por tenant.

**Implementação:**
1. Domain entity `StorageBucket`:
   - `BucketName`, `BackendType` (Elasticsearch | ClickHouse | PostgreSQL), `RetentionDays`, `Filter` (JSON condition), `Priority`
2. `StorageBucketRouter` service: avalia buckets por priority, encaminha eventos para backend correto
3. Buckets default por tenant: `audit` (2555 dias, ES), `debug` (3 dias, CH), `default` (90 dias, ES)
4. CRUD endpoints: `POST/GET/PUT/DELETE /api/v1/integrations/storage-buckets`
5. Integração com `TenantPipelineEngine` como último stage (após Enrichment)
6. Migration: tabela `int_storage_buckets`
7. Config keys: `pipeline.routing.default_bucket`, `pipeline.routing.fallback_on_error`

**Esforço:** 1–1.5 semanas

---

## PIP-05 — CatalogEnrichmentProcessor

**Problema:** Spans e logs ingeridos não têm contexto do Service Catalog (owner, tier, contracts).

**Implementação:**
1. `CatalogEnrichmentProcessor` implementa `IEnrichmentProcessor`:
   - Lookup de `ServiceAsset` por `service.name` do span (cache 5min, `IMemoryCache`)
   - Injeta atributos: `nextraceone.service.owner`, `nextraceone.service.tier`, `nextraceone.service.contract_count`, `nextraceone.team.name`
   - Fallback gracioso: enriquecimento não bloqueia ingestão se service não encontrado
2. Registar como processor no `TenantPipelineEngine` (stage Enrichment)
3. Usa `ICatalogGraphModule` (cross-module interface já existente)
4. Config keys: `enrichment.catalog.enabled`, `enrichment.catalog.cache_ttl_seconds`
5. Testes: 12+ unitários com mock de `ICatalogGraphModule`

**Esforço:** 3–4 dias

---

## PIP-06 — LogToMetricProcessor

**Problema:** Sem transformação server-side de logs em métricas (log → metric pipeline).

**Implementação:**
1. Domain entity `LogToMetricRule`:
   - `Pattern` (regex ou JSON path condition)
   - `MetricName`, `MetricType` (Counter | Gauge | Histogram)
   - `ValueExtractor` (campo do log body ou constante 1)
   - `Labels` (campos do log a promover a labels)
2. `LogToMetricProcessor` implementa `ITransformProcessor`:
   - Para cada log que match `Pattern`, emite métrica via `ITelemetryWriterService`
   - Batch: acumula métricas em buffer de 5s antes de emitir
3. CRUD endpoints: `POST/GET/PUT/DELETE /api/v1/integrations/log-metric-rules`
4. Migration: tabela `int_log_to_metric_rules`
5. Config keys: `pipeline.log_to_metric.enabled`, `pipeline.log_to_metric.max_rules_per_tenant`, `pipeline.log_to_metric.buffer_flush_seconds`
6. Testes: 15+ unitários

**Esforço:** 1 semana

---

## Ordem de Execução

```
Sprint 1 (2 semanas):  PIP-01 + PIP-02
Sprint 2 (2 semanas):  PIP-03
Sprint 3 (2 semanas):  PIP-04 + PIP-05
Sprint 4 (2 semanas):  PIP-06 + integração end-to-end + testes
```

## Critérios de Aceite

- [ ] `ModuleOutboxProcessorJob` nunca descarta silenciosamente — sempre persiste em DLQ
- [ ] Dashboard Platform Admin mostra métricas de ingestão por tenant em tempo real
- [ ] Tenant pode configurar regras de masking de PII via API
- [ ] Spans enriquecidos com `nextraceone.service.*` atributos visíveis no Elasticsearch
- [ ] Log pattern `error_code=5xx` pode gerar métrica `api.error.count` server-side
- [ ] Todos os novos endpoints cobertos por testes (mínimo 80% statement coverage)

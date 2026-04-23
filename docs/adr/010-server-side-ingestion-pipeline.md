# ADR-010: Server-Side Ingestion Pipeline — Configurável por Tenant

## Status

Proposed

## Data

2026-04-23

## Contexto

O NexTraceOne possui hoje uma `Ingestion.Api` (porta 8082) que recebe dados de deployments, runtime signals, contratos, custos e incidentes. O processamento desses dados segue o seguinte fluxo actual:

```
Ingestion API → Domain Handler (MediatR) → PostgreSQL (Outbox)
                                                    ↓ (5s cycles)
                                          In-process event handlers

OTel Collector → Elasticsearch / ClickHouse (telemetria raw)
```

Este modelo tem limitações estruturais identificadas por análise comparativa com o Dynatrace OpenPipeline (Abril 2026):

### Problemas observados

1. **Pipeline global, não por tenant** — todas as regras de masking, sampling, routing e retenção estão hardcoded no `otel-collector.yaml`. Um tenant não consegue configurar as suas próprias políticas.

2. **Routing estático** — todos os dados vão para o mesmo destino (Elasticsearch ou ClickHouse). Não existe routing condicional por tipo de dado, por tenant, ou por política de retenção.

3. **Sem Dead Letter Queue** — o `ModuleOutboxProcessorJob` tenta 5 vezes e descarta. Mensagens exaustas perdem-se sem possibilidade de reprocessamento ou análise post-mortem.

4. **Sem observabilidade da ingestão** — não existem métricas de throughput, latência ou taxa de falha por tenant ou por fonte de ingestão.

5. **Enriquecimento não cruza com Service Catalog** — spans e logs chegam ao storage sem contexto do serviço (owner, criticidade, SLO tier, environment) — contexto que existe no `Catalog` mas nunca é injectado nos dados de observabilidade.

6. **Sem transformação Log → Metric** — só existe trace → metric via `spanmetrics`. Um log com `status=ERROR` ou `duration_ms=450` não pode gerar métricas sintéticas on-the-fly.

7. **Kafka como stub** — `KafkaConsumerWorker` existe mas não tem dispatcher; todo o processamento async é in-process (outbox → in-memory handlers), limitando a capacidade de escalar horizontalmente.

### Referência de mercado

O Dynatrace **OpenPipeline** resolve exactamente estes problemas com uma arquitectura de três fases:
`Routing Layer → Processing Stages (Masking, Parsing, Enrichment, Transformation, Routing) → Storage Buckets`.

A diferença decisiva não é o número de features — é a **configurabilidade por tenant**: cada cliente define o seu pipeline sem alterar código da plataforma.

---

## Decisão

Implementar um **Server-Side Ingestion Pipeline** configurável por tenant no NexTraceOne, composto por seis componentes independentes implementados faseadamente:

### Componente 1 — Dead Letter Queue (DLQ)

Nova entidade `DeadLetterMessage` em cada `DbContext` que tem outbox:

```csharp
// DeadLetterMessage.cs
public class DeadLetterMessage
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string MessageType { get; private set; }
    public string Payload { get; private set; }         // JSON original
    public string FailureReason { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset ExhaustedAt { get; private set; }
    public DateTimeOffset? ReprocessedAt { get; private set; }
    public DlqMessageStatus Status { get; private set; } // Pending | Reprocessing | Resolved | Discarded
}
```

O `ModuleOutboxProcessorJob` move para DLQ (em vez de descartar) após esgotar retries. Novo endpoint `POST /api/v1/admin/dlq/{id}/reprocess` permite reprocessamento manual.

### Componente 2 — Ingestion Observability

Métricas de ingestão expostas via OpenTelemetry (sem polling, sem cron):

```
nextraceone.ingestion.messages.received{tenant_id, source_type, data_type}
nextraceone.ingestion.messages.processed{tenant_id, source_type, data_type}
nextraceone.ingestion.messages.failed{tenant_id, source_type, failure_reason}
nextraceone.ingestion.processing.duration{tenant_id, source_type} (histograma)
nextraceone.ingestion.dlq.depth{tenant_id}
```

Registadas em `IngestRuntimeSnapshotFeature` e equivalentes via `IMeter` do .NET. Visíveis no próprio `nextraceone-obs-*` (auto-observabilidade).

### Componente 3 — TenantPipelineRule (motor de pipeline configurável)

Nova entidade no módulo `Integrations`:

```csharp
public class TenantPipelineRule
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public PipelineStage Stage { get; private set; }
    // Masking | Filtering | Enrichment | Transformation | Routing
    public string Matcher { get; private set; }          // expressão CEL/DQL sobre os dados
    public string ProcessorDefinition { get; private set; } // JSON com a lógica do processor
    public int Order { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
}

public enum PipelineStage { Masking, Filtering, Enrichment, Transformation, Routing }
```

Motor de execução `TenantPipelineEngine` (serviço singleton):

```csharp
public interface ITenantPipelineEngine
{
    Task<PipelineResult> ProcessAsync(
        TenantId tenantId,
        TelemetryRecord record,
        CancellationToken ct);
}
```

Executa as regras ordenadas por `Stage → Order` em cada ingestão. Cache das regras por tenant com TTL de 60s (invalidada quando um tenant actualiza as suas regras via API).

### Componente 4 — StorageBucket com retenção por tenant

Substituição das políticas de retenção hardcoded:

```csharp
public class StorageBucket
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public StorageBackend Backend { get; private set; }  // Elasticsearch | ClickHouse | Both
    public string? IndexPattern { get; private set; }    // ex: "logs-audit-{tenant}"
    public RetentionPolicy Retention { get; private set; }
    public BucketCondition? Condition { get; private set; } // null = default bucket
}

public record RetentionPolicy(int Days, bool Compress, bool Archive);
```

Um tenant pode ter múltiplos buckets: `audit` (2555 dias), `debug` (3 dias), `default` (90 dias). O `TenantPipelineEngine` decide em que bucket cada record aterra via regras `PipelineStage.Routing`.

### Componente 5 — Service Catalog Enrichment

Novo processor `CatalogEnrichmentProcessor` (implementa `IPipelineProcessor`):

- Para cada span/log/metric recebido, resolve `service.name` → `ServiceEntry` no `Catalog`
- Injesta atributos: `nextraceone.service.owner`, `nextraceone.service.criticality`, `nextraceone.service.slo_tier`, `nextraceone.service.environment`
- Usa `IServiceCatalogReader` (cross-module via interface) com cache in-memory por `service.name` + TTL de 5 minutos
- Degradação graciosa: se o serviço não existe no Catalog, o record passa sem enriquecimento (sem erro)

### Componente 6 — Log → Metric Transformation

Novo processor `LogToMetricProcessor`:

```json
// Exemplo de ProcessorDefinition para TenantPipelineRule (Stage = Transformation)
{
  "type": "log_to_metric",
  "matcher": "body contains 'ERROR'",
  "metric_name": "app.errors.count",
  "metric_type": "counter",
  "attributes_from": ["service.name", "http.route"]
}
```

```json
{
  "type": "log_to_metric",
  "matcher": "attributes['duration_ms'] exists",
  "metric_name": "app.request.duration",
  "metric_type": "histogram",
  "value_from": "attributes['duration_ms']",
  "attributes_from": ["service.name", "http.method"]
}
```

Métricas sintéticas produzidas são injectadas no pipeline de métricas existente — chegam ao mesmo destino que as métricas OTel normais.

---

## Migração

### Fase 1 (semanas 1–2)
1. Adicionar `DeadLetterMessage` a todos os DbContexts que têm outbox (migration EF Core).
2. Actualizar `ModuleOutboxProcessorJob` para mover para DLQ em vez de descartar.
3. Adicionar métricas de ingestão (`IMeter`) nos 21 handlers de ingestão existentes.

### Fase 2 (semanas 3–5)
4. Criar entidades `TenantPipelineRule` e `StorageBucket` no módulo `Integrations` (migration EF Core).
5. Implementar `TenantPipelineEngine` com suporte inicial a `Masking` e `Filtering`.
6. Expor API de gestão de regras: `GET/POST/PUT/DELETE /api/v1/pipeline/rules`.
7. Migrar regras globais actuais do `otel-collector.yaml` para regras default por tenant.

### Fase 3 (semanas 6–8)
8. Implementar `StorageBucket` routing no engine.
9. Migrar políticas de retenção hardcoded para `StorageBucket` default por tenant (backward compatible — comportamento igual ao actual enquanto não configurado).
10. Implementar `CatalogEnrichmentProcessor`.

### Fase 4 (semanas 9–11)
11. Implementar `LogToMetricProcessor`.
12. Activar `KafkaConsumerWorker` dispatcher para os handlers de ingestão existentes.
13. Dashboard de saúde da pipeline por tenant no frontend.

A cada fase, o comportamento para tenants sem regras configuradas é **idêntico ao actual** — zero breaking changes.

---

## Consequências

### Positivas

- **Diferenciação enterprise** — pipeline configurável por tenant é o que separa uma plataforma de observabilidade de um simples "colector + dashboard".
- **Compliance** — masking profiles por tenant permite clientes em regimes regulatórios diferentes (GDPR, LGPD, HIPAA) coexistirem na mesma instância.
- **Flexibilidade de retenção** — clientes com audit requirements (7 anos) e clientes de baixo custo (7 dias) podem usar a mesma plataforma.
- **Resiliência** — DLQ elimina perda silenciosa de dados; reprocessamento manual fecha o ciclo de recovery.
- **Auto-observabilidade** — métricas de ingestão permitem ao operador da plataforma detectar degradação por tenant antes que o cliente reporte.

### Negativas

- **Complexidade operacional** aumenta — o motor de pipeline é um novo componente crítico que precisa de testes extensivos.
- **Latência de ingestão** aumenta ligeiramente — cada record passa pelo motor antes de ser gravado (mitigar com cache de regras).
- **Surface de API nova** — regras de pipeline são um modelo de dados não trivial para documentar e versionar.
- **Curva de aprendizagem** — tenants precisam de compreender o modelo de stages e matchers para tirar partido.

### Neutras

- Os 21 handlers de ingestão existentes não mudam a sua assinatura — o motor é invocado como decorator.
- O `otel-collector.yaml` mantém as regras globais actuais como fallback enquanto o motor não estiver em produção.
- Configuração de Kafka permanece opcional (`DEG-09` mantém o padrão `NullKafkaEventProducer`).

---

## Critérios de aceite

- [ ] `DeadLetterMessage` persistida em todos os DbContexts com outbox; `ModuleOutboxProcessorJob` nunca descarta silenciosamente.
- [ ] Endpoint `POST /api/v1/admin/dlq/{id}/reprocess` funcional com autorização `platform:admin`.
- [ ] 5 métricas de ingestão registadas e visíveis no próprio Elasticsearch/ClickHouse da instância.
- [ ] `TenantPipelineRule` CRUD com validação (matcher CEL válido, stage enum válido, order único por tenant+stage).
- [ ] `TenantPipelineEngine` executa stages em ordem determinística; cache invalida em < 5s após update de regra.
- [ ] `StorageBucket` com routing condicional; tenants sem buckets configurados têm comportamento idêntico ao actual.
- [ ] `CatalogEnrichmentProcessor` injesta atributos de serviço; degrada graciosamente quando serviço não existe no Catalog.
- [ ] `LogToMetricProcessor` suporta `counter` e `histogram`; métricas chegam ao storage de métricas existente.
- [ ] Zero breaking changes para tenants sem regras configuradas.
- [ ] Testes unitários do motor: ordenação de stages, cache invalidation, degradação graciosa.
- [ ] i18n em pt-PT, pt-BR, en, es para todas as strings de UI de gestão de pipeline.
- [ ] `HONEST-GAPS.md` actualizado com `PIP-01..PIP-06` como fechados quando cada fase concluir.

---

## Referências

- [ADR-003: Elasticsearch as Observability Provider](./003-elasticsearch-observability.md)
- [ADR-001: Modular Monolith](./001-modular-monolith.md)
- [HONEST-GAPS.md](../HONEST-GAPS.md)
- [INGESTION-PIPELINE-IMPLEMENTATION.md](../INGESTION-PIPELINE-IMPLEMENTATION.md)
- [docs/onprem/WAVE-04-AI-LOCAL.md](../onprem/WAVE-04-AI-LOCAL.md)
- Dynatrace OpenPipeline — https://docs.dynatrace.com/docs/platform/openpipeline

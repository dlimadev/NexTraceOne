# NexTraceOne — Ingestion Pipeline: Plano de Implementação

> **Versão:** 1.0 — Abril 2026
> **ADR de referência:** [ADR-010](./adr/010-server-side-ingestion-pipeline.md)
> **Motivação:** Fechar os 6 gaps identificados por análise comparativa com o Dynatrace OpenPipeline.

---

## Visão geral

O objectivo é transformar o pipeline de ingestão actual (linear, global, sem configurabilidade por tenant) num motor de stream-processing configurável equivalente ao Dynatrace OpenPipeline.

### Estado actual

```
OTel Collector ──────────────────────────────────► Elasticsearch / ClickHouse
                 (regras globais hardcoded)

Ingestion API → MediatR Handler → PostgreSQL Outbox → In-process handlers
                                  (5 retries, depois descarta silenciosamente)
```

### Estado alvo

```
Ingestion API / OTel Collector
        │
        ▼
TenantPipelineEngine  (regras por tenant, cache 60s)
  Stage 1: Masking     ──── redacta PII per tenant profile
  Stage 2: Filtering   ──── descarta records sem valor por regra
  Stage 3: Enrichment  ──── injeta contexto do Service Catalog
  Stage 4: Transform   ──── log → metric, normalização de campos
  Stage 5: Routing     ──── decide StorageBucket (índice + retenção)
        │
        ├──► StorageBucket "audit"   (2555 dias, Elasticsearch)
        ├──► StorageBucket "debug"   (3 dias, ClickHouse)
        └──► StorageBucket "default" (90 dias, Elasticsearch)

Outbox (falhas) ──► DeadLetterMessage (reprocessamento manual)

Métricas de ingestão ──► mesmo Elasticsearch/ClickHouse (auto-observabilidade)
```

---

## Gap 1 — Dead Letter Queue

**Problema:** O `ModuleOutboxProcessorJob` tenta 5 vezes e depois descarta silenciosamente. Dados perdem-se sem rastreio.

### Entidade

```csharp
// src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Outbox/DeadLetterMessage.cs
public class DeadLetterMessage
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string MessageType { get; private set; }
    public string Payload { get; private set; }
    public string FailureReason { get; private set; }
    public string? LastException { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset ExhaustedAt { get; private set; }
    public DateTimeOffset? ReprocessedAt { get; private set; }
    public DlqMessageStatus Status { get; private set; }

    public static DeadLetterMessage From(OutboxMessage msg, string exception) { ... }
    public void MarkReprocessing() { ... }
    public void MarkResolved() { ... }
    public void MarkDiscarded(string reason) { ... }
}

public enum DlqMessageStatus { Pending, Reprocessing, Resolved, Discarded }
```

### Alteração no Job

```csharp
// ModuleOutboxProcessorJob.cs — substituir o bloco de descarte:
// ANTES:
_logger.LogError("Message {Id} exhausted after {N} retries. Discarding.", msg.Id, msg.RetryCount);

// DEPOIS:
var dlq = DeadLetterMessage.From(msg, lastException);
await context.Set<DeadLetterMessage>().AddAsync(dlq, ct);
await context.SaveChangesAsync(ct);
_logger.LogWarning("Message {Id} moved to DLQ after {N} retries.", msg.Id, msg.RetryCount);
```

### API de reprocessamento

```
POST /api/v1/admin/dlq/{id}/reprocess   → autorização: platform:admin
GET  /api/v1/admin/dlq?tenantId=&status= → lista paginada
PUT  /api/v1/admin/dlq/{id}/discard     → descarte manual documentado
```

### Ficheiros a criar/alterar

| Ficheiro | Acção |
|---|---|
| `BuildingBlocks.Infrastructure/Outbox/DeadLetterMessage.cs` | Criar |
| `BuildingBlocks.Infrastructure/Outbox/DlqMessageStatus.cs` | Criar |
| `BuildingBlocks.Infrastructure/Jobs/ModuleOutboxProcessorJob.cs` | Alterar |
| Migration EF Core em cada DbContext com outbox | Criar |
| `NexTraceOne.ApiHost/Endpoints/DlqEndpoints.cs` | Criar |

---

## Gap 2 — Ingestion Observability

**Problema:** Não existem métricas de throughput, latência ou falhas por tenant/fonte. Impossível detectar degradação antes que o cliente reporte.

### Métricas a registar

```csharp
// src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Telemetry/IngestionMetrics.cs
public sealed class IngestionMetrics
{
    private readonly Counter<long> _received;
    private readonly Counter<long> _processed;
    private readonly Counter<long> _failed;
    private readonly Histogram<double> _duration;
    private readonly ObservableGauge<long> _dlqDepth;

    public const string MeterName = "NexTraceOne.Ingestion";

    public IngestionMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName);
        _received  = meter.CreateCounter<long>("nextraceone.ingestion.messages.received");
        _processed = meter.CreateCounter<long>("nextraceone.ingestion.messages.processed");
        _failed    = meter.CreateCounter<long>("nextraceone.ingestion.messages.failed");
        _duration  = meter.CreateHistogram<double>("nextraceone.ingestion.processing.duration", "ms");
        // _dlqDepth via CreateObservableGauge com callback ao repositório
    }

    public void RecordReceived(string tenantId, string sourceType, string dataType)
        => _received.Add(1, new("tenant_id", tenantId), new("source_type", sourceType), new("data_type", dataType));

    public void RecordProcessed(string tenantId, string sourceType, double durationMs)
    {
        _processed.Add(1, new("tenant_id", tenantId), new("source_type", sourceType));
        _duration.Record(durationMs, new("tenant_id", tenantId), new("source_type", sourceType));
    }

    public void RecordFailed(string tenantId, string sourceType, string reason)
        => _failed.Add(1, new("tenant_id", tenantId), new("source_type", sourceType), new("failure_reason", reason));
}
```

Injectar `IngestionMetrics` como singleton nos 21 handlers de ingestão via decorator ou directamente no handler command.

### Ficheiros a criar/alterar

| Ficheiro | Acção |
|---|---|
| `BuildingBlocks.Infrastructure/Telemetry/IngestionMetrics.cs` | Criar |
| `BuildingBlocks.Infrastructure/DependencyInjection.cs` | Registar `IngestionMetrics` |
| `Ingestion.Api/Endpoints/*.cs` (21 endpoints) | Injectar e chamar `IngestionMetrics` |

---

## Gap 3 — TenantPipelineRule (motor configurável)

**Problema:** Regras de masking, filtering e sampling estão hardcoded no `otel-collector.yaml`. Um tenant não consegue configurar o seu próprio pipeline.

### Modelo de dados

```csharp
// src/modules/integrations/NexTraceOne.Integrations.Domain/Pipeline/TenantPipelineRule.cs
public class TenantPipelineRule : AggregateRoot
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public PipelineStage Stage { get; private set; }
    public string Name { get; private set; }
    public string Matcher { get; private set; }           // expressão CEL: "attributes['env'] == 'prod'"
    public string ProcessorDefinition { get; private set; } // JSON tipado por Stage
    public int Order { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
}

public enum PipelineStage
{
    Masking      = 1,
    Filtering    = 2,
    Enrichment   = 3,
    Transformation = 4,
    Routing      = 5
}
```

### Motor de execução

```csharp
// src/modules/integrations/NexTraceOne.Integrations.Application/Pipeline/TenantPipelineEngine.cs
public interface ITenantPipelineEngine
{
    Task<PipelineResult> ProcessAsync(TenantId tenantId, TelemetryRecord record, CancellationToken ct);
}

public class TenantPipelineEngine : ITenantPipelineEngine
{
    // Cache das regras por tenant — invalida após 60s ou quando regra é actualizada
    private readonly IMemoryCache _rulesCache;
    private readonly ITenantPipelineRuleRepository _repo;
    private readonly IEnumerable<IPipelineProcessor> _processors;

    public async Task<PipelineResult> ProcessAsync(TenantId tenantId, TelemetryRecord record, CancellationToken ct)
    {
        var rules = await GetRulesAsync(tenantId, ct);
        var result = PipelineResult.From(record);

        foreach (var stage in Enum.GetValues<PipelineStage>().OrderBy(s => (int)s))
        {
            var stageRules = rules.Where(r => r.Stage == stage && r.IsEnabled).OrderBy(r => r.Order);
            foreach (var rule in stageRules)
            {
                if (!MatcherEvaluator.Evaluate(rule.Matcher, result.Record))
                    continue;

                var processor = _processors.FirstOrDefault(p => p.Handles(stage, rule.ProcessorDefinition));
                if (processor is null) continue;

                result = await processor.ApplyAsync(result, rule.ProcessorDefinition, ct);
                if (result.IsFiltered) return result; // filtrado — não prossegue
            }
        }

        return result;
    }
}
```

### Processors (um por tipo de regra)

```
IPipelineProcessor
  ├── RegexMaskingProcessor      (Stage: Masking)
  ├── AttributeFilterProcessor   (Stage: Filtering)
  ├── CatalogEnrichmentProcessor (Stage: Enrichment)  ← Gap 5
  ├── LogToMetricProcessor       (Stage: Transformation) ← Gap 6
  └── BucketRoutingProcessor     (Stage: Routing)     ← Gap 4
```

### API de gestão

```
GET    /api/v1/pipeline/rules              → lista regras do tenant autenticado
POST   /api/v1/pipeline/rules              → criar regra
PUT    /api/v1/pipeline/rules/{id}         → actualizar regra
DELETE /api/v1/pipeline/rules/{id}         → remover regra
POST   /api/v1/pipeline/rules/{id}/enable  → activar
POST   /api/v1/pipeline/rules/{id}/disable → desactivar
POST   /api/v1/pipeline/rules/test         → testar matcher contra payload de exemplo
```

### Ficheiros a criar

| Ficheiro | Acção |
|---|---|
| `Integrations.Domain/Pipeline/TenantPipelineRule.cs` | Criar |
| `Integrations.Domain/Pipeline/PipelineStage.cs` | Criar |
| `Integrations.Application/Pipeline/TenantPipelineEngine.cs` | Criar |
| `Integrations.Application/Pipeline/IPipelineProcessor.cs` | Criar |
| `Integrations.Application/Pipeline/Processors/RegexMaskingProcessor.cs` | Criar |
| `Integrations.Application/Pipeline/Processors/AttributeFilterProcessor.cs` | Criar |
| `Integrations.Application/Pipeline/Matchers/MatcherEvaluator.cs` | Criar |
| `Integrations.Infrastructure/Pipeline/TenantPipelineRuleRepository.cs` | Criar |
| Migration EF Core em `IntegrationsDbContext` | Criar |
| `Ingestion.Api/Endpoints/PipelineRuleEndpoints.cs` | Criar |

---

## Gap 4 — StorageBucket com retenção por tenant

**Problema:** Retenção hardcoded: 7 dias (minuto), 90 dias (hora), 180 dias (anomalias). Um tenant de sector financeiro que precisa de 7 anos de audit logs e um startup que quer pagar o mínimo têm o mesmo comportamento.

### Modelo de dados

```csharp
// src/modules/integrations/NexTraceOne.Integrations.Domain/Pipeline/StorageBucket.cs
public class StorageBucket : AggregateRoot
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; }              // "audit", "debug", "default"
    public StorageBackend Backend { get; private set; }   // Elasticsearch | ClickHouse | Both
    public string? IndexPattern { get; private set; }     // "logs-{tenant}-audit"
    public RetentionPolicy Retention { get; private set; }
    public bool IsDefault { get; private set; }
}

public record RetentionPolicy(int Days, bool Compress, bool Archive);
public enum StorageBackend { Elasticsearch, ClickHouse, Both }
```

### BucketRoutingProcessor

```json
// ProcessorDefinition (PipelineStage.Routing):
{
  "type": "bucket_routing",
  "conditions": [
    { "matcher": "attributes['log.source'] == 'audit'", "bucket": "audit" },
    { "matcher": "attributes['log.level'] == 'DEBUG'",  "bucket": "debug" }
  ],
  "default_bucket": "default"
}
```

O `BucketRoutingProcessor` resolve o `StorageBucket` e anota o record com `nextraceone.storage.bucket` antes de ser gravado. O exporter lê este atributo para escolher o índice/tabela de destino.

### Migração da retenção actual

Os valores actuais hardcoded tornam-se o bucket `default` criado automaticamente para cada tenant no registo. Zero breaking changes.

---

## Gap 5 — Service Catalog Enrichment

**Problema:** Spans e logs chegam ao storage sem contexto do serviço (owner, SLO tier, criticidade). Impossível fazer queries como "todos os erros em serviços críticos" sem joins caros.

### CatalogEnrichmentProcessor

```csharp
// src/modules/integrations/NexTraceOne.Integrations.Application/Pipeline/Processors/CatalogEnrichmentProcessor.cs
public class CatalogEnrichmentProcessor : IPipelineProcessor
{
    private readonly IServiceCatalogReader _catalog;  // cross-module via interface
    private readonly IMemoryCache _cache;

    public async Task<PipelineResult> ApplyAsync(PipelineResult result, string definition, CancellationToken ct)
    {
        var serviceName = result.Record.GetAttribute("service.name");
        if (string.IsNullOrEmpty(serviceName)) return result;

        var entry = await _cache.GetOrCreateAsync($"catalog:{serviceName}", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _catalog.FindByNameAsync(serviceName, ct);
        });

        if (entry is null) return result; // degrada graciosamente

        result.Record.SetAttribute("nextraceone.service.owner",       entry.Owner);
        result.Record.SetAttribute("nextraceone.service.criticality",  entry.Criticality.ToString());
        result.Record.SetAttribute("nextraceone.service.slo_tier",     entry.SloTier.ToString());
        result.Record.SetAttribute("nextraceone.service.environment",  entry.Environment);

        return result;
    }
}
```

`IServiceCatalogReader` é uma interface em `Catalog.Application` exposta como cross-module reader — segue o mesmo padrão de `IChangeIntelligenceReader` já existente.

---

## Gap 6 — Log → Metric Transformation

**Problema:** Logs com `status=ERROR` ou `duration_ms=450` não geram métricas. Apenas `spanmetrics` (trace → metric) existe, e só no OTel Collector.

### LogToMetricProcessor

```csharp
// src/modules/integrations/NexTraceOne.Integrations.Application/Pipeline/Processors/LogToMetricProcessor.cs
```

Suporta duas definições:

```json
// Counter — contar ocorrências
{
  "type": "log_to_metric",
  "metric_name": "app.errors.count",
  "metric_type": "counter",
  "attributes_from": ["service.name", "http.route", "error.type"]
}

// Histogram — distribuição de valores numéricos
{
  "type": "log_to_metric",
  "metric_name": "app.request.duration",
  "metric_type": "histogram",
  "value_from": "attributes['duration_ms']",
  "attributes_from": ["service.name", "http.method"],
  "buckets": [5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000]
}
```

As métricas geradas são enviadas para o handler `IngestOtelMetrics` existente no módulo `Governance` — reutilizando todo o pipeline de métricas já implementado.

---

## Gap 7 (Bónus) — Kafka Dispatcher

**Problema:** `KafkaConsumerWorker` consome mensagens mas não as entrega a nenhum handler. É um stub com `TODO`.

### Dispatcher

```csharp
// src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Kafka/KafkaMessageDispatcher.cs
public class KafkaMessageDispatcher
{
    private readonly IPublisher _mediator;
    private readonly ILogger<KafkaMessageDispatcher> _logger;

    public async Task DispatchAsync(ConsumeResult<string, string> message, CancellationToken ct)
    {
        var command = KafkaCommandFactory.Create(message.Topic, message.Message.Value);
        if (command is null)
        {
            _logger.LogWarning("No handler for Kafka topic {Topic}", message.Topic);
            return;
        }
        await _mediator.Publish(command, ct);
    }
}
```

`KafkaCommandFactory` mapeia topics para commands MediatR — os mesmos commands já existentes nos handlers de ingestão.

---

## Plano de fases

| Fase | Gaps | Duração estimada | Dependências |
|---|---|---|---|
| **Fase 1** | Gap 1 (DLQ) + Gap 2 (Métricas) | 2 semanas | Nenhuma — fundação |
| **Fase 2** | Gap 3 (Motor + Masking + Filtering) | 3 semanas | Fase 1 |
| **Fase 3** | Gap 4 (StorageBucket + Routing) | 2 semanas | Fase 2 |
| **Fase 4** | Gap 5 (Catalog Enrichment) | 2 semanas | Fase 2 + `IServiceCatalogReader` |
| **Fase 5** | Gap 6 (Log → Metric) | 2 semanas | Fase 2 + `IngestOtelMetrics` handler |
| **Fase 6** | Gap 7 (Kafka Dispatcher) | 1 semana | Fase 2 |

**Total estimado:** 12 semanas para paridade completa com OpenPipeline.

---

## Critérios de "done" por fase

### Fase 1
- [ ] `DeadLetterMessage` com migration em todos os DbContexts com outbox
- [ ] `ModuleOutboxProcessorJob` nunca descarta silenciosamente
- [ ] Endpoint de reprocessamento funcional
- [ ] 5 métricas visíveis no Elasticsearch/ClickHouse da própria instância

### Fase 2
- [ ] `TenantPipelineRule` CRUD com validação
- [ ] Motor executa Masking e Filtering por tenant
- [ ] Cache invalida em < 5s após update
- [ ] Endpoint `/pipeline/rules/test` funcional
- [ ] Regras globais actuais do `otel-collector.yaml` migradas como defaults por tenant

### Fase 3
- [ ] `StorageBucket` com 3 buckets default por tenant (audit/debug/default)
- [ ] Routing condicional funcional
- [ ] Políticas de retenção por bucket configuráveis
- [ ] Tenants sem buckets configurados têm comportamento idêntico ao actual

### Fase 4
- [ ] `IServiceCatalogReader` exposto pelo módulo Catalog
- [ ] `CatalogEnrichmentProcessor` injeta 4 atributos
- [ ] Degrada graciosamente quando serviço não existe no Catalog
- [ ] Cache de 5 minutos por `service.name`

### Fase 5
- [ ] `LogToMetricProcessor` suporta `counter` e `histogram`
- [ ] Métricas sintéticas chegam ao handler `IngestOtelMetrics` existente
- [ ] Visíveis nos dashboards de métricas existentes

### Fase 6
- [ ] `KafkaConsumerWorker` despacha para handlers MediatR existentes
- [ ] `KafkaCommandFactory` mapeável via config (sem hardcode de topics)
- [ ] Continua opcional (`DEG-09` mantém `NullKafkaEventProducer` como padrão)

---

## Referências

- [ADR-010: Server-Side Ingestion Pipeline](./adr/010-server-side-ingestion-pipeline.md)
- [ADR-001: Modular Monolith](./adr/001-modular-monolith.md)
- [ADR-003: Elasticsearch as Observability Provider](./adr/003-elasticsearch-observability.md)
- [HONEST-GAPS.md](./HONEST-GAPS.md)
- [docs/onprem/INDEX.md](./onprem/INDEX.md)
- Dynatrace OpenPipeline Docs — https://docs.dynatrace.com/docs/platform/openpipeline

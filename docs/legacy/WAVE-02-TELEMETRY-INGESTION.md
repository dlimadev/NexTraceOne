# Onda 2 — Ingestão de Telemetria Legacy

> **Duração estimada:** 4-6 semanas
> **Dependências:** Onda 1
> **Risco:** Alto — dependência de fontes externas reais para teste end-to-end
> **Referência:** [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)

---

## Objetivo

Permitir que o NexTraceOne ingira telemetria de fontes mainframe — logs operacionais, métricas, eventos, e traces onde suportado — usando OpenTelemetry Collector como backbone e suportando múltiplas estratégias de ingestão.

---

## Entregáveis

- [ ] Novos endpoints na Ingestion API: `POST /batch/events`, `POST /mq/events`, `POST /mainframe/events`
- [ ] Parsers básicos: `SmfRecordParser`, `SyslogParser` (para formato JSON normalizado)
- [ ] Normalização semântica de eventos legacy
- [ ] Novas tabelas ClickHouse para eventos operacionais mainframe
- [ ] Templates de configuração OTel Collector para fontes legacy
- [ ] Background job para processamento assíncrono de payloads legacy
- [ ] Documentação de configuração do Collector
- [ ] UI para status de ingestão legacy

---

## Estratégia de Ingestão Híbrida

```
┌────────────────────────────────────────────────────────────────┐
│                 FONTES DE TELEMETRIA LEGACY                    │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  Z Common Data Provider ──┐                                    │
│  OMEGAMON Data Provider ──┼─→ OTel Collector ─→ NexTraceOne   │
│  z/OS Connect telemetry ──┘   (receivers)       (Ingestion)   │
│                                                                │
│  SMF Records ─────────────┐                                    │
│  SYSLOG ──────────────────┼─→ Log Forwarder ─→ OTel Collector │
│  Job Logs ────────────────┘   (Filebeat /       (filelog       │
│                                Fluent Bit)       receiver)     │
│                                                                │
│  CICS Statistics ─────────┐                                    │
│  CICS TG Events ──────────┼─→ CICS Event    ─→ OTel Collector │
│  IMS Statistics ──────────┘   Processing                       │
│                                                                │
│  MQ Statistics ───────────┐                                    │
│  MQ Accounting ───────────┼─→ MQ Exporter   ─→ NexTraceOne    │
│  MQ Events ───────────────┘   (custom/IBM)      (Ingestion)   │
│                                                                │
│  Manual Import ───────────┐                                    │
│  CSV/Excel Upload ────────┼─→ NexTraceOne UI ─→ Application   │
│  API Bulk Sync ───────────┘   (Frontend)         Layer         │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

---

## Impacto Backend

### Novos Endpoints — Ingestion API

#### `POST /api/v1/batch/events`

```csharp
public sealed record BatchEventRequest(
    string? Provider,              // "CA7", "TWS", "ControlM", "JES2", "manual"
    string? CorrelationId,
    string? JobName,               // Nome do job
    string? JobId,                 // ID JES
    string? StepName,              // Step atual/final
    string? ProgramName,           // Programa executado
    string? ReturnCode,            // Return code (e.g., "0000", "0004", "ABEND S0C7")
    string? Status,                // "started", "completed", "failed", "abended"
    string? SystemName,            // Sistema mainframe
    string? LparName,              // LPAR de execução
    DateTimeOffset? StartedAt,     // Início da execução
    DateTimeOffset? CompletedAt,   // Fim da execução
    long? DurationMs,              // Duração em milissegundos
    string? ChainName,             // Chain/schedule group
    Dictionary<string, string>? Metadata
);
```

#### `POST /api/v1/mq/events`

```csharp
public sealed record MqEventRequest(
    string? Provider,              // "IBM MQ", "OMEGAMON", "manual"
    string? CorrelationId,
    string? QueueManagerName,
    string? QueueName,
    string? ChannelName,
    string? EventType,             // "depth_threshold", "dlq_message", "channel_status", "statistics"
    int? QueueDepth,
    int? MaxDepth,
    long? EnqueueCount,
    long? DequeueCount,
    string? ChannelStatus,
    DateTimeOffset? EventTimestamp,
    Dictionary<string, string>? Metadata
);
```

#### `POST /api/v1/mainframe/events`

```csharp
public sealed record MainframeEventRequest(
    string? Provider,              // "Z_CDP", "OMEGAMON", "SYSLOG", "SMF", "manual"
    string? CorrelationId,
    string? SourceType,            // "smf", "syslog", "cics_stat", "ims_stat", "operational"
    string? SystemName,
    string? LparName,
    string? EventType,
    string? Message,
    string? Severity,              // "info", "warning", "error", "critical"
    DateTimeOffset? EventTimestamp,
    Dictionary<string, string>? Metadata
);
```

### Parsers

| Parser | Input | Output |
|---|---|---|
| `SmfRecordParser` | JSON normalizado de SMF record | Evento semântico normalizado |
| `SyslogParser` | Linha de SYSLOG z/OS | Evento semântico normalizado |
| `BatchEventParser` | `BatchEventRequest` | `BatchExecutionEvent` canónico |
| `MqEventParser` | `MqEventRequest` | `MqOperationalEvent` canónico |
| `MainframeEventParser` | `MainframeEventRequest` | `MainframeOperationalEvent` canónico |

**Nota:** Os parsers de SMF e SYSLOG assumem entrada em **formato JSON pré-normalizado**, não formato binário raw. O processamento de SMF binário real é responsabilidade de ferramentas IBM (Z CDP, OMEGAMON) que exportam em JSON/OTLP.

### Normalização Semântica

Modelo canónico para eventos legacy:

```csharp
public sealed record NormalizedLegacyEvent(
    string EventId,
    string EventType,               // "batch_execution", "mq_statistics", "cics_transaction", etc.
    string SourceType,              // "batch", "mq", "cics", "ims", "mainframe"
    string? SystemName,
    string? LparName,
    string? ServiceName,            // Mapeamento para service catalog
    string? AssetName,              // Nome do ativo específico
    string Severity,
    string? Message,
    DateTimeOffset Timestamp,
    Dictionary<string, string> Attributes
);
```

---

## Impacto Base de Dados

### Novas Tabelas ClickHouse (schema `nextraceone_obs`)

#### `mf_operational_events`

```sql
CREATE TABLE nextraceone_obs.mf_operational_events (
    Timestamp DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimestampDate Date DEFAULT toDate(Timestamp),
    EventId String CODEC(ZSTD(1)),
    EventType LowCardinality(String),
    SourceType LowCardinality(String),
    SystemName LowCardinality(String),
    LparName LowCardinality(String),
    ServiceName String CODEC(ZSTD(1)),
    AssetName String CODEC(ZSTD(1)),
    Severity LowCardinality(String),
    Message String CODEC(ZSTD(1)),
    TenantId String CODEC(ZSTD(1)),
    Attributes Map(LowCardinality(String), String) CODEC(ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY (TenantId, toYYYYMM(TimestampDate))
ORDER BY (TenantId, SystemName, EventType, Timestamp)
TTL TimestampDate + INTERVAL 30 DAY;
```

#### `mf_cics_statistics`

```sql
CREATE TABLE nextraceone_obs.mf_cics_statistics (
    Timestamp DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimestampDate Date DEFAULT toDate(Timestamp),
    RegionName LowCardinality(String),
    TransactionId String CODEC(ZSTD(1)),
    ProgramName String CODEC(ZSTD(1)),
    ResponseTimeMs Float64 CODEC(ZSTD(1)),
    CpuTimeMs Float64 CODEC(ZSTD(1)),
    TransactionCount UInt64 CODEC(ZSTD(1)),
    AbendCount UInt64 CODEC(ZSTD(1)),
    TenantId String CODEC(ZSTD(1)),
    Attributes Map(LowCardinality(String), String) CODEC(ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY (TenantId, toYYYYMM(TimestampDate))
ORDER BY (TenantId, RegionName, TransactionId, Timestamp)
TTL TimestampDate + INTERVAL 30 DAY;
```

#### `mf_ims_statistics`

Estrutura similar a `mf_cics_statistics` com campos específicos IMS.

---

## OTel Collector — Templates de Configuração

### Template para Z Common Data Provider

```yaml
# otel-collector-zcdp.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  attributes/legacy:
    actions:
      - key: nextraceone.source.type
        value: "mainframe"
        action: upsert
      - key: nextraceone.source.provider
        value: "z_cdp"
        action: upsert
  
  redaction/sensitive:
    blocked_values:
      - pattern: '\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b'

exporters:
  otlphttp/nextraceone:
    endpoint: http://nextraceone-ingestion:8082
    headers:
      X-Api-Key: "${NEXTRACEONE_API_KEY}"

service:
  pipelines:
    logs:
      receivers: [otlp]
      processors: [attributes/legacy, redaction/sensitive]
      exporters: [otlphttp/nextraceone]
```

### Template para Log Files (SMF/SYSLOG)

```yaml
# otel-collector-filelog.yaml
receivers:
  filelog/smf:
    include: [/var/log/smf-export/*.json]
    operators:
      - type: json_parser
        timestamp:
          parse_from: attributes.timestamp
          layout: '%Y-%m-%dT%H:%M:%S.%LZ'
  
  filelog/syslog:
    include: [/var/log/zos-syslog/*.log]
    operators:
      - type: regex_parser
        regex: '^(?P<timestamp>\S+)\s+(?P<system>\S+)\s+(?P<message>.+)$'

processors:
  attributes/mainframe:
    actions:
      - key: nextraceone.source.type
        value: "mainframe"
        action: upsert

exporters:
  otlphttp/nextraceone:
    endpoint: http://nextraceone-ingestion:8082/api/v1/mainframe/events

service:
  pipelines:
    logs:
      receivers: [filelog/smf, filelog/syslog]
      processors: [attributes/mainframe]
      exporters: [otlphttp/nextraceone]
```

---

## Impacto Frontend

- Extensão da **Integration Hub** com status de conectores legacy
- Indicador de ingestão legacy na dashboard de integrações
- Configuração de conectores legacy (provider, endpoint, polling interval)

---

## Testes

### Testes Unitários (~60)
- Parsers: SmfRecordParser, SyslogParser, BatchEventParser, MqEventParser
- Normalizers: cada parser produz output correto
- Handlers: processamento de cada tipo de evento
- Validators: validação de inputs

### Testes de Integração (~15)
- Endpoints de ingestão aceitam payloads corretos
- ClickHouse recebe e persiste eventos
- Payloads inválidos retornam erro adequado

---

## Critérios de Aceite

1. ✅ Ingestão via API funcional para batch, MQ e mainframe events
2. ✅ Dados persistidos em ClickHouse com TTL adequado
3. ✅ OTel Collector configs documentados e testáveis
4. ✅ Parsing de SMF/SYSLOG básico funcional (formato JSON)
5. ✅ Normalização semântica produz modelo canónico
6. ✅ Status de ingestão visível na Integration Hub
7. ✅ Rate limiting nos endpoints de ingestão

---

## Riscos

| Risco | Severidade | Mitigação |
|---|---|---|
| Fontes reais indisponíveis para teste E2E | Alta | Mocks realistas com formatos documentados |
| Volume de SMF/SYSLOG pode ser massivo | Alta | ClickHouse com TTL. Rate limiting. Sampling |
| Formatos variam entre versões z/OS | Média | Suportar JSON normalizado como formato canónico |
| OTel Collector config complexa | Média | Templates prontos e documentação detalhada |

---

## Stories

| ID | Story | Prioridade | Estado |
|---|---|---|---|
| W2-S01 | Criar endpoint `POST /api/v1/ingestion/legacy/batch/events` | P1 | ✅ Implementado |
| W2-S02 | Criar endpoint `POST /api/v1/ingestion/legacy/mq/events` | P1 | ✅ Implementado |
| W2-S03 | Criar endpoint `POST /api/v1/ingestion/legacy/mainframe/events` | P1 | ✅ Implementado |
| W2-S04 | Implementar `BatchEventParser` com normalização | P1 | ✅ Implementado |
| W2-S05 | Implementar `MqEventParser` com normalização | P1 | ✅ Implementado |
| W2-S06 | Implementar `MainframeEventParser` com normalização | P1 | ✅ Implementado |
| W2-S07 | Criar modelo canónico `NormalizedLegacyEvent` | P0 | ✅ Implementado |
| W2-S08 | Criar tabelas ClickHouse (`mf_operational_events`, `mf_cics_statistics`, `mf_ims_statistics`) | P1 | ✅ Implementado |
| W2-S09 | Implementar ClickHouse writer para eventos legacy | P1 | ✅ Implementado |
| W2-S10 | Criar `SmfRecordParser` básico (JSON input) | P2 | ✅ Implementado |
| W2-S11 | Criar `SyslogParser` básico | P2 | ✅ Implementado |
| W2-S12 | Criar templates OTel Collector (Z CDP, filelog) | P2 | ⏳ Pendente |
| W2-S13 | Documentar formatos esperados de input | P1 | ✅ Implementado (WAVE-02-INPUT-FORMATS.md) |
| W2-S14 | Extensão da Integration Hub com status legacy | P2 | ⏳ Pendente (frontend) |
| W2-S15 | Criar testes unitários (~60) | P1 | ✅ Implementado (91 testes) |
| W2-S16 | Criar testes de integração (~15) | P2 | ⏳ Pendente |

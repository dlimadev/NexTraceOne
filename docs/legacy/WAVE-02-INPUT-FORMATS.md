# WAVE-02 — Legacy Telemetry Input Format Reference

> **Story:** W2-S13 — Documentar formatos esperados de input
> **Módulo:** Integrations → Legacy Telemetry
> **Referência:** [WAVE-02-TELEMETRY-INGESTION.md](./WAVE-02-TELEMETRY-INGESTION.md)

---

## Visão Geral

Este documento descreve os formatos de entrada aceites pelos endpoints de ingestão de telemetria legacy do NexTraceOne. Todos os payloads são JSON e enviados via HTTP POST. Os parsers normalizam a entrada para o modelo canónico `NormalizedLegacyEvent`.

---

## Endpoints

| Endpoint | Descrição | Parser |
|---|---|---|
| `POST /api/v1/batch/events` | Eventos de execução batch (JES2, CA7, TWS, Control-M) | `BatchEventParser` |
| `POST /api/v1/mq/events` | Eventos MQ (IBM MQ, OMEGAMON) | `MqEventParser` |
| `POST /api/v1/mainframe/events` | Eventos genéricos mainframe (SMF, SYSLOG, CICS, IMS) | `MainframeEventParser` |

Todos os endpoints aceitam um array de eventos no body (máximo **1000** por request).

---

## 1. Batch Events — `POST /api/v1/batch/events`

### Request Body

```json
{
  "events": [
    {
      "provider": "JES2",
      "correlationId": "corr-abc-123",
      "jobName": "PAYROLL1",
      "jobId": "JOB00123",
      "stepName": "STEP01",
      "programName": "PGMPAY01",
      "returnCode": "0000",
      "status": "completed",
      "systemName": "SYS1",
      "lparName": "LPAR01",
      "startedAt": "2026-03-29T08:00:00Z",
      "completedAt": "2026-03-29T08:05:00Z",
      "durationMs": 300000,
      "chainName": "DAILY_PAY",
      "metadata": {
        "scheduler": "TWS",
        "region": "US-EAST"
      }
    }
  ]
}
```

### Campos

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `provider` | string | Não | Fonte do evento: `JES2`, `CA7`, `TWS`, `ControlM`, `manual` |
| `correlationId` | string | Não | ID de correlação externo |
| `jobName` | string | **Sim** | Nome do job batch |
| `jobId` | string | Não | ID JES do job |
| `stepName` | string | Não | Step actual/final |
| `programName` | string | Não | Programa executado |
| `returnCode` | string | Não | Return code (e.g., `0000`, `0004`, `ABEND S0C7`) |
| `status` | string | Não | `started`, `completed`, `failed`, `abended` |
| `systemName` | string | Não | Nome do sistema mainframe |
| `lparName` | string | Não | LPAR de execução |
| `startedAt` | ISO 8601 | Não | Início da execução |
| `completedAt` | ISO 8601 | Não | Fim da execução |
| `durationMs` | long | Não | Duração em milissegundos |
| `chainName` | string | Não | Chain/schedule group |
| `metadata` | object | Não | Pares chave-valor adicionais |

### Regras de Severidade

| Condição | Severidade |
|---|---|
| `status` = `abended` ou `returnCode` contém `ABEND` | `critical` |
| `status` = `failed` ou `returnCode` ≥ `0008` | `error` |
| `returnCode` entre `0001` e `0007` (excluindo `0004`) | `warning` |
| Tudo o resto | `info` |

---

## 2. MQ Events — `POST /api/v1/mq/events`

### Request Body

```json
{
  "events": [
    {
      "provider": "IBM MQ",
      "correlationId": null,
      "queueManagerName": "QMGR01",
      "queueName": "APP.REQ.QUEUE",
      "channelName": null,
      "eventType": "statistics",
      "queueDepth": 10,
      "maxDepth": 1000,
      "enqueueCount": 5000,
      "dequeueCount": 4990,
      "channelStatus": null,
      "eventTimestamp": "2026-03-29T10:00:00Z",
      "metadata": {}
    }
  ]
}
```

### Campos

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `provider` | string | Não | `IBM MQ`, `OMEGAMON`, `manual` |
| `correlationId` | string | Não | ID de correlação externo |
| `queueManagerName` | string | Não | Nome do Queue Manager |
| `queueName` | string | Não | Nome da queue (obrigatório se não for channel event) |
| `channelName` | string | Não | Nome do channel |
| `eventType` | string | Não | `statistics`, `depth_threshold`, `dlq_message`, `channel_status` |
| `queueDepth` | int | Não | Profundidade actual da queue |
| `maxDepth` | int | Não | Profundidade máxima configurada |
| `enqueueCount` | long | Não | Total de mensagens enfileiradas |
| `dequeueCount` | long | Não | Total de mensagens desenfileiradas |
| `channelStatus` | string | Não | `running`, `stopped`, `retrying`, `binding` |
| `eventTimestamp` | ISO 8601 | Não | Timestamp do evento |
| `metadata` | object | Não | Pares chave-valor adicionais |

### Regras de Severidade

| Condição | Severidade |
|---|---|
| `eventType` = `dlq_message` | `error` |
| `queueDepth` / `maxDepth` ≥ 90% | `critical` |
| `queueDepth` / `maxDepth` ≥ 70% | `warning` |
| `channelStatus` = `stopped` ou `retrying` | `warning` |
| Tudo o resto | `info` |

### Mapeamento de EventType

| Input `eventType` | Output `EventType` no modelo canónico |
|---|---|
| `dlq_message` | `mq_dead_letter` |
| `channel_status` | `mq_channel_status` |
| `statistics` | `mq_statistics` |
| `depth_threshold` | `mq_statistics` |
| Outro/null | `mq_operational` |

---

## 3. Mainframe Events — `POST /api/v1/mainframe/events`

### Request Body

```json
{
  "events": [
    {
      "provider": "Z_CDP",
      "correlationId": null,
      "sourceType": "operational",
      "systemName": "SYS1",
      "lparName": "LPAR01",
      "eventType": null,
      "message": "System operational event",
      "severity": "info",
      "eventTimestamp": "2026-03-29T10:30:00Z",
      "metadata": {
        "subsystem": "DB2",
        "version": "12.1"
      }
    }
  ]
}
```

### Campos

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `provider` | string | Não | `Z_CDP`, `OMEGAMON`, `SYSLOG`, `SMF`, `manual` |
| `correlationId` | string | Não | ID de correlação externo |
| `sourceType` | string | Não | `smf`, `syslog`, `cics_stat`, `ims_stat`, `operational` |
| `systemName` | string | Não | Nome do sistema mainframe |
| `lparName` | string | Não | LPAR |
| `eventType` | string | Não | Tipo explícito (overrides default baseado em sourceType) |
| `message` | string | Não | Mensagem do evento (max 10000 caracteres) |
| `severity` | string | Não | `info`, `warning`, `error`, `critical` (case-insensitive) |
| `eventTimestamp` | ISO 8601 | Não | Timestamp do evento |
| `metadata` | object | Não | Pares chave-valor adicionais |

### Mapeamento de SourceType → EventType/SourceType

| Input `sourceType` | Output `EventType` | Output `SourceType` |
|---|---|---|
| `cics_stat` | `cics_statistics` | `cics` |
| `ims_stat` | `ims_statistics` | `ims` |
| `smf` | `mainframe_smf` | `mainframe` |
| `syslog` | `mainframe_syslog` | `mainframe` |
| `operational` ou outro | `mainframe_operational` | `mainframe` |

### Normalização de Severidade

A severidade é normalizada case-insensitively:

| Input | Normalized |
|---|---|
| `info`, `Info`, `INFO`, `information`, `informational` | `info` |
| `warn`, `warning`, `WARNING` | `warning` |
| `error`, `err`, `ERROR` | `error` |
| `critical`, `crit`, `fatal`, `FATAL` | `critical` |
| Qualquer outro / null / vazio | `info` |

---

## 4. SMF Record Parser (JSON Input)

O `SmfRecordParser` aceita registos SMF pré-convertidos em JSON. O NexTraceOne **não** processa formato binário SMF — essa conversão é responsabilidade de ferramentas IBM (Z CDP, OMEGAMON).

### Exemplo de Input

```json
{
  "record_type": "30",
  "system_name": "SYS1",
  "lpar_name": "LPAR01",
  "timestamp": "2026-03-29T10:30:00Z",
  "severity": "info",
  "message": "Program execution completed",
  "program_name": "PGMTEST",
  "cpu_time": 15.3,
  "elapsed_time": 120.5
}
```

### Campos Reconhecidos

| Campo | Alternativa | Descrição |
|---|---|---|
| `record_type` | `smf_type` | Tipo de registo SMF (e.g., `30`, `72`, `89`) |
| `system_name` | `system` | Nome do sistema |
| `lpar_name` | `lpar` | Nome do LPAR |
| `timestamp` | — | ISO 8601 timestamp |
| `severity` | — | Severidade (normalizada) |
| `message` | `description` | Mensagem/descrição |

Campos não reconhecidos são extraídos automaticamente como atributos no modelo canónico.

O `EventType` resultante é `smf_{record_type}` (e.g., `smf_30`).

---

## 5. Syslog Parser

O `SyslogParser` aceita linhas de SYSLOG z/OS no formato:

```
<timestamp> <system_name> <message>
```

### Exemplo

```
2026-03-29T10:00:00Z SYS1 IEF285I BATCH1 ENDED. NAME-PAYROLL01
```

### Regras de Severidade (baseadas no conteúdo da mensagem)

| Padrão na Mensagem | Severidade |
|---|---|
| Contém `ABEND` | `critical` |
| Contém `ERROR` | `error` |
| Tudo o resto | `info` |

### Notas

- Linhas sem formato reconhecível são aceites como fallback, com `SystemName` = null
- O campo `raw_line` nos atributos é truncado a 500 caracteres

---

## Modelo Canónico — `NormalizedLegacyEvent`

Todos os parsers produzem instâncias do modelo canónico:

```csharp
public sealed record NormalizedLegacyEvent(
    string EventId,            // GUID gerado automaticamente
    string EventType,          // e.g., "batch_execution", "mq_statistics"
    string SourceType,         // "batch", "mq", "cics", "ims", "mainframe"
    string? SystemName,
    string? LparName,
    string? ServiceName,       // Mapeamento para service catalog
    string? AssetName,         // Nome do ativo específico
    string Severity,           // "info", "warning", "error", "critical"
    string? Message,
    DateTimeOffset Timestamp,
    Dictionary<string, string> Attributes
);
```

---

## Tabelas ClickHouse

Os eventos normalizados são persistidos nas seguintes tabelas no schema `nextraceone_obs`:

| Tabela | Uso |
|---|---|
| `mf_operational_events` | Todos os eventos legacy normalizados |
| `mf_cics_statistics` | Estatísticas CICS específicas |
| `mf_ims_statistics` | Estatísticas IMS específicas |

Todas as tabelas usam:
- **Particionamento:** `(TenantId, toYYYYMM(TimestampDate))`
- **TTL:** 30 dias
- **Engine:** MergeTree

---

## Limites e Validação

| Regra | Valor |
|---|---|
| Máximo de eventos por request | 1000 |
| Tamanho máximo de `message` (mainframe) | 10000 caracteres |
| `jobName` (batch) | Obrigatório, não vazio |
| Raw syslog line truncation | 500 caracteres |

---

## Referências

- [WAVE-02: Telemetry Ingestion](./WAVE-02-TELEMETRY-INGESTION.md)
- [LEGACY-MAINFRAME-WAVES.md](../LEGACY-MAINFRAME-WAVES.md)
- [DATA-ARCHITECTURE.md](../DATA-ARCHITECTURE.md)
- [INTEGRATIONS-ARCHITECTURE.md](../INTEGRATIONS-ARCHITECTURE.md)

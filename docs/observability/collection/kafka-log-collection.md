# Kafka — Coleta de Logs e Sinais Operacionais

> **Módulo:** Observability › Collection  
> **Fonte de sinais:** Kafka (brokers, producers, consumers)  
> **Integração:** Via OTLP + OTel Collector  
> **Activity Source:** `NexTraceOne.Integrations`

---

## Índice

1. [Objetivo](#objetivo)
2. [Quando usar](#quando-usar)
3. [Fontes de sinais](#fontes-de-sinais)
4. [Sinais relevantes](#sinais-relevantes)
5. [Arquitetura resumida](#arquitetura-resumida)
6. [Correlação](#correlação)
7. [Campos relevantes](#campos-relevantes)
8. [Configuração](#configuração)
9. [Métricas Kafka](#métricas-kafka)
10. [Validação](#validação)
11. [Troubleshooting](#troubleshooting)
12. [Limitações](#limitações)

---

## Objetivo

Coletar sinais operacionais da infraestrutura Kafka para observabilidade
contextualizada no NexTraceOne. A coleta abrange logs de brokers, producers e
consumers, métricas de performance e eventos de comportamento anómalo — tudo
correlacionado com serviços, tópicos, partições e consumer groups.

O Kafka não é monitorizado como um fim em si mesmo. No contexto do NexTraceOne,
os sinais Kafka são correlacionados com:

- **Serviços** que produzem e consomem mensagens
- **Contratos de eventos** definidos no Contract Governance
- **Incidentes** relacionados com falhas de messaging
- **Mudanças** que afetam fluxos de eventos (Change Intelligence)

---

## Quando usar

| Cenário | Recomendação |
|---------|--------------|
| Kafka como backbone de mensagens da arquitetura | ✅ Essencial |
| Contratos de eventos definidos no NexTraceOne | ✅ Correlação automática |
| Monitorização de consumer lag e throughput | ✅ Métricas JMX/Prometheus |
| Deteção de falhas de produção/consumo | ✅ Logs estruturados + alertas |
| Correlação de incidentes com backlog Kafka | ✅ Sinais cruzados |
| Kafka gerido externamente (Confluent, AWS MSK) | ✅ Suportado (com adaptações) |

---

## Fontes de sinais

O NexTraceOne recolhe sinais Kafka a partir de quatro fontes complementares:

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Fontes de Sinais Kafka                       │
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │ Broker Logs  │  │ Producer     │  │ Consumer     │              │
│  │ (JMX/Prom.)  │  │ Logs (OTLP)  │  │ Logs (OTLP)  │              │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘              │
│         │                 │                 │                       │
│  ┌──────┴─────────────────┴─────────────────┴───────┐              │
│  │              OTel Collector                       │              │
│  │  (normalização, redação, correlação, batching)    │              │
│  └──────────────────────┬────────────────────────────┘              │
│                         │                                           │
│  ┌──────────────────────┴────────────────────────────┐              │
│  │           ClickHouse / Elastic                     │              │
│  │  (otel_logs, otel_traces, otel_metrics)            │              │
│  └────────────────────────────────────────────────────┘              │
│                                                                     │
│  ┌──────────────┐                                                   │
│  │ Topic Metrics│  JMX Exporter → Prometheus → OTel Collector       │
│  │ (JMX Export) │                                                   │
│  └──────────────┘                                                   │
└─────────────────────────────────────────────────────────────────────┘
```

### 1. Broker Logs

Logs dos brokers Kafka recolhidos via:
- **JMX Exporter** → Prometheus → OTel Collector (métricas)
- **Filelog Receiver** → OTel Collector (logs de texto)

### 2. Producer Logs

Logs estruturados das aplicações que produzem mensagens, enviados via OTLP
com o activity source `NexTraceOne.Integrations`.

### 3. Consumer Logs

Logs estruturados das aplicações que consomem mensagens, incluindo:
- Eventos de rebalance
- Erros de desserialização
- Timeouts de processamento

### 4. Topic Metrics

Métricas por tópico e partição recolhidas via JMX/Prometheus:
- Throughput (mensagens/segundo)
- Tamanho das mensagens
- Consumer lag (atraso de consumo)

---

## Sinais relevantes

### Sinais de erro e falha

| Sinal | Severidade | Fonte | Descrição |
|-------|------------|-------|-----------|
| **Production failure** | Error | Producer | Mensagem não entregue ao broker |
| **Consumption failure** | Error | Consumer | Erro ao processar mensagem |
| **Deserialization error** | Error | Consumer | Mensagem com formato inválido |
| **Connection timeout** | Warning | Producer/Consumer | Perda de conectividade com broker |
| **Rebalance triggered** | Warning | Consumer | Redistribuição de partições |
| **Broker unavailable** | Critical | Broker | Broker não responde |

### Sinais de degradação

| Sinal | Severidade | Fonte | Descrição |
|-------|------------|-------|-----------|
| **Consumer lag elevado** | Warning | Metrics | Atraso de consumo acima do limiar |
| **Retry excessivo** | Warning | Producer | Muitas retentativas de produção |
| **Throughput anómalo** | Warning | Metrics | Variação significativa no volume |
| **Backlog crescente** | Warning | Metrics | Acumulação de mensagens não processadas |
| **Processing timeout** | Warning | Consumer | Processamento a exceder o timeout |
| **Partition skew** | Info | Metrics | Distribuição desigual entre partições |

### Sinais de comportamento anómalo

| Sinal | Severidade | Fonte | Descrição |
|-------|------------|-------|-----------|
| **Mensagem duplicada** | Info | Consumer | Deteção de idempotência violada |
| **Mensagem fora de ordem** | Warning | Consumer | Offset não sequencial |
| **Tópico sem consumers** | Warning | Metrics | Tópico com produção mas sem consumidores |
| **Consumer group vazio** | Warning | Metrics | Grupo sem membros ativos |

---

## Arquitetura resumida

```
┌─────────────────────────────────────────────────────────────────┐
│  Aplicações NexTraceOne                                         │
│                                                                  │
│  ┌─────────────────┐              ┌─────────────────┐           │
│  │  Serviço A       │   produce   │  Tópico Kafka   │           │
│  │  (Producer)      │────────────▶│  order-events   │           │
│  │  OTel SDK        │             │                  │           │
│  └────────┬─────────┘             └────────┬─────────┘           │
│           │ OTLP                           │                     │
│           │ (traces + logs)                │ consume             │
│           │                                ▼                     │
│           │                       ┌─────────────────┐           │
│           │                       │  Serviço B       │           │
│           │                       │  (Consumer)      │           │
│           │                       │  OTel SDK        │           │
│           │                       └────────┬─────────┘           │
│           │                                │ OTLP                │
│           └──────────┬─────────────────────┘                     │
│                      ▼                                           │
│           ┌──────────────────────┐                               │
│           │  OTel Collector      │                               │
│           │  ┌────────────────┐  │                               │
│           │  │ Correlação:    │  │                               │
│           │  │ service ↔ topic│  │                               │
│           │  │ ↔ consumer_grp │  │                               │
│           │  │ ↔ trace_id     │  │                               │
│           │  └────────────────┘  │                               │
│           └──────────┬───────────┘                               │
│                      ▼                                           │
│           ┌──────────────────────┐                               │
│           │  ClickHouse          │                               │
│           │  (correlação em      │                               │
│           │   query time)        │                               │
│           └──────────────────────┘                               │
└─────────────────────────────────────────────────────────────────┘
```

### Fluxo de correlação

1. **Producer:** Cria span de produção com `messaging.system=kafka`, `messaging.destination=order-events`
2. **Kafka:** Propaga o trace context via headers da mensagem
3. **Consumer:** Cria span de consumo ligado ao span do producer via trace propagation
4. **OTel Collector:** Enriquece com atributos de correlação NexTraceOne
5. **ClickHouse:** Armazena com índices para correlação eficiente em query time

---

## Correlação

A correlação é o aspeto mais importante da monitorização Kafka no NexTraceOne.
Os sinais Kafka devem ser correlacionáveis com todas as dimensões operacionais.

### Dimensões de correlação

| Dimensão | Atributo OpenTelemetry | Exemplo |
|----------|------------------------|---------|
| **Serviço** | `service.name` | `order-service` |
| **Tópico** | `messaging.destination.name` | `order-events` |
| **Partição** | `messaging.kafka.destination.partition` | `3` |
| **Consumer Group** | `messaging.kafka.consumer.group` | `order-processing-group` |
| **Broker** | `messaging.kafka.broker.address` | `kafka-broker-0:9092` |
| **Trace** | `trace_id` | `abc123def456` |
| **Operação** | `messaging.operation` | `publish` / `receive` / `process` |
| **Ambiente** | `deployment.environment` | `production` |

### Correlação com contratos de eventos

No NexTraceOne, os tópicos Kafka correspondem a **contratos de eventos** definidos
no módulo Contract Governance. A correlação permite:

- Identificar que serviço viola um contrato (schema incompatível)
- Rastrear mudanças no schema até ao incidente (Change Intelligence)
- Verificar compatibilidade via `KafkaSchemaCompatibility` (Backward, Forward, Full, etc.)

### Exemplo de correlação completa

```
Incidente: Consumer lag elevado no tópico order-events
│
├── Serviço afetado: order-processing-service
│   └── Consumer group: order-processing-group
│       └── Partições: 0, 1, 2 (lag: 15000, 12000, 18000)
│
├── Causa provável: Mudança recente (Change Intelligence)
│   └── Deploy: order-service v2.4.1 → v2.5.0
│       └── Contrato alterado: order-events (field adicionado)
│           └── Compatibilidade: Backward ✓ (não é a causa)
│
├── Causa real: Erro de desserialização
│   └── Trace: abc123def456
│       └── Span: consumer.process (ERROR)
│           └── Exception: JsonException - unexpected field "priority"
│
└── Mitigação: Atualizar consumer para v2.5.0 do schema
```

---

## Campos relevantes

### Atributos em `LogEntry.Attributes`

Os sinais Kafka são armazenados como `LogEntry` com atributos contextuais no
dicionário `Attributes`. Os campos relevantes para Kafka são:

| Campo (Attribute Key) | Tipo | Descrição | Exemplo |
|------------------------|------|-----------|---------|
| `messaging.system` | `string` | Sistema de messaging | `"kafka"` |
| `messaging.destination.name` | `string` | Nome do tópico | `"order-events"` |
| `messaging.kafka.destination.partition` | `string` | Partição | `"3"` |
| `messaging.kafka.consumer.group` | `string` | Grupo de consumidores | `"order-processing-group"` |
| `messaging.kafka.broker.address` | `string` | Endereço do broker | `"kafka-0:9092"` |
| `messaging.operation` | `string` | Operação | `"publish"`, `"receive"`, `"process"` |
| `messaging.kafka.message.key` | `string` | Chave da mensagem | `"order-42"` |
| `messaging.kafka.message.offset` | `string` | Offset da mensagem | `"125890"` |
| `messaging.batch.message_count` | `string` | Tamanho do batch | `"100"` |

### Mapeamento para o modelo `LogEntry`

```
LogEntry {
  Timestamp:     2024-01-15T10:30:45Z
  Environment:   "production"
  ServiceName:   "order-processing-service"
  Level:         "Error"
  Message:       "Failed to deserialize message from topic order-events"
  Exception:     "System.Text.Json.JsonException: ..."
  TraceId:       "abc123def456"
  SpanId:        "span789"
  CorrelationId: "order-42"
  HostName:      "order-processing-pod-abc12"
  ContainerName: "order-processing"
  Attributes: {
    "messaging.system":                        "kafka",
    "messaging.destination.name":              "order-events",
    "messaging.kafka.destination.partition":    "3",
    "messaging.kafka.consumer.group":          "order-processing-group",
    "messaging.operation":                     "process",
    "messaging.kafka.message.offset":          "125890"
  }
}
```

### Atributos em spans (traces)

Para traces de operações Kafka, os seguintes atributos são definidos nas spans:

```
SpanDetail {
  TraceId:       "abc123def456"
  SpanId:        "span789"
  ParentSpanId:  "span456"           // span do producer (propagação)
  ServiceName:   "order-processing-service"
  OperationName: "order-events process"
  DurationMs:    45
  StatusCode:    "ERROR"
  SpanAttributes: {
    "messaging.system":                     "kafka",
    "messaging.destination.name":           "order-events",
    "messaging.kafka.consumer.group":       "order-processing-group",
    "messaging.kafka.destination.partition": "3",
    "messaging.operation":                  "process"
  }
}
```

---

## Configuração

### Configuração da aplicação (`appsettings.json`)

#### Secção de fontes Kafka

```json
{
  "Telemetry": {
    "Sources": {
      "Kafka": {
        "Enabled": true,
        "CollectBrokerLogs": true,
        "CollectApplicationLogs": true,
        "BrokerAddresses": ["kafka-0:9092", "kafka-1:9092", "kafka-2:9092"],
        "JmxEndpoints": ["kafka-0:9999", "kafka-1:9999", "kafka-2:9999"],
        "MonitoredTopics": ["order-events", "payment-events", "notification-events"],
        "ConsumerGroups": ["order-processing-group", "payment-group"],
        "LagThresholdWarning": 1000,
        "LagThresholdCritical": 10000
      }
    }
  }
}
```

| Parâmetro | Tipo | Default | Descrição |
|-----------|------|---------|-----------|
| `Enabled` | `bool` | `false` | Ativa a coleta de sinais Kafka |
| `CollectBrokerLogs` | `bool` | `true` | Coleta logs dos brokers (via JMX/filelog) |
| `CollectApplicationLogs` | `bool` | `true` | Coleta logs de producers/consumers (via OTLP) |
| `BrokerAddresses` | `string[]` | — | Endereços dos brokers para health check |
| `JmxEndpoints` | `string[]` | — | Endpoints JMX dos brokers para métricas |
| `MonitoredTopics` | `string[]` | — | Tópicos específicos a monitorizar (vazio = todos) |
| `ConsumerGroups` | `string[]` | — | Consumer groups específicos (vazio = todos) |
| `LagThresholdWarning` | `int` | `1000` | Limiar de lag para alerta Warning |
| `LagThresholdCritical` | `int` | `10000` | Limiar de lag para alerta Critical |

### Instrumentação de producers (.NET)

```csharp
using System.Diagnostics;

// Utilizar o activity source NexTraceOne.Integrations para operações Kafka
var activitySource = NexTraceActivitySources.Integrations;

public async Task PublishOrderEventAsync(OrderEvent orderEvent, CancellationToken ct)
{
    using var activity = activitySource.StartActivity(
        "order-events publish",
        ActivityKind.Producer);

    activity?.SetTag("messaging.system", "kafka");
    activity?.SetTag("messaging.destination.name", "order-events");
    activity?.SetTag("messaging.operation", "publish");
    activity?.SetTag("messaging.kafka.message.key", orderEvent.OrderId.ToString());

    try
    {
        // Propagar trace context nos headers da mensagem Kafka
        var headers = new Headers();
        Propagators.DefaultTextMapPropagator.Inject(
            new PropagationContext(activity?.Context ?? default, Baggage.Current),
            headers,
            (h, key, value) => h.Add(key, Encoding.UTF8.GetBytes(value)));

        var message = new Message<string, string>
        {
            Key = orderEvent.OrderId.ToString(),
            Value = JsonSerializer.Serialize(orderEvent),
            Headers = headers
        };

        var result = await _producer.ProduceAsync("order-events", message, ct);

        activity?.SetTag("messaging.kafka.destination.partition", result.Partition.Value.ToString());
        activity?.SetTag("messaging.kafka.message.offset", result.Offset.Value.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);

        _logger.LogInformation(
            "Evento publicado no tópico {Topic}, partição {Partition}, offset {Offset}",
            "order-events", result.Partition.Value, result.Offset.Value);
    }
    catch (ProduceException<string, string> ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);

        _logger.LogError(ex,
            "Falha ao publicar evento no tópico {Topic}: {Error}",
            "order-events", ex.Error.Reason);
        throw;
    }
}
```

### Instrumentação de consumers (.NET)

```csharp
using System.Diagnostics;

var activitySource = NexTraceActivitySources.Integrations;

public async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
{
    // Extrair trace context dos headers da mensagem
    var parentContext = Propagators.DefaultTextMapPropagator.Extract(
        default,
        result.Message.Headers,
        (headers, key) =>
        {
            var header = headers.FirstOrDefault(h => h.Key == key);
            return header != null
                ? new[] { Encoding.UTF8.GetString(header.GetValueBytes()) }
                : Enumerable.Empty<string>();
        });

    using var activity = activitySource.StartActivity(
        "order-events process",
        ActivityKind.Consumer,
        parentContext.ActivityContext);

    activity?.SetTag("messaging.system", "kafka");
    activity?.SetTag("messaging.destination.name", result.Topic);
    activity?.SetTag("messaging.kafka.consumer.group", _consumerGroupId);
    activity?.SetTag("messaging.kafka.destination.partition", result.Partition.Value.ToString());
    activity?.SetTag("messaging.kafka.message.offset", result.Offset.Value.ToString());
    activity?.SetTag("messaging.operation", "process");

    try
    {
        var orderEvent = JsonSerializer.Deserialize<OrderEvent>(result.Message.Value);
        await _handler.HandleAsync(orderEvent!, ct);

        activity?.SetStatus(ActivityStatusCode.Ok);

        _logger.LogInformation(
            "Mensagem processada: tópico {Topic}, partição {Partition}, offset {Offset}, grupo {Group}",
            result.Topic, result.Partition.Value, result.Offset.Value, _consumerGroupId);
    }
    catch (JsonException ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "Deserialization failed");
        activity?.RecordException(ex);

        _logger.LogError(ex,
            "Erro de desserialização: tópico {Topic}, partição {Partition}, offset {Offset}",
            result.Topic, result.Partition.Value, result.Offset.Value);
        throw;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);

        _logger.LogError(ex,
            "Erro ao processar mensagem: tópico {Topic}, partição {Partition}, offset {Offset}",
            result.Topic, result.Partition.Value, result.Offset.Value);
        throw;
    }
}
```

### Configuração do OTel Collector para métricas JMX

Adicionar um receiver Prometheus para fazer scraping das métricas JMX dos brokers:

```yaml
receivers:
  prometheus/kafka:
    config:
      scrape_configs:
        - job_name: 'kafka-brokers'
          scrape_interval: 30s
          static_configs:
            - targets:
                - 'kafka-0:9999'
                - 'kafka-1:9999'
                - 'kafka-2:9999'
          metric_relabel_configs:
            - source_labels: [__name__]
              regex: 'kafka_.*'
              action: keep

# Adicionar à pipeline de métricas
service:
  pipelines:
    metrics:
      receivers: [otlp, hostmetrics, spanmetrics, prometheus/kafka]
      processors: [memory_limiter, resourcedetection, attributes/normalize, batch]
      exporters: [clickhouse]
```

### Configuração do JMX Exporter nos brokers Kafka

```yaml
# kafka-jmx-exporter.yaml — configurar no broker
lowercaseOutputName: true
lowercaseOutputLabelNames: true
rules:
  # Consumer Lag
  - pattern: kafka.server<type=FetcherLagMetrics, name=ConsumerLag, clientId=(.+), topic=(.+), partition=(.+)><>Value
    name: kafka_consumer_lag
    labels:
      client_id: "$1"
      topic: "$2"
      partition: "$3"

  # Messages In Per Second
  - pattern: kafka.server<type=BrokerTopicMetrics, name=MessagesInPerSec, topic=(.+)><>OneMinuteRate
    name: kafka_messages_in_per_second
    labels:
      topic: "$1"

  # Bytes In/Out Per Second
  - pattern: kafka.server<type=BrokerTopicMetrics, name=BytesInPerSec, topic=(.+)><>OneMinuteRate
    name: kafka_bytes_in_per_second
    labels:
      topic: "$1"

  - pattern: kafka.server<type=BrokerTopicMetrics, name=BytesOutPerSec, topic=(.+)><>OneMinuteRate
    name: kafka_bytes_out_per_second
    labels:
      topic: "$1"

  # Request Metrics
  - pattern: kafka.network<type=RequestMetrics, name=TotalTimeMs, request=(.+)><>Mean
    name: kafka_request_total_time_ms
    labels:
      request: "$1"

  # Under-replicated Partitions
  - pattern: kafka.server<type=ReplicaManager, name=UnderReplicatedPartitions><>Value
    name: kafka_under_replicated_partitions

  # ISR Shrinks/Expands
  - pattern: kafka.server<type=ReplicaManager, name=(IsrShrinksPerSec|IsrExpandsPerSec)><>OneMinuteRate
    name: kafka_isr_$1
```

---

## Métricas Kafka

### Métricas de brokers

| Métrica | Descrição | Alerta |
|---------|-----------|--------|
| `kafka_messages_in_per_second` | Throughput de mensagens por tópico | Anomalia se desviar >50% da baseline |
| `kafka_bytes_in_per_second` | Bytes recebidos por tópico | Pico pode indicar flood |
| `kafka_bytes_out_per_second` | Bytes servidos por tópico | Proporcional ao número de consumers |
| `kafka_under_replicated_partitions` | Partições sub-replicadas | Alerta se > 0 |
| `kafka_isr_IsrShrinksPerSec` | Taxa de ISR shrinks | Alerta se persistente |
| `kafka_request_total_time_ms` | Latência de pedidos ao broker | Alerta se > 100ms |

### Métricas de consumers

| Métrica | Descrição | Alerta |
|---------|-----------|--------|
| `kafka_consumer_lag` | Atraso de consumo (mensagens) | Warning > 1000, Critical > 10000 |
| `kafka_consumer_commit_rate` | Taxa de commits de offset | Queda pode indicar consumer parado |
| `kafka_consumer_records_consumed_rate` | Taxa de consumo | Queda indica degradação |
| `kafka_consumer_rebalance_rate` | Taxa de rebalances | Alerta se frequente |

### Métricas derivadas (SpanMetrics)

O connector `spanmetrics` do OTel Collector deriva automaticamente métricas
a partir dos traces de operações Kafka:

| Métrica derivada | Fonte | Dimensões |
|------------------|-------|-----------|
| `traces_spanmetrics_latency_bucket{operation="order-events publish"}` | Producer spans | `service.name`, `messaging.destination.name` |
| `traces_spanmetrics_latency_bucket{operation="order-events process"}` | Consumer spans | `service.name`, `messaging.kafka.consumer.group` |
| `traces_spanmetrics_calls_total{status_code="ERROR"}` | Spans com erro | `service.name`, `messaging.operation` |

### Dashboard de métricas recomendado

```
┌─────────────────────────────────────────────────────────┐
│  Kafka Operations Dashboard                              │
│                                                          │
│  ┌──────────────────┐  ┌──────────────────┐             │
│  │ Consumer Lag      │  │ Throughput/Topic │             │
│  │ por consumer group│  │ msgs/s           │             │
│  └──────────────────┘  └──────────────────┘             │
│                                                          │
│  ┌──────────────────┐  ┌──────────────────┐             │
│  │ Error Rate        │  │ Processing       │             │
│  │ por serviço       │  │ Latency P95      │             │
│  └──────────────────┘  └──────────────────┘             │
│                                                          │
│  ┌──────────────────┐  ┌──────────────────┐             │
│  │ Rebalance Events  │  │ Under-replicated │             │
│  │ timeline          │  │ Partitions       │             │
│  └──────────────────┘  └──────────────────┘             │
└─────────────────────────────────────────────────────────┘
```

---

## Validação

### 1. Verificar que sinais Kafka estão a ser recebidos

```bash
# Verificar métricas de spans com atributos Kafka
curl -s http://otel-collector:8888/metrics | grep 'otelcol_receiver_accepted_spans'

# Procurar spans com messaging.system=kafka no ClickHouse
```

```sql
-- Traces com operações Kafka
SELECT
    ServiceName,
    OperationName,
    SpanAttributes['messaging.destination.name'] AS topic,
    SpanAttributes['messaging.operation'] AS operation,
    Duration,
    StatusCode
FROM nextraceone_obs.otel_traces
WHERE SpanAttributes['messaging.system'] = 'kafka'
ORDER BY Timestamp DESC
LIMIT 20;
```

### 2. Verificar logs Kafka no provider

```sql
-- Logs relacionados com Kafka
SELECT
    Timestamp,
    ServiceName,
    Level,
    Message,
    Attributes['messaging.destination.name'] AS topic,
    Attributes['messaging.kafka.consumer.group'] AS consumer_group
FROM nextraceone_obs.otel_logs
WHERE Attributes['messaging.system'] = 'kafka'
ORDER BY Timestamp DESC
LIMIT 20;
```

### 3. Verificar métricas JMX dos brokers

```bash
# Verificar que o Prometheus receiver está a coletar métricas Kafka
curl -s http://otel-collector:8888/metrics | grep 'kafka_'
```

```sql
-- Métricas Kafka no ClickHouse
SELECT MetricName, Value, Labels
FROM nextraceone_obs.otel_metrics
WHERE MetricName LIKE 'kafka_%'
ORDER BY Timestamp DESC
LIMIT 20;
```

### 4. Verificar correlação trace producer → consumer

```sql
-- Traces com propagação producer → consumer
SELECT
    t1.ServiceName AS producer_service,
    t1.OperationName AS producer_op,
    t2.ServiceName AS consumer_service,
    t2.OperationName AS consumer_op,
    t1.TraceId
FROM nextraceone_obs.otel_traces t1
JOIN nextraceone_obs.otel_traces t2
    ON t1.TraceId = t2.TraceId
WHERE t1.SpanAttributes['messaging.operation'] = 'publish'
  AND t2.SpanAttributes['messaging.operation'] = 'process'
  AND t1.SpanAttributes['messaging.system'] = 'kafka'
ORDER BY t1.Timestamp DESC
LIMIT 10;
```

### 5. Verificar consumer lag

```sql
-- Consumer lag por grupo e tópico
SELECT
    Labels['consumer_group'] AS consumer_group,
    Labels['topic'] AS topic,
    Labels['partition'] AS partition,
    max(Value) AS max_lag
FROM nextraceone_obs.otel_metrics
WHERE MetricName = 'kafka_consumer_lag'
GROUP BY consumer_group, topic, partition
ORDER BY max_lag DESC;
```

---

## Troubleshooting

### Correlação incompleta (trace não propaga do producer ao consumer)

**Sintomas:** Traces de produção e consumo aparecem separados, sem ligação.

**Causas comuns:**

1. **Headers não propagados** — Verificar que o producer injeta trace context nos headers Kafka
2. **Consumer não extrai context** — Verificar que o consumer extrai o `traceparent` dos headers
3. **Serializador custom** — Serializadores personalizados podem perder os headers

**Diagnóstico:**

```csharp
// Verificar se os headers contêm traceparent
foreach (var header in consumeResult.Message.Headers)
{
    _logger.LogDebug("Header: {Key} = {Value}",
        header.Key,
        Encoding.UTF8.GetString(header.GetValueBytes()));
}
// Deve conter: traceparent = 00-<trace_id>-<span_id>-01
```

**Solução:** Garantir que tanto o producer como o consumer utilizam o
`Propagators.DefaultTextMapPropagator` para injetar/extrair o contexto.

### Métricas JMX não aparecem

**Sintomas:** Métricas `kafka_*` não visíveis no ClickHouse.

**Causas comuns:**

1. **JMX Exporter não configurado** — Verificar que o JMX Exporter está ativo nos brokers
2. **Porta JMX bloqueada** — Verificar firewall/network policy para a porta 9999
3. **Receiver Prometheus não configurado** — Verificar que `prometheus/kafka` está na pipeline

**Diagnóstico:**

```bash
# Testar acesso direto ao JMX Exporter
curl -s http://kafka-0:9999/metrics | grep kafka_ | head -10

# Verificar se o receiver está no Collector
curl -s http://otel-collector:8888/metrics | grep prometheus_target
```

### Consumer lag elevado sem causa aparente

**Sintomas:** Lag a crescer mas consumer aparenta estar saudável.

**Diagnóstico:**

```sql
-- Verificar se o consumer está a processar (tem spans recentes)
SELECT count(*), max(Timestamp) AS last_activity
FROM nextraceone_obs.otel_traces
WHERE SpanAttributes['messaging.kafka.consumer.group'] = 'order-processing-group'
  AND SpanAttributes['messaging.operation'] = 'process'
  AND Timestamp > now() - INTERVAL 5 MINUTE;

-- Verificar taxa de erros do consumer
SELECT
    count(*) AS total,
    countIf(StatusCode = 'ERROR') AS errors,
    round(errors / total * 100, 2) AS error_rate
FROM nextraceone_obs.otel_traces
WHERE SpanAttributes['messaging.kafka.consumer.group'] = 'order-processing-group'
  AND Timestamp > now() - INTERVAL 1 HOUR;
```

**Causas comuns:**

1. **Processamento lento** — Verificar latência P95 do consumer
2. **Rebalances frequentes** — Consumers a entrar/sair do grupo
3. **Partições desbalanceadas** — Uma partição com muito mais carga
4. **Recursos insuficientes** — CPU/memória do consumer no limite

### Mensagens duplicadas detetadas

**Diagnóstico:**

```sql
-- Verificar offsets duplicados por partição
SELECT
    SpanAttributes['messaging.destination.name'] AS topic,
    SpanAttributes['messaging.kafka.destination.partition'] AS partition,
    SpanAttributes['messaging.kafka.message.offset'] AS offset,
    count(*) AS process_count
FROM nextraceone_obs.otel_traces
WHERE SpanAttributes['messaging.operation'] = 'process'
  AND SpanAttributes['messaging.system'] = 'kafka'
GROUP BY topic, partition, offset
HAVING process_count > 1
ORDER BY process_count DESC
LIMIT 20;
```

**Solução:** Implementar idempotência no consumer ou usar `exactly-once` semantics
quando disponível.

---

## Limitações

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| **Depende de instrumentação da aplicação** | Sem SDK/instrumentação, não há traces nem logs estruturados | Usar auto-instrumentação .NET ou SDK OTel da linguagem |
| **Acesso JMX necessário para métricas de broker** | Sem JMX, apenas métricas de aplicação disponíveis | Configurar JMX Exporter nos brokers |
| **Kafka gerido (Confluent/MSK)** | JMX pode não estar disponível | Usar métricas nativas do provider (Confluent Cloud Metrics API, CloudWatch) |
| **Trace propagation via headers** | Mensagens sem headers não propagam contexto | Garantir que o produtor inclui headers |
| **Volume de métricas** | Muitos tópicos/partições geram alto volume de métricas | Filtrar tópicos com `MonitoredTopics` |
| **Consumer lag em tempo real** | Lag é calculado com delay do scraping interval (30s) | Reduzir `scrape_interval` se necessário (mín. 10s) |
| **Schema validation** | Validação é responsabilidade do Contract Governance, não do pipeline de coleta | Integrar alertas de schema incompatibility com o pipeline de observabilidade |

---

## Referências internas

- **Activity Source:** [`NexTraceActivitySources.cs`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Tracing/NexTraceActivitySources.cs) — `NexTraceOne.Integrations`
- **Modelo de dados:** [`TelemetryModels.cs`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Models/TelemetryModels.cs) — `LogEntry`, `SpanDetail`
- **Configuração:** [`TelemetryStoreOptions.cs`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Telemetry/Configuration/TelemetryStoreOptions.cs)
- **Compatibilidade Kafka:** [`KafkaSchemaCompatibility.cs`](../../../src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Enums/KafkaSchemaCompatibility.cs)
- **OTel Collector:** [`build/otel-collector/otel-collector.yaml`](../../../build/otel-collector/otel-collector.yaml)
- **Coleta K8s:** [Kubernetes + OTel Collector](./kubernetes-otel-collector.md)

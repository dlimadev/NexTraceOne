# Kubernetes + OpenTelemetry Collector — Coleta Centralizada de Telemetria

> **Módulo:** Observability › Collection  
> **Modo de coleta:** `OpenTelemetryCollector`  
> **Ambiente alvo:** Kubernetes / Contentor  
> **Estratégia:** `OpenTelemetryCollectorStrategy` (`ICollectionModeStrategy`)  
> **Configuração de referência:** [`build/otel-collector/otel-collector.yaml`](../../../build/otel-collector/otel-collector.yaml)

---

## Índice

1. [Objetivo](#objetivo)
2. [Quando usar](#quando-usar)
3. [Quando não usar](#quando-não-usar)
4. [Pré-requisitos](#pré-requisitos)
5. [Arquitetura resumida](#arquitetura-resumida)
6. [Instalação](#instalação)
7. [Configuração](#configuração)
8. [Pipelines](#pipelines)
9. [Processadores](#processadores)
10. [Coleta de logs de pods/containers](#coleta-de-logs-de-podscontainers)
11. [Validação](#validação)
12. [Troubleshooting](#troubleshooting)
13. [Segurança](#segurança)
14. [Desempenho](#desempenho)

---

## Objetivo

Centralizar a coleta, processamento e exportação de traces, métricas e logs de
todos os workloads Kubernetes num único pipeline de observabilidade. O OTel Collector
atua como proxy inteligente entre as aplicações e o provider de armazenamento
(ClickHouse ou Elastic), aplicando normalização, redação de dados sensíveis,
sampling e enriquecimento de contexto.

Este é o **modo padrão** do NexTraceOne (`ActiveMode: "OpenTelemetryCollector"`),
implementado pela classe `OpenTelemetryCollectorStrategy`.

---

## Quando usar

| Cenário | Recomendação |
|---------|--------------|
| Workloads em Kubernetes | ✅ Modo padrão recomendado |
| Ambientes containerizados (Docker, Podman) | ✅ Recomendado |
| Aplicações multi-linguagem (.NET, Java, Go, Python, Node.js) | ✅ Recomendado |
| Ambientes com requisitos de PII redaction | ✅ Pipeline nativa de redação |
| Cenários com alta cardinalidade de serviços | ✅ Sampling e batching centralizados |
| Desenvolvimento local com Docker Compose | ✅ Suportado |

---

## Quando não usar

| Cenário | Alternativa |
|---------|-------------|
| IIS / Windows Server sem contentor | Usar **CLR Profiler** (`ActiveMode: "ClrProfiler"`) |
| Ambientes sem orquestração de contentores | Avaliar CLR Profiler ou instalação standalone do Collector |

---

## Pré-requisitos

1. **Kubernetes cluster** (1.24+) ou Docker Compose para desenvolvimento local
2. **OTel Collector** implantado como DaemonSet, Sidecar ou Deployment
3. **Endpoints OTLP** configurados nas aplicações (gRPC 4317 ou HTTP 4318)
4. **Provider de armazenamento:** ClickHouse (padrão, TCP 9000) ou Elastic
5. **Recursos:** Mínimo 512 MB de memória por instância do Collector

---

## Arquitetura resumida

```
┌──────────────────────────────────────────────────────────┐
│  Kubernetes Cluster                                      │
│                                                          │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐      │
│  │ App .NET │ │ App Java│ │ App Go  │ │ App Node│      │
│  │ OTel SDK │ │ OTel SDK│ │ OTel SDK│ │ OTel SDK│      │
│  └────┬─────┘ └────┬────┘ └────┬────┘ └────┬────┘      │
│       │             │           │            │           │
│       └─────────────┴─────┬─────┴────────────┘           │
│                           │ OTLP (gRPC :4317 / HTTP :4318)│
│                           ▼                               │
│  ┌──────────────────────────────────────────────┐        │
│  │           OTel Collector (DaemonSet)          │        │
│  │                                               │        │
│  │  Receivers:  OTLP, Prometheus, Host Metrics   │        │
│  │  Processors: memory_limiter → batch →         │        │
│  │              resource_detection → normalize →  │        │
│  │              filter → tail_sampling →          │        │
│  │              redaction → transform             │        │
│  │  Exporters:  ClickHouse, SpanMetrics, Debug   │        │
│  │  Extensions: health_check :13133              │        │
│  └──────────────────────┬────────────────────────┘        │
│                         │                                 │
└─────────────────────────┼─────────────────────────────────┘
                          │
            ┌─────────────┼─────────────┐
            ▼             ▼             ▼
   ┌──────────────┐ ┌──────────┐ ┌──────────────┐
   │  ClickHouse  │ │ SpanMet. │ │   Debug      │
   │  TCP :9000   │ │ Connector│ │  (dev only)  │
   └──────────────┘ └──────────┘ └──────────────┘
```

### Padrões de implantação

| Padrão | Quando usar | Vantagens | Desvantagens |
|--------|-------------|-----------|--------------|
| **DaemonSet** | Clusters multi-tenant, muitos pods | Um Collector por nó, eficiente | Configuração partilhada |
| **Sidecar** | Isolamento por serviço necessário | Configuração independente | Mais recursos consumidos |
| **Deployment** | Gateway centralizado | Simples de gerir | Ponto único de falha |

---

## Instalação

### Desenvolvimento local — Docker Compose

O ficheiro `docker-compose.yml` do repositório já inclui o Collector:

```yaml
services:
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    command: ["--config", "/etc/otel-collector-config.yaml"]
    volumes:
      - ./build/otel-collector/otel-collector.yaml:/etc/otel-collector-config.yaml:ro
    ports:
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
      - "13133:13133" # Health check
      - "8888:8888"   # Prometheus metrics do próprio Collector
      - "55679:55679" # z-pages (debugging)
    environment:
      - CLICKHOUSE_ENDPOINT=tcp://clickhouse:9000
    depends_on:
      - clickhouse
```

```bash
# Iniciar o stack completo
docker compose up -d otel-collector clickhouse

# Verificar que o Collector está saudável
curl -s http://localhost:13133/health/status | jq .
```

### Produção — Helm Chart (Kubernetes)

```bash
# Adicionar o repositório Helm do OpenTelemetry
helm repo add open-telemetry https://open-telemetry.github.io/opentelemetry-helm-charts
helm repo update

# Instalar como DaemonSet
helm install otel-collector open-telemetry/opentelemetry-collector \
  --namespace nextraceone-observability \
  --create-namespace \
  --set mode=daemonset \
  --set config.receivers.otlp.protocols.grpc.endpoint="0.0.0.0:4317" \
  --set config.receivers.otlp.protocols.http.endpoint="0.0.0.0:4318" \
  -f values-nextraceone.yaml
```

#### Exemplo de `values-nextraceone.yaml`

```yaml
mode: daemonset

config:
  # Utilizar o conteúdo de build/otel-collector/otel-collector.yaml
  # Ver secção "Configuração" abaixo para detalhes completos

resources:
  limits:
    memory: 1Gi
    cpu: 500m
  requests:
    memory: 512Mi
    cpu: 200m

ports:
  otlp:
    enabled: true
    containerPort: 4317
    servicePort: 4317
    protocol: TCP
  otlp-http:
    enabled: true
    containerPort: 4318
    servicePort: 4318
    protocol: TCP
  health:
    enabled: true
    containerPort: 13133

livenessProbe:
  httpGet:
    path: /health/status
    port: 13133

readinessProbe:
  httpGet:
    path: /health/status
    port: 13133
```

### Produção — Sidecar

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: meu-servico
spec:
  template:
    spec:
      containers:
        - name: app
          image: meu-servico:latest
          env:
            - name: OTEL_EXPORTER_OTLP_ENDPOINT
              value: "http://localhost:4317"
        - name: otel-collector
          image: otel/opentelemetry-collector-contrib:latest
          args: ["--config", "/etc/otel/config.yaml"]
          volumeMounts:
            - name: otel-config
              mountPath: /etc/otel
          ports:
            - containerPort: 4317
            - containerPort: 13133
          resources:
            limits:
              memory: 512Mi
              cpu: 250m
      volumes:
        - name: otel-config
          configMap:
            name: otel-collector-config
```

---

## Configuração

### Configuração da aplicação (`appsettings.json`)

```json
{
  "Telemetry": {
    "CollectionMode": {
      "ActiveMode": "OpenTelemetryCollector",
      "OpenTelemetryCollector": {
        "Enabled": true,
        "OtlpGrpcEndpoint": "http://otel-collector:4317",
        "OtlpHttpEndpoint": "http://otel-collector:4318"
      }
    },
    "Collector": {
      "OtlpGrpcEndpoint": "http://otel-collector:4317",
      "OtlpHttpEndpoint": "http://otel-collector:4318",
      "EnablePrometheusReceiver": false,
      "MemoryLimitMb": 512,
      "MemorySpikeLimitMb": 128,
      "BatchSize": 8192,
      "BatchTimeoutMs": 5000,
      "TracesSamplingRate": 1.0
    },
    "ObservabilityProvider": {
      "Provider": "ClickHouse",
      "ClickHouse": {
        "Enabled": true,
        "ConnectionString": "Host=clickhouse;Port=8123;Database=nextraceone_obs;Username=default;Password=",
        "Database": "nextraceone_obs",
        "LogsRetentionDays": 30,
        "TracesRetentionDays": 30,
        "MetricsRetentionDays": 90
      }
    }
  },
  "OpenTelemetry": {
    "ServiceName": "NexTraceOne",
    "Endpoint": "http://otel-collector:4317"
  }
}
```

### Configuração do OTel Collector (`build/otel-collector/otel-collector.yaml`)

A configuração de referência está em `build/otel-collector/otel-collector.yaml`.
Segue uma descrição detalhada de cada secção.

#### Receivers

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: "0.0.0.0:4317"        # Ingestão principal — gRPC
      http:
        endpoint: "0.0.0.0:4318"        # Ingestão alternativa — HTTP

  prometheus:                             # Scraping de métricas Prometheus
    config:
      scrape_configs:
        - job_name: 'otel-collector'
          scrape_interval: 30s
          static_configs:
            - targets: ['localhost:8888']

  hostmetrics:                            # Métricas do nó (CPU, memória, disco, rede)
    collection_interval: 60s
    scrapers:
      cpu: {}
      memory: {}
      disk: {}
      network: {}
```

| Receiver | Porta | Protocolo | Finalidade |
|----------|-------|-----------|------------|
| OTLP gRPC | 4317 | gRPC | Ingestão principal de traces, métricas e logs |
| OTLP HTTP | 4318 | HTTP/protobuf | Alternativa quando gRPC não é viável |
| Prometheus | 8888 | HTTP | Scraping de métricas do próprio Collector |
| Host Metrics | — | Local | CPU, memória, disco e rede do nó |

#### Exporters

```yaml
exporters:
  clickhouse:
    endpoint: "${CLICKHOUSE_ENDPOINT}"   # tcp://clickhouse:9000
    database: nextraceone_obs
    ttl: 720h                             # 30 dias de retenção
    logs_table_name: otel_logs
    traces_table_name: otel_traces
    metrics_table_name: otel_metrics
    retry_on_failure:
      enabled: true
      initial_interval: 5s
      max_interval: 30s
      max_elapsed_time: 120s

  otlp/spanmetrics:
    endpoint: "${PRODUCT_STORE_OTLP_ENDPOINT:-localhost:4317}"
    tls:
      insecure: true

  debug:
    verbosity: basic                      # Apenas desenvolvimento
```

#### Connectors

```yaml
connectors:
  spanmetrics:
    histogram:
      explicit:
        buckets:
          - 5ms
          - 10ms
          - 25ms
          - 50ms
          - 100ms
          - 250ms
          - 500ms
          - 1s
          - 2.5s
          - 5s
          - 10s
    dimensions:
      - name: http.method
      - name: http.status_code
      - name: http.route
      - name: rpc.method
      - name: db.system
    resource_metrics_key_attributes:
      - service.name
      - deployment.environment
      - service.namespace
```

O connector `spanmetrics` **deriva métricas automaticamente a partir dos traces**,
calculando latência (histograma) e throughput por operação, sem necessidade de
instrumentação adicional nas aplicações.

---

## Pipelines

A configuração define três pipelines independentes, cada uma com a sua cadeia
de processadores otimizada para o tipo de sinal.

### Pipeline de Traces

```yaml
traces:
  receivers: [otlp]
  processors:
    - memory_limiter
    - resourcedetection
    - attributes/normalize
    - filter/drop_noise
    - transform/correlation
    - redaction
    - tail_sampling
    - batch
  exporters: [clickhouse, spanmetrics]
```

**Fluxo:**

```
OTLP → Proteção OOM → Enriquecimento K8s → Normalização →
  Filtragem (health checks) → Correlação NexTrace →
  Redação PII → Sampling inteligente → Batching → ClickHouse + SpanMetrics
```

### Pipeline de Métricas

```yaml
metrics:
  receivers: [otlp, hostmetrics, spanmetrics]
  processors:
    - memory_limiter
    - resourcedetection
    - attributes/normalize
    - batch
  exporters: [clickhouse, otlp/spanmetrics]
```

**Fontes:**
- **OTLP:** métricas enviadas pelas aplicações
- **Host Metrics:** CPU, memória, disco, rede do nó
- **SpanMetrics:** métricas derivadas dos traces (latência, throughput)

### Pipeline de Logs

```yaml
logs:
  receivers: [otlp]
  processors:
    - memory_limiter
    - resourcedetection
    - attributes/normalize
    - filter/drop_noise
    - transform/correlation
    - redaction
    - batch
  exporters: [clickhouse]
```

**Nota:** A pipeline de logs inclui `redaction` para garantir que dados sensíveis
(PII, tokens) nunca chegam ao provider de armazenamento.

---

## Processadores

### memory_limiter

Proteção contra Out-of-Memory. Rejeita dados quando o consumo se aproxima do limite.

```yaml
memory_limiter:
  limit_mib: 512          # Limite máximo: 512 MB
  spike_limit_mib: 128    # Tolerância para picos: 128 MB
  check_interval: 5s      # Verificação a cada 5 segundos
```

**Comportamento:** Quando o uso de memória ultrapassa `limit_mib - spike_limit_mib`
(384 MB), o processador começa a rejeitar dados. Isto protege o Collector de ser
terminado pelo OOM Killer do Kubernetes.

### batch

Agrupa sinais em lotes para reduzir a sobrecarga de rede e melhorar o throughput.

```yaml
batch:
  send_batch_size: 8192         # Enviar quando acumular 8192 items
  send_batch_max_size: 16384    # Nunca exceder 16384 items por lote
  timeout: 5s                   # Ou enviar a cada 5 segundos (o que acontecer primeiro)
```

### resourcedetection

Enriquece automaticamente todos os sinais com metadados do ambiente de execução.

```yaml
resourcedetection:
  detectors:
    - env               # Variáveis de ambiente (OTEL_RESOURCE_ATTRIBUTES)
    - system            # Hostname, OS
    - docker            # Container ID (quando em Docker)
    - k8snode           # Nome do nó Kubernetes
    - k8spod            # Nome do pod, namespace, labels
```

**Atributos adicionados automaticamente:**

- `host.name`, `os.type`
- `k8s.node.name`, `k8s.pod.name`, `k8s.namespace.name`
- `container.id`
- Variáveis definidas em `OTEL_RESOURCE_ATTRIBUTES`

### attributes/normalize

Normaliza atributos para garantir consistência entre todos os serviços.

```yaml
attributes/normalize:
  actions:
    - key: deployment.environment
      action: upsert
      from_attribute: environment
    - key: service.namespace
      action: upsert
      value: nextraceone
```

### filter/drop_noise

Remove telemetria de baixo valor para reduzir volume e custos.

```yaml
filter/drop_noise:
  error_mode: ignore
  traces:
    span:
      - 'attributes["http.route"] == "/health"'
      - 'attributes["http.route"] == "/healthz"'
      - 'attributes["http.route"] == "/ready"'
      - 'attributes["http.route"] == "/readyz"'
      - 'attributes["http.route"] == "/livez"'
      - 'attributes["http.route"] == "/metrics"'
  logs:
    log_record:
      - 'body == "Health check OK"'
```

### tail_sampling

Sampling inteligente que preserva 100% dos traces com erros ou latência elevada
e aplica amostragem probabilística ao restante.

```yaml
tail_sampling:
  decision_wait: 10s
  policies:
    - name: errors-always
      type: status_code
      status_code:
        status_codes: [ERROR]           # 100% dos erros

    - name: slow-traces
      type: latency
      latency:
        threshold_ms: 2000             # 100% dos traces >2s

    - name: probabilistic-sampling
      type: probabilistic
      probabilistic:
        sampling_percentage: 10        # 10% do restante
```

**Resultado prático:**

| Tipo de trace | Taxa de retenção |
|---------------|------------------|
| Com erro (status ERROR) | 100% |
| Lentos (>2 segundos) | 100% |
| Normais | 10% |

### redaction

Remove dados sensíveis antes do armazenamento — PII, tokens, documentos fiscais.

```yaml
redaction:
  allow_all_keys: true
  blocked_values:
    - '\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b'   # Email
    - '\b\d{3}\.\d{3}\.\d{3}-\d{2}\b'                            # CPF
    - '\b\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}\b'                     # CNPJ
    - '\b(Bearer\s+)?[A-Za-z0-9\-._~+/]+=*\b'                   # Tokens
  summary: debug
```

### transform/correlation

Adiciona atributos de correlação específicos do NexTraceOne.

```yaml
transform/correlation:
  trace_statements:
    - context: span
      statements:
        - set(attributes["nextraceone.pipeline"], "collector")
        - set(attributes["nextraceone.version"], "1.0")
```

---

## Coleta de logs de pods/containers

### Via OTLP (recomendado)

As aplicações enviam logs estruturados diretamente via OTLP usando o SDK do
OpenTelemetry. Este é o método recomendado porque preserva contexto (TraceId,
SpanId, atributos) e permite correlação completa.

```csharp
// .NET — logs automaticamente enviados via OTLP
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.AddOtlpExporter(otlp =>
    {
        otlp.Endpoint = new Uri("http://otel-collector:4317");
        otlp.Protocol = OtlpExportProtocol.Grpc;
    });
});
```

### Via Filelog Receiver (logs stdout/stderr)

Para capturar logs de containers que escrevem para stdout/stderr:

```yaml
receivers:
  filelog/k8s:
    include:
      - /var/log/pods/*/*/*.log
    exclude:
      - /var/log/pods/*/otel-collector/*.log
    start_at: end
    include_file_path: true
    include_file_name: false
    operators:
      - type: router
        routes:
          - output: parse_docker
            expr: 'body matches "^\\{"'
          - output: parse_cri
            expr: 'body matches "^[^ Z]+ "'

      - id: parse_docker
        type: json_parser
        timestamp:
          parse_from: attributes.time
          layout: '%Y-%m-%dT%H:%M:%S.%fZ'

      - id: parse_cri
        type: regex_parser
        regex: '^(?P<time>[^ Z]+) (?P<stream>stdout|stderr) (?P<logtag>[^ ]*) ?(?P<log>.*)$'
        timestamp:
          parse_from: attributes.time
          layout: '%Y-%m-%dT%H:%M:%S.%LZ'

      - type: move
        from: attributes.log
        to: body
```

### Via DaemonSet com hostPath

O Collector como DaemonSet deve montar os logs dos pods:

```yaml
apiVersion: apps/v1
kind: DaemonSet
metadata:
  name: otel-collector
spec:
  template:
    spec:
      containers:
        - name: collector
          image: otel/opentelemetry-collector-contrib:latest
          volumeMounts:
            - name: varlogpods
              mountPath: /var/log/pods
              readOnly: true
      volumes:
        - name: varlogpods
          hostPath:
            path: /var/log/pods
```

---

## Validação

### 1. Health check do Collector

```bash
# Endpoint de saúde (porta 13133)
curl -s http://otel-collector:13133/health/status | jq .
# Resposta esperada:
# {
#   "status": "Server available",
#   "upSince": "2024-01-15T10:00:00Z",
#   "uptime": "24h0m0s"
# }
```

### 2. Métricas do próprio Collector

```bash
# Métricas Prometheus (porta 8888)
curl -s http://otel-collector:8888/metrics | grep otelcol_

# Spans recebidos
curl -s http://otel-collector:8888/metrics | grep otelcol_receiver_accepted_spans

# Spans exportados
curl -s http://otel-collector:8888/metrics | grep otelcol_exporter_sent_spans

# Spans rejeitados (indica problemas)
curl -s http://otel-collector:8888/metrics | grep otelcol_receiver_refused_spans
```

**Métricas chave a monitorizar:**

| Métrica | Significado | Valor ideal |
|---------|-------------|-------------|
| `otelcol_receiver_accepted_spans` | Spans aceites | Crescente |
| `otelcol_receiver_refused_spans` | Spans rejeitados | 0 |
| `otelcol_exporter_sent_spans` | Spans exportados com sucesso | ≈ accepted × sampling_rate |
| `otelcol_exporter_send_failed_spans` | Falhas de exportação | 0 |
| `otelcol_processor_batch_batch_send_size` | Tamanho dos lotes | ≈ 8192 |
| `otelcol_process_memory_rss` | Memória RSS | < 512 MB |

### 3. z-pages (debugging detalhado)

```bash
# Disponível na porta 55679 (apenas em ambientes de desenvolvimento)
curl -s http://otel-collector:55679/debug/tracez | head -50
curl -s http://otel-collector:55679/debug/pipelinez
```

### 4. Verificar dados no ClickHouse

```sql
-- Traces recentes
SELECT ServiceName, OperationName, Duration, StatusCode
FROM nextraceone_obs.otel_traces
ORDER BY Timestamp DESC
LIMIT 10;

-- Métricas recentes
SELECT MetricName, Value, Attributes
FROM nextraceone_obs.otel_metrics
ORDER BY Timestamp DESC
LIMIT 10;

-- Logs recentes
SELECT Timestamp, SeverityText, Body, ServiceName
FROM nextraceone_obs.otel_logs
ORDER BY Timestamp DESC
LIMIT 10;
```

### 5. Validação programática (`OpenTelemetryCollectorStrategy.IsHealthyAsync`)

A estratégia verifica automaticamente:

- Se `Enabled` é `true`
- Se `OtlpGrpcEndpoint` está configurado e acessível
- O `GetExportConfig()` retorna `UsesCollectorProxy = true` e `Protocol = "grpc"`

---

## Troubleshooting

### O Collector não inicia

**Sintomas:** Pod em CrashLoopBackOff ou container a terminar imediatamente.

```bash
# Ver logs do container
kubectl logs -n nextraceone-observability -l app=otel-collector --tail=100

# Verificar eventos do pod
kubectl describe pod -n nextraceone-observability -l app=otel-collector
```

**Causas comuns:**

1. **Erro de configuração YAML** — Validar com `otelcol validate --config=config.yaml`
2. **Porta já em uso** — Outro processo na mesma porta (4317, 4318)
3. **Permissões insuficientes** — RBAC para aceder a metadados K8s
4. **Imagem não encontrada** — Verificar que usa `otel/opentelemetry-collector-contrib`

### Dados não chegam ao provider

**Sintomas:** Collector a funcionar, mas ClickHouse/Elastic sem dados novos.

```bash
# Verificar métricas de exportação
curl -s http://otel-collector:8888/metrics | grep "otelcol_exporter_send_failed"

# Verificar conectividade com ClickHouse
kubectl exec -it otel-collector-pod -- nc -zv clickhouse 9000
```

**Causas comuns:**

1. **ClickHouse indisponível** — Verificar `CLICKHOUSE_ENDPOINT`
2. **Timeout de rede** — Network policies a bloquear tráfego
3. **Tabelas não criadas** — O exporter ClickHouse cria tabelas automaticamente, mas precisa de permissões DDL
4. **Retry esgotado** — `max_elapsed_time: 120s` excedido; dados perdidos

### Problemas de memória (OOM Kill)

**Sintomas:** Pod reinicia frequentemente, logs indicam memory pressure.

```bash
# Verificar uso de memória
kubectl top pod -n nextraceone-observability -l app=otel-collector

# Verificar se memory_limiter está a rejeitar
curl -s http://otel-collector:8888/metrics | grep "otelcol_processor_refused"
```

**Soluções:**

1. Aumentar `memory_limiter.limit_mib` e o resource limit do pod proporcionalmente
2. Reduzir `batch.send_batch_max_size`
3. Aumentar `tail_sampling.policies.probabilistic.sampling_percentage` (reduzir amostra)
4. Ativar filtragem mais agressiva em `filter/drop_noise`

### Sampling não está a funcionar

**Sintomas:** Volume de dados não diminuiu após configurar tail_sampling.

**Causas comuns:**

1. **Processador não incluído na pipeline** — Verificar que `tail_sampling` está na lista de `processors`
2. **decision_wait muito baixo** — Traces incompletos são rejeitados; aumentar para 10-30s
3. **Ordem dos processadores** — `tail_sampling` deve vir antes de `batch`

---

## Segurança

### Redação de PII

O processador `redaction` garante que dados pessoais nunca chegam ao armazenamento:

| Padrão | O que detecta | Exemplo |
|--------|---------------|---------|
| Email | `user@example.com` | `****@****` |
| CPF | `123.456.789-00` | `***.***.***-**` |
| CNPJ | `12.345.678/0001-00` | `**.***.***/**00-**` |
| Bearer Token | `Bearer eyJhbGciOiJ...` | `****` |

### Sanitização de tokens

Tokens de autenticação em headers e atributos são automaticamente removidos pela
configuração de redação.

### Network Policies (Kubernetes)

Restringir o tráfego ao mínimo necessário:

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: otel-collector-policy
  namespace: nextraceone-observability
spec:
  podSelector:
    matchLabels:
      app: otel-collector
  policyTypes:
    - Ingress
    - Egress
  ingress:
    - from:
        - namespaceSelector: {}           # Aceitar de todos os namespaces (aplicações)
      ports:
        - port: 4317                       # OTLP gRPC
        - port: 4318                       # OTLP HTTP
  egress:
    - to:
        - podSelector:
            matchLabels:
              app: clickhouse              # Apenas para ClickHouse
      ports:
        - port: 9000                       # ClickHouse TCP
    - to:
        - namespaceSelector: {}
      ports:
        - port: 53                         # DNS
          protocol: UDP
```

### RBAC para o Collector (K8s metadata)

```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: otel-collector
rules:
  - apiGroups: [""]
    resources: ["pods", "nodes", "namespaces"]
    verbs: ["get", "list", "watch"]
  - apiGroups: ["apps"]
    resources: ["replicasets", "deployments"]
    verbs: ["get", "list", "watch"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: otel-collector
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: otel-collector
subjects:
  - kind: ServiceAccount
    name: otel-collector
    namespace: nextraceone-observability
```

---

## Desempenho

### Dimensionamento recomendado

| Volume de spans/segundo | Instâncias Collector | Memória por instância | CPU por instância |
|--------------------------|----------------------|-----------------------|-------------------|
| < 1.000 | 1 (Deployment) | 512 MB | 250m |
| 1.000 – 10.000 | DaemonSet (1/nó) | 512 MB | 500m |
| 10.000 – 100.000 | DaemonSet + Gateway | 1 GB | 1000m |
| > 100.000 | Gateway com HPA | 2 GB | 2000m |

### Parâmetros de tuning

| Parâmetro | Default | Efeito de aumento | Efeito de redução |
|-----------|---------|-------------------|-------------------|
| `batch.send_batch_size` | 8192 | Menos chamadas, mais latência | Mais chamadas, menos latência |
| `batch.timeout` | 5s | Lotes maiores | Exportação mais rápida |
| `memory_limiter.limit_mib` | 512 | Mais buffer, mais risco OOM | Rejeita mais cedo |
| `tail_sampling.probabilistic.sampling_percentage` | 10 | Mais dados, mais custo | Menos dados, mais economia |
| `hostmetrics.collection_interval` | 60s | Resolução mais fina | Menos sobrecarga |

### Fórmula de estimativa de memória

```
Memória necessária ≈ (spans_por_segundo × decision_wait_seconds × tamanho_médio_span)
                   + (batch_size × tamanho_médio_span)
                   + overhead_base (≈100 MB)
```

**Exemplo:** 5.000 spans/s × 10s wait × 1 KB/span + 8192 × 1 KB + 100 MB ≈ 158 MB

---

## Referências internas

- **Configuração Collector:** [`build/otel-collector/otel-collector.yaml`](../../../build/otel-collector/otel-collector.yaml)
- **Estratégia:** [`OpenTelemetryCollectorStrategy.cs`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Collection/OpenTelemetryCollector/OpenTelemetryCollectorStrategy.cs)
- **Interface:** [`ICollectionModeStrategy`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Abstractions/IObservabilityProvider.cs)
- **Configuração:** [`TelemetryStoreOptions.cs`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Telemetry/Configuration/TelemetryStoreOptions.cs) — secções `CollectionModeOptions` e `CollectorOptions`
- **Registo DI:** [`DependencyInjection.cs`](../../../src/building-blocks/NexTraceOne.BuildingBlocks.Observability/DependencyInjection.cs) — seleção por `ActiveMode`
- **Provider ClickHouse:** [`docs/observability/providers/clickhouse.md`](../providers/clickhouse.md)
- **Alternativa IIS:** [IIS + CLR Profiler](./iis-clr-profiler.md)

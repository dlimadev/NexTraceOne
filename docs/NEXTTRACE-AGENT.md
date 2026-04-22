# NexTrace Agent

O NexTrace Agent é uma distribuição customizada do **OpenTelemetry Collector** construída com o `ocb` (OpenTelemetry Collector Builder). É o equivalente ao Dynatrace OneAgent ou Datadog Agent — um binário único que coleta APM, infraestrutura, databases e messaging sem depender de agentes separados por tecnologia.

---

## 1. Princípios de Design

- **Baseado em OTel Collector**: não construído do zero — reutiliza 100% do ecossistema OTel contrib
- **Binário único**: um executável para Linux, Windows e contêiner Docker
- **Zero-config auto-discovery**: detecta serviços via `k8s_observer` (K8s) e `host_observer` (Linux/Windows)
- **Disk-backed queue**: dados sobrevivem a restart e falhas de rede transitórias
- **Remote config via OpAMP**: configuração atualizada remotamente sem restart do agent
- **Auth nativa**: API Key para o endpoint de ingestion do NexTraceOne

---

## 2. Componentes Customizados

O agent inclui componentes OTel padrão + 3 componentes próprios:

### `nextraceexporter`
- Envia dados OTLP para `https://ingest.nextraceone.io`
- Disk-backed queue com retry automático (exponential backoff)
- Injeta `Authorization: ApiKey <key>` em todos os requests
- Compressão gzip por padrão

### `nextraceprocessor`
- Enriquece spans/métricas/logs com:
  - `nextraceone.agent_version`
  - `nextraceone.deployment_id` (lido de config ou variável de ambiente)
  - `nextraceone.release_id` (lido de deployment annotation em K8s)
  - `nextraceone.host_unit_id` (UUID estável por host, persiste entre restarts)

### `nextraceconfigurator`
- Implementa protocolo **OpAMP** (Open Agent Management Protocol)
- Conecta ao NexTraceOne Server para receber config remota
- Permite ativar/desativar receivers específicos sem restart
- Reporta health e métricas do próprio agent ao servidor

---

## 3. Builder Config (`ocb`)

```yaml
# build/nexttrace-agent/builder-config.yaml
dist:
  name: nexttrace-agent
  description: NexTrace Agent — OTel Collector Distribution
  output_path: ./bin
  otelcol_version: "0.105.0"

extensions:
  - gomod: go.opentelemetry.io/collector/extension/ballastextension v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/extension/healthcheckextension v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/extension/opampextension v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/extension/storage/filestorage v0.105.0

receivers:
  # === HOST / INFRA ===
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/hostmetricsreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/dockerstatsreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/filelogreceiver v0.105.0
  # === WINDOWS ===
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/iisreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/windowsperfcountersreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/windowseventlogreceiver v0.105.0
  # === KUBERNETES ===
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/kubeletstatsreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/k8sclusterreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/k8sobjectsreceiver v0.105.0
  # === DATABASES ===
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/postgresqlreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/sqlserverreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/mysqlreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/mongodbreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/redisreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/cassandrareceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/elasticsearchreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/sqlqueryreceiver v0.105.0
  # === MESSAGING ===
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/kafkametricsreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/rabbitmqreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/jmxreceiver v0.105.0
  # === APM / INBOUND ===
  - gomod: go.opentelemetry.io/collector/receiver/otlpreceiver v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/receiver/prometheusreceiver v0.105.0

processors:
  - gomod: go.opentelemetry.io/collector/processor/batchprocessor v0.105.0
  - gomod: go.opentelemetry.io/collector/processor/memorylimiterprocessor v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/processor/resourcedetectionprocessor v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/processor/k8sattributesprocessor v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/processor/tailsamplingprocessor v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/processor/redactionprocessor v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/processor/filterprocessor v0.105.0
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/processor/transformprocessor v0.105.0
  # Custom:
  - gomod: github.com/nextraceone/nexttrace-agent/processor/nextraceprocessor v0.1.0

exporters:
  - gomod: go.opentelemetry.io/collector/exporter/otlphttpexporter v0.105.0
  - gomod: go.opentelemetry.io/collector/exporter/debugexporter v0.105.0
  # Custom:
  - gomod: github.com/nextraceone/nexttrace-agent/exporter/nextraceexporter v0.1.0

connectors:
  - gomod: github.com/open-telemetry/opentelemetry-collector-contrib/connector/spanmetricsconnector v0.105.0
```

---

## 4. Configuração por Perfil

### Perfil 1: Linux / Kubernetes

```yaml
# config/nexttrace-agent-linux.yaml
extensions:
  file_storage:
    directory: /var/lib/nexttrace-agent/queue
  health_check:
    endpoint: "0.0.0.0:13133"
  opamp:
    server:
      ws:
        endpoint: wss://control.nextraceone.io/v1/opamp
    agent_description:
      identifying_attributes:
        - key: service.name
          value: nexttrace-agent

receivers:
  # Aceita dados das apps instrumentadas
  otlp:
    protocols:
      grpc:
        endpoint: "0.0.0.0:4317"
      http:
        endpoint: "0.0.0.0:4318"

  # Métricas do host
  hostmetrics:
    collection_interval: 30s
    scrapers:
      cpu: {}
      memory: {}
      disk: {}
      network: {}
      load: {}
      filesystem: {}
      process:
        include:
          match_type: regexp
          names: [".*"]

  # Containers Docker
  docker_stats:
    endpoint: unix:///var/run/docker.sock
    collection_interval: 30s

  # Logs de arquivos
  filelog:
    include:
      - /var/log/**/*.log
      - /var/log/syslog
    include_file_path: true
    operators:
      - type: json_parser
        if: 'body matches "^{"'

  # Prometheus scraping (wildcard para qualquer exporter)
  prometheus:
    config:
      scrape_configs:
        - job_name: "nexttrace-autodiscovery"
          scrape_interval: 30s
          file_sd_configs:
            - files: ["/etc/nexttrace-agent/targets/*.yaml"]

processors:
  memory_limiter:
    check_interval: 1s
    limit_mib: 400

  resourcedetection:
    detectors: [env, system, docker, gcp, aws, azure]
    timeout: 2s

  k8sattributes:
    passthrough: false
    extract:
      metadata:
        - k8s.namespace.name
        - k8s.deployment.name
        - k8s.pod.name
        - k8s.node.name

  nextraceprocessor:
    api_key: "${NEXTTRACE_API_KEY}"
    endpoint: "https://ingest.nextraceone.io"

  tail_sampling:
    decision_wait: 10s
    policies:
      - name: errors
        type: status_code
        status_code: {status_codes: [ERROR]}
      - name: slow
        type: latency
        latency: {threshold_ms: 2000}
      - name: sample
        type: probabilistic
        probabilistic: {sampling_percentage: 10}

  redaction:
    allow_all_keys: true
    blocked_values:
      - "\\b\\d{3}\\.\\d{3}\\.\\d{3}-\\d{2}\\b"  # CPF
      - "Bearer\\s+[A-Za-z0-9\\-._~+/]+=*"          # Bearer tokens

  batch:
    timeout: 5s
    send_batch_size: 1000

exporters:
  nextraceexporter:
    endpoint: "https://ingest.nextraceone.io"
    api_key: "${NEXTTRACE_API_KEY}"
    sending_queue:
      enabled: true
      storage: file_storage
      num_consumers: 4
      queue_size: 10000
    retry_on_failure:
      enabled: true
      initial_interval: 5s
      max_interval: 30s
      max_elapsed_time: 300s

service:
  extensions: [health_check, file_storage, opamp]
  pipelines:
    traces:
      receivers: [otlp]
      processors: [memory_limiter, resourcedetection, k8sattributes, nextraceprocessor, tail_sampling, redaction, batch]
      exporters: [nextraceexporter]
    metrics:
      receivers: [otlp, hostmetrics, docker_stats, prometheus]
      processors: [memory_limiter, resourcedetection, k8sattributes, nextraceprocessor, batch]
      exporters: [nextraceexporter]
    logs:
      receivers: [otlp, filelog]
      processors: [memory_limiter, resourcedetection, nextraceprocessor, batch]
      exporters: [nextraceexporter]
```

### Perfil 2: Windows / IIS

```yaml
# config/nexttrace-agent-windows.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: "0.0.0.0:4317"

  hostmetrics:
    collection_interval: 30s
    scrapers:
      cpu: {}
      memory: {}
      disk: {}
      network: {}

  # IIS Application Pools e Sites
  iis:
    collection_interval: 30s

  # Performance Counters nativos do Windows
  windowsperfcounters:
    collection_interval: 30s
    perfcounters:
      - object: "Processor"
        instances: ["*"]
        counters:
          - name: "% Processor Time"
      - object: "ASP.NET Applications"
        instances: ["*"]
        counters:
          - name: "Requests/Sec"
          - name: "Request Execution Time"
          - name: "Errors Total/Sec"
      - object: ".NET CLR Memory"
        instances: ["*"]
        counters:
          - name: "# Bytes in all Heaps"
          - name: "% Time in GC"

  # Event Viewer do Windows
  windows_event_log:
    channel: Application
    operators:
      - type: filter
        expr: 'body.EventID.Value in [1000, 1001, 1002]'  # Crash events
```

---

## 5. Auto-Instrumentação IIS (.NET CLR Profiler)

Para aplicações .NET no IIS sem mudança de código, o NexTrace Agent instala o **OTel .NET Auto-Instrumentation** via CLR Profiler:

```powershell
# install.ps1 — executado pelo NexTrace Agent installer no Windows
param(
    [string]$AppPoolName = "*",
    [string]$ApiKey,
    [string]$Endpoint = "https://ingest.nextraceone.io"
)

$profilerPath = "C:\Program Files\NexTraceAgent\otel-dotnet-auto\OpenTelemetry.AutoInstrumentation.Native.dll"
$managedPath  = "C:\Program Files\NexTraceAgent\otel-dotnet-auto"

# Ativa CLR Profiler nas variáveis de ambiente do IIS Application Pool
$appPools = if ($AppPoolName -eq "*") {
    Get-WebConfiguration "system.applicationHost/applicationPools/add" | Select-Object -ExpandProperty name
} else {
    @($AppPoolName)
}

foreach ($pool in $appPools) {
    $env = @{
        CORECLR_ENABLE_PROFILING   = "1"
        CORECLR_PROFILER           = "{918728DD-259F-4A6A-AC2B-B85E1B658318}"
        CORECLR_PROFILER_PATH      = $profilerPath
        OTEL_DOTNET_AUTO_HOME      = $managedPath
        OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4318"  # Agent local
        OTEL_RESOURCE_ATTRIBUTES   = "nextraceone.api_key=$ApiKey"
    }
    foreach ($key in $env.Keys) {
        Set-WebConfigurationProperty `
            -Filter "system.applicationHost/applicationPools/add[@name='$pool']/environmentVariables" `
            -Name "." `
            -Value @{ name = $key; value = $env[$key] }
    }
}

# Restart dos app pools para activar o profiler
foreach ($pool in $appPools) {
    Restart-WebAppPool -Name $pool
    Write-Host "NexTrace Agent activado no App Pool: $pool"
}
```

O fluxo resultante:

```
IIS App Pool (.NET)
    │ CLR Profiler injectado
    ▼
OTel .NET Auto-Instrumentation
    │ gera spans automáticos (HTTP, SQL, etc.)
    ▼
localhost:4318 (NexTrace Agent)
    │ tail_sampling + redaction + batch
    ▼
https://ingest.nextraceone.io
    │ nextraceexporter (disk-backed queue)
    ▼
NexTraceOne Ingestion API
```

---

## 6. Auto-Instrumentação Kubernetes

### Via OTel Operator

```yaml
# nexttrace-instrumentation.yaml
apiVersion: opentelemetry.io/v1alpha1
kind: Instrumentation
metadata:
  name: nexttrace-auto-instrument
  namespace: nexttrace-system
spec:
  exporter:
    endpoint: http://nexttrace-agent:4317  # DaemonSet local no nó
  propagators:
    - tracecontext
    - baggage
  sampler:
    type: AlwaysOn  # Tail sampling feito no agent, não aqui
  dotnet:
    env:
      - name: OTEL_DOTNET_AUTO_HOME
        value: /otel-auto-instrumentation-dotnet
  java:
    image: ghcr.io/open-telemetry/opentelemetry-operator/autoinstrumentation-java:latest
  nodejs:
    image: ghcr.io/open-telemetry/opentelemetry-operator/autoinstrumentation-nodejs:latest
  python:
    image: ghcr.io/open-telemetry/opentelemetry-operator/autoinstrumentation-python:latest
```

```yaml
# Anotação no Deployment do cliente (apenas isto é necessário):
metadata:
  annotations:
    instrumentation.opentelemetry.io/inject-dotnet: "nexttrace-system/nexttrace-auto-instrument"
```

### DaemonSet do NexTrace Agent

```yaml
apiVersion: apps/v1
kind: DaemonSet
metadata:
  name: nexttrace-agent
  namespace: nexttrace-system
spec:
  selector:
    matchLabels:
      app: nexttrace-agent
  template:
    spec:
      serviceAccountName: nexttrace-agent  # RBAC para k8sattributes
      containers:
        - name: agent
          image: nextraceone/nexttrace-agent:latest
          env:
            - name: NEXTTRACE_API_KEY
              valueFrom:
                secretKeyRef:
                  name: nexttrace-credentials
                  key: api-key
          ports:
            - containerPort: 4317  # OTLP gRPC
            - containerPort: 4318  # OTLP HTTP
            - containerPort: 13133 # Health check
          volumeMounts:
            - name: varlog
              mountPath: /var/log
              readOnly: true
            - name: dockersock
              mountPath: /var/run/docker.sock
            - name: queue-storage
              mountPath: /var/lib/nexttrace-agent/queue
      volumes:
        - name: varlog
          hostPath:
            path: /var/log
        - name: dockersock
          hostPath:
            path: /var/run/docker.sock
        - name: queue-storage
          hostPath:
            path: /var/lib/nexttrace-agent/queue
```

---

## 7. Monitoramento de Databases

### Databases com Receiver Nativo

| Database | Receiver | Métricas Chave |
|---|---|---|
| PostgreSQL | `postgresqlreceiver` | connections, commits, rollbacks, bgwriter, table/index bloat |
| SQL Server | `sqlserverreceiver` | batch requests/sec, page life expectancy, deadlocks, wait stats |
| MySQL | `mysqlreceiver` | threads connected, query cache hits, InnoDB buffer pool, slow queries |
| MongoDB | `mongodbreceiver` | opcounters, connections, replication lag, document ops |
| Redis | `redisreceiver` | keyspace hits/misses, memory used, connected clients, evictions |
| Cassandra | `cassandrareceiver` | read/write latency, compactions, pending tasks, dropped messages |
| Elasticsearch | `elasticsearchreceiver` | indexing rate, search rate, JVM heap, shard allocation |

### Oracle — Solução sem Receiver Nativo

Oracle não tem receiver OTel nativo por restrições de licença do driver JDBC.

**Opção A: `sqlqueryreceiver` + cliente fornece driver**
```yaml
receivers:
  sqlquery/oracle:
    driver: oracle
    datasource: "oracle://user:pass@host:1521/ORCLDB"
    queries:
      - sql: "SELECT metric_name, value FROM v$sysmetric WHERE group_id = 2"
        metrics:
          - metric_name: oracle.sysmetric
            value_column: value
            attribute_columns: [metric_name]
```
O cliente deposita o `ojdbc11.jar` em `/etc/nexttrace-agent/drivers/` e configura o path.

**Opção B: `oracledb_exporter` + `prometheusreceiver`**
```yaml
# docker-compose.yml (cliente executa sidecar)
services:
  oracle-exporter:
    image: iamseth/oracledb_exporter:latest
    environment:
      DATA_SOURCE_NAME: "user/pass@//host:1521/ORCLDB"
    ports:
      - "9161:9161"

# nexttrace-agent config
receivers:
  prometheus:
    config:
      scrape_configs:
        - job_name: oracle
          static_configs:
            - targets: ["localhost:9161"]
```

### Configuração Database no Agent (exemplo SQL Server)

```yaml
receivers:
  sqlserver:
    collection_interval: 60s
    username: nexttrace_monitor  # usuário read-only
    password: "${SQLSERVER_MONITOR_PASSWORD}"
    server: "sqlserver.internal"
    port: 1433
```

---

## 8. Monitoramento de Messaging Systems

### Kafka

```yaml
receivers:
  kafkametrics:
    brokers: ["kafka-1:9092", "kafka-2:9092", "kafka-3:9092"]
    protocol_version: "2.6.0"
    collection_interval: 30s
    scrapers:
      - brokers        # broker metrics (leader count, offline partitions)
      - topics         # topic metrics (partition count, oldest/newest offset)
      - consumers      # consumer group lag (métrica mais crítica)
```

**Métricas disponíveis**: `kafka.brokers`, `kafka.topic.partitions`, `kafka.partition.current_offset`, `kafka.partition.oldest_offset`, `kafka.consumer_group.lag`, `kafka.consumer_group.lag_sum`

### RabbitMQ

```yaml
receivers:
  rabbitmq:
    endpoint: "http://rabbitmq-mgmt:15672"
    username: nexttrace_monitor
    password: "${RABBITMQ_MONITOR_PASSWORD}"
    collection_interval: 30s
```

**Métricas disponíveis**: `rabbitmq.consumer.count`, `rabbitmq.message.acknowledged`, `rabbitmq.message.current` (mensagens na fila), `rabbitmq.message.delivered`, `rabbitmq.message.published`, `rabbitmq.queue.consumers`, `rabbitmq.queue.messages`

### ActiveMQ / IBM MQ (via JMX)

```yaml
receivers:
  jmx:
    jar_path: /opt/opentelemetry-jmx-metrics.jar
    endpoint: "service:jmx:rmi:///jndi/rmi://activemq-host:1099/jmxrmi"
    target_system: activemq
    collection_interval: 30s
```

### NATS (via Prometheus endpoint nativo)

```yaml
receivers:
  prometheus:
    config:
      scrape_configs:
        - job_name: nats
          static_configs:
            - targets: ["nats-server:8222"]
          metrics_path: /varz
```

---

## 9. Estrutura do Repositório

```
nexttrace-agent/
├── builder-config.yaml           # ocb builder manifest
├── cmd/
│   └── nexttrace-agent/
│       └── main.go               # entry point (gerado pelo ocb)
├── processor/
│   └── nextraceprocessor/
│       ├── config.go
│       ├── factory.go
│       └── processor.go          # enriquece com agent_version, host_unit_id
├── exporter/
│   └── nextraceexporter/
│       ├── config.go
│       ├── factory.go
│       └── exporter.go           # disk-backed queue + API key auth
├── extension/
│   └── nextraceconfigurator/
│       ├── config.go
│       ├── factory.go
│       └── extension.go          # OpAMP client
├── config/
│   ├── nexttrace-agent-linux.yaml
│   ├── nexttrace-agent-windows.yaml
│   └── nexttrace-agent-k8s.yaml
├── install/
│   ├── install.sh                # Linux installer
│   ├── install.ps1               # Windows/IIS installer
│   └── helm/                     # Helm chart para K8s
│       ├── Chart.yaml
│       ├── values.yaml
│       └── templates/
│           ├── daemonset.yaml
│           ├── configmap.yaml
│           ├── serviceaccount.yaml
│           └── clusterrole.yaml
├── Makefile                      # `make build`, `make package`, `make release`
└── Dockerfile
```

---

## 10. Build e Release

```makefile
# Makefile
OCB_VERSION = 0.105.0
AGENT_VERSION = $(shell git describe --tags --always)

build-linux:
	ocb --config builder-config.yaml
	GOOS=linux GOARCH=amd64 go build -o bin/nexttrace-agent-linux-amd64 ./cmd/nexttrace-agent

build-windows:
	GOOS=windows GOARCH=amd64 go build -o bin/nexttrace-agent-windows-amd64.exe ./cmd/nexttrace-agent

package-linux:
	tar czf dist/nexttrace-agent-$(AGENT_VERSION)-linux-amd64.tar.gz \
		bin/nexttrace-agent-linux-amd64 \
		config/nexttrace-agent-linux.yaml \
		install/install.sh

package-windows:
	zip dist/nexttrace-agent-$(AGENT_VERSION)-windows.zip \
		bin/nexttrace-agent-windows-amd64.exe \
		config/nexttrace-agent-windows.yaml \
		install/install.ps1

docker:
	docker build -t nextraceone/nexttrace-agent:$(AGENT_VERSION) .
	docker push nextraceone/nexttrace-agent:$(AGENT_VERSION)
```

---

*Ver também: `SAAS-STRATEGY.md`, `SAAS-LICENSING.md`, `SAAS-ROADMAP.md`*

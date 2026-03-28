# ClickHouse — Provider de Observabilidade Padrão

> **Módulo:** Observability · **Provider:** ClickHouse  
> **Papel no produto:** Armazenamento analítico de logs, traces e métricas para o NexTraceOne.

---

## Índice

1. [Objetivo](#objetivo)
2. [Quando usar](#quando-usar)
3. [Quando não usar](#quando-não-usar)
4. [Pré-requisitos](#pré-requisitos)
5. [Arquitetura resumida](#arquitetura-resumida)
6. [Instalação](#instalação)
7. [Configuração](#configuração)
8. [Schema](#schema)
9. [Volume persistente](#volume-persistente)
10. [Health checks](#health-checks)
11. [Bootstrap local](#bootstrap-local)
12. [Retenção](#retenção)
13. [Validação](#validação)
14. [Troubleshooting](#troubleshooting)
15. [Segurança](#segurança)
16. [Desempenho](#desempenho)
17. [Limitações](#limitações)
18. [Próximos passos](#próximos-passos)

---

## Objetivo

O ClickHouse é o **provider padrão de observabilidade** do NexTraceOne. Ele armazena
dados analíticos de telemetria — logs, traces e métricas — provenientes do
OpenTelemetry Collector. O PostgreSQL permanece exclusivo para dados transacionais e
de domínio; o ClickHouse lida exclusivamente com dados de observabilidade de alto
volume.

Esta separação garante que a ingestão de telemetria não impacte a performance das
operações transacionais do produto e permite escalar cada workload de forma independente.

---

## Quando usar

| Cenário | Recomendação |
|---|---|
| Desenvolvimento local | ✅ Padrão — já configurado no `docker-compose.yml` |
| Ambientes de staging | ✅ Recomendado |
| Self-hosted / on-premises | ✅ Recomendado |
| Ambientes com alto volume de telemetria | ✅ Ideal — ClickHouse é otimizado para analytics de alta ingestão |
| Produção sem stack Elastic existente | ✅ Recomendado como provider principal |

---

## Quando não usar

| Cenário | Alternativa |
|---|---|
| Empresa já possui Elastic Stack operacional | Considerar o [provider Elastic](elastic.md) |
| Requisito de SaaS gerido (sem infra própria) | Considerar Elastic Cloud ou outro SaaS |
| Volume extremamente baixo com restrição de recursos | Avaliar se a complexidade adicional compensa |

> **Nota:** Mesmo em ambientes com Elastic, o ClickHouse pode coexistir para
> workloads analíticos específicos.

---

## Pré-requisitos

- **Docker** (≥ 20.10) e **Docker Compose** (≥ 2.0) — para execução via containers
- Ou **ClickHouse Server** (≥ 24.x) instalado nativamente
- **Volume persistente** configurado (nunca usar filesystem efêmero)
- Acesso de rede nas portas **8123** (HTTP) e **9000** (TCP nativo)
- Script de inicialização do schema: `build/clickhouse/init-schema.sql`

---

## Arquitetura resumida

```
┌─────────────────┐     OTLP gRPC/HTTP      ┌──────────────────────┐
│  Serviços do    │ ──────────────────────►  │  OpenTelemetry       │
│  NexTraceOne    │     :4317 / :4318        │  Collector           │
└─────────────────┘                          └──────────┬───────────┘
                                                        │
                                              TCP :9000 │ HTTP :8123
                                                        ▼
                                             ┌──────────────────────┐
                                             │     ClickHouse       │
                                             │   nextraceone_obs    │
                                             │                      │
                                             │  ┌── otel_logs ────┐ │
                                             │  ┌── otel_traces ──┐ │
                                             │  ┌── otel_metrics  ┐ │
                                             │  │  _gauge         │ │
                                             │  │  _sum           │ │
                                             │  │  _histogram     │ │
                                             │  │  _exp_histogram │ │
                                             │  │  _summary       │ │
                                             └──────────────────────┘
                                                        │
                                                        ▼
                                             ┌──────────────────────┐
                                             │  Volume persistente  │
                                             │  clickhouse-data     │
                                             └──────────────────────┘
```

**Fluxo de dados:**

1. Os serviços do NexTraceOne exportam telemetria via OTLP para o Collector.
2. O OTel Collector processa, transforma e envia os dados para o ClickHouse.
3. O ClickHouse armazena na base `nextraceone_obs` em sete tabelas dedicadas.
4. Os dados ficam disponíveis para consulta analítica pelo produto.

**Tabelas:**

| Tabela | Sinal | TTL padrão |
|---|---|---|
| `otel_logs` | Logs | 30 dias |
| `otel_traces` | Traces/Spans | 30 dias |
| `otel_metrics_gauge` | Métricas tipo Gauge | 90 dias |
| `otel_metrics_sum` | Métricas tipo Sum (counters) | 90 dias |
| `otel_metrics_histogram` | Histogramas explícitos | 90 dias |
| `otel_metrics_exponential_histogram` | Histogramas exponenciais | 90 dias |
| `otel_metrics_summary` | Summaries | 90 dias |

> **Nota:** O `otel-collector-contrib` >= v0.100 usa tabelas separadas por tipo de métrica.
> O prefixo (`otel_metrics`) é definido por `metrics_table_name` no `otel-collector.yaml`.

---

## Instalação

### Via Docker Compose (recomendado)

O ClickHouse já está configurado no `docker-compose.yml` do repositório. Basta:

```bash
docker compose up -d clickhouse
```

O container irá:
1. Iniciar o ClickHouse Server 24.8 (Alpine).
2. Executar automaticamente os scripts `build/clickhouse/init-schema.sql` e
   `build/clickhouse/analytics-schema.sql` via `/docker-entrypoint-initdb.d/`.
3. Criar a base `nextraceone_obs` com as tabelas OTEL e a base `nextraceone_analytics`
   com as tabelas de analytics de domínio.

### Instalação standalone

Para ambientes sem Docker:

```bash
# 1. Instalar ClickHouse (Ubuntu/Debian)
sudo apt-get install -y apt-transport-https ca-certificates
curl -fsSL https://packages.clickhouse.com/rpm/lts/repodata/repomd.xml.key | \
  sudo gpg --dearmor -o /usr/share/keyrings/clickhouse-keyring.gpg
echo "deb [signed-by=/usr/share/keyrings/clickhouse-keyring.gpg] \
  https://packages.clickhouse.com/deb stable main" | \
  sudo tee /etc/apt/sources.list.d/clickhouse.list
sudo apt-get update
sudo apt-get install -y clickhouse-server clickhouse-client

# 2. Iniciar o serviço
sudo systemctl enable clickhouse-server
sudo systemctl start clickhouse-server

# 3. Aplicar o schema
clickhouse-client --multiquery < build/clickhouse/init-schema.sql
```

---

## Configuração

### appsettings.json

A configuração do provider ClickHouse está na secção
`Telemetry:ObservabilityProvider`:

```json
{
  "Telemetry": {
    "ObservabilityProvider": {
      "Provider": "ClickHouse",
      "ClickHouse": {
        "Enabled": true,
        "ConnectionString": "Host=clickhouse;Port=8123;Database=nextraceone_obs;Username=default;Password=secret",
        "Database": "nextraceone_obs",
        "LogsRetentionDays": 30,
        "TracesRetentionDays": 30,
        "MetricsRetentionDays": 90
      }
    }
  }
}
```

| Propriedade | Descrição | Valor padrão |
|---|---|---|
| `Provider` | Provider ativo de observabilidade | `"ClickHouse"` |
| `Enabled` | Ativa/desativa o provider | `true` |
| `ConnectionString` | String de ligação ao ClickHouse | Ver formato abaixo |
| `Database` | Nome da base de dados | `"nextraceone_obs"` |
| `LogsRetentionDays` | Retenção de logs em dias | `30` |
| `TracesRetentionDays` | Retenção de traces em dias | `30` |
| `MetricsRetentionDays` | Retenção de métricas em dias | `90` |

### Formato da connection string

```
Host=<hostname>;Port=<porta>;Database=<base>;Username=<user>;Password=<password>
```

**Exemplos:**

```
# Docker Compose (nome do serviço como host)
Host=clickhouse;Port=8123;Database=nextraceone_obs;Username=default;Password=secret

# Localhost (desenvolvimento)
Host=localhost;Port=8123;Database=nextraceone_obs;Username=default;Password=

# Servidor remoto
Host=clickhouse.internal.example.com;Port=8123;Database=nextraceone_obs;Username=nextraceone;Password=S3cur3P@ss!
```

### docker-compose.yml

O serviço ClickHouse no `docker-compose.yml`:

```yaml
clickhouse:
  image: clickhouse/clickhouse-server:24.8-alpine
  restart: unless-stopped
  environment:
    CLICKHOUSE_DB: nextraceone_obs
    CLICKHOUSE_USER: default
    CLICKHOUSE_PASSWORD: ${CLICKHOUSE_PASSWORD:-}
    CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT: 1
  volumes:
    - clickhouse-data:/var/lib/clickhouse
    - ./build/clickhouse/init-schema.sql:/docker-entrypoint-initdb.d/init-schema.sql:ro
  ports:
    - "8123:8123"    # HTTP interface
    - "9000:9000"    # Native protocol
  healthcheck:
    test: ["CMD-SHELL", "clickhouse-client --query 'SELECT 1' || exit 1"]
    interval: 10s
    timeout: 5s
    retries: 5
    start_period: 30s
  networks:
    - nextraceone-net
```

### Variáveis de ambiente

As seguintes variáveis de ambiente permitem sobrescrever a configuração em runtime:

| Variável | Descrição | Exemplo |
|---|---|---|
| `OBSERVABILITY_PROVIDER` | Provider ativo | `ClickHouse` |
| `CLICKHOUSE_HOST` | Hostname do servidor | `clickhouse` |
| `CLICKHOUSE_PORT` | Porta HTTP | `8123` |
| `CLICKHOUSE_DATABASE` | Base de dados | `nextraceone_obs` |
| `CLICKHOUSE_USER` | Utilizador | `default` |
| `CLICKHOUSE_PASSWORD` | Password | *(vazio para dev)* |
| `CLICKHOUSE_LOGS_RETENTION_DAYS` | Retenção de logs | `30` |
| `CLICKHOUSE_TRACES_RETENTION_DAYS` | Retenção de traces | `30` |
| `CLICKHOUSE_METRICS_RETENTION_DAYS` | Retenção de métricas | `90` |

---

## Schema

O schema de inicialização está em:

```
build/clickhouse/init-schema.sql
```

### Base de dados

```sql
CREATE DATABASE IF NOT EXISTS nextraceone_obs;
```

### Tabela `otel_logs`

Armazena logs recebidos via OpenTelemetry.

| Coluna | Tipo | Descrição |
|---|---|---|
| `Timestamp` | `DateTime64(9)` | Timestamp com precisão de nanosegundos |
| `TimestampDate` | `Date` | Data derivada (usada para particionamento e TTL) |
| `TraceId` | `String` | ID do trace associado |
| `SpanId` | `String` | ID do span associado |
| `SeverityText` | `LowCardinality(String)` | Nível de severidade (INFO, WARN, ERROR, etc.) |
| `SeverityNumber` | `Int32` | Código numérico de severidade |
| `ServiceName` | `LowCardinality(String)` | Nome do serviço emissor |
| `Body` | `String` | Conteúdo do log |
| `ResourceAttributes` | `Map(LowCardinality(String), String)` | Atributos do recurso |
| `LogAttributes` | `Map(LowCardinality(String), String)` | Atributos do log |

**Engine:** `MergeTree()` · **Partição:** `toYYYYMM(TimestampDate)` · **Ordem:**
`(ServiceName, SeverityText, Timestamp)` · **TTL:** 30 dias

### Tabela `otel_traces`

Armazena spans de traces distribuídos.

| Coluna | Tipo | Descrição |
|---|---|---|
| `Timestamp` | `DateTime64(9)` | Timestamp de início do span |
| `TraceId` | `String` | ID do trace |
| `SpanId` | `String` | ID do span |
| `ParentSpanId` | `String` | ID do span pai |
| `SpanName` | `LowCardinality(String)` | Nome da operação |
| `SpanKind` | `LowCardinality(String)` | Tipo do span (SERVER, CLIENT, etc.) |
| `ServiceName` | `LowCardinality(String)` | Nome do serviço |
| `Duration` | `Int64` | Duração em nanosegundos |
| `StatusCode` | `LowCardinality(String)` | Código de status (OK, ERROR, UNSET) |
| `Events` | `Nested(...)` | Eventos associados ao span |
| `Links` | `Nested(...)` | Links para outros spans/traces |

**Engine:** `MergeTree()` · **Partição:** `toYYYYMM(TimestampDate)` · **Ordem:**
`(ServiceName, SpanName, Timestamp)` · **TTL:** 30 dias

### Tabela `otel_metrics`

Armazena métricas numéricas.

| Coluna | Tipo | Descrição |
|---|---|---|
| `Timestamp` | `DateTime64(9)` | Timestamp da amostra |
| `MetricName` | `LowCardinality(String)` | Nome da métrica |
| `MetricDescription` | `String` | Descrição da métrica |
| `MetricUnit` | `String` | Unidade (ms, bytes, etc.) |
| `ServiceName` | `LowCardinality(String)` | Nome do serviço |
| `Value` | `Float64` | Valor da métrica |
| `AggregationTemporality` | `LowCardinality(String)` | Temporalidade (CUMULATIVE, DELTA) |
| `IsMonotonic` | `Bool` | Se a métrica é monotónica |

**Engine:** `MergeTree()` · **Partição:** `toYYYYMM(TimestampDate)` · **Ordem:**
`(ServiceName, MetricName, Timestamp)` · **TTL:** 90 dias

### Características comuns

- **Compressão:** Todas as colunas usam `ZSTD(1)`. Timestamps usam `Delta + ZSTD(1)`.
- **Particionamento:** Mensal via `toYYYYMM(TimestampDate)`.
- **TTL automático:** Configurado por tabela com `ttl_only_drop_parts = 1`.
- **Granularidade de índice:** `index_granularity = 8192` (padrão otimizado).

---

## Volume persistente

> ⚠️ **CRÍTICO:** O ClickHouse é stateful. Os dados de observabilidade devem ser
> armazenados em **volume persistente**. Nunca utilizar filesystem efêmero do container.

### Docker Compose

O volume nomeado `clickhouse-data` já está configurado:

```yaml
volumes:
  clickhouse-data:    # Volume nomeado — dados persistem entre restarts
```

O mapeamento no serviço:

```yaml
volumes:
  - clickhouse-data:/var/lib/clickhouse
```

### Kubernetes

Em Kubernetes, usar um `PersistentVolumeClaim`:

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: clickhouse-data
spec:
  accessModes: [ReadWriteOnce]
  resources:
    requests:
      storage: 50Gi
  storageClassName: standard
```

### Consequências de NÃO usar volume persistente

- **Perda total de dados** ao reiniciar o container.
- Necessidade de re-ingestão de toda a telemetria.
- Impossibilidade de análise histórica de mudanças, incidentes e tendências.

---

## Health checks

### Via CLI (utilizado no docker-compose)

```bash
clickhouse-client --query 'SELECT 1'
```

Retorna `1` se o servidor estiver operacional. Saída diferente ou erro indica problema.

### Via HTTP

```bash
# Ping simples
curl -s http://localhost:8123/ping
# Resposta esperada: Ok.

# Query de verificação
curl -s 'http://localhost:8123/?query=SELECT%201'
# Resposta esperada: 1

# Verificar se a base existe
curl -s 'http://localhost:8123/?query=SELECT%20name%20FROM%20system.databases%20WHERE%20name=%27nextraceone_obs%27'
# Resposta esperada: nextraceone_obs
```

### Configuração no docker-compose.yml

```yaml
healthcheck:
  test: ["CMD-SHELL", "clickhouse-client --query 'SELECT 1' || exit 1"]
  interval: 10s
  timeout: 5s
  retries: 5
  start_period: 30s
```

O `start_period: 30s` permite que o ClickHouse complete a inicialização e execute o
script do schema antes do primeiro health check.

---

## Bootstrap local

Guia passo a passo para começar a usar o ClickHouse localmente:

```bash
# 1. Clonar o repositório (se ainda não o fez)
git clone <repo-url> && cd NexTraceOne

# 2. Iniciar apenas o ClickHouse
docker compose up -d clickhouse

# 3. Verificar que o container está healthy
docker compose ps clickhouse
# Estado esperado: healthy

# 4. Verificar que o schema foi aplicado
docker compose exec clickhouse \
  clickhouse-client --query "SHOW TABLES FROM nextraceone_obs"
# Resultado esperado:
#   otel_logs
#   otel_metrics
#   otel_traces

# 5. Iniciar o OTel Collector
docker compose up -d otel-collector

# 6. Iniciar os serviços do NexTraceOne
docker compose up -d apihost

# 7. Verificar ingestão (após gerar tráfego)
docker compose exec clickhouse \
  clickhouse-client --query "SELECT count() FROM nextraceone_obs.otel_logs"
```

---

## Retenção

O NexTraceOne utiliza **TTL nativo do ClickHouse** para gestão automática de retenção.

### Configuração padrão

| Sinal | Retenção | Configuração TTL na tabela |
|---|---|---|
| Logs | 30 dias | `TTL TimestampDate + INTERVAL 30 DAY` |
| Traces | 30 dias | `TTL TimestampDate + INTERVAL 30 DAY` |
| Métricas | 90 dias | `TTL TimestampDate + INTERVAL 90 DAY` |

### Como funciona

1. O ClickHouse avalia o TTL periodicamente (por defeito, a cada hora).
2. Partições inteiras cujos dados excedem o TTL são removidas.
3. A setting `ttl_only_drop_parts = 1` garante que apenas partições completas são
   eliminadas, evitando reescrita parcial de dados.

### Personalizar retenção

Para alterar a retenção, atualizar tanto o `appsettings.json` como o TTL da tabela:

```sql
-- Exemplo: aumentar retenção de logs para 60 dias
ALTER TABLE nextraceone_obs.otel_logs
  MODIFY TTL TimestampDate + INTERVAL 60 DAY;
```

```json
{
  "ClickHouse": {
    "LogsRetentionDays": 60
  }
}
```

### Verificar TTL ativo

```sql
SELECT
    name,
    engine,
    partition_key,
    sorting_key
FROM system.tables
WHERE database = 'nextraceone_obs';
```

---

## Validação

Após configurar o ClickHouse, utilizar as seguintes queries para validar a ingestão:

### Verificar contagem de registos

```sql
-- Logs
SELECT count() AS total_logs FROM nextraceone_obs.otel_logs;

-- Traces
SELECT count() AS total_traces FROM nextraceone_obs.otel_traces;

-- Métricas
SELECT count() AS total_metrics FROM nextraceone_obs.otel_metrics;
```

### Verificar serviços a emitir telemetria

```sql
SELECT DISTINCT ServiceName, count() AS events
FROM nextraceone_obs.otel_logs
GROUP BY ServiceName
ORDER BY events DESC;
```

### Verificar logs recentes

```sql
SELECT
    Timestamp,
    ServiceName,
    SeverityText,
    Body
FROM nextraceone_obs.otel_logs
ORDER BY Timestamp DESC
LIMIT 10;
```

### Verificar traces recentes

```sql
SELECT
    Timestamp,
    ServiceName,
    SpanName,
    Duration / 1000000 AS duration_ms,
    StatusCode
FROM nextraceone_obs.otel_traces
ORDER BY Timestamp DESC
LIMIT 10;
```

### Verificar métricas recentes

```sql
SELECT
    Timestamp,
    ServiceName,
    MetricName,
    Value,
    MetricUnit
FROM nextraceone_obs.otel_metrics
ORDER BY Timestamp DESC
LIMIT 10;
```

### Via curl (sem clickhouse-client)

```bash
curl -s 'http://localhost:8123/?query=SELECT+count()+FROM+nextraceone_obs.otel_logs'
```

---

## Troubleshooting

### Connection refused na porta 8123 ou 9000

**Causa:** O ClickHouse não está em execução ou não está acessível.

```bash
# Verificar estado do container
docker compose ps clickhouse

# Verificar logs do container
docker compose logs clickhouse --tail 50

# Verificar se a porta está a escutar
ss -tlnp | grep -E '8123|9000'
```

### Schema não foi criado automaticamente

**Causa:** O script de init não foi executado (container já tinha dados anteriores).

```bash
# O init script só executa na primeira inicialização.
# Para forçar a criação manual:
docker compose exec clickhouse \
  clickhouse-client --multiquery < build/clickhouse/init-schema.sql

# Ou diretamente:
docker compose exec clickhouse \
  clickhouse-client --query "CREATE DATABASE IF NOT EXISTS nextraceone_obs"
```

### Disco cheio / sem espaço

**Causa:** Volume de dados excede o espaço disponível.

```bash
# Verificar uso de disco
docker compose exec clickhouse \
  clickhouse-client --query "
    SELECT
        database,
        table,
        formatReadableSize(sum(bytes_on_disk)) AS size
    FROM system.parts
    WHERE database = 'nextraceone_obs'
    GROUP BY database, table
    ORDER BY sum(bytes_on_disk) DESC
  "

# Forçar limpeza de TTL
docker compose exec clickhouse \
  clickhouse-client --query "OPTIMIZE TABLE nextraceone_obs.otel_logs FINAL"
```

### Dados não aparecem nas tabelas

**Causa:** OTel Collector não está a enviar dados, ou os dados não correspondem ao
schema.

```bash
# Verificar que o OTel Collector está healthy
docker compose ps otel-collector

# Verificar logs do Collector
docker compose logs otel-collector --tail 50

# Verificar configuração do exporter ClickHouse no Collector
cat build/otel-collector/otel-collector.yaml
```

### Container reinicia constantemente

**Causa:** Possível corrupção de dados ou configuração inválida.

```bash
# Verificar logs de erro
docker compose logs clickhouse --tail 100 | grep -i error

# Em último caso, recriar o volume (PERDA DE DADOS)
docker compose down
docker volume rm nextraceone_clickhouse-data
docker compose up -d clickhouse
```

### Queries lentas

```sql
-- Verificar queries em execução
SELECT query_id, elapsed, query
FROM system.processes
WHERE is_initial_query = 1;

-- Ver métricas de partições
SELECT
    table,
    count() AS parts,
    formatReadableSize(sum(bytes_on_disk)) AS total_size,
    min(min_date) AS oldest_data,
    max(max_date) AS newest_data
FROM system.parts
WHERE database = 'nextraceone_obs' AND active
GROUP BY table;
```

---

## Segurança

### Password

Em ambientes Docker Compose, a password é configurada via variável de ambiente:

```bash
# .env
CLICKHOUSE_PASSWORD=S3cur3P@ssw0rd!
```

> **IMPORTANTE:** Em desenvolvimento local, a password pode estar vazia. Em qualquer
> ambiente partilhado ou de produção, **definir sempre uma password forte**.

### Gestão de acessos

O ClickHouse suporta RBAC. Para criar um utilizador dedicado ao NexTraceOne:

```sql
CREATE USER nextraceone IDENTIFIED BY 'S3cur3P@ssw0rd!'
  SETTINGS max_threads = 4;

GRANT SELECT, INSERT ON nextraceone_obs.* TO nextraceone;
```

### Isolamento de rede

- Em Docker Compose, o ClickHouse está na rede `nextraceone-net` e só é acessível
  pelos outros serviços do stack.
- **Não expor** as portas 8123/9000 ao exterior em produção.
- Em Kubernetes, usar `NetworkPolicy` para restringir acesso.

### TLS

Para ativar TLS no ClickHouse:

```xml
<!-- /etc/clickhouse-server/config.d/ssl.xml -->
<clickhouse>
  <https_port>8443</https_port>
  <openSSL>
    <server>
      <certificateFile>/path/to/cert.pem</certificateFile>
      <privateKeyFile>/path/to/key.pem</privateKeyFile>
    </server>
  </openSSL>
</clickhouse>
```

Atualizar a connection string para usar a porta HTTPS:

```
Host=clickhouse;Port=8443;Database=nextraceone_obs;Username=nextraceone;Password=S3cur3P@ssw0rd!;Secure=true
```

---

## Desempenho

### Compressão

Todas as colunas utilizam **ZSTD(1)**, que oferece excelente equilíbrio entre taxa de
compressão e velocidade de descompressão. Colunas de timestamp usam adicionalmente
codec **Delta** para maximizar a compressão de dados temporais sequenciais.

Taxa de compressão típica: **5x a 15x** dependendo da cardinalidade dos dados.

### Particionamento

As tabelas são particionadas **mensalmente** (`toYYYYMM(TimestampDate)`):

- Permite ao ClickHouse eliminar partições inteiras durante TTL cleanup.
- Queries com filtro de data beneficiam de partition pruning automático.
- Cada partição é um conjunto de ficheiros independente em disco.

### Chaves de ordenação (ORDER BY)

| Tabela | Ordem | Otimizado para |
|---|---|---|
| `otel_logs` | `(ServiceName, SeverityText, Timestamp)` | Filtrar por serviço + severidade |
| `otel_traces` | `(ServiceName, SpanName, Timestamp)` | Filtrar por serviço + operação |
| `otel_metrics` | `(ServiceName, MetricName, Timestamp)` | Filtrar por serviço + métrica |

### LowCardinality

Colunas com poucos valores distintos (ServiceName, SeverityText, SpanKind, etc.) usam
`LowCardinality(String)`, que funciona como um encoding de dicionário automático e
reduz significativamente o uso de memória e disco.

### Recomendações de performance

- **Não fazer** `SELECT *` em tabelas grandes — selecionar apenas colunas necessárias.
- **Usar filtros de tempo** em todas as queries analíticas.
- **Evitar** `ORDER BY Timestamp` sem filtro de `ServiceName` — a ordering key
  começa por `ServiceName`.
- **Monitorizar** o número de parts ativos — muitas parts indicam necessidade de
  `OPTIMIZE TABLE`.

---

## Limitações

| Limitação | Impacto | Mitigação |
|---|---|---|
| **Não é OLTP** | Não suporta UPDATE/DELETE eficientes | Usar PostgreSQL para dados transacionais |
| **Eventual consistency** | Dados inseridos podem levar milissegundos a ficar visíveis | Aceitar para workloads analíticos |
| **Sem transações ACID** | Não garante atomicidade entre tabelas | Desenhar fluxos idempotentes |
| **Merge assíncrono** | Parts são merged em background | Não depender de merge imediato |
| **Eliminação por TTL** | Granularidade de partição (mensal) | Dados podem persistir até ao fim do mês |
| **Sem joins complexos** | JOINs são mais limitados que em RDBMS | Desnormalizar quando necessário |

---

## Próximos passos

- 📖 [Configuração do módulo de observabilidade](../configuration/) — opções avançadas
  de configuração do provider e modos de coleta.
- 📖 [Modos de coleta](../collection/) — diferenças entre OpenTelemetry Collector e
  Direct Push.
- 📖 [Provider Elastic](elastic.md) — alternativa para ambientes com Elastic Stack
  existente.
- 📖 [Arquitetura de observabilidade](../architecture-overview.md) — visão geral da
  arquitetura de observabilidade do NexTraceOne.

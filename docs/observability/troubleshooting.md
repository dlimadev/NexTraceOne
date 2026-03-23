# Troubleshooting de Observabilidade — NexTraceOne

> **Guia de diagnóstico e resolução de problemas para a stack de observabilidade do NexTraceOne.**
>
> Este documento cobre cenários comuns de falha, comandos de diagnóstico e procedimentos de verificação
> para cada componente da stack: ClickHouse, Elastic, OTel Collector, CLR Profiler, Kafka e pipeline
> end-to-end.

---

## Índice

1. [Objetivo](#objetivo)
2. [Diagnóstico geral](#diagnóstico-geral)
3. [Problemas com ClickHouse](#problemas-com-clickhouse)
4. [Problemas com Elastic](#problemas-com-elastic)
5. [Problemas com OTel Collector](#problemas-com-otel-collector)
6. [Problemas com CLR Profiler](#problemas-com-clr-profiler)
7. [Problemas com coleta Kafka](#problemas-com-coleta-kafka)
8. [Verificação end-to-end](#verificação-end-to-end)
9. [Logs úteis](#logs-úteis)
10. [Comandos de diagnóstico](#comandos-de-diagnóstico)
11. [Problemas comuns com configuração](#problemas-comuns-com-configuração)

---

## Objetivo

Este guia ajuda operadores, engenheiros e tech leads a diagnosticar e resolver problemas na stack
de observabilidade do NexTraceOne. Cada secção segue o padrão:

1. **Sintoma** — O que o operador observa.
2. **Causa provável** — As causas mais comuns para este sintoma.
3. **Diagnóstico** — Comandos e procedimentos para confirmar a causa.
4. **Resolução** — Passos para corrigir o problema.

---

## Diagnóstico geral

Antes de investigar um componente específico, verificar o estado geral da stack.

### Verificar que todos os serviços estão a correr

```bash
docker compose ps
```

Resultado esperado — todos os serviços com estado `Up`:

```
NAME              STATUS       PORTS
postgres          Up           5432/tcp
clickhouse        Up           8123/tcp, 9000/tcp
otel-collector    Up           4317/tcp, 4318/tcp
apihost           Up           5000/tcp
workers           Up
ingestion         Up
frontend          Up           3000/tcp
```

### Verificar o endpoint de saúde

```bash
curl -s http://localhost:5000/health | python3 -m json.tool
```

### Verificar logs de arranque da aplicação

```bash
docker compose logs apihost --tail=50 --no-log-prefix | grep -i "observability\|telemetry\|provider"
```

### Verificar variáveis de ambiente

```bash
docker compose exec apihost env | grep -i "OBSERVABILITY\|CLICKHOUSE\|ELASTIC\|COLLECTION"
```

---

## Problemas com ClickHouse

### 3.1 — Connection refused

**Sintoma:**
Logs da aplicação mostram erro de conexão ao ClickHouse:
```
[ERR] Failed to connect to ClickHouse: Connection refused (Host=clickhouse, Port=8123)
```

**Causa provável:**
- O serviço ClickHouse não está a correr.
- O hostname ou porta estão incorretos na connection string.
- Regras de firewall ou rede Docker impedem a conexão.

**Diagnóstico:**

```bash
# Verificar se o container ClickHouse está a correr
docker compose ps clickhouse

# Verificar logs do ClickHouse
docker compose logs clickhouse --tail=30

# Testar conectividade a partir do container da aplicação
docker compose exec apihost curl -s http://clickhouse:8123/ping
```

**Resolução:**

1. Se o container não está a correr, verificar os logs para erros de arranque:
   ```bash
   docker compose logs clickhouse --tail=100
   ```
2. Se o container está a correr mas não responde ao ping, verificar a configuração de rede:
   ```bash
   docker compose exec apihost nslookup clickhouse
   ```
3. Reiniciar o serviço ClickHouse:
   ```bash
   docker compose restart clickhouse
   ```
4. Verificar que a connection string no `appsettings.json` usa o hostname e porta corretos.

---

### 3.2 — Schema não criado

**Sintoma:**
A aplicação arranca mas dados não são escritos. Logs mostram erros de tabela não encontrada:
```
[ERR] Table nextraceone_obs.traces does not exist
```

**Causa provável:**
- A base de dados ou tabelas não foram criadas durante o arranque inicial.
- A migração de schema falhou silenciosamente.
- O utilizador ClickHouse não tem permissões para criar tabelas.

**Diagnóstico:**

```bash
# Verificar se a base de dados existe
docker compose exec clickhouse clickhouse-client --query "SHOW DATABASES"

# Verificar se as tabelas existem
docker compose exec clickhouse clickhouse-client --query "SHOW TABLES FROM nextraceone_obs"

# Verificar permissões do utilizador
docker compose exec clickhouse clickhouse-client --query "SHOW GRANTS FOR default"
```

**Resolução:**

1. Se a base de dados não existe, criar manualmente:
   ```bash
   docker compose exec clickhouse clickhouse-client --query "CREATE DATABASE IF NOT EXISTS nextraceone_obs"
   ```
2. Reiniciar a aplicação para que a migração de schema execute novamente:
   ```bash
   docker compose restart apihost workers ingestion
   ```
3. Verificar logs de migração durante o arranque:
   ```bash
   docker compose logs apihost --tail=100 | grep -i "migration\|schema"
   ```

---

### 3.3 — Disco cheio

**Sintoma:**
Escritas no ClickHouse falham. Logs mostram erros de espaço em disco:
```
[ERR] ClickHouse write failed: No space left on device
```

**Causa provável:**
- Volume de dados excedeu a capacidade do disco.
- Retenção configurada demasiado longa para o volume de dados.
- TTL do ClickHouse não está a limpar dados antigos.

**Diagnóstico:**

```bash
# Verificar espaço em disco do container
docker compose exec clickhouse df -h /var/lib/clickhouse

# Verificar tamanho das tabelas
docker compose exec clickhouse clickhouse-client --query "
  SELECT
    table,
    formatReadableSize(sum(bytes_on_disk)) AS size,
    sum(rows) AS total_rows
  FROM system.parts
  WHERE database = 'nextraceone_obs' AND active
  GROUP BY table
  ORDER BY sum(bytes_on_disk) DESC
"

# Verificar TTL configurado
docker compose exec clickhouse clickhouse-client --query "
  SELECT table, engine_full
  FROM system.tables
  WHERE database = 'nextraceone_obs'
"
```

**Resolução:**

1. Reduzir a retenção no `appsettings.json`:
   ```json
   "LogsRetentionDays": 14,
   "TracesRetentionDays": 14,
   "MetricsRetentionDays": 30
   ```
2. Forçar limpeza de dados antigos:
   ```bash
   docker compose exec clickhouse clickhouse-client --query "
     OPTIMIZE TABLE nextraceone_obs.traces FINAL
   "
   ```
3. Aumentar o volume de disco alocado ao container ClickHouse.
4. Verificar se o TTL está configurado corretamente nas tabelas.

---

### 3.4 — Consultas lentas

**Sintoma:**
Dashboards e consultas de observabilidade demoram muito tempo ou fazem timeout.

**Causa provável:**
- Consultas sem filtro de tempo adequado.
- Tabelas sem índices ou partições optimizadas.
- ClickHouse com recursos insuficientes (CPU/RAM).
- Volume de dados excessivo na camada hot.

**Diagnóstico:**

```bash
# Verificar consultas lentas em execução
docker compose exec clickhouse clickhouse-client --query "
  SELECT query_id, elapsed, query
  FROM system.processes
  WHERE elapsed > 5
  ORDER BY elapsed DESC
"

# Verificar consumo de recursos
docker compose exec clickhouse clickhouse-client --query "
  SELECT metric, value
  FROM system.metrics
  WHERE metric IN ('MemoryTracking', 'Query', 'Merge')
"

# Verificar tamanho das partições
docker compose exec clickhouse clickhouse-client --query "
  SELECT
    table,
    partition,
    formatReadableSize(sum(bytes_on_disk)) AS size,
    sum(rows) AS rows
  FROM system.parts
  WHERE database = 'nextraceone_obs' AND active
  GROUP BY table, partition
  ORDER BY sum(bytes_on_disk) DESC
  LIMIT 20
"
```

**Resolução:**

1. Verificar que todas as consultas da aplicação incluem filtro por intervalo de tempo.
2. Verificar que as tabelas usam `ORDER BY` e partições adequadas.
3. Considerar aumentar os recursos (CPU/RAM) do container ClickHouse.
4. Reduzir a retenção hot para diminuir o volume de dados consultados.

---

### 3.5 — Dados não aparecem

**Sintoma:**
A aplicação parece funcionar, mas dashboards e consultas não mostram dados recentes.

**Causa provável:**
- O pipeline de ingestão não está a enviar dados para o ClickHouse.
- O OTel Collector não está a exportar corretamente.
- A aplicação não está a gerar telemetria.
- Desalinhamento entre o provider configurado e o efetivamente ativo.

**Diagnóstico:**

```bash
# Verificar se existem dados recentes
docker compose exec clickhouse clickhouse-client --query "
  SELECT count(), max(timestamp)
  FROM nextraceone_obs.traces
  WHERE timestamp > now() - INTERVAL 1 HOUR
"

# Verificar o provider ativo nos logs
docker compose logs apihost --tail=30 | grep -i "provider"

# Verificar se o OTel Collector está a receber dados
docker compose logs otel-collector --tail=30 | grep -i "export\|batch\|error"
```

**Resolução:**

1. Confirmar que `Telemetry.ObservabilityProvider.Provider` corresponde ao provider com `Enabled: true`.
2. Verificar o [pipeline end-to-end](#verificação-end-to-end).
3. Gerar tráfego de teste e verificar se traces aparecem.

---

## Problemas com Elastic

### 4.1 — Connection refused

**Sintoma:**
```
[ERR] Failed to connect to Elastic: Connection refused (Endpoint=https://elastic.example.com:9200)
```

**Causa provável:**
- O cluster Elastic não está acessível.
- O endpoint está incorreto.
- Firewall a bloquear a conexão.

**Diagnóstico:**

```bash
# Testar conectividade
curl -s -o /dev/null -w "%{http_code}" https://elastic.example.com:9200

# Testar a partir do container da aplicação
docker compose exec apihost curl -s -k https://elastic.example.com:9200
```

**Resolução:**

1. Verificar que o endpoint Elastic está correto e acessível.
2. Verificar regras de firewall e segurança de rede.
3. Se o Elastic está na mesma rede Docker, verificar que o nome do serviço está correto.
4. Verificar que o Elastic está a aceitar conexões na porta configurada.

---

### 4.2 — Autenticação falhada

**Sintoma:**
```
[ERR] Elastic authentication failed: 401 Unauthorized
```

**Causa provável:**
- API key inválida, expirada ou não configurada.
- Variável de ambiente `ELASTIC_API_KEY` não definida.
- API key sem permissões adequadas.

**Diagnóstico:**

```bash
# Verificar se a variável de ambiente está definida
docker compose exec apihost env | grep ELASTIC_API_KEY

# Testar autenticação manualmente
curl -s -H "Authorization: ApiKey <api-key>" https://elastic.example.com:9200/_cluster/health
```

**Resolução:**

1. Verificar que `ELASTIC_API_KEY` está definida no ambiente do container.
2. Gerar nova API key no Elastic com permissões adequadas:
   - Permissões de leitura e escrita nos índices `nextraceone-*`.
3. Atualizar a variável de ambiente e reiniciar os serviços.

---

### 4.3 — Índice não criado

**Sintoma:**
Dados não aparecem no Elastic. Erro nos logs:
```
[ERR] Index nextraceone-traces-2024.01.15 does not exist
```

**Causa provável:**
- A API key não tem permissão para criar índices.
- O index template não foi criado.
- O IndexPrefix não corresponde ao esperado.

**Diagnóstico:**

```bash
# Listar índices existentes
curl -s -H "Authorization: ApiKey <api-key>" \
  https://elastic.example.com:9200/_cat/indices/nextraceone-*?v

# Listar index templates
curl -s -H "Authorization: ApiKey <api-key>" \
  https://elastic.example.com:9200/_index_template/nextraceone-*
```

**Resolução:**

1. Verificar permissões da API key para criação de índices.
2. Reiniciar a aplicação para recriar index templates.
3. Verificar que o `IndexPrefix` na configuração corresponde ao esperado.

---

### 4.4 — Timeout errors

**Sintoma:**
```
[ERR] Elastic request timeout after 30000ms
```

**Causa provável:**
- Cluster Elastic sobrecarregado.
- Rede lenta entre a aplicação e o Elastic.
- Bulk requests demasiado grandes.

**Diagnóstico:**

```bash
# Verificar saúde do cluster
curl -s -H "Authorization: ApiKey <api-key>" \
  https://elastic.example.com:9200/_cluster/health?pretty

# Verificar métricas de ingestão
curl -s -H "Authorization: ApiKey <api-key>" \
  https://elastic.example.com:9200/_nodes/stats/indices?pretty | head -50
```

**Resolução:**

1. Verificar a saúde do cluster Elastic (status verde/amarelo/vermelho).
2. Se o cluster está vermelho, resolver os problemas de shards primeiro.
3. Verificar a latência de rede entre a aplicação e o Elastic.
4. Considerar reduzir o batch size de ingestão.

---

## Problemas com OTel Collector

### 5.1 — Collector não arranca

**Sintoma:**
O container `otel-collector` não inicia ou reinicia continuamente.

**Causa provável:**
- Ficheiro de configuração do collector inválido.
- Porta já em uso.
- Imagem Docker não encontrada.

**Diagnóstico:**

```bash
# Verificar estado do container
docker compose ps otel-collector

# Verificar logs de arranque
docker compose logs otel-collector --tail=50

# Verificar se as portas estão disponíveis
docker compose exec otel-collector netstat -tlnp 2>/dev/null || \
  ss -tlnp | grep -E "4317|4318"
```

**Resolução:**

1. Verificar logs de arranque para erros de parsing de configuração.
2. Verificar que as portas 4317 e 4318 não estão ocupadas por outros processos.
3. Validar a configuração do collector:
   ```bash
   docker compose exec otel-collector otelcol validate --config=/etc/otelcol/config.yaml
   ```
4. Reconstruir o container se a imagem está corrompida:
   ```bash
   docker compose build otel-collector && docker compose up -d otel-collector
   ```

---

### 5.2 — Dados não chegam ao collector

**Sintoma:**
O collector está a correr mas não recebe dados das aplicações.

**Causa provável:**
- As aplicações não estão configuradas para enviar dados via OTLP.
- Os endpoints OTLP estão incorretos na configuração.
- Problema de DNS ou rede entre aplicações e collector.

**Diagnóstico:**

```bash
# Verificar métricas do collector (se prometheus receiver ativo)
curl -s http://localhost:8888/metrics | grep otelcol_receiver_accepted

# Testar envio manual de trace via HTTP
curl -X POST http://localhost:4318/v1/traces \
  -H "Content-Type: application/json" \
  -d '{
    "resourceSpans": [{
      "resource": {"attributes": [{"key": "service.name", "value": {"stringValue": "test"}}]},
      "scopeSpans": [{
        "spans": [{
          "traceId": "00000000000000000000000000000001",
          "spanId": "0000000000000001",
          "name": "test-span",
          "kind": 1,
          "startTimeUnixNano": 1700000000000000000,
          "endTimeUnixNano": 1700000001000000000
        }]
      }]
    }]
  }'

# Verificar que o endpoint está acessível a partir da aplicação
docker compose exec apihost curl -s http://otel-collector:4318/v1/traces
```

**Resolução:**

1. Verificar que `OtlpGrpcEndpoint` e `OtlpHttpEndpoint` apontam para o collector correto.
2. Verificar que o collector está a escutar nas interfaces correctas (não apenas localhost).
3. Verificar resolução DNS do hostname `otel-collector` a partir dos containers da aplicação.

---

### 5.3 — Memória excedida

**Sintoma:**
O collector é terminado pelo OOM killer ou mostra warnings de memória:
```
[WRN] Memory usage exceeded soft limit
```

**Causa provável:**
- Volume de telemetria excede a capacidade configurada.
- Batch processor com buffers demasiado grandes.
- Memory limiter não configurado ou mal dimensionado.

**Diagnóstico:**

```bash
# Verificar consumo de memória
docker stats otel-collector --no-stream

# Verificar configuração do memory limiter
docker compose exec otel-collector cat /etc/otelcol/config.yaml | grep -A 5 "memory_limiter"
```

**Resolução:**

1. Configurar o memory limiter no collector:
   ```yaml
   processors:
     memory_limiter:
       check_interval: 5s
       limit_mib: 512
       spike_limit_mib: 128
   ```
2. Reduzir o batch size:
   ```yaml
   processors:
     batch:
       send_batch_size: 512
       timeout: 5s
   ```
3. Aumentar o limite de memória do container no `docker-compose.yml`:
   ```yaml
   deploy:
     resources:
       limits:
         memory: 1g
   ```

---

### 5.4 — Problemas de sampling

**Sintoma:**
Apenas uma fração dos traces esperados aparece nos dashboards.

**Causa provável:**
- Tail sampling ou probabilistic sampling configurado com ratio baixo.
- Traces são descartados pelo batch processor.

**Diagnóstico:**

```bash
# Verificar configuração de sampling
docker compose exec otel-collector cat /etc/otelcol/config.yaml | grep -A 10 "sampl"

# Verificar métricas de spans recebidos vs exportados
curl -s http://localhost:8888/metrics | grep -E "otelcol_receiver_accepted|otelcol_exporter_sent"
```

**Resolução:**

1. Verificar o ratio de sampling configurado.
2. Se 100% dos traces são necessários, desativar o sampling ou usar ratio `1.0`.
3. Verificar que o pipeline não descarta spans por pressão de memória.

---

### 5.5 — Erros no exporter

**Sintoma:**
O collector recebe dados mas não consegue exportar para o provider de armazenamento:
```
[ERR] Exporting failed. Will retry. error: connection refused
```

**Causa provável:**
- O provider de armazenamento (ClickHouse/Elastic) não está acessível.
- Credenciais de exportação incorretas.
- Exporter mal configurado.

**Diagnóstico:**

```bash
# Verificar logs de erro do exporter
docker compose logs otel-collector --tail=100 | grep -i "error\|export\|retry"

# Verificar configuração dos exporters
docker compose exec otel-collector cat /etc/otelcol/config.yaml | grep -A 15 "exporters"
```

**Resolução:**

1. Verificar que o provider de armazenamento está a correr e acessível.
2. Verificar credenciais configuradas no exporter.
3. Testar conectividade a partir do container do collector:
   ```bash
   docker compose exec otel-collector curl -s http://clickhouse:8123/ping
   ```

---

## Problemas com CLR Profiler

### 6.1 — Profiler não carrega

**Sintoma:**
A aplicação arranca normalmente mas não gera traces automáticos. Não há indicação de que o profiler
está carregado.

**Causa provável:**
- Variáveis de ambiente do profiler não estão configuradas.
- O ficheiro do profiler (.dll/.so) não está acessível.
- O modo (`IIS` vs `SelfHosted`) está incorreto para o tipo de aplicação.

**Diagnóstico:**

```bash
# Verificar variáveis de ambiente do profiler
docker compose exec apihost env | grep -i "COR_\|CORECLR_\|DOTNET_"

# Verificar se o ficheiro do profiler existe
docker compose exec apihost ls -la /opt/nextraceone/profiler/

# Verificar logs de carregamento do profiler
docker compose logs apihost --tail=100 | grep -i "profiler\|instrumentation"
```

**Resolução:**

1. Verificar que `ClrProfiler.Enabled` está `true` e `CollectionMode.ActiveMode` é `ClrProfiler`.
2. Verificar que as variáveis de ambiente de instrumentação CLR estão configuradas:
   ```
   CORECLR_ENABLE_PROFILING=1
   CORECLR_PROFILER={...guid...}
   CORECLR_PROFILER_PATH=/opt/nextraceone/profiler/libprofiler.so
   ```
3. Verificar que o modo corresponde ao tipo de aplicação (IIS vs SelfHosted).

---

### 6.2 — Traces não aparecem

**Sintoma:**
O profiler parece estar carregado (mensagem nos logs), mas traces não aparecem no provider.

**Causa provável:**
- O `ExportTarget` está incorreto.
- O `OtlpEndpoint` do profiler não está acessível.
- O `InstrumentationMode` é `Manual` mas não foram criados spans manualmente.

**Diagnóstico:**

```bash
# Verificar configuração do profiler nos logs
docker compose logs apihost --tail=50 | grep -i "export\|otlp\|profiler"

# Verificar conectividade ao endpoint OTLP
docker compose exec apihost curl -s http://otel-collector:4317
```

**Resolução:**

1. Se `InstrumentationMode` é `Manual`, verificar que a aplicação cria spans explicitamente.
2. Se `ExportTarget` é `Collector`, verificar que o OTel Collector está a correr.
3. Se `ExportTarget` é `Direct`, verificar que o endpoint do provider está acessível.
4. Alterar para `AutoInstrumentation` se a captura automática é desejada.

---

### 6.3 — Problemas de permissão

**Sintoma:**
```
[ERR] Failed to load CLR profiler: Permission denied
```

**Causa provável:**
- O processo da aplicação não tem permissão para carregar o profiler.
- O ficheiro do profiler não tem permissões de leitura/execução.

**Diagnóstico:**

```bash
# Verificar permissões do ficheiro
docker compose exec apihost ls -la /opt/nextraceone/profiler/

# Verificar utilizador do processo
docker compose exec apihost whoami
```

**Resolução:**

1. Corrigir permissões do ficheiro do profiler:
   ```bash
   docker compose exec apihost chmod 755 /opt/nextraceone/profiler/libprofiler.so
   ```
2. Verificar que o Dockerfile copia o profiler com permissões adequadas.

---

### 6.4 — Overhead elevado

**Sintoma:**
Após ativar o CLR Profiler, a aplicação mostra degradação de performance significativa
(latência aumentada, CPU elevada).

**Causa provável:**
- `AutoInstrumentation` a capturar demasiadas dependências.
- Volume de spans gerados demasiado alto.
- Exportação síncrona ao provider.

**Diagnóstico:**

```bash
# Verificar CPU/memória do container
docker stats apihost --no-stream

# Verificar volume de spans
docker compose logs otel-collector --tail=30 | grep "accepted_spans"
```

**Resolução:**

1. Considerar mudar para `InstrumentationMode: Manual` e instrumentar apenas operações críticas.
2. Configurar sampling para reduzir o volume de spans.
3. Verificar que o `ExportTarget` é `Collector` (para beneficiar de batching e async export).
4. Se o overhead é inaceitável, considerar mudar para `OpenTelemetryCollector` com instrumentação
   via SDK.

---

## Problemas com coleta Kafka

### 7.1 — Correlação ausente

**Sintoma:**
Mensagens Kafka aparecem nos traces, mas sem correlação com o trace/span pai. As mensagens
aparecem como traces independentes.

**Causa provável:**
- O propagation context não está a ser injetado nos headers das mensagens Kafka.
- O consumer não está a extrair o context dos headers.
- A instrumentação Kafka não suporta W3C TraceContext.

**Diagnóstico:**

```bash
# Verificar se traces Kafka têm parent span
docker compose exec clickhouse clickhouse-client --query "
  SELECT trace_id, span_id, parent_span_id, service_name, span_name
  FROM nextraceone_obs.traces
  WHERE span_name LIKE '%kafka%' OR span_name LIKE '%produce%' OR span_name LIKE '%consume%'
  ORDER BY timestamp DESC
  LIMIT 20
"

# Verificar headers das mensagens Kafka
docker compose exec apihost env | grep -i "OTEL.*PROPAGATOR"
```

**Resolução:**

1. Verificar que a instrumentação do producer injeta headers `traceparent` e `tracestate`.
2. Verificar que a instrumentação do consumer extrai estes headers.
3. Configurar o propagador como W3C TraceContext:
   ```
   OTEL_PROPAGATORS=tracecontext,baggage
   ```

---

### 7.2 — JMX não acessível

**Sintoma:**
Métricas dos brokers Kafka não aparecem. Logs do collector mostram:
```
[ERR] Failed to connect to JMX endpoint: Connection refused
```

**Causa provável:**
- JMX não está ativo nos brokers Kafka.
- Porta JMX bloqueada por firewall.
- Credenciais JMX incorretas.

**Diagnóstico:**

```bash
# Verificar se JMX está ativo nos brokers
docker compose exec kafka env | grep JMX

# Testar conectividade à porta JMX
docker compose exec otel-collector nc -zv kafka 9999
```

**Resolução:**

1. Ativar JMX nos brokers Kafka:
   ```
   KAFKA_JMX_OPTS=-Dcom.sun.management.jmxremote -Dcom.sun.management.jmxremote.port=9999 -Dcom.sun.management.jmxremote.authenticate=false -Dcom.sun.management.jmxremote.ssl=false
   ```
2. Verificar que a porta JMX está exposta e acessível.
3. Se JMX requer autenticação, configurar credenciais no receiver do collector.

---

### 7.3 — Métricas incompletas

**Sintoma:**
Apenas algumas métricas Kafka aparecem (por exemplo, throughput mas não consumer lag).

**Causa provável:**
- O JMX receiver está configurado para capturar apenas um subconjunto de MBeans.
- Consumer groups não estão a reportar métricas.
- O intervalo de scraping é demasiado longo.

**Diagnóstico:**

```bash
# Listar MBeans disponíveis no broker
docker compose exec kafka java -jar /opt/jmx-exporter/cmdline.jar list localhost:9999 2>/dev/null | head -30

# Verificar configuração do JMX receiver no collector
docker compose exec otel-collector cat /etc/otelcol/config.yaml | grep -A 20 "jmx"
```

**Resolução:**

1. Adicionar MBeans em falta à configuração do JMX receiver.
2. Verificar que os consumer groups estão ativos e a reportar offsets.
3. Reduzir o intervalo de scraping se necessário (por exemplo, de 60s para 15s).

---

## Verificação end-to-end

Procedimento completo para verificar que o pipeline de observabilidade está funcional
do início ao fim.

### Passo 1 — Verificar serviços

```bash
docker compose ps
```

Todos os serviços (`postgres`, `clickhouse`, `otel-collector`, `apihost`, `workers`,
`ingestion`, `frontend`) devem estar `Up`.

### Passo 2 — Verificar endpoint de saúde

```bash
curl -s http://localhost:5000/health | python3 -m json.tool
```

Todos os checks devem estar `Healthy`.

### Passo 3 — Gerar tráfego de teste

```bash
# Fazer algumas chamadas à API para gerar traces
for i in $(seq 1 5); do
  curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5000/api/health
done
```

### Passo 4 — Verificar que o collector recebeu dados

```bash
# Verificar logs do collector
docker compose logs otel-collector --tail=20 | grep -i "accepted\|exported"
```

### Passo 5 — Verificar dados no provider

**Para ClickHouse:**

```bash
docker compose exec clickhouse clickhouse-client --query "
  SELECT count(*) AS trace_count,
         min(timestamp) AS oldest,
         max(timestamp) AS newest
  FROM nextraceone_obs.traces
  WHERE timestamp > now() - INTERVAL 5 MINUTE
"
```

**Para Elastic:**

```bash
curl -s -H "Authorization: ApiKey <api-key>" \
  "https://elastic.example.com:9200/nextraceone-traces-*/_count" | python3 -m json.tool
```

### Passo 6 — Verificar dados no frontend

1. Aceder a `http://localhost:3000`.
2. Navegar para a secção de observabilidade.
3. Verificar que traces recentes aparecem na lista.

### Passo 7 — Verificar retenção

```bash
# Verificar dados mais antigos no ClickHouse
docker compose exec clickhouse clickhouse-client --query "
  SELECT
    min(timestamp) AS oldest_trace,
    max(timestamp) AS newest_trace,
    count(*) AS total_traces
  FROM nextraceone_obs.traces
"
```

---

## Logs úteis

Localização dos logs de diagnóstico para cada componente da stack.

### Aplicação (apihost, workers, ingestion)

```bash
# Logs gerais
docker compose logs apihost --tail=100

# Filtrar por nível de erro
docker compose logs apihost --tail=200 | grep "\[ERR\]"

# Filtrar por componente de observabilidade
docker compose logs apihost --tail=200 | grep -i "telemetry\|observability\|otel"
```

### OTel Collector

```bash
# Logs gerais
docker compose logs otel-collector --tail=100

# Filtrar erros de exportação
docker compose logs otel-collector --tail=200 | grep -i "error\|failed\|retry"

# Filtrar métricas de pipeline
docker compose logs otel-collector --tail=200 | grep -i "accepted\|dropped\|refused"
```

### ClickHouse

```bash
# Logs gerais
docker compose logs clickhouse --tail=100

# Verificar queries recentes no system.query_log
docker compose exec clickhouse clickhouse-client --query "
  SELECT event_time, query_duration_ms, query, exception
  FROM system.query_log
  WHERE event_time > now() - INTERVAL 1 HOUR
    AND exception != ''
  ORDER BY event_time DESC
  LIMIT 20
"
```

### Frontend

```bash
docker compose logs frontend --tail=100
```

### Todos os serviços em simultâneo

```bash
docker compose logs --tail=30 --no-log-prefix 2>&1 | grep -i "error\|warn\|fail"
```

---

## Comandos de diagnóstico

Referência rápida de comandos úteis para diagnóstico.

### Docker Compose

```bash
# Estado dos serviços
docker compose ps

# Recursos consumidos
docker stats --no-stream

# Reiniciar stack completa
docker compose down && docker compose up -d

# Reiniciar apenas um serviço
docker compose restart <service>

# Ver variáveis de ambiente de um serviço
docker compose exec <service> env
```

### ClickHouse

```bash
# Ping
docker compose exec clickhouse clickhouse-client --query "SELECT 1"

# Versão
docker compose exec clickhouse clickhouse-client --query "SELECT version()"

# Bases de dados
docker compose exec clickhouse clickhouse-client --query "SHOW DATABASES"

# Tabelas
docker compose exec clickhouse clickhouse-client --query "SHOW TABLES FROM nextraceone_obs"

# Contagem de registos
docker compose exec clickhouse clickhouse-client --query "
  SELECT 'traces' AS type, count(*) AS cnt FROM nextraceone_obs.traces
  UNION ALL
  SELECT 'logs', count(*) FROM nextraceone_obs.logs
  UNION ALL
  SELECT 'metrics', count(*) FROM nextraceone_obs.metrics
"

# Espaço em disco
docker compose exec clickhouse clickhouse-client --query "
  SELECT
    database,
    table,
    formatReadableSize(sum(bytes_on_disk)) AS size
  FROM system.parts
  WHERE active
  GROUP BY database, table
  ORDER BY sum(bytes_on_disk) DESC
"
```

### OTel Collector

```bash
# Métricas internas do collector
curl -s http://localhost:8888/metrics | grep otelcol

# Health check
curl -s http://localhost:13133/

# Teste de envio OTLP HTTP
curl -X POST http://localhost:4318/v1/traces \
  -H "Content-Type: application/json" \
  -d '{"resourceSpans":[]}'
```

### Rede

```bash
# Testar conectividade entre containers
docker compose exec apihost curl -s http://otel-collector:4318/v1/traces
docker compose exec apihost curl -s http://clickhouse:8123/ping
docker compose exec otel-collector curl -s http://clickhouse:8123/ping

# Verificar resolução DNS
docker compose exec apihost nslookup otel-collector
docker compose exec apihost nslookup clickhouse
```

### Aplicação

```bash
# Health check
curl -s http://localhost:5000/health | python3 -m json.tool

# Verificar configuração ativa
docker compose logs apihost --tail=10 | grep -i "provider\|collection\|mode"
```

---

## Problemas comuns com configuração

### 11.1 — Provider incorreto

**Sintoma:**
Dados não aparecem em nenhum provider.

**Causa:**
`Telemetry.ObservabilityProvider.Provider` não corresponde ao provider com `Enabled: true`.

**Exemplo de erro:**
```json
{
  "Provider": "ClickHouse",
  "ClickHouse": { "Enabled": false },
  "Elastic": { "Enabled": true }
}
```

**Resolução:**
Alinhar o campo `Provider` com o bloco que tem `Enabled: true`.

---

### 11.2 — Variáveis de ambiente não definidas

**Sintoma:**
A aplicação arranca com configuração default ou falha com erro de credenciais.

**Causa:**
Variáveis de ambiente obrigatórias não estão definidas no ambiente do container.

**Diagnóstico:**

```bash
docker compose exec apihost env | grep -i "OBSERVABILITY\|CLICKHOUSE\|ELASTIC\|COLLECTION"
```

**Resolução:**
Definir todas as variáveis obrigatórias no `docker-compose.yml` ou no ficheiro `.env`.

---

### 11.3 — Modo de coleta incompatível com fonte

**Sintoma:**
Fontes habilitadas mas sem dados.

**Causa:**
O modo de coleta ativo não suporta a fonte configurada. Por exemplo, `ClrProfiler` ativo com
`Kubernetes.Enabled: true` — o CLR Profiler não captura telemetria nativa de Kubernetes.

**Resolução:**
Verificar compatibilidade entre o modo de coleta e as fontes:

| Fonte       | OpenTelemetry Collector | CLR Profiler |
|-------------|------------------------|--------------|
| IIS         | Parcial (via SDK)      | **Nativo**   |
| Kubernetes  | **Nativo**             | Limitado     |
| Kafka       | **Nativo**             | Parcial      |

---

### 11.4 — Retenção desalinhada

**Sintoma:**
Dados desaparecem antes do esperado ou disco enche rapidamente.

**Causa:**
Os valores de `HotRetentionDays`/`WarmRetentionDays`/`ColdRetentionDays` não estão alinhados com
os valores específicos do provider (`LogsRetentionDays`, `TracesRetentionDays`, `MetricsRetentionDays`).

**Resolução:**
Alinhar os valores de retenção:

```json
{
  "ClickHouse": {
    "LogsRetentionDays": 30,
    "TracesRetentionDays": 30,
    "MetricsRetentionDays": 90
  },
  "Retention": {
    "HotRetentionDays": 30,
    "WarmRetentionDays": 90,
    "ColdRetentionDays": 365
  }
}
```

Os valores de `LogsRetentionDays` e `TracesRetentionDays` do ClickHouse devem ser iguais ou
inferiores ao `HotRetentionDays`.

---

### 11.5 — Endpoints com protocolo errado

**Sintoma:**
Conexão falha apesar do serviço estar a correr.

**Causa:**
Uso de `https://` quando o serviço espera `http://` (ou vice-versa), ou porta gRPC usada
para HTTP.

**Resolução:**
Verificar protocolos e portas:

| Endpoint              | Protocolo | Porta  |
|-----------------------|-----------|--------|
| OTel Collector gRPC   | `http://` | `4317` |
| OTel Collector HTTP   | `http://` | `4318` |
| ClickHouse HTTP       | `http://` | `8123` |
| Elastic (produção)    | `https://`| `9200` |

> **Nota:** O OTel Collector usa `http://` nos endpoints OTLP mesmo para gRPC, pois o TLS é
> geralmente terminado num load balancer ou proxy em produção.

---

> **Documento mantido pela equipa NexTraceOne.**
> Para questões ou sugestões, abrir uma issue no repositório com a label `docs/observability`.

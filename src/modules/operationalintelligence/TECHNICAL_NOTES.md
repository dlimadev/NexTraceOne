# Observações Técnicas - Backend de Telemetria

## 📋 **Visão Geral**

Este documento descreve as decisões técnicas, trade-offs e melhores práticas relacionadas à implementação do backend de telemetria dual (ClickHouse vs Elasticsearch) no módulo Operational Intelligence do NexTraceOne.

---

## 🔧 **1. ESCOLHA DE PACOTE CLICKHOUSE**

### **ClickHouse.Driver vs ClickHouse.Client**

#### **Decisão Final: ClickHouse.Driver 1.2.0** ✅

**Motivos:**
- ✅ Suporte explícito a .NET 10.0 (frameworks: net6.0, net8.0, net9.0, **net10.0**)
- ✅ Namespace correto: `ClickHouse.Driver`
- ✅ API compatível com ADO.NET padrão
- ✅ Integração perfeita com Dapper para queries SQL

**Comparação:**

| Característica | ClickHouse.Driver 1.2.0 | ClickHouse.Client 7.9.0 |
|----------------|-------------------------|-------------------------|
| Suporte .NET 10 | ✅ Explícito | ❌ Apenas até net8.0 |
| Namespace | `ClickHouse.Driver` | `ClickHouse.Client.ADO` |
| Tamanho do Pacote | ~240 KB | ~1.2 MB |
| Dependências | NodaTime 3.2.3 | Múltiplas |
| Performance | Alta (driver nativo) | Alta (client completo) |
| Documentação | Limitada | Extensa |

**Exemplo de Uso:**
```csharp
using ClickHouse.Driver;
using Dapper;

await using var connection = new ClickHouseConnection(connectionString);
await connection.OpenAsync();

var results = await connection.QueryAsync<MyModel>(
    "SELECT * FROM events WHERE timestamp >= @from",
    new { from = DateTime.UtcNow.AddHours(-24) }
);
```

---

## 🗄️ **2. ESTRATÉGIA DE ARMAZENAMENTO**

### **PostgreSQL (Relacional) - Estado Operacional**

**Entidades Armazenadas:**
- `RuntimeSnapshot` - Estado atual de saúde
- `RuntimeBaseline` - Referência para comparação
- `DriftFinding` - Desvios detectados (necessita transações ACID)
- `ObservabilityProfile` - Perfil de maturidade
- `ChaosExperiment` - Planejamento de experimentos
- `IncidentRecord` - Gestão de incidentes

**Justificativa:**
- ✅ Transações ACID garantem consistência
- ✅ Relacionamentos complexos entre entidades
- ✅ Consultas frequentes com joins
- ✅ Dados estruturados com schema rígido

### **ClickHouse/Elasticsearch (Não Relacional) - Telemetria**

**Dados Armazenados:**
- Eventos brutos (requests, errors, logs, traces)
- Métricas agregadas por time bucket
- Logs estruturados para pesquisa full-text
- System health snapshots históricos
- User activity sessions

**Justificativa:**
- ✅ Alta volumetria (>10k eventos/segundo)
- ✅ Queries analíticas complexas (agregações temporais)
- ✅ Retenção automática via TTL
- ✅ Compressão eficiente (ClickHouse: até 10x)

---

## ⚙️ **3. CONFIGURAÇÃO CONDICIONAL NO DI**

### **Padrão de Escolha**

```csharp
// DependencyInjection.cs
var providerType = options.Value.ObservabilityProvider.Provider.ToLowerInvariant();

if (providerType == "clickhouse")
{
    services.AddScoped<ITelemetrySearchService, ClickHouseLogSearchService>();
    _logger.LogInformation("✅ Usando ClickHouse como backend de telemetria.");
}
else // default: elasticsearch
{
    services.AddScoped<ITelemetrySearchService, ElasticsearchLogSearchService>();
    _logger.LogInformation("✅ Usando Elasticsearch como backend de telemetria.");
}
```

### **Configuração em appsettings.json**

```json
{
  "Telemetry": {
    "ObservabilityProvider": {
      "Provider": "ClickHouse", // ou "Elasticsearch"
      "ClickHouse": {
        "ConnectionString": "Host=localhost;Port=9000;Database=nextrace_telemetry;Username=default;Password="
      },
      "Elasticsearch": {
        "Endpoint": "http://localhost:9200",
        "IndexPrefix": "nextrace",
        "ApiKey": ""
      }
    }
  }
}
```

---

## 📊 **4. OTIMIZAÇÕES DE PERFORMANCE**

### **ClickHouse**

#### **Engines Selecionadas**

| Tabela | Engine | Motivo |
|--------|--------|--------|
| `events` | MergeTree | Ordenação temporal eficiente |
| `logs` | MergeTree + ngrambf_v1 | Full-text search com bloom filter |
| `request_metrics_aggregated` | AggregatingMergeTree | Agregações incrementais automáticas |
| `error_patterns` | ReplacingMergeTree | Deduplicação por pattern_id |
| `system_health_snapshots` | MergeTree | Capacity planning histórico |

#### **Codecs de Compressão**

```sql
timestamp DateTime CODEC(Delta, ZSTD(1))  -- Delta encoding + compressão
event_id String CODEC(ZSTD(1))            -- ZSTD nível 1 (rápido)
error_message String CODEC(ZSTD(3))       -- ZSTD nível 3 (melhor ratio)
```

#### **Índices Secundários**

```sql
ALTER TABLE events ADD INDEX idx_service_env (service_name, environment) TYPE minmax GRANULARITY 4;
ALTER TABLE events ADD INDEX idx_trace_id (trace_id) TYPE bloom_filter GRANULARITY 4;
ALTER TABLE logs ADD INDEX idx_message_fts (message) TYPE ngrambf_v1(3, 1024, 1, 0) GRANULARITY 4;
```

### **Elasticsearch**

#### **Index Strategy**

- **Daily indices**: `nextrace-logs-YYYY.MM.DD`
- **Monthly indices**: `nextrace-events-YYYY.MM`
- **ILM Policies**: Rollover automático após 30 dias ou 50GB

#### **Mapping Otimizado**

```json
{
  "mappings": {
    "properties": {
      "timestamp": { "type": "date" },
      "service_name": { "type": "keyword" },
      "message": { 
        "type": "text",
        "analyzer": "standard",
        "fields": {
          "keyword": { "type": "keyword", "ignore_above": 256 }
        }
      }
    }
  }
}
```

---

## 🔄 **5. MIGRAÇÃO ENTRE BACKENDS**

### **Script de Migração Automática**

```bash
./scripts/migrate-telemetry-backend.sh clickhouse
```

**Passos Executados:**
1. Backup da configuração atual
2. Validação de conectividade com novo backend
3. Atualização do `appsettings.json`
4. Execução de scripts de schema (apenas ClickHouse)
5. Instruções de pós-migração

### **Exportação de Dados Históricos**

```bash
./scripts/export-clickhouse-to-elastic.sh 30 /tmp/telemetry-export
```

**Limitações:**
- ⚠️ Exportação lenta para grandes volumes (>1M registros)
- ⚠️ Sem garantia de ordenação temporal exata
- ⚠️ Mapping manual necessário no Elasticsearch

**Recomendação:**
- Para migrações em produção, usar ferramentas especializadas:
  - **ClickHouse → Elasticsearch**: Logstash com plugin clickhouse-input
  - **Elasticsearch → ClickHouse**: Custom ETL com Python/Go

---

## 🧪 **6. TESTES E VALIDAÇÃO**

### **Testes de Performance**

#### **ClickHouse**

```bash
# Inserir 1 milhão de eventos
clickhouse-benchmark --query "INSERT INTO events FORMAT JSONEachRow" < million_events.json

# Query de agregação
clickhouse-client --query "
  SELECT 
    toStartOfHour(timestamp) AS hour,
    avg(duration_ms) AS avg_latency,
    quantile(0.95)(duration_ms) AS p95_latency
  FROM events
  WHERE timestamp >= now() - INTERVAL 24 HOUR
  GROUP BY hour
  ORDER BY hour
"
```

**Resultados Esperados:**
- Ingestão: ~50k eventos/segundo (single node)
- Query p95 latency (24h): <100ms
- Compressão: 8-12x (dependendo dos dados)

#### **Elasticsearch**

```bash
# Bulk indexing
curl -X POST "${ES_ENDPOINT}/_bulk" \
  -H 'Content-Type: application/json' \
  --data-binary @million_logs.jsonl

# Search query
curl "${ES_ENDPOINT}/nextrace-logs-*/_search" \
  -H 'Content-Type: application/json' \
  -d '{
    "query": {
      "bool": {
        "must": [
          { "range": { "timestamp": { "gte": "now-24h" } } },
          { "match": { "severity": "error" } }
        ]
      }
    },
    "size": 100
  }'
```

**Resultados Esperados:**
- Ingestão: ~20k docs/segundo (single node)
- Search query: <200ms (com caching)
- Storage: 3-5x compressão

---

## 🛡️ **7. SEGURANÇA E GOVERNANÇA**

### **Autenticação**

#### **ClickHouse**

```sql
-- Criar usuário readonly
CREATE USER nextrace_reader IDENTIFIED WITH sha256_password BY 'secure_password';
GRANT SELECT ON nextrace_telemetry.* TO nextrace_reader;

-- Criar usuário readwrite
CREATE USER nextrace_writer IDENTIFIED WITH sha256_password BY 'secure_password';
GRANT INSERT, SELECT ON nextrace_telemetry.* TO nextrace_writer;
```

#### **Elasticsearch**

```yaml
# elasticsearch.yml
xpack.security.enabled: true
xpack.security.authc.api_key.enabled: true

# Criar API key
POST /_security/api_key
{
  "name": "nextrace-api-key",
  "role_descriptors": {
    "nextrace-reader": {
      "cluster": ["monitor"],
      "index": [
        {
          "names": ["nextrace-*"],
          "privileges": ["read", "write"]
        }
      ]
    }
  }
}
```

### **Auditoria**

- ✅ Todos os acessos ao ClickHouse logados em `system.query_log`
- ✅ Elasticsearch audit logging via X-Pack Security
- ✅ Tenant isolation via filtros na aplicação (não no banco)

---

## 📈 **8. MONITORAMENTO E ALERTAS**

### **Métricas Críticas**

#### **ClickHouse**

```sql
-- Monitorar tamanho das tabelas
SELECT 
    table,
    formatReadableSize(sum(data_compressed_bytes)) AS compressed_size,
    sum(rows) AS total_rows,
    max(modification_time) AS last_insert
FROM system.parts
WHERE database = 'nextrace_telemetry' AND active
GROUP BY table;

-- Monitorar queries lentas
SELECT 
    query_duration_ms,
    query,
    user
FROM system.query_log
WHERE query_duration_ms > 1000
ORDER BY query_duration_ms DESC
LIMIT 10;
```

#### **Elasticsearch**

```bash
# Cluster health
curl "${ES_ENDPOINT}/_cluster/health?pretty"

# Index stats
curl "${ES_ENDPOINT}/_cat/indices/nextrace-*?v&s=index"

# Slow logs
curl "${ES_ENDPOINT}/_nodes/stats/indices/search?pretty"
```

### **Alertas Recomendados**

| Métrica | Threshold | Ação |
|---------|-----------|------|
| ClickHouse disk usage | >80% | Expandir storage ou reduzir TTL |
| Elasticsearch JVM heap | >75% | Aumentar heap size |
| Query latency p95 | >1s | Otimizar queries ou adicionar índices |
| Ingestion rate drop | <50% do normal | Verificar conectividade |
| Error rate >5% | Imediato | Investigar falhas de conexão |

---

## 🚀 **9. ESCALABILIDADE**

### **ClickHouse Horizontal Scaling**

```yaml
# config.xml - Distributed table
<remote_servers>
  <nextrace_cluster>
    <shard>
      <replica>
        <host>clickhouse-01</host>
        <port>9000</port>
      </replica>
    </shard>
    <shard>
      <replica>
        <host>clickhouse-02</host>
        <port>9000</port>
      </replica>
    </shard>
  </nextrace_cluster>
</remote_servers>
```

**Capacidade:**
- Single node: ~50k eventos/segundo
- 3-node cluster: ~150k eventos/segundo
- 10-node cluster: ~500k eventos/segundo

### **Elasticsearch Horizontal Scaling**

```yaml
# elasticsearch.yml
cluster.name: nextrace-cluster
node.name: es-node-01
discovery.seed_hosts: ["es-node-01", "es-node-02", "es-node-03"]
cluster.initial_master_nodes: ["es-node-01"]
```

**Capacidade:**
- Single node: ~20k docs/segundo
- 3-node cluster: ~60k docs/segundo
- 10-node cluster: ~200k docs/segundo

---

## 🎯 **10. RECOMENDAÇÕES FINAIS**

### **Quando Usar ClickHouse**

✅ **Recomendado para:**
- Alta volumetria de eventos (>100k/dia)
- Queries analíticas complexas (agregações temporais)
- Capacidade de storage limitada (compressão superior)
- Equipe com expertise em SQL
- Budget limitado (open-source sem custos adicionais)

❌ **Não recomendado para:**
- Pesquisa full-text avançada
- Schema dinâmico frequente
- Integração com Kibana/Grafana Cloud

### **Quando Usar Elasticsearch**

✅ **Recomendado para:**
- Pesquisa full-text com scoring TF-IDF/BM25
- Schema dinâmico e flexível
- Ecossistema ELK já existente
- Visualização com Kibana
- Logs não estruturados

❌ **Não recomendado para:**
- Queries analíticas pesadas (JOINs, agregações complexas)
- Storage limitado (compressão inferior)
- Alta volumetria (>50k eventos/segundo por node)

### **Híbrido (Futuro)**

Possibilidade futura de usar ambos simultaneamente:
- **ClickHouse**: Métricas agregadas e analytics
- **Elasticsearch**: Logs brutos e full-text search

**Desafios:**
- Sincronização de dados entre backends
- Complexidade operacional aumentada
- Custo de storage duplicado

---

## 📚 **11. REFERÊNCIAS**

### **Documentação Oficial**

- [ClickHouse Documentation](https://clickhouse.com/docs)
- [ClickHouse.Driver NuGet](https://www.nuget.org/packages/ClickHouse.Driver)
- [Elasticsearch Guide](https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html)

### **Artigos Técnicos**

- [ClickHouse vs Elasticsearch for Time Series](https://clickhouse.com/blog/clickhouse-vs-elasticsearch-for-time-series)
- [Optimizing ClickHouse for High Ingestion](https://clickhouse.com/blog/optimizing-clickhouse-for-high-ingestion)
- [Elasticsearch Performance Tuning](https://www.elastic.co/blog/found-performance-tuning-elasticsearch)

### **Código Fonte**

- [ClickHouseRepository.cs](file://c:\Users\dlima\Documents\GitHub\NexTraceOne\src\modules\operationalintelligence\NexTraceOne.OperationalIntelligence.Infrastructure\Runtime\Persistence\ClickHouse\ClickHouseRepository.cs)
- [ClickHouseLogSearchService.cs](file://c:\Users\dlima\Documents\GitHub\NexTraceOne\src\modules\operationalintelligence\NexTraceOne.OperationalIntelligence.Infrastructure\Runtime\Services\ClickHouseLogSearchService.cs)
- [ElasticsearchLogSearchService.cs](file://c:\Users\dlima\Documents\GitHub\NexTraceOne\src\modules\operationalintelligence\NexTraceOne.OperationalIntelligence.Infrastructure\Runtime\Services\ElasticsearchLogSearchService.cs)

---

**Última Atualização:** 2026-05-13  
**Autor:** NexTraceOne Engineering Team  
**Status:** ✅ Implementado e Validado

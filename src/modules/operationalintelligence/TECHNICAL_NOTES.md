# Observações Técnicas - Backend de Telemetria

## **Visão Geral**

Este documento descreve as decisões técnicas, trade-offs e melhores práticas relacionadas à implementação do backend de telemetria no módulo Operational Intelligence do NexTraceOne.

Provider analítico único: **ClickHouse** (Elasticsearch foi removido).

---

## **1. PACOTE CLICKHOUSE**

### **ClickHouse.Driver 1.2.0**

**Motivos:**
- Suporte explícito a .NET 10.0 (frameworks: net6.0, net8.0, net9.0, **net10.0**)
- Namespace correto: `ClickHouse.Driver`
- API compatível com ADO.NET padrão
- Integração perfeita com Dapper para queries SQL

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

## **2. ESTRATÉGIA DE ARMAZENAMENTO**

### **PostgreSQL (Relacional) - Estado Operacional**

**Entidades Armazenadas:**
- `RuntimeSnapshot` - Estado atual de saúde
- `RuntimeBaseline` - Referência para comparação
- `DriftFinding` - Desvios detectados (necessita transações ACID)
- `ObservabilityProfile` - Perfil de maturidade
- `ChaosExperiment` - Planejamento de experimentos
- `IncidentRecord` - Gestão de incidentes

**Justificativa:**
- Transações ACID garantem consistência
- Relacionamentos complexos entre entidades
- Consultas frequentes com joins
- Dados estruturados com schema rígido

### **ClickHouse (Não Relacional) - Telemetria**

**Dados Armazenados:**
- Eventos brutos (requests, errors, logs, traces)
- Métricas agregadas por time bucket
- Logs estruturados para pesquisa com bloom filters
- System health snapshots históricos
- User activity sessions

**Justificativa:**
- Alta volumetria (>10k eventos/segundo)
- Queries analíticas complexas (agregações temporais)
- Retenção automática via TTL
- Compressão eficiente (até 10x)

---

## **3. CONFIGURAÇÃO NO DI**

```csharp
// DependencyInjection.cs
services.AddScoped<ITelemetrySearchService, ClickHouseLogSearchService>();
```

### **Configuração em appsettings.json**

```json
{
  "Telemetry": {
    "ObservabilityProvider": {
      "Provider": "ClickHouse",
      "ClickHouse": {
        "ConnectionString": "Host=localhost;Port=9000;Database=nextrace_telemetry;Username=default;Password="
      }
    }
  }
}
```

---

## **4. OTIMIZAÇÕES DE PERFORMANCE**

### **Engines Selecionadas**

| Tabela | Engine | Motivo |
|--------|--------|--------|
| `events` | MergeTree | Ordenação temporal eficiente |
| `logs` | MergeTree + ngrambf_v1 | Full-text search com bloom filter |
| `request_metrics_aggregated` | AggregatingMergeTree | Agregações incrementais automáticas |
| `error_patterns` | ReplacingMergeTree | Deduplicação por pattern_id |
| `system_health_snapshots` | MergeTree | Capacity planning histórico |

### **Codecs de Compressão**

```sql
timestamp DateTime CODEC(Delta, ZSTD(1))  -- Delta encoding + compressão
event_id String CODEC(ZSTD(1))            -- ZSTD nível 1 (rápido)
error_message String CODEC(ZSTD(3))       -- ZSTD nível 3 (melhor ratio)
```

### **Índices Secundários**

```sql
ALTER TABLE events ADD INDEX idx_service_env (service_name, environment) TYPE minmax GRANULARITY 4;
ALTER TABLE events ADD INDEX idx_trace_id (trace_id) TYPE bloom_filter GRANULARITY 4;
ALTER TABLE logs ADD INDEX idx_message_fts (message) TYPE ngrambf_v1(3, 1024, 1, 0) GRANULARITY 4;
```

---

## **5. TESTES E VALIDAÇÃO**

### **Testes de Performance**

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

---

## **6. SEGURANÇA E GOVERNANÇA**

### **Autenticação**

```sql
-- Criar usuário readonly
CREATE USER nextrace_reader IDENTIFIED WITH sha256_password BY 'secure_password';
GRANT SELECT ON nextrace_telemetry.* TO nextrace_reader;

-- Criar usuário readwrite
CREATE USER nextrace_writer IDENTIFIED WITH sha256_password BY 'secure_password';
GRANT INSERT, SELECT ON nextrace_telemetry.* TO nextrace_writer;
```

### **Auditoria**

- Todos os acessos ao ClickHouse logados em `system.query_log`
- Tenant isolation via filtros na aplicação (não no banco)

---

## **7. MONITORAMENTO E ALERTAS**

### **Métricas Críticas**

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

### **Alertas Recomendados**

| Métrica | Threshold | Ação |
|---------|-----------|------|
| ClickHouse disk usage | >80% | Expandir storage ou reduzir TTL |
| Query latency p95 | >1s | Otimizar queries ou adicionar índices |
| Ingestion rate drop | <50% do normal | Verificar conectividade |
| Error rate >5% | Imediato | Investigar falhas de conexão |

---

## **8. ESCALABILIDADE**

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

---

## **9. REFERÊNCIAS**

- [ClickHouse Documentation](https://clickhouse.com/docs)
- [ClickHouse.Driver NuGet](https://www.nuget.org/packages/ClickHouse.Driver)
- [ClickHouse vs Elasticsearch for Time Series](https://clickhouse.com/blog/clickhouse-vs-elasticsearch-for-time-series)
- [Optimizing ClickHouse for High Ingestion](https://clickhouse.com/blog/optimizing-clickhouse-for-high-ingestion)

---

**Última Atualização:** 2026-06-07
**Autor:** NexTraceOne Engineering Team
**Status:** ClickHouse como único provider analítico — Elasticsearch completamente removido

# Integrations — ClickHouse Data Placement Review

> **Module:** Integrations (12)  
> **Date:** 2026-03-25  
> **Status:** Data placement defined

---

## 1. Dados que DEVEM ficar no PostgreSQL

| Dados | Tabela | Razão |
|-------|--------|-------|
| Definição de conectores | `int_integration_connectors` | ACID, referential integrity, low volume, CRUD operacional |
| Fontes de ingestão | `int_ingestion_sources` | ACID, FK com connectors, configuração operacional |
| Estado operacional actual | Colunas status/health em connectors + sources | Transaccional, precisa de consistência forte |
| Configuração de conectores | Endpoint, auth, polling, teams | Dados sensíveis, CRUD, referential |
| Credenciais encriptadas | `credential_encrypted` | Segurança, encriptação AES-256-GCM, ACID |
| Retry policy | `max_retry_attempts`, `retry_backoff_seconds` | Configuração operacional, low volume |

---

## 2. Dados que DEVEM ir para ClickHouse (quando pipeline analítico existir)

| Dados | Razão | Volume esperado |
|-------|-------|----------------|
| **Execuções de ingestão (histórico)** | Alto volume, append-only, consultas analíticas de range temporal | 10K-100K/dia por tenant activo |
| **Métricas de performance de conectores** | Time-series, agregações por hora/dia, dashboards | Agregações periódicas |
| **Freshness tracking time-series** | Histórico de lag por domínio ao longo do tempo | 1 registo/fonte/minuto |
| **Health status history** | Histórico de transições de health de conectores | Eventos de transição |

---

## 3. Dados que NÃO devem ir para ClickHouse

| Dados | Razão |
|-------|-------|
| Definição de conectores (CRUD) | Requerem ACID, UPDATE, DELETE, FK |
| Configuração de fontes | Referential integrity, transaccional |
| Credenciais | Segurança, não devem existir fora do PostgreSQL |
| Estado operacional corrente | Precisa de consistência forte e updates frequentes |
| Retry policies | Configuração, não analítico |

---

## 4. Eventos de alto volume

| Evento | Volume estimado | Natureza |
|--------|----------------|----------|
| `IngestionExecution` records | 10K-100K/dia (depende de conectores activos) | Append-only, ideal para ClickHouse |
| Freshness checks | 1/fonte/minuto = ~1.4K/dia por 1 fonte | Time-series |
| Health transitions | Dezenas/dia (event-based) | Baixo volume, mas útil para histórico |

---

## 5. Métricas e agregações relevantes para ClickHouse

| Métrica | Granularidade | Agregação |
|---------|--------------|-----------|
| Execuções por conector por hora | Hora | COUNT, GROUP BY connector_id |
| Taxa de sucesso por conector | Hora/Dia | COUNT(success) / COUNT(total) |
| Duração média de execução | Hora/Dia | AVG(duration_ms) |
| Items processados por domínio | Hora/Dia | SUM(items_processed) GROUP BY data_domain |
| Freshness lag por domínio | Minuto | LAST(lag_minutes) por data_domain |
| Tempo médio de recuperação | Semana | AVG(recovery_time) após falhas |

---

## 6. Chaves de correlação com PostgreSQL

| Chave | PostgreSQL | ClickHouse |
|-------|-----------|------------|
| `connector_id` | PK em `int_integration_connectors` | FK lógica em tabelas ClickHouse |
| `source_id` | PK em `int_ingestion_sources` | FK lógica em tabelas ClickHouse |
| `tenant_id` | RLS filter | Partitioning key |
| `correlation_id` | Em `int_ingestion_executions` | Tracking key |

---

## 7. Nível de necessidade do ClickHouse

| Nível | Valor |
|-------|-------|
| **Decisão** | **RECOMMENDED** |

### Justificação

1. **Execuções de ingestão** são naturalmente append-only e de alto volume — padrão ideal para ClickHouse
2. **Freshness tracking** é time-series puro — ClickHouse é muito superior a PostgreSQL para este tipo de consulta
3. **Métricas de performance** de conectores beneficiam de agregações rápidas sobre grandes volumes
4. O módulo **já tem volume crescente** — cada conector gera execuções periódicas
5. **Não é REQUIRED** porque o módulo funciona com PostgreSQL para volumes iniciais moderados
6. **Não é OPTIONAL_LATER** porque a natureza dos dados (execuções, time-series) justifica ClickHouse desde a fase de design

### Implementação recomendada

- **Fase 1 (actual):** Manter tudo em PostgreSQL com tabela `int_ingestion_executions`
- **Fase 2 (ClickHouse pipeline):** Replicar execuções completadas para ClickHouse via CDC ou batch
- **Fase 3 (optimização):** Mover consultas analíticas de execuções para ClickHouse, manter PostgreSQL para execuções em Running e recentes

---

## 8. Esquema ClickHouse preliminar

### `int_ingestion_executions_ch` (ClickHouse)

```sql
CREATE TABLE int_ingestion_executions_ch (
    id UUID,
    tenant_id UUID,
    connector_id UUID,
    source_id Nullable(UUID),
    connector_name String,
    connector_type String,
    provider String,
    data_domain String,
    correlation_id Nullable(String),
    started_at DateTime64(3, 'UTC'),
    completed_at Nullable(DateTime64(3, 'UTC')),
    duration_ms Nullable(Int64),
    result String,
    items_processed Int32,
    items_succeeded Int32,
    items_failed Int32,
    error_code Nullable(String),
    retry_attempt Int32,
    created_at DateTime64(3, 'UTC')
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(started_at))
ORDER BY (tenant_id, connector_id, started_at)
TTL created_at + INTERVAL 365 DAY;
```

### `int_connector_health_history_ch` (ClickHouse)

```sql
CREATE TABLE int_connector_health_history_ch (
    tenant_id UUID,
    connector_id UUID,
    connector_name String,
    health String,
    previous_health String,
    changed_at DateTime64(3, 'UTC'),
    freshness_lag_minutes Nullable(Int32)
) ENGINE = MergeTree()
PARTITION BY (tenant_id, toYYYYMM(changed_at))
ORDER BY (tenant_id, connector_id, changed_at)
TTL changed_at + INTERVAL 365 DAY;
```

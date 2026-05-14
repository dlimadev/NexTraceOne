-- ============================================================================
-- ClickHouse Schema para NexTraceOne Operational Intelligence
-- ============================================================================
-- Este script cria as tabelas necessárias no ClickHouse para armazenamento
-- de eventos de observabilidade (logs, métricas, traces).
-- 
-- Uso: cat clickhouse_schema.sql | clickhouse-client --database=nextrace_telemetry
-- ============================================================================

-- Criar banco de dados (opcional)
CREATE DATABASE IF NOT EXISTS nextrace_telemetry ENGINE = Atomic;

USE nextrace_telemetry;

-- ============================================================================
-- Tabela: events
-- Descrição: Armazena todos os eventos de observabilidade (requests, errors, metrics, logs, traces)
-- Engine: MergeTree com ordenação por timestamp para queries temporais eficientes
-- ============================================================================
CREATE TABLE IF NOT EXISTS events
(
    timestamp DateTime CODEC(Delta, ZSTD(1)),
    event_id String CODEC(ZSTD(1)),
    event_type LowCardinality(String) CODEC(ZSTD(1)), -- 'request', 'error', 'log', 'trace', 'metric', 'system_health'
    service_name LowCardinality(String) CODEC(ZSTD(1)),
    environment LowCardinality(String) CODEC(ZSTD(1)), -- 'production', 'staging', 'development'
    trace_id String CODEC(ZSTD(1)),
    span_id String CODEC(ZSTD(1)),
    user_id String CODEC(ZSTD(1)),
    endpoint String CODEC(ZSTD(1)),
    http_method LowCardinality(String) CODEC(ZSTD(1)), -- 'GET', 'POST', 'PUT', 'DELETE', etc.
    status_code UInt16 CODEC(ZSTD(1)),
    duration_ms Float64 CODEC(ZSTD(1)),
    error_message String CODEC(ZSTD(3)),
    error_type LowCardinality(String) CODEC(ZSTD(1)),
    tags String CODEC(ZSTD(3)), -- JSON array: ["tag1", "tag2"]
    metadata String CODEC(ZSTD(3)) -- JSON object com metadados adicionais
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(timestamp)
ORDER BY (timestamp, service_name, environment, event_type)
TTL timestamp + INTERVAL 90 DAY
SETTINGS index_granularity = 8192;

-- Índices secundários para queries frequentes
ALTER TABLE events ADD INDEX idx_service_env (service_name, environment) TYPE minmax GRANULARITY 4;
ALTER TABLE events ADD INDEX idx_trace_id (trace_id) TYPE bloom_filter GRANULARITY 4;
ALTER TABLE events ADD INDEX idx_error_type (error_type) TYPE set(100) GRANULARITY 4;

-- ============================================================================
-- Tabela: logs
-- Descrição: Armazena logs estruturados para pesquisa full-text e análise
-- Engine: MergeTree otimizada para buscas por severidade e serviço
-- ============================================================================
CREATE TABLE IF NOT EXISTS logs
(
    log_id String CODEC(ZSTD(1)),
    timestamp DateTime CODEC(Delta, ZSTD(1)),
    service_name LowCardinality(String) CODEC(ZSTD(1)),
    environment LowCardinality(String) CODEC(ZSTD(1)),
    severity LowCardinality(String) CODEC(ZSTD(1)), -- 'debug', 'info', 'warning', 'error', 'critical'
    message String CODEC(ZSTD(3)),
    attributes_json String CODEC(ZSTD(3)) -- JSON object com atributos customizados
)
ENGINE = MergeTree()
PARTITION BY toYYYYMMDD(timestamp)
ORDER BY (timestamp, service_name, severity)
TTL timestamp + INTERVAL 30 DAY
SETTINGS index_granularity = 8192;

-- Índice full-text para busca em mensagens (usando ngrambf_v1)
ALTER TABLE logs ADD INDEX idx_message_fts (message) TYPE ngrambf_v1(3, 1024, 1, 0) GRANULARITY 4;
ALTER TABLE logs ADD INDEX idx_service_severity (service_name, severity) TYPE minmax GRANULARITY 4;

-- ============================================================================
-- Tabela: request_metrics_aggregated
-- Descrição: Métricas de requisições pré-agregadas por minuto para dashboards rápidos
-- Engine: AggregatingMergeTree para agregações incrementais
-- ============================================================================
CREATE TABLE IF NOT EXISTS request_metrics_aggregated
(
    time_bucket DateTime CODEC(Delta, ZSTD(1)),
    service_name LowCardinality(String) CODEC(ZSTD(1)),
    environment LowCardinality(String) CODEC(ZSTD(1)),
    endpoint String CODEC(ZSTD(1)),
    http_method LowCardinality(String) CODEC(ZSTD(1)),
    request_count AggregateFunction(count, UInt64),
    avg_duration AggregateFunction(avg, Float64),
    p50_duration AggregateFunction(quantileTiming(50), Float64),
    p95_duration AggregateFunction(quantileTiming(95), Float64),
    p99_duration AggregateFunction(quantileTiming(99), Float64),
    error_count AggregateFunction(countIf, UInt8),
    error_rate AggregateFunction(avg, Float64)
)
ENGINE = AggregatingMergeTree()
PARTITION BY toYYYYMM(time_bucket)
ORDER BY (time_bucket, service_name, environment, endpoint)
TTL time_bucket + INTERVAL 180 DAY
SETTINGS index_granularity = 8192;

-- Materialized View para popular automaticamente a tabela agregada
CREATE MATERIALIZED VIEW IF NOT EXISTS request_metrics_mv TO request_metrics_aggregated AS
SELECT
    toStartOfMinute(timestamp) AS time_bucket,
    service_name,
    environment,
    endpoint,
    http_method,
    countState() AS request_count,
    avgState(duration_ms) AS avg_duration,
    quantileTimingState(50)(duration_ms) AS p50_duration,
    quantileTimingState(95)(duration_ms) AS p95_duration,
    quantileTimingState(99)(duration_ms) AS p99_duration,
    countIfState(status_code >= 400) AS error_count,
    avgState(if(status_code >= 400, 1.0, 0.0)) AS error_rate
FROM events
WHERE event_type = 'request'
GROUP BY time_bucket, service_name, environment, endpoint, http_method;

-- ============================================================================
-- Tabela: error_patterns
-- Descrição: Padrões de erros identificados automaticamente para detecção de anomalias
-- Engine: ReplacingMergeTree para manter apenas a versão mais recente de cada padrão
-- ============================================================================
CREATE TABLE IF NOT EXISTS error_patterns
(
    pattern_id String CODEC(ZSTD(1)),
    detected_at DateTime CODEC(Delta, ZSTD(1)),
    service_name LowCardinality(String) CODEC(ZSTD(1)),
    environment LowCardinality(String) CODEC(ZSTD(1)),
    error_type LowCardinality(String) CODEC(ZSTD(1)),
    error_signature String CODEC(ZSTD(3)), -- Hash ou fingerprint do erro
    occurrence_count UInt64 CODEC(ZSTD(1)),
    first_seen DateTime CODEC(Delta, ZSTD(1)),
    last_seen DateTime CODEC(Delta, ZSTD(1)),
    affected_endpoints Array(String) CODEC(ZSTD(3)),
    sample_stack_trace String CODEC(ZSTD(3)),
    severity LowCardinality(String) CODEC(ZSTD(1)), -- 'low', 'medium', 'high', 'critical'
    is_resolved UInt8 DEFAULT 0 CODEC(T64, ZSTD(1))
)
ENGINE = ReplacingMergeTree(detected_at)
PARTITION BY toYYYYMM(detected_at)
ORDER BY (service_name, environment, error_type, error_signature)
TTL detected_at + INTERVAL 365 DAY
SETTINGS index_granularity = 8192;

-- ============================================================================
-- Tabela: system_health_snapshots
-- Descrição: Snapshots periódicos de saúde do sistema (CPU, memória, disco, conexões)
-- Engine: MergeTree com TTL longo para capacity planning histórico
-- ============================================================================
CREATE TABLE IF NOT EXISTS system_health_snapshots
(
    snapshot_id String CODEC(ZSTD(1)),
    timestamp DateTime CODEC(Delta, ZSTD(1)),
    service_name LowCardinality(String) CODEC(ZSTD(1)),
    environment LowCardinality(String) CODEC(ZSTD(1)),
    host_name String CODEC(ZSTD(1)),
    cpu_usage_percent Float32 CODEC(ZSTD(1)),
    memory_usage_mb Float64 CODEC(ZSTD(1)),
    memory_usage_percent Float32 CODEC(ZSTD(1)),
    disk_usage_percent Float32 CODEC(ZSTD(1)),
    active_connections UInt32 CODEC(ZSTD(1)),
    requests_per_second Float64 CODEC(ZSTD(1)),
    error_rate_percent Float32 CODEC(ZSTD(1)),
    gc_collections UInt32 CODEC(ZSTD(1)),
    thread_count UInt32 CODEC(ZSTD(1))
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(timestamp)
ORDER BY (timestamp, service_name, environment, host_name)
TTL timestamp + INTERVAL 730 DAY -- 2 anos para capacity planning
SETTINGS index_granularity = 8192;

-- ============================================================================
-- Tabela: user_activity_sessions
-- Descrição: Sessões de atividade de usuários para análise de comportamento
-- Engine: MergeTree com agregações por hora
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_activity_sessions
(
    session_id String CODEC(ZSTD(1)),
    user_id String CODEC(ZSTD(1)),
    start_time DateTime CODEC(Delta, ZSTD(1)),
    end_time DateTime CODEC(Delta, ZSTD(1)),
    service_name LowCardinality(String) CODEC(ZSTD(1)),
    environment LowCardinality(String) CODEC(ZSTD(1)),
    action_count UInt32 CODEC(ZSTD(1)),
    unique_endpoints Array(String) CODEC(ZSTD(3)),
    avg_response_time_ms Float64 CODEC(ZSTD(1)),
    error_count UInt32 CODEC(ZSTD(1)),
    ip_address String CODEC(ZSTD(1)),
    user_agent String CODEC(ZSTD(3))
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(start_time)
ORDER BY (start_time, user_id, service_name)
TTL start_time + INTERVAL 180 DAY
SETTINGS index_granularity = 8192;

-- ============================================================================
-- Configurações de Retenção de Dados (TTL Policies)
-- ============================================================================

-- Logs detalhados: 30 dias
-- Eventos brutos: 90 dias
-- Métricas agregadas: 180 dias
-- Health snapshots: 730 dias (2 anos)
-- User activity: 180 dias
-- Error patterns: 365 dias

-- ============================================================================
-- Verificação de Criação
-- ============================================================================

SHOW TABLES FROM nextrace_telemetry;

SELECT 
    table,
    formatReadableSize(sum(data_compressed_bytes)) AS compressed_size,
    formatReadableSize(sum(data_uncompressed_bytes)) AS uncompressed_size,
    round(sum(data_uncompressed_bytes) / sum(data_compressed_bytes), 2) AS compression_ratio,
    sum(rows) AS total_rows
FROM system.parts
WHERE database = 'nextrace_telemetry' AND active
GROUP BY table
ORDER BY table;

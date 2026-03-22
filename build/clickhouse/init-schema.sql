-- ═══════════════════════════════════════════════════════════════════════════════
-- NexTraceOne — ClickHouse Schema for Observability Data
--
-- Este schema cria as tabelas dedicadas para armazenamento analítico de
-- logs, traces e métricas crus no ClickHouse.
--
-- O ClickHouse é o provider padrão de observabilidade do NexTraceOne.
-- PostgreSQL permanece exclusivo para dados transacionais e de domínio.
--
-- IMPORTANTE:
-- - O ClickHouse é stateful — requer volume persistente
-- - Não usar filesystem efêmero do container
-- - Tabelas usam MergeTree engine com TTL para retenção automática
-- ═══════════════════════════════════════════════════════════════════════════════

CREATE DATABASE IF NOT EXISTS nextraceone_obs;

-- ── Logs ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS nextraceone_obs.otel_logs
(
    Timestamp           DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimestampDate       Date DEFAULT toDate(Timestamp),
    TraceId             String CODEC(ZSTD(1)),
    SpanId              String CODEC(ZSTD(1)),
    TraceFlags          UInt32,
    SeverityText        LowCardinality(String) CODEC(ZSTD(1)),
    SeverityNumber      Int32,
    ServiceName         LowCardinality(String) CODEC(ZSTD(1)),
    Body                String CODEC(ZSTD(1)),
    ResourceSchemaUrl   String CODEC(ZSTD(1)),
    ResourceAttributes  Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ScopeSchemaUrl      String CODEC(ZSTD(1)),
    ScopeName           String CODEC(ZSTD(1)),
    ScopeVersion        String CODEC(ZSTD(1)),
    ScopeAttributes     Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    LogAttributes       Map(LowCardinality(String), String) CODEC(ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(TimestampDate)
ORDER BY (ServiceName, SeverityText, Timestamp)
TTL TimestampDate + INTERVAL 30 DAY
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

-- ── Traces (Spans) ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS nextraceone_obs.otel_traces
(
    Timestamp           DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimestampDate       Date DEFAULT toDate(Timestamp),
    TraceId             String CODEC(ZSTD(1)),
    SpanId              String CODEC(ZSTD(1)),
    ParentSpanId        String CODEC(ZSTD(1)),
    TraceState          String CODEC(ZSTD(1)),
    SpanName            LowCardinality(String) CODEC(ZSTD(1)),
    SpanKind            LowCardinality(String) CODEC(ZSTD(1)),
    ServiceName         LowCardinality(String) CODEC(ZSTD(1)),
    ResourceAttributes  Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ScopeSchemaUrl      String CODEC(ZSTD(1)),
    ScopeName           String CODEC(ZSTD(1)),
    ScopeVersion        String CODEC(ZSTD(1)),
    ScopeAttributes     Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    SpanAttributes      Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    Duration            Int64 CODEC(ZSTD(1)),
    StatusCode          LowCardinality(String) CODEC(ZSTD(1)),
    StatusMessage       String CODEC(ZSTD(1)),
    Events              Nested (
        Timestamp DateTime64(9),
        Name LowCardinality(String),
        Attributes Map(LowCardinality(String), String)
    ) CODEC(ZSTD(1)),
    Links               Nested (
        TraceId String,
        SpanId String,
        TraceState String,
        Attributes Map(LowCardinality(String), String)
    ) CODEC(ZSTD(1))
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(TimestampDate)
ORDER BY (ServiceName, SpanName, Timestamp)
TTL TimestampDate + INTERVAL 30 DAY
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

-- ── Métricas ────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS nextraceone_obs.otel_metrics
(
    Timestamp           DateTime64(9) CODEC(Delta, ZSTD(1)),
    TimestampDate       Date DEFAULT toDate(Timestamp),
    MetricName          LowCardinality(String) CODEC(ZSTD(1)),
    MetricDescription   String CODEC(ZSTD(1)),
    MetricUnit          String CODEC(ZSTD(1)),
    ServiceName         LowCardinality(String) CODEC(ZSTD(1)),
    ResourceAttributes  Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    ScopeSchemaUrl      String CODEC(ZSTD(1)),
    ScopeName           String CODEC(ZSTD(1)),
    ScopeVersion        String CODEC(ZSTD(1)),
    ScopeAttributes     Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    Attributes          Map(LowCardinality(String), String) CODEC(ZSTD(1)),
    Value               Float64 CODEC(ZSTD(1)),
    AggregationTemporality LowCardinality(String) CODEC(ZSTD(1)),
    IsMonotonic         Bool
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(TimestampDate)
ORDER BY (ServiceName, MetricName, Timestamp)
TTL TimestampDate + INTERVAL 90 DAY
SETTINGS index_granularity = 8192, ttl_only_drop_parts = 1;

-- Create database
CREATE DATABASE IF NOT EXISTS nextraceone;

-- Main events table (MergeTree engine for high-performance analytics)
CREATE TABLE IF NOT EXISTS nextraceone.events
(
    timestamp DateTime CODEC(Delta, ZSTD(1)),
    event_id String,
    event_type LowCardinality(String),
    service_name LowCardinality(String),
    environment LowCardinality(String),
    trace_id Nullable(String),
    span_id Nullable(String),
    user_id Nullable(String),
    endpoint Nullable(String),
    http_method Nullable(LowCardinality(String)),
    status_code Nullable(UInt32),
    duration_ms Nullable(UInt64),
    error_message Nullable(String),
    error_type Nullable(String),
    tags String,
    metadata String
)
ENGINE = MergeTree()
PARTITION BY toYYYYMMDD(timestamp)
ORDER BY (timestamp, service_name, event_type)
TTL timestamp + INTERVAL 90 DAY
SETTINGS index_granularity = 8192;

-- Materialized view for request metrics aggregation
CREATE TABLE IF NOT EXISTS nextraceone.request_metrics_agg
(
    time_bucket DateTime,
    endpoint String,
    http_method LowCardinality(String),
    request_count UInt64,
    avg_duration_ms Float64,
    p50_duration_ms Float64,
    p95_duration_ms Float64,
    p99_duration_ms Float64,
    error_count UInt64,
    error_rate Float64
)
ENGINE = SummingMergeTree()
PARTITION BY toYYYYMMDD(time_bucket)
ORDER BY (time_bucket, endpoint, http_method);

-- Materialized view for error analytics
CREATE TABLE IF NOT EXISTS nextraceone.error_analytics_agg
(
    time_bucket DateTime,
    error_type String,
    error_message String,
    service_name LowCardinality(String),
    occurrence_count UInt64,
    affected_endpoints Array(String),
    sample_stack_traces Array(String)
)
ENGINE = AggregatingMergeTree()
PARTITION BY toYYYYMMDD(time_bucket)
ORDER BY (time_bucket, error_type, error_message);

-- System health metrics table
CREATE TABLE IF NOT EXISTS nextraceone.system_health
(
    timestamp DateTime,
    service_name LowCardinality(String),
    cpu_usage_percent Float32,
    memory_usage_mb Float64,
    disk_usage_percent Float32,
    active_connections UInt64,
    requests_per_second Float64,
    error_rate_percent Float32
)
ENGINE = MergeTree()
PARTITION BY toYYYYMMDD(timestamp)
ORDER BY (timestamp, service_name)
TTL timestamp + INTERVAL 30 DAY;

-- Create indexes for common queries
ALTER TABLE nextraceone.events ADD INDEX idx_endpoint (endpoint) TYPE bloom_filter GRANULARITY 4;
ALTER TABLE nextraceone.events ADD INDEX idx_user (user_id) TYPE bloom_filter GRANULARITY 4;
ALTER TABLE nextraceone.events ADD INDEX idx_trace (trace_id) TYPE bloom_filter GRANULARITY 4;

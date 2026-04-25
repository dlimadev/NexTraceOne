namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Backend de storage para onde um StorageBucket encaminha os dados de telemetria.
/// </summary>
public enum StorageBucketBackendType
{
    /// <summary>Elasticsearch (padrão para audit e full-text search).</summary>
    Elasticsearch = 1,

    /// <summary>ClickHouse (para workloads OLAP de alto volume).</summary>
    ClickHouse = 2,

    /// <summary>PostgreSQL (para audit compacto e correlação transaccional).</summary>
    PostgreSQL = 3
}

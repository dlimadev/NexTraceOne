namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Backend de storage para onde um StorageBucket encaminha os dados de telemetria.
/// </summary>
public enum StorageBucketBackendType
{
    /// <summary>ClickHouse (padrão para workloads OLAP de alto volume).</summary>
    ClickHouse = 1,

    /// <summary>PostgreSQL (para audit compacto e correlação transaccional).</summary>
    PostgreSQL = 2
}

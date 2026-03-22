namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

/// <summary>
/// Tipos de sinal de telemetria suportados pela plataforma.
/// Alinhado com os três pilares do OpenTelemetry: traces, metrics e logs.
/// Cada tipo segue pipeline, retenção e storage distintos.
/// </summary>
public enum TelemetrySignalType
{
    /// <summary>Traces distribuídos (spans) — traces crus vão para o provider de observabilidade.</summary>
    Traces = 1,

    /// <summary>Métricas de runtime — agregados vão para Product Store, alta cardinalidade para provider de observabilidade.</summary>
    Metrics = 2,

    /// <summary>Logs estruturados — logs crus vão para o provider de observabilidade.</summary>
    Logs = 3
}

/// <summary>
/// Nível de agregação de dados de telemetria no Product Store.
/// Define a granularidade temporal dos dados armazenados em PostgreSQL.
/// </summary>
public enum AggregationLevel
{
    /// <summary>Dados crus (não armazenados no Product Store — ficam no provider de observabilidade).</summary>
    Raw = 0,

    /// <summary>Agregados por minuto — alta granularidade para investigação recente.</summary>
    OneMinute = 1,

    /// <summary>Agregados por hora — granularidade média para tendências e dashboards.</summary>
    OneHour = 2,

    /// <summary>Agregados por dia — consolidação para relatórios de longo prazo.</summary>
    OneDay = 3
}

/// <summary>
/// Tier de armazenamento no modelo hot/warm/cold.
/// Determina o backend e a performance de acesso dos dados.
/// </summary>
public enum StorageTier
{
    /// <summary>Hot — SSD/memória, acesso imediato, custo alto por GB.</summary>
    Hot = 1,

    /// <summary>Warm — disco standard, acesso com pequena latência, custo médio.</summary>
    Warm = 2,

    /// <summary>Cold — object storage (S3, MinIO), compliance de longo prazo, custo baixo.</summary>
    Cold = 3
}

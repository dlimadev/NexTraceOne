namespace NexTraceOne.BuildingBlocks.Observability.Analytics.Configuration;

/// <summary>
/// Configuração central da camada analítica ClickHouse do NexTraceOne.
/// Define connection string, timeouts, modo de escrita e comportamento de falha.
/// </summary>
public sealed class AnalyticsOptions
{
    /// <summary>Chave da secção de configuração no appsettings.json.</summary>
    public const string SectionName = "Analytics";

    /// <summary>
    /// Habilita a escrita para ClickHouse.
    /// Quando false, NullAnalyticsWriter é usado (sem I/O analítico).
    /// Padrão: false — activar explicitamente quando ClickHouse estiver disponível.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Connection string do ClickHouse para a base de dados analítica de domínio.
    /// Base de dados alvo: nextraceone_analytics
    /// Formato: http://clickhouse:8123/?database=nextraceone_analytics
    /// </summary>
    public string ConnectionString { get; set; } = "http://clickhouse:8123/?database=nextraceone_analytics";

    /// <summary>
    /// Timeout em segundos para operações de escrita no ClickHouse.
    /// Padrão: 10 segundos.
    /// </summary>
    public int WriteTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Número máximo de registos por batch de escrita.
    /// Escritas em batch são mais eficientes no ClickHouse.
    /// Padrão: 500.
    /// </summary>
    public int MaxBatchSize { get; set; } = 500;

    /// <summary>
    /// Quando true, falhas de escrita no ClickHouse são apenas logadas (não propagadas).
    /// Mantém o domínio transacional funcional mesmo quando ClickHouse está indisponível.
    /// Padrão: true — analytics nunca deve bloquear o fluxo de domínio.
    /// </summary>
    public bool SuppressWriteErrors { get; set; } = true;
}

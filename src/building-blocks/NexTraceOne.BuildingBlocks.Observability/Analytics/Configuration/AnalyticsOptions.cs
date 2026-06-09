namespace NexTraceOne.BuildingBlocks.Observability.Analytics.Configuration;

/// <summary>
/// Configuração central da camada analítica do NexTraceOne.
/// Provider: ClickHouse.
/// Define endpoint, timeouts, modo de escrita e comportamento de falha.
/// </summary>
public sealed class AnalyticsOptions
{
    /// <summary>Chave da secção de configuração no appsettings.json.</summary>
    public const string SectionName = "Analytics";

    /// <summary>
    /// Habilita a escrita para o storage analítico (ClickHouse).
    /// Quando false, NullAnalyticsWriter é usado (sem I/O analítico).
    /// Padrão: false — activar explicitamente quando ClickHouse estiver disponível.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Endpoint do storage analítico ClickHouse.
    /// Exemplo: http://clickhouse:8123/?database=nextraceone_analytics
    /// </summary>
    public string ConnectionString { get; set; } = "http://clickhouse:8123";

    /// <summary>
    /// Token de autenticação ClickHouse (opcional).
    /// Deixar vazio quando autenticação não está configurada (desenvolvimento local).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Prefixo para tabelas criadas pelo NexTraceOne no ClickHouse.
    /// Exemplo: "nextraceone-analytics" gera tabelas como "nextraceone-analytics-pan-events".
    /// </summary>
    public string TablePrefix { get; set; } = "nextraceone-analytics";

    /// <summary>
    /// Timeout em segundos para operações de escrita.
    /// Padrão: 10 segundos.
    /// </summary>
    public int WriteTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Número máximo de registos por batch de escrita.
    /// Escritas em batch são mais eficientes via Bulk API.
    /// Padrão: 500.
    /// </summary>
    public int MaxBatchSize { get; set; } = 500;

    /// <summary>
    /// Quando true, falhas de escrita no storage analítico são apenas logadas (não propagadas).
    /// Mantém o domínio transacional funcional mesmo quando o storage analítico está indisponível.
    /// Padrão: true — analytics nunca deve bloquear o fluxo de domínio.
    /// </summary>
    public bool SuppressWriteErrors { get; set; } = true;
}

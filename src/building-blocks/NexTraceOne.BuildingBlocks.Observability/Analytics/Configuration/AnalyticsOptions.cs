namespace NexTraceOne.BuildingBlocks.Observability.Analytics.Configuration;

/// <summary>
/// Configuração central da camada analítica do NexTraceOne.
/// Provider padrão: Elasticsearch. ClickHouse mantém-se como alternativa.
/// Define endpoint, timeouts, modo de escrita e comportamento de falha.
/// </summary>
public sealed class AnalyticsOptions
{
    /// <summary>Chave da secção de configuração no appsettings.json.</summary>
    public const string SectionName = "Analytics";

    /// <summary>
    /// Habilita a escrita para o storage analítico (Elasticsearch por padrão).
    /// Quando false, NullAnalyticsWriter é usado (sem I/O analítico).
    /// Padrão: false — activar explicitamente quando Elasticsearch estiver disponível.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Endpoint do storage analítico.
    /// Para Elasticsearch: http://elasticsearch:9200
    /// Para ClickHouse (alternativa): http://clickhouse:8123/?database=nextraceone_analytics
    ///
    /// NOTA: O nome "ConnectionString" é mantido por backward-compatibility com
    /// configurações existentes (Analytics:ConnectionString). Para Elasticsearch,
    /// o valor esperado é o URL base do cluster (ex: http://elasticsearch:9200).
    /// </summary>
    public string ConnectionString { get; set; } = "http://elasticsearch:9200";

    /// <summary>
    /// API Key para autenticação no Elasticsearch (recomendado sobre user/password).
    /// Deixar vazio quando xpack.security.enabled=false (desenvolvimento local).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Prefixo para índices criados pelo NexTraceOne no Elasticsearch.
    /// Exemplo: "nextraceone-analytics" gera índices como "nextraceone-analytics-pan-events".
    /// </summary>
    public string IndexPrefix { get; set; } = "nextraceone-analytics";

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

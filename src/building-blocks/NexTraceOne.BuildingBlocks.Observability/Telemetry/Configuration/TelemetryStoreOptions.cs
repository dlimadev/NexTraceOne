namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;

/// <summary>
/// Configuração central de telemetria da plataforma NexTraceOne.
/// Define a separação entre Product Store (PostgreSQL — agregados, correlações, topologia)
/// e Telemetry Store (storage especializado — traces e logs crus em alto volume).
///
/// Princípio arquitetural: o NexTraceOne é OpenTelemetry-native na ingestão,
/// correlation-first no produto e storage-aware na persistência.
///
/// PostgreSQL NÃO deve ser usado como storage principal de logs/traces crus em larga escala.
/// Os dados crus residem em backends especializados (Tempo, Loki, ClickHouse, etc.),
/// e o Product Store mantém apenas referências, agregados e contextos investigativos.
/// </summary>
public sealed class TelemetryStoreOptions
{
    /// <summary>Chave da seção de configuração no appsettings.json.</summary>
    public const string SectionName = "Telemetry";

    /// <summary>
    /// Configuração do Product Store (PostgreSQL).
    /// Armazena: métricas agregadas, topologia observada, anomalias, correlações,
    /// contextos investigativos e referências para dados crus no Telemetry Store.
    /// </summary>
    public ProductStoreOptions ProductStore { get; set; } = new();

    /// <summary>
    /// Configuração do Telemetry Store (storage especializado para traces e logs crus).
    /// Backends suportados: Tempo (traces), Loki (logs), ClickHouse, ou qualquer
    /// backend compatível com OTLP via OpenTelemetry Collector.
    /// </summary>
    public TelemetryBackendOptions TelemetryStore { get; set; } = new();

    /// <summary>
    /// Configuração do OpenTelemetry Collector usado como pipeline de ingestão.
    /// Define endpoint OTLP, receivers habilitados e configurações de pipeline.
    /// </summary>
    public CollectorOptions Collector { get; set; } = new();

    /// <summary>
    /// Políticas de retenção por tipo de sinal e nível de agregação.
    /// Controla hot/warm/cold e TTL de cada categoria de dado de telemetria.
    /// </summary>
    public RetentionPolicyOptions Retention { get; set; } = new();
}

/// <summary>
/// Configuração do Product Store (PostgreSQL) para dados agregados de telemetria.
/// O PostgreSQL serve como store operacional de métricas, topologia e correlações,
/// nunca como storage principal de alto volume para traces/logs crus.
/// </summary>
public sealed class ProductStoreOptions
{
    /// <summary>
    /// Connection string do PostgreSQL para dados agregados de telemetria.
    /// Pode apontar para o mesmo banco do produto ou para um schema separado.
    /// Default: usa a connection string principal "NexTraceOne".
    /// </summary>
    public string ConnectionStringName { get; set; } = "NexTraceOne";

    /// <summary>
    /// Schema PostgreSQL dedicado a tabelas de telemetria agregada.
    /// Separar em schema próprio facilita gestão de partições e retenção.
    /// </summary>
    public string Schema { get; set; } = "telemetry";

    /// <summary>
    /// Habilita particionamento automático por tempo nas tabelas de métricas.
    /// Essencial para performance e gestão de retenção em ambientes enterprise.
    /// </summary>
    public bool EnableTimePartitioning { get; set; } = true;

    /// <summary>
    /// Intervalo de particionamento para tabelas de métricas por minuto.
    /// Default: partições diárias para métricas de alta frequência.
    /// </summary>
    public string MinuteMetricsPartitionInterval { get; set; } = "1 day";

    /// <summary>
    /// Intervalo de particionamento para tabelas de métricas por hora.
    /// Default: partições mensais para métricas já consolidadas.
    /// </summary>
    public string HourlyMetricsPartitionInterval { get; set; } = "1 month";
}

/// <summary>
/// Configuração dos backends de Telemetry Store para traces e logs crus.
/// Estes backends recebem dados crus diretamente do OpenTelemetry Collector,
/// sem passar pelo PostgreSQL do produto.
/// </summary>
public sealed class TelemetryBackendOptions
{
    /// <summary>
    /// Backend para armazenamento de traces crus.
    /// Exemplos: "tempo" (Grafana Tempo), "jaeger", "clickhouse", "otlp".
    /// O Collector exporta traces diretamente para este backend.
    /// </summary>
    public string TracesBackend { get; set; } = "tempo";

    /// <summary>
    /// Endpoint do backend de traces (ex: "http://tempo:3200", "http://jaeger:4317").
    /// </summary>
    public string TracesEndpoint { get; set; } = "http://localhost:3200";

    /// <summary>
    /// Backend para armazenamento de logs crus.
    /// Exemplos: "loki" (Grafana Loki), "elasticsearch", "clickhouse", "otlp".
    /// O Collector exporta logs diretamente para este backend.
    /// </summary>
    public string LogsBackend { get; set; } = "loki";

    /// <summary>
    /// Endpoint do backend de logs (ex: "http://loki:3100", "http://elasticsearch:9200").
    /// </summary>
    public string LogsEndpoint { get; set; } = "http://localhost:3100";

    /// <summary>
    /// Backend para métricas de alta cardinalidade (opcional).
    /// Métricas agregadas de baixa cardinalidade ficam no Product Store (PostgreSQL).
    /// Métricas de alta cardinalidade podem ir para Mimir, Prometheus, ClickHouse.
    /// </summary>
    public string? MetricsBackend { get; set; }

    /// <summary>Endpoint do backend de métricas de alta cardinalidade (opcional).</summary>
    public string? MetricsEndpoint { get; set; }
}

/// <summary>
/// Configuração do OpenTelemetry Collector — pipeline central de ingestão.
/// O Collector normaliza, enriquece, filtra e roteia sinais de telemetria
/// para os backends corretos (Product Store e Telemetry Store).
/// </summary>
public sealed class CollectorOptions
{
    /// <summary>
    /// Endpoint gRPC do Collector OTLP (ex: "http://localhost:4317").
    /// Usado por receivers OTLP para ingestão de traces, metrics e logs.
    /// </summary>
    public string OtlpGrpcEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Endpoint HTTP do Collector OTLP (ex: "http://localhost:4318").
    /// Alternativa HTTP para ambientes que não suportam gRPC.
    /// </summary>
    public string OtlpHttpEndpoint { get; set; } = "http://localhost:4318";

    /// <summary>
    /// Habilita o receiver Prometheus no Collector para scraping de métricas.
    /// Útil para workloads que expõem /metrics no formato Prometheus.
    /// </summary>
    public bool EnablePrometheusReceiver { get; set; }

    /// <summary>
    /// Limite de memória em MB para o memory_limiter processor do Collector.
    /// Protege contra OOM quando o volume de ingestão excede a capacidade.
    /// </summary>
    public int MemoryLimitMb { get; set; } = 512;

    /// <summary>
    /// Percentual de spike no memory_limiter (proteção contra picos súbitos).
    /// </summary>
    public int MemorySpikeLimitMb { get; set; } = 128;

    /// <summary>
    /// Tamanho do batch no batch processor (número de spans/logs/metrics por batch).
    /// </summary>
    public int BatchSize { get; set; } = 8192;

    /// <summary>
    /// Timeout do batch processor em milissegundos.
    /// Se o batch não atingir o tamanho máximo, envia após este timeout.
    /// </summary>
    public int BatchTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Taxa de amostragem para traces (0.0 a 1.0).
    /// Em produção enterprise, valor típico: 0.1 (10% dos traces).
    /// Pode ser ajustado dinamicamente via configuração.
    /// </summary>
    public double TracesSamplingRate { get; set; } = 1.0;
}

/// <summary>
/// Políticas de retenção por tipo de sinal e nível de agregação.
/// Separa claramente a retenção de dados crus (curta) da retenção
/// de dados agregados (média/longa) e de auditoria (compliance).
///
/// Modelo hot/warm/cold:
/// - Hot: dados acessíveis imediatamente (SSD/memória)
/// - Warm: dados acessíveis com pequena latência (disco standard)
/// - Cold: dados em object storage para compliance de longo prazo
/// </summary>
public sealed class RetentionPolicyOptions
{
    /// <summary>
    /// Retenção de traces crus no Telemetry Store.
    /// Default: 7 dias em hot, 30 dias em warm (enterprise típico).
    /// </summary>
    public RetentionTier RawTraces { get; set; } = new()
    {
        HotDays = 7,
        WarmDays = 30,
        ColdDays = 0
    };

    /// <summary>
    /// Retenção de logs crus no Telemetry Store.
    /// Default: 7 dias em hot, 30 dias em warm.
    /// </summary>
    public RetentionTier RawLogs { get; set; } = new()
    {
        HotDays = 7,
        WarmDays = 30,
        ColdDays = 0
    };

    /// <summary>
    /// Retenção de métricas agregadas por minuto (Product Store — PostgreSQL).
    /// Default: 7 dias. Após expiração, são consolidadas para nível horário.
    /// </summary>
    public RetentionTier MinuteAggregates { get; set; } = new()
    {
        HotDays = 7,
        WarmDays = 0,
        ColdDays = 0
    };

    /// <summary>
    /// Retenção de métricas agregadas por hora (Product Store — PostgreSQL).
    /// Default: 90 dias em hot, 365 dias em warm.
    /// </summary>
    public RetentionTier HourlyAggregates { get; set; } = new()
    {
        HotDays = 90,
        WarmDays = 365,
        ColdDays = 0
    };

    /// <summary>
    /// Retenção de snapshots, anomalias e contextos investigativos.
    /// Default: 90 dias em hot, 365 dias em warm.
    /// </summary>
    public RetentionTier Snapshots { get; set; } = new()
    {
        HotDays = 90,
        WarmDays = 365,
        ColdDays = 0
    };

    /// <summary>
    /// Retenção de topologia observada agregada.
    /// Default: 90 dias (apenas a versão mais recente é operacional).
    /// </summary>
    public RetentionTier ObservedTopology { get; set; } = new()
    {
        HotDays = 90,
        WarmDays = 0,
        ColdDays = 0
    };

    /// <summary>
    /// Retenção de dados de auditoria e compliance.
    /// SEPARADA da observabilidade — segue política regulatória.
    /// Default: 365 dias em hot, 2555 dias (7 anos) em cold.
    /// </summary>
    public RetentionTier AuditCompliance { get; set; } = new()
    {
        HotDays = 365,
        WarmDays = 0,
        ColdDays = 2555
    };
}

/// <summary>
/// Tier de retenção hot/warm/cold com TTL em dias.
/// Zero significa que o tier não está ativo para aquele tipo de dado.
/// </summary>
public sealed class RetentionTier
{
    /// <summary>Dias de retenção em storage hot (SSD, acesso imediato).</summary>
    public int HotDays { get; set; }

    /// <summary>Dias de retenção em storage warm (disco standard, acesso com latência).</summary>
    public int WarmDays { get; set; }

    /// <summary>Dias de retenção em storage cold (object storage, compliance).</summary>
    public int ColdDays { get; set; }

    /// <summary>
    /// Total de dias de retenção considerando todos os tiers.
    /// Útil para calcular janela de busca máxima por tipo de sinal.
    /// </summary>
    public int TotalRetentionDays => HotDays + WarmDays + ColdDays;
}

namespace NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;

/// <summary>
/// Configuração central de telemetria da plataforma NexTraceOne.
/// Define a separação entre Product Store (PostgreSQL — agregados, correlações, topologia)
/// e Observability Provider (storage analítico configurável — ClickHouse ou Elastic).
///
/// Princípio arquitetural: o NexTraceOne é OpenTelemetry-native na ingestão,
/// correlation-first no produto e storage-aware na persistência.
///
/// PostgreSQL NÃO deve ser usado como storage principal de logs/traces crus em larga escala.
/// Os dados crus residem em providers analíticos configuráveis (ClickHouse, Elastic),
/// e o Product Store mantém apenas referências, agregados e contextos investigativos.
///
/// A plataforma trata coleta, transporte, storage e análise como preocupações separadas.
/// </summary>
public sealed class TelemetryStoreOptions
{
    /// <summary>Chave da seção de configuração no appsettings.json.</summary>
    public const string SectionName = "Telemetry";

    /// <summary>
    /// Configuração do Product Store (PostgreSQL).
    /// Armazena: métricas agregadas, topologia observada, anomalias, correlações,
    /// contextos investigativos e referências para dados crus no provider de observabilidade.
    /// </summary>
    public ProductStoreOptions ProductStore { get; set; } = new();

    /// <summary>
    /// Configuração do provider de observabilidade (storage analítico para traces, logs e métricas crus).
    /// Providers suportados: ClickHouse, Elastic.
    /// A escolha do provider é feita por configuração e não acopla o domínio do NexTraceOne.
    /// </summary>
    public ObservabilityProviderOptions ObservabilityProvider { get; set; } = new();

    /// <summary>
    /// Configuração do modo de coleta de telemetria por ambiente.
    /// Modos suportados: OpenTelemetryCollector (Kubernetes), ClrProfiler (IIS/Windows).
    /// </summary>
    public CollectionModeOptions CollectionMode { get; set; } = new();

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
/// Configuração do provider de observabilidade — storage analítico configurável.
/// O NexTraceOne suporta dois providers: Elastic (padrão) e ClickHouse (alternativa).
/// A empresa escolhe o provider por configuração, sem acoplar o domínio.
///
/// Combinações suportadas:
/// - CLR Profiler + Elastic
/// - CLR Profiler + ClickHouse
/// - OpenTelemetry Collector + Elastic
/// - OpenTelemetry Collector + ClickHouse
/// </summary>
public sealed class ObservabilityProviderOptions
{
    /// <summary>
    /// Provider ativo de observabilidade: "Elastic" ou "ClickHouse".
    /// Determina onde traces, logs e métricas crus são armazenados e consultados.
    /// Padrão: Elastic. ClickHouse mantém-se como opção alternativa.
    /// </summary>
    public string Provider { get; set; } = "Elastic";

    /// <summary>
    /// Configuração do provider ClickHouse.
    /// Usado quando Provider = "ClickHouse".
    /// ClickHouse é stateful — requer volume persistente.
    /// </summary>
    public ClickHouseProviderOptions ClickHouse { get; set; } = new();

    /// <summary>
    /// Configuração do provider Elastic.
    /// Usado quando Provider = "Elastic".
    /// Prioriza integração com stack Elastic já existente na empresa.
    /// </summary>
    public ElasticProviderOptions Elastic { get; set; } = new();
}

/// <summary>
/// Configuração do provider ClickHouse para armazenamento analítico de observabilidade.
/// Usado para logs, traces e métricas de alta cardinalidade em larga escala.
///
/// ClickHouse é stateful — não usar filesystem efêmero do container como armazenamento.
/// Utilizar volume persistente e documentar claramente.
/// </summary>
public sealed class ClickHouseProviderOptions
{
    /// <summary>Habilita o provider ClickHouse (alternativa ao Elastic).</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Connection string do ClickHouse.
    /// Formato: Host=clickhouse;Port=8123;Database=nextraceone_obs;Username=default;Password=
    /// </summary>
    public string ConnectionString { get; set; } = "Host=clickhouse;Port=8123;Database=nextraceone_obs;Username=default;Password=";

    /// <summary>Database dedicada para dados de observabilidade.</summary>
    public string Database { get; set; } = "nextraceone_obs";

    /// <summary>
    /// Retenção em dias para logs no ClickHouse. Default: 30 dias.
    /// Dados expirados são removidos automaticamente via TTL.
    /// </summary>
    public int LogsRetentionDays { get; set; } = 30;

    /// <summary>
    /// Retenção em dias para traces no ClickHouse. Default: 30 dias.
    /// </summary>
    public int TracesRetentionDays { get; set; } = 30;

    /// <summary>
    /// Retenção em dias para métricas no ClickHouse. Default: 90 dias.
    /// </summary>
    public int MetricsRetentionDays { get; set; } = 90;
}

/// <summary>
/// Configuração do provider Elastic para armazenamento analítico de observabilidade.
/// Prioriza integração com stack Elastic já existente na empresa,
/// evitando duplicação desnecessária de infraestrutura.
/// </summary>
public sealed class ElasticProviderOptions
{
    /// <summary>Habilita o provider Elastic (padrão).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Endpoint do cluster Elastic (ex: "https://elastic.example.com:9200").</summary>
    public string Endpoint { get; set; } = "http://elasticsearch:9200";

    /// <summary>API Key para autenticação no Elastic (recomendado sobre user/password).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Prefixo para índices criados pelo NexTraceOne no Elastic.
    /// Exemplo: "nextraceone" gera índices como "nextraceone-logs-2024.01.15".
    /// </summary>
    public string IndexPrefix { get; set; } = "nextraceone";

    /// <summary>
    /// Retenção em dias para logs no Elastic. Default: 30 dias.
    /// Managed via ILM (Index Lifecycle Management).
    /// </summary>
    public int LogsRetentionDays { get; set; } = 30;

    /// <summary>Retenção em dias para traces no Elastic. Default: 30 dias.</summary>
    public int TracesRetentionDays { get; set; } = 30;

    /// <summary>Retenção em dias para métricas no Elastic. Default: 90 dias.</summary>
    public int MetricsRetentionDays { get; set; } = 90;
}

/// <summary>
/// Configuração do modo de coleta de telemetria por ambiente.
/// O NexTraceOne reconhece que nem todos os clientes terão a mesma topologia operacional.
///
/// Modos suportados:
/// - OpenTelemetryCollector: para ambientes Kubernetes (padrão)
/// - ClrProfiler: para ambientes IIS/Windows com aplicações .NET
///
/// A modelagem separa como os dados são coletados, armazenados e analisados.
/// </summary>
public sealed class CollectionModeOptions
{
    /// <summary>
    /// Modo de coleta ativo: "OpenTelemetryCollector" ou "ClrProfiler".
    /// Default: "OpenTelemetryCollector" (mais comum em ambientes Kubernetes).
    /// </summary>
    public string ActiveMode { get; set; } = "OpenTelemetryCollector";

    /// <summary>
    /// Configuração do OpenTelemetry Collector (modo Kubernetes).
    /// Usado quando ActiveMode = "OpenTelemetryCollector".
    /// </summary>
    public OpenTelemetryCollectorModeOptions OpenTelemetryCollector { get; set; } = new();

    /// <summary>
    /// Configuração do CLR Profiler (modo IIS/Windows).
    /// Usado quando ActiveMode = "ClrProfiler".
    /// </summary>
    public ClrProfilerModeOptions ClrProfiler { get; set; } = new();
}

/// <summary>
/// Configuração do modo de coleta via OpenTelemetry Collector (Kubernetes).
/// O Collector atua como pipeline de ingestão, normalização e roteamento de telemetria.
/// </summary>
public sealed class OpenTelemetryCollectorModeOptions
{
    /// <summary>Habilita o modo de coleta via OpenTelemetry Collector.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Endpoint gRPC do Collector OTLP (ex: "http://otel-collector:4317").
    /// Obrigatório — configurar via appsettings.{Environment}.json ou variável de ambiente.
    /// </summary>
    public string OtlpGrpcEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint HTTP do Collector OTLP (ex: "http://otel-collector:4318").
    /// Obrigatório — configurar via appsettings.{Environment}.json ou variável de ambiente.
    /// </summary>
    public string OtlpHttpEndpoint { get; set; } = string.Empty;
}

/// <summary>
/// Configuração do modo de coleta via CLR Profiler (IIS/Windows).
/// Para aplicações .NET hospedadas em IIS com menor intrusão manual.
/// O profiler captura sinais relevantes sem exigir reescrita da aplicação.
/// </summary>
public sealed class ClrProfilerModeOptions
{
    /// <summary>Habilita o modo de coleta via CLR Profiler.</summary>
    public bool Enabled { get; set; }

    /// <summary>Modo de hospedagem: "IIS" ou "SelfHosted".</summary>
    public string Mode { get; set; } = "IIS";

    /// <summary>
    /// Tipo de profiler: "AutoInstrumentation" (recomendado) ou "Manual".
    /// Auto-instrumentação captura sinais sem alterações no código da aplicação.
    /// </summary>
    public string ProfilerType { get; set; } = "AutoInstrumentation";

    /// <summary>
    /// Destino de exportação dos dados coletados pelo profiler.
    /// "Collector" envia via OTLP para o OTel Collector.
    /// "Direct" envia diretamente para o provider configurado.
    /// </summary>
    public string ExportTarget { get; set; } = "Collector";

    /// <summary>
    /// Endpoint OTLP do destino de exportação.
    /// Quando ExportTarget = "Collector", aponta para o OTel Collector.
    /// Obrigatório — configurar via appsettings.{Environment}.json ou variável de ambiente.
    /// </summary>
    public string OtlpEndpoint { get; set; } = string.Empty;
}

/// <summary>
/// Configuração do OpenTelemetry Collector — pipeline central de ingestão.
/// O Collector normaliza, enriquece, filtra e roteia sinais de telemetria
/// para o provider de observabilidade configurado (Elastic ou ClickHouse).
/// </summary>
public sealed class CollectorOptions
{
    /// <summary>
    /// Endpoint gRPC do Collector OTLP (ex: "http://otel-collector:4317").
    /// Default aponta para localhost com porta padrão OTLP gRPC.
    /// Em produção, configurar via appsettings.{Environment}.json ou variável de ambiente.
    /// </summary>
    public string OtlpGrpcEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Endpoint HTTP do Collector OTLP (ex: "http://otel-collector:4318").
    /// Default aponta para localhost com porta padrão OTLP HTTP.
    /// Em produção, configurar via appsettings.{Environment}.json ou variável de ambiente.
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

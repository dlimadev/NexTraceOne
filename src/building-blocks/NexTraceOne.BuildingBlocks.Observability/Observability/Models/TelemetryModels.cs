namespace NexTraceOne.BuildingBlocks.Observability.Observability.Models;

/// <summary>
/// Constantes para os valores de ServiceKind inferidos pelo SpanKindResolver.
/// Baseados nas convenções semânticas OpenTelemetry.
/// </summary>
public static class ServiceKindValues
{
    /// <summary>HTTP/REST — presença de http.method ou http.request.method nas SpanAttributes.</summary>
    public const string Rest = "REST";

    /// <summary>SOAP/WS — presença de rpc.system = "soap" nas SpanAttributes.</summary>
    public const string Soap = "SOAP";

    /// <summary>Apache Kafka — presença de messaging.system = "kafka" nas SpanAttributes.</summary>
    public const string Kafka = "Kafka";

    /// <summary>Processamento em background ou lotes (SpanKind = Internal sem atributos de rede).</summary>
    public const string Background = "Background";

    /// <summary>Base de dados — presença de db.system nas SpanAttributes.</summary>
    public const string Db = "DB";

    /// <summary>gRPC — presença de rpc.system = "grpc" nas SpanAttributes.</summary>
    public const string GRpc = "gRPC";

    /// <summary>Mensageria genérica (RabbitMQ, etc.) — messaging.system != kafka.</summary>
    public const string Messaging = "Messaging";

    /// <summary>Tipo não determinado ou instrumentação incompleta.</summary>
    public const string Unknown = "Unknown";
}

/// <summary>
/// Entrada de log orientada ao produto NexTraceOne.
/// Modelagem independente do provider (ClickHouse ou Elastic).
/// </summary>
public sealed record LogEntry
{
    /// <summary>Timestamp do log.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Ambiente (production, staging, development).</summary>
    public required string Environment { get; init; }

    /// <summary>Nome do serviço que gerou o log.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Nome da aplicação.</summary>
    public string? ApplicationName { get; init; }

    /// <summary>Módulo ou componente interno.</summary>
    public string? ModuleName { get; init; }

    /// <summary>Nível do log (Information, Warning, Error, Critical, Debug, Trace).</summary>
    public required string Level { get; init; }

    /// <summary>Mensagem do log.</summary>
    public required string Message { get; init; }

    /// <summary>Exceção completa (quando aplicável).</summary>
    public string? Exception { get; init; }

    /// <summary>Trace ID para correlação com traces distribuídos.</summary>
    public string? TraceId { get; init; }

    /// <summary>Span ID para correlação granular.</summary>
    public string? SpanId { get; init; }

    /// <summary>Correlation ID para correlação de negócio.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Nome do host/servidor.</summary>
    public string? HostName { get; init; }

    /// <summary>Nome do container (quando em Kubernetes).</summary>
    public string? ContainerName { get; init; }

    /// <summary>Tenant ID (quando aplicável em cenários multi-tenant).</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Atributos adicionais relevantes.</summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}

/// <summary>
/// Resumo de um trace distribuído para listagens e dashboards.
/// </summary>
public sealed record TraceSummary
{
    /// <summary>Identificador único do trace.</summary>
    public required string TraceId { get; init; }

    /// <summary>Nome do serviço raiz do trace.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Nome da operação raiz.</summary>
    public required string OperationName { get; init; }

    /// <summary>Timestamp de início do trace.</summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>Duração total em milissegundos.</summary>
    public required double DurationMs { get; init; }

    /// <summary>Código de status do span raiz.</summary>
    public string? StatusCode { get; init; }

    /// <summary>Ambiente.</summary>
    public required string Environment { get; init; }

    /// <summary>Número total de spans no trace.</summary>
    public int SpanCount { get; init; }

    /// <summary>Indica se o trace contém erros.</summary>
    public bool HasErrors { get; init; }

    /// <summary>
    /// Tipo de serviço inferido do span raiz (REST, SOAP, Kafka, Background, DB, gRPC, Unknown).
    /// Deriva das convenções semânticas OpenTelemetry dos atributos do span.
    /// </summary>
    public string RootServiceKind { get; init; } = ServiceKindValues.Unknown;
}

/// <summary>
/// Detalhes completos de um trace, incluindo todos os spans.
/// </summary>
public sealed record TraceDetail
{
    /// <summary>Identificador único do trace.</summary>
    public required string TraceId { get; init; }

    /// <summary>Lista de spans do trace.</summary>
    public required IReadOnlyList<SpanDetail> Spans { get; init; }

    /// <summary>Duração total em milissegundos.</summary>
    public required double DurationMs { get; init; }

    /// <summary>Serviços envolvidos no trace.</summary>
    public required IReadOnlyList<string> Services { get; init; }
}

/// <summary>
/// Detalhe de um span individual dentro de um trace.
/// </summary>
public sealed record SpanDetail
{
    /// <summary>Trace ID ao qual este span pertence.</summary>
    public required string TraceId { get; init; }

    /// <summary>Identificador único do span.</summary>
    public required string SpanId { get; init; }

    /// <summary>Span ID do pai (null para root span).</summary>
    public string? ParentSpanId { get; init; }

    /// <summary>Nome do serviço que gerou o span.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Nome da operação.</summary>
    public required string OperationName { get; init; }

    /// <summary>Timestamp de início.</summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>Timestamp de fim.</summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>Duração em milissegundos.</summary>
    public required double DurationMs { get; init; }

    /// <summary>Código de status (Ok, Error, Unset).</summary>
    public string? StatusCode { get; init; }

    /// <summary>Mensagem de status (quando erro).</summary>
    public string? StatusMessage { get; init; }

    /// <summary>Ambiente.</summary>
    public required string Environment { get; init; }

    /// <summary>
    /// Tipo de span OpenTelemetry (Internal, Server, Client, Producer, Consumer).
    /// Mapeado diretamente do campo span_kind no storage.
    /// </summary>
    public string? SpanKind { get; init; }

    /// <summary>
    /// Tipo de serviço inferido das convenções semânticas OTel (REST, SOAP, Kafka, Background, DB, gRPC, Unknown).
    /// Calculado pelo SpanKindResolver a partir de SpanAttributes e SpanKind.
    /// </summary>
    public string ServiceKind { get; init; } = ServiceKindValues.Unknown;

    /// <summary>Atributos de recurso (service.name, host.name, etc.).</summary>
    public IReadOnlyDictionary<string, string>? ResourceAttributes { get; init; }

    /// <summary>Atributos do span (http.method, db.system, etc.).</summary>
    public IReadOnlyDictionary<string, string>? SpanAttributes { get; init; }

    /// <summary>Eventos do span (exceções, anotações).</summary>
    public IReadOnlyList<SpanEvent>? Events { get; init; }
}

/// <summary>
/// Evento dentro de um span (exceção, anotação, etc.).
/// </summary>
public sealed record SpanEvent
{
    /// <summary>Nome do evento.</summary>
    public required string Name { get; init; }

    /// <summary>Timestamp do evento.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Atributos do evento.</summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}

/// <summary>
/// Ponto de métrica técnica de telemetria.
/// </summary>
public sealed record TelemetryMetricPoint
{
    /// <summary>Timestamp da medição.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Nome da métrica (cpu_usage, memory_usage, request_duration, error_rate, throughput).</summary>
    public required string MetricName { get; init; }

    /// <summary>Valor da métrica.</summary>
    public required double Value { get; init; }

    /// <summary>Nome do serviço.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Ambiente.</summary>
    public required string Environment { get; init; }

    /// <summary>Labels adicionais (host, container, operation, etc.).</summary>
    public IReadOnlyDictionary<string, string>? Labels { get; init; }
}

/// <summary>
/// Filtro para consulta de logs.
/// </summary>
public sealed record LogQueryFilter
{
    /// <summary>Ambiente (obrigatório).</summary>
    public required string Environment { get; init; }

    /// <summary>Início do intervalo de tempo.</summary>
    public required DateTimeOffset From { get; init; }

    /// <summary>Fim do intervalo de tempo.</summary>
    public required DateTimeOffset Until { get; init; }

    /// <summary>Filtrar por nome de serviço.</summary>
    public string? ServiceName { get; init; }

    /// <summary>Filtrar por nível de log (Error, Warning, etc.).</summary>
    public string? Level { get; init; }

    /// <summary>Filtrar por texto na mensagem.</summary>
    public string? MessageContains { get; init; }

    /// <summary>Filtrar por trace ID.</summary>
    public string? TraceId { get; init; }

    /// <summary>Número máximo de resultados.</summary>
    public int Limit { get; init; } = 100;
}

/// <summary>
/// Filtro para consulta de traces.
/// </summary>
public sealed record TraceQueryFilter
{
    /// <summary>Ambiente (obrigatório).</summary>
    public required string Environment { get; init; }

    /// <summary>Início do intervalo de tempo.</summary>
    public required DateTimeOffset From { get; init; }

    /// <summary>Fim do intervalo de tempo.</summary>
    public required DateTimeOffset Until { get; init; }

    /// <summary>Filtrar por nome de serviço.</summary>
    public string? ServiceName { get; init; }

    /// <summary>Filtrar por nome de operação.</summary>
    public string? OperationName { get; init; }

    /// <summary>Duração mínima em milissegundos.</summary>
    public double? MinDurationMs { get; init; }

    /// <summary>Filtrar apenas traces com erro.</summary>
    public bool? HasErrors { get; init; }

    /// <summary>
    /// Filtrar por tipo de serviço inferido (REST, SOAP, Kafka, Background, DB, gRPC).
    /// Quando null, retorna todos os tipos.
    /// </summary>
    public string? ServiceKind { get; init; }

    /// <summary>Número máximo de resultados.</summary>
    public int Limit { get; init; } = 50;
}

/// <summary>
/// Filtro para consulta de métricas.
/// </summary>
public sealed record MetricQueryFilter
{
    /// <summary>Ambiente (obrigatório).</summary>
    public required string Environment { get; init; }

    /// <summary>Início do intervalo de tempo.</summary>
    public required DateTimeOffset From { get; init; }

    /// <summary>Fim do intervalo de tempo.</summary>
    public required DateTimeOffset Until { get; init; }

    /// <summary>Nome da métrica (cpu_usage, memory_usage, request_duration, error_rate, throughput).</summary>
    public required string MetricName { get; init; }

    /// <summary>Filtrar por nome de serviço.</summary>
    public string? ServiceName { get; init; }
}

/// <summary>
/// Frequência de erros por tipo/mensagem.
/// </summary>
public sealed record ErrorFrequency
{
    /// <summary>Mensagem ou tipo do erro.</summary>
    public required string ErrorMessage { get; init; }

    /// <summary>Número de ocorrências.</summary>
    public required long Count { get; init; }

    /// <summary>Nome do serviço com mais ocorrências.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Última ocorrência.</summary>
    public required DateTimeOffset LastSeen { get; init; }

    /// <summary>Nível de log predominante.</summary>
    public required string Level { get; init; }
}

/// <summary>
/// Comparação de latência entre dois ambientes.
/// </summary>
public sealed record LatencyComparison
{
    /// <summary>Nome do serviço comparado.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Ambiente A.</summary>
    public required string EnvironmentA { get; init; }

    /// <summary>Ambiente B.</summary>
    public required string EnvironmentB { get; init; }

    /// <summary>P50 do ambiente A em milissegundos.</summary>
    public double LatencyP50MsA { get; init; }

    /// <summary>P50 do ambiente B em milissegundos.</summary>
    public double LatencyP50MsB { get; init; }

    /// <summary>P95 do ambiente A em milissegundos.</summary>
    public double LatencyP95MsA { get; init; }

    /// <summary>P95 do ambiente B em milissegundos.</summary>
    public double LatencyP95MsB { get; init; }

    /// <summary>P99 do ambiente A em milissegundos.</summary>
    public double LatencyP99MsA { get; init; }

    /// <summary>P99 do ambiente B em milissegundos.</summary>
    public double LatencyP99MsB { get; init; }

    /// <summary>Percentual de diferença no P95.</summary>
    public double DriftPercentP95 { get; init; }
}

/// <summary>
/// Análise de impacto pós-release.
/// </summary>
public sealed record PostReleaseAnalysis
{
    /// <summary>Nome do serviço analisado.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Ambiente.</summary>
    public required string Environment { get; init; }

    /// <summary>Timestamp da release.</summary>
    public required DateTimeOffset ReleaseTimestamp { get; init; }

    /// <summary>Error rate antes da release (percentual).</summary>
    public double ErrorRateBefore { get; init; }

    /// <summary>Error rate depois da release (percentual).</summary>
    public double ErrorRateAfter { get; init; }

    /// <summary>Latência P95 antes da release (ms).</summary>
    public double LatencyP95MsBefore { get; init; }

    /// <summary>Latência P95 depois da release (ms).</summary>
    public double LatencyP95MsAfter { get; init; }

    /// <summary>Throughput antes da release (req/min).</summary>
    public double ThroughputBefore { get; init; }

    /// <summary>Throughput depois da release (req/min).</summary>
    public double ThroughputAfter { get; init; }

    /// <summary>Indica se houve degradação significativa.</summary>
    public bool HasDegradation { get; init; }

    /// <summary>Score de impacto (0.0 a 1.0).</summary>
    public double ImpactScore { get; init; }
}

/// <summary>
/// Resumo de uso de recursos por serviço.
/// </summary>
public sealed record ResourceUsageSummary
{
    /// <summary>Nome do serviço.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Ambiente.</summary>
    public required string Environment { get; init; }

    /// <summary>Uso médio de CPU (percentual).</summary>
    public double CpuAvgPercent { get; init; }

    /// <summary>Uso médio de memória (MB).</summary>
    public double MemoryAvgMb { get; init; }

    /// <summary>CPU do processo (percentual).</summary>
    public double ProcessCpuPercent { get; init; }

    /// <summary>Memória do processo (MB).</summary>
    public double ProcessMemoryMb { get; init; }
}

/// <summary>
/// Operação com regressão de performance detectada.
/// </summary>
public sealed record OperationRegression
{
    /// <summary>Nome do serviço.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Nome da operação.</summary>
    public required string OperationName { get; init; }

    /// <summary>Latência baseline P95 (ms).</summary>
    public double BaselineP95Ms { get; init; }

    /// <summary>Latência atual P95 (ms).</summary>
    public double CurrentP95Ms { get; init; }

    /// <summary>Percentual de regressão.</summary>
    public double RegressionPercent { get; init; }

    /// <summary>Quando a regressão foi detectada pela primeira vez.</summary>
    public DateTimeOffset DetectedAt { get; init; }
}

/// <summary>
/// Sinais correlacionados por trace_id (logs + spans).
/// </summary>
public sealed record CorrelatedSignals
{
    /// <summary>Trace ID utilizado na correlação.</summary>
    public required string TraceId { get; init; }

    /// <summary>Logs correlacionados com este trace.</summary>
    public IReadOnlyList<LogEntry> Logs { get; init; } = [];

    /// <summary>Spans do trace.</summary>
    public IReadOnlyList<SpanDetail> Spans { get; init; } = [];
}

/// <summary>
/// Evidências de telemetria para cálculo de risco de release.
/// </summary>
public sealed record ReleaseRiskEvidence
{
    /// <summary>Nome do serviço.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Ambiente.</summary>
    public required string Environment { get; init; }

    /// <summary>Score de risco (0.0 a 1.0).</summary>
    public double RiskScore { get; init; }

    /// <summary>Indicadores que contribuem para o score.</summary>
    public IReadOnlyList<string> RiskIndicators { get; init; } = [];

    /// <summary>Evidências de anomalia detectadas.</summary>
    public IReadOnlyList<string> AnomalyEvidence { get; init; } = [];

    /// <summary>Recomendação baseada nas evidências.</summary>
    public string? Recommendation { get; init; }
}

/// <summary>
/// Snapshot de telemetria de um ambiente para consumo pela IA interna.
/// Agrega informações relevantes para análise e recomendações.
/// </summary>
public sealed record EnvironmentTelemetrySnapshot
{
    /// <summary>Ambiente.</summary>
    public required string Environment { get; init; }

    /// <summary>Intervalo de tempo do snapshot.</summary>
    public required DateTimeOffset From { get; init; }

    /// <summary>Intervalo de tempo do snapshot.</summary>
    public required DateTimeOffset Until { get; init; }

    /// <summary>Total de serviços ativos no período.</summary>
    public int ActiveServiceCount { get; init; }

    /// <summary>Total de erros no período.</summary>
    public long TotalErrors { get; init; }

    /// <summary>Error rate global (percentual).</summary>
    public double GlobalErrorRate { get; init; }

    /// <summary>Latência P95 global (ms).</summary>
    public double GlobalLatencyP95Ms { get; init; }

    /// <summary>Throughput total (req/min).</summary>
    public double TotalThroughput { get; init; }

    /// <summary>Serviços com mais erros.</summary>
    public IReadOnlyList<string> TopErrorServices { get; init; } = [];

    /// <summary>Serviços com maior latência.</summary>
    public IReadOnlyList<string> TopLatencyServices { get; init; } = [];

    /// <summary>Anomalias ativas.</summary>
    public int ActiveAnomalyCount { get; init; }
}

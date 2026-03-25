using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Aggregate Root que representa um snapshot pontual de saúde e performance de um serviço em runtime.
/// Captura métricas operacionais (latência, taxa de erro, throughput, recursos)
/// num instante específico, permitindo análise histórica, detecção de drift e correlação com releases.
///
/// A classificação de saúde é determinada automaticamente pelo factory method com base em
/// limiares de erro e latência — não existe snapshot com estado de saúde inconsistente.
///
/// Invariantes:
/// - ErrorRate sempre entre 0 e 1 (clamp aplicado).
/// - CpuUsagePercent sempre entre 0 e 100 (clamp aplicado).
/// - HealthStatus é derivado das métricas — não pode ser definido externamente.
/// </summary>
public sealed class RuntimeSnapshot : AuditableEntity<RuntimeSnapshotId>
{
    /// <summary>Limiar de taxa de erro para considerar o serviço degradado (5%).</summary>
    private const decimal DegradedErrorRateThreshold = 0.05m;

    /// <summary>Limiar de taxa de erro para considerar o serviço unhealthy (10%).</summary>
    private const decimal UnhealthyErrorRateThreshold = 0.10m;

    /// <summary>Limiar de latência P99 para considerar o serviço degradado (1000ms).</summary>
    private const decimal DegradedLatencyThresholdMs = 1000m;

    /// <summary>Limiar de latência P99 para considerar o serviço unhealthy (3000ms).</summary>
    private const decimal UnhealthyLatencyThresholdMs = 3000m;

    private RuntimeSnapshot() { }

    /// <summary>Nome do serviço ao qual este snapshot de runtime se refere.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente onde as métricas foram coletadas (dev, staging, prod).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Latência média em milissegundos no instante do snapshot.</summary>
    public decimal AvgLatencyMs { get; private set; }

    /// <summary>Latência no percentil 99 em milissegundos — indicador de cauda longa.</summary>
    public decimal P99LatencyMs { get; private set; }

    /// <summary>Taxa de erro do serviço, expressa como fração entre 0 e 1 (ex: 0.05 = 5%).</summary>
    public decimal ErrorRate { get; private set; }

    /// <summary>Número de requisições por segundo no instante da captura.</summary>
    public decimal RequestsPerSecond { get; private set; }

    /// <summary>Percentual de utilização de CPU (0–100) do serviço.</summary>
    public decimal CpuUsagePercent { get; private set; }

    /// <summary>Consumo de memória em megabytes do serviço.</summary>
    public decimal MemoryUsageMb { get; private set; }

    /// <summary>Número de instâncias ativas do serviço no momento da captura.</summary>
    public int ActiveInstances { get; private set; }

    /// <summary>Estado de saúde classificado automaticamente com base nas métricas coletadas.</summary>
    public HealthStatus HealthStatus { get; private set; } = HealthStatus.Unknown;

    /// <summary>Data/hora UTC em que o snapshot foi capturado na fonte original.</summary>
    public DateTimeOffset CapturedAt { get; private set; }

    /// <summary>Fonte dos dados de runtime (ex: "Prometheus", "Datadog", "CloudWatch").</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Indica se o serviço está operando normalmente — derivado do HealthStatus.</summary>
    public bool IsHealthy => HealthStatus == HealthStatus.Healthy;

    /// <summary>Indica se o serviço está degradado — derivado do HealthStatus.</summary>
    public bool IsDegraded => HealthStatus == HealthStatus.Degraded;

    /// <summary>Indica se o serviço está com falha — derivado do HealthStatus.</summary>
    public bool IsUnhealthy => HealthStatus == HealthStatus.Unhealthy;

    /// <summary>
    /// Cria um novo snapshot de runtime com validações de guarda nos campos obrigatórios.
    /// Métricas percentuais são validadas dentro dos limites aceitos.
    /// A classificação de saúde é executada automaticamente — invariante garantida pelo factory.
    ///
    /// REGRA DDD: A classificação de saúde é encapsulada — não pode ser chamada externamente.
    /// </summary>
    public static RuntimeSnapshot Create(
        string serviceName,
        string environment,
        decimal avgLatencyMs,
        decimal p99LatencyMs,
        decimal errorRate,
        decimal requestsPerSecond,
        decimal cpuUsagePercent,
        decimal memoryUsageMb,
        int activeInstances,
        DateTimeOffset capturedAt,
        string source)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.Negative(avgLatencyMs);
        Guard.Against.Negative(p99LatencyMs);
        Guard.Against.Negative(errorRate);
        Guard.Against.Negative(requestsPerSecond);
        Guard.Against.Negative(cpuUsagePercent);
        Guard.Against.Negative(memoryUsageMb);
        Guard.Against.NegativeOrZero(activeInstances);
        Guard.Against.NullOrWhiteSpace(source);

        var snapshot = new RuntimeSnapshot
        {
            Id = RuntimeSnapshotId.New(),
            ServiceName = serviceName,
            Environment = environment,
            AvgLatencyMs = avgLatencyMs,
            P99LatencyMs = p99LatencyMs,
            ErrorRate = Math.Clamp(errorRate, 0m, 1m),
            RequestsPerSecond = requestsPerSecond,
            CpuUsagePercent = Math.Clamp(cpuUsagePercent, 0m, 100m),
            MemoryUsageMb = memoryUsageMb,
            ActiveInstances = activeInstances,
            CapturedAt = capturedAt,
            Source = source
        };

        snapshot.ClassifyHealth();

        return snapshot;
    }

    /// <summary>
    /// Calcula o desvio percentual de cada métrica em relação a uma baseline.
    /// Retorna um dicionário nome_metrica→desvio_percentual para uso em detecção de drift.
    /// Valores positivos indicam piora; valores negativos indicam melhora.
    /// </summary>
    public IDictionary<string, decimal> CalculateDeviationsFrom(RuntimeBaseline baseline)
    {
        Guard.Against.Null(baseline);

        var deviations = new Dictionary<string, decimal>();

        AddDeviation(deviations, "AvgLatencyMs", baseline.ExpectedAvgLatencyMs, AvgLatencyMs);
        AddDeviation(deviations, "P99LatencyMs", baseline.ExpectedP99LatencyMs, P99LatencyMs);
        AddDeviation(deviations, "ErrorRate", baseline.ExpectedErrorRate, ErrorRate);
        AddDeviation(deviations, "RequestsPerSecond", baseline.ExpectedRequestsPerSecond, RequestsPerSecond);

        return deviations;
    }

    /// <summary>
    /// Classifica automaticamente o estado de saúde do serviço com base na taxa de erro
    /// e na latência P99. O status mais severo entre os dois indicadores prevalece.
    /// Encapsulado: chamado apenas pelo factory method para garantir invariante.
    /// </summary>
    private void ClassifyHealth()
    {
        if (ErrorRate >= UnhealthyErrorRateThreshold || P99LatencyMs >= UnhealthyLatencyThresholdMs)
        {
            HealthStatus = HealthStatus.Unhealthy;
            return;
        }

        if (ErrorRate >= DegradedErrorRateThreshold || P99LatencyMs >= DegradedLatencyThresholdMs)
        {
            HealthStatus = HealthStatus.Degraded;
            return;
        }

        HealthStatus = HealthStatus.Healthy;
    }

    /// <summary>Calcula desvio percentual de uma métrica individual.</summary>
    private static void AddDeviation(IDictionary<string, decimal> deviations, string metric, decimal expected, decimal actual)
    {
        var deviation = expected == 0m
            ? (actual == 0m ? 0m : 100m)
            : Math.Round((actual - expected) / Math.Abs(expected) * 100m, 2);

        deviations[metric] = deviation;
    }
}

/// <summary>Identificador fortemente tipado de RuntimeSnapshot.</summary>
public sealed record RuntimeSnapshotId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RuntimeSnapshotId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RuntimeSnapshotId From(Guid id) => new(id);
}

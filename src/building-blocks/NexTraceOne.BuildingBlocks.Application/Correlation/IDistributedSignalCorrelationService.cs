namespace NexTraceOne.BuildingBlocks.Application.Correlation;

// IMPLEMENTATION STATUS: Planned — no implementation exists.
// This interface defines the distributed signal correlation service for AI-assisted analysis.
// It will be implemented when the correlation pipeline is operational.
// Do NOT register in DI or reference in handlers until an implementation exists.

/// <summary>
/// Serviço de correlação de sinais distribuídos para análise operacional e IA.
///
/// Prepara a plataforma para correlacionar, no futuro, os seguintes sinais:
/// - telemetria (traces, métricas, logs)
/// - topologia (dependências observadas, mudanças de topologia)
/// - incidentes (criação, correlação, mitigação)
/// - contratos (drift, violações, compatibilidade)
/// - releases/deploys (mudanças, rollbacks, blast radius)
///
/// CONTEXTO DE IA:
/// Este serviço é a base para a IA analisar, por tenant e ambiente:
/// - sinais de regressão antes de promoção para produção
/// - correlação entre release e incidente
/// - readiness de ambiente para promoção
/// - risco de promoção com base em sinais acumulados
///
/// ISOLAMENTO: Toda correlação é isolada por TenantId — nunca cruza tenants.
/// Correlações entre ambientes do mesmo tenant são permitidas e incentivadas
/// para análise comparativa.
/// </summary>
public interface IDistributedSignalCorrelationService
{
    /// <summary>
    /// Correlaciona sinais distribuídos de um serviço em um ambiente específico
    /// para análise de risco e readiness.
    ///
    /// Retorna sinais correlacionados que podem ser usados pela IA para:
    /// - avaliar se o serviço está pronto para promoção
    /// - identificar regressões em relação ao baseline
    /// - detectar anomalias de comportamento distribuído
    /// </summary>
    Task<DistributedSignalCorrelation> CorrelateSignalsAsync(
        Guid tenantId,
        Guid environmentId,
        string serviceName,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compara sinais entre dois ambientes do mesmo tenant para um serviço específico.
    ///
    /// Usado pela IA para detectar regressões entre:
    /// - ambiente de staging vs produção
    /// - ambiente de QA vs baseline esperado
    /// - versão atual vs versão anterior no mesmo ambiente
    ///
    /// REGRA: sourceEnvironmentId e targetEnvironmentId devem pertencer ao mesmo tenant.
    /// </summary>
    Task<EnvironmentSignalComparison> CompareEnvironmentsAsync(
        Guid tenantId,
        Guid sourceEnvironmentId,
        Guid targetEnvironmentId,
        string serviceName,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}

/// <summary>Correlação de sinais distribuídos de um serviço em um ambiente.</summary>
public sealed record DistributedSignalCorrelation
{
    public required Guid TenantId { get; init; }
    public required Guid EnvironmentId { get; init; }
    public required string ServiceName { get; init; }
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }

    /// <summary>Número de incidentes no período.</summary>
    public int IncidentCount { get; init; }

    /// <summary>Número de releases no período.</summary>
    public int ReleaseCount { get; init; }

    /// <summary>Score de correlação (0.0 = sem correlação notável, 1.0 = forte correlação).</summary>
    public double CorrelationScore { get; init; }

    /// <summary>Indica se há sinais que sugerem risco de promoção.</summary>
    public bool HasPromotionRiskSignals { get; init; }

    /// <summary>Sinais identificados (mensagens descritivas).</summary>
    public IReadOnlyList<string> Signals { get; init; } = [];
}

/// <summary>Comparação de sinais entre dois ambientes do mesmo tenant.</summary>
public sealed record EnvironmentSignalComparison
{
    public required Guid TenantId { get; init; }
    public required Guid SourceEnvironmentId { get; init; }
    public required Guid TargetEnvironmentId { get; init; }
    public required string ServiceName { get; init; }

    /// <summary>Indica se foi detectada regressão no ambiente source vs target.</summary>
    public bool HasRegression { get; init; }

    /// <summary>Score de divergência (0.0 = idêntico, 1.0 = completamente diferente).</summary>
    public double DivergenceScore { get; init; }

    /// <summary>Sinais de divergência identificados.</summary>
    public IReadOnlyList<string> DivergenceSignals { get; init; } = [];
}

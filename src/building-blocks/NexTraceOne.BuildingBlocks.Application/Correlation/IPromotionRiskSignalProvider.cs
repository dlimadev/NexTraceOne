namespace NexTraceOne.BuildingBlocks.Application.Correlation;

/// <summary>
/// Provider de sinais de risco de promoção para a IA.
///
/// Agrega sinais de múltiplos módulos (incidents, releases, telemetry, contracts, topology)
/// para construir uma avaliação de risco antes de promover uma release de um ambiente
/// para outro.
///
/// Responde à pergunta fundamental da IA:
/// "Que sinais distribuídos indicam que esta release NÃO deveria ser promovida para PROD?"
///
/// ISOLAMENTO: Todo acesso é via TenantId. Nunca cruza tenants.
/// NEUTRALIDADE: Não decide pela promoção — apenas fornece sinais. A decisão é do usuário.
/// </summary>
public interface IPromotionRiskSignalProvider
{
    /// <summary>
    /// Coleta sinais de risco para a promoção de uma release entre ambientes.
    ///
    /// Analisa:
    /// - incidentes recentes no ambiente source
    /// - correlações de release com incidentes
    /// - comportamento de telemetria pós-release
    /// - violações de contrato detectadas
    /// - divergência de topologia entre source e target
    /// </summary>
    Task<PromotionRiskAssessment> AssessPromotionRiskAsync(
        Guid tenantId,
        Guid sourceEnvironmentId,
        Guid targetEnvironmentId,
        string? serviceName,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}

/// <summary>Avaliação de risco de promoção entre ambientes.</summary>
public sealed record PromotionRiskAssessment
{
    public required Guid TenantId { get; init; }
    public required Guid SourceEnvironmentId { get; init; }
    public required Guid TargetEnvironmentId { get; init; }
    public required string? ServiceName { get; init; }
    public required DateTimeOffset AssessedAt { get; init; }

    /// <summary>
    /// Nível de risco: None, Low, Medium, High, Critical.
    /// Baseado nos sinais encontrados — não é uma decisão de bloqueio.
    /// </summary>
    public required PromotionRiskLevel RiskLevel { get; init; }

    /// <summary>Score de risco (0.0 = sem risco, 1.0 = risco máximo).</summary>
    public double RiskScore { get; init; }

    /// <summary>Sinais que contribuíram para a avaliação de risco.</summary>
    public IReadOnlyList<PromotionRiskSignal> Signals { get; init; } = [];

    /// <summary>Indica se há sinais que deveriam bloquear a promoção.</summary>
    public bool ShouldBlock => RiskLevel is PromotionRiskLevel.High or PromotionRiskLevel.Critical;
}

/// <summary>Sinal individual de risco de promoção.</summary>
public sealed record PromotionRiskSignal
{
    public required string SignalType { get; init; }
    public required string Description { get; init; }
    public required PromotionRiskLevel Severity { get; init; }
    public string? RelatedEntityId { get; init; }
    public string? Module { get; init; }
}

/// <summary>Nível de risco de promoção.</summary>
public enum PromotionRiskLevel
{
    /// <summary>Sem sinais de risco identificados.</summary>
    None = 0,

    /// <summary>Sinais menores identificados — monitorar.</summary>
    Low = 1,

    /// <summary>Sinais moderados — revisão recomendada.</summary>
    Medium = 2,

    /// <summary>Sinais graves — revisão obrigatória antes de promover.</summary>
    High = 3,

    /// <summary>Sinais críticos — promoção deve ser bloqueada até resolução.</summary>
    Critical = 4
}

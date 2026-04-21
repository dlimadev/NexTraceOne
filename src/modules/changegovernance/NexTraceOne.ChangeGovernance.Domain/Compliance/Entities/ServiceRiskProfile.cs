using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;

/// <summary>
/// Perfil de risco de um serviço calculado pelo Risk Center.
/// Agrega sinais de risco de múltiplas dimensões: vulnerabilidades,
/// change failure rate, blast radius e violações de política.
/// Usado pela persona Platform Admin / CTO para priorização de atenção operacional.
/// Wave F.2 — Risk Center.
/// </summary>
public sealed class ServiceRiskProfile : AuditableEntity<ServiceRiskProfileId>
{
    private ServiceRiskProfile() { }

    /// <summary>Identificador do tenant.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Identificador do serviço (ServiceAssetId do Catalog).</summary>
    public Guid ServiceAssetId { get; private set; }

    /// <summary>Nome do serviço para display (desnormalizado para consulta rápida).</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Nível de risco global calculado.</summary>
    public RiskLevel OverallRiskLevel { get; private set; }

    /// <summary>Score de risco normalizado (0-100). 100 = risco máximo.</summary>
    public int OverallScore { get; private set; }

    /// <summary>Score de vulnerabilidades (0-100).</summary>
    public int VulnerabilityScore { get; private set; }

    /// <summary>Score baseado na change failure rate (0-100).</summary>
    public int ChangeFailureScore { get; private set; }

    /// <summary>Score de blast radius de falha (0-100).</summary>
    public int BlastRadiusScore { get; private set; }

    /// <summary>Score de violações de política (0-100).</summary>
    public int PolicyViolationScore { get; private set; }

    /// <summary>
    /// JSON com os sinais de risco activos (lista de RiskSignalType + razão).
    /// Formato: [{"signal": 0, "reason": "2 critical CVEs unmitigated"}]
    /// </summary>
    public string ActiveSignalsJson { get; private set; } = "[]";

    /// <summary>Número de sinais de risco activos.</summary>
    public int ActiveSignalCount { get; private set; }

    /// <summary>Momento em que o perfil foi calculado (UTC).</summary>
    public DateTimeOffset ComputedAt { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Computa um perfil de risco para o serviço com base nos scores dimensionais.
    /// O score global é a média ponderada: vuln 40%, change_failure 25%, blast_radius 20%, policy 15%.
    /// </summary>
    public static ServiceRiskProfile Compute(
        string tenantId,
        Guid serviceAssetId,
        string serviceName,
        int vulnerabilityScore,
        int changeFailureScore,
        int blastRadiusScore,
        int policyViolationScore,
        IReadOnlyList<(RiskSignalType Signal, string Reason)> activeSignals,
        DateTimeOffset computedAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(serviceName);

        var overall = (int)Math.Round(
            vulnerabilityScore * 0.40m +
            changeFailureScore * 0.25m +
            blastRadiusScore * 0.20m +
            policyViolationScore * 0.15m);

        overall = Math.Clamp(overall, 0, 100);

        var level = overall switch
        {
            >= 80 => RiskLevel.Critical,
            >= 60 => RiskLevel.High,
            >= 40 => RiskLevel.Medium,
            >= 20 => RiskLevel.Low,
            _ => RiskLevel.Negligible
        };

        var signalsJson = System.Text.Json.JsonSerializer.Serialize(
            activeSignals.Select(s => new { signal = (int)s.Signal, reason = s.Reason }));

        return new ServiceRiskProfile
        {
            Id = ServiceRiskProfileId.New(),
            TenantId = tenantId,
            ServiceAssetId = serviceAssetId,
            ServiceName = serviceName.Trim(),
            OverallRiskLevel = level,
            OverallScore = overall,
            VulnerabilityScore = Math.Clamp(vulnerabilityScore, 0, 100),
            ChangeFailureScore = Math.Clamp(changeFailureScore, 0, 100),
            BlastRadiusScore = Math.Clamp(blastRadiusScore, 0, 100),
            PolicyViolationScore = Math.Clamp(policyViolationScore, 0, 100),
            ActiveSignalsJson = signalsJson,
            ActiveSignalCount = activeSignals.Count,
            ComputedAt = computedAt
        };
    }
}

/// <summary>Strongly-typed ID para ServiceRiskProfile.</summary>
public sealed record ServiceRiskProfileId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria um novo ID único.</summary>
    public static ServiceRiskProfileId New() => new(Guid.NewGuid());

    /// <summary>Cria a partir de um Guid.</summary>
    public static ServiceRiskProfileId From(Guid value) => new(value);
}

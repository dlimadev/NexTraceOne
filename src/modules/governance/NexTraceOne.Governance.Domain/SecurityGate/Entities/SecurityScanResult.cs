using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;
using NexTraceOne.Governance.Domain.SecurityGate.ValueObjects;

namespace NexTraceOne.Governance.Domain.SecurityGate.Entities;

/// <summary>
/// Resultado de um scan de segurança — aggregate root do SecurityGate.
/// Agrega SecurityFinding com análise SAST, contratos e templates.
/// PassedGate = true quando não há achados críticos e achados altos ≤ MaxHighFindings (padrão: 3).
/// </summary>
public sealed class SecurityScanResult : AuditableEntity<SecurityScanResultId>
{
    private List<SecurityFinding> _findings = [];
    private SecurityScanResult() { }

    /// <summary>Tipo de alvo do scan.</summary>
    public ScanTarget TargetType { get; private set; }

    /// <summary>Identificador do artefacto scaneado (ScaffoldId, ContractVersionId, etc.).</summary>
    public Guid TargetId { get; private set; }

    /// <summary>Momento em que o scan foi executado.</summary>
    public DateTimeOffset ScannedAt { get; private set; }

    /// <summary>Provedor do scan.</summary>
    public ScanProvider ScanProvider { get; private set; }

    /// <summary>Nível de risco global do resultado.</summary>
    public SecurityRiskLevel OverallRisk { get; private set; }

    /// <summary>Indica se o artefacto passou o security gate.</summary>
    public bool PassedGate { get; private set; }

    /// <summary>Resumo dos achados.</summary>
    public SecurityScanSummary Summary { get; private set; } = SecurityScanSummary.Empty;

    /// <summary>Lista de achados do scan.</summary>
    public IReadOnlyList<SecurityFinding> Findings => _findings.AsReadOnly();

    /// <summary>Cria um novo resultado de scan.</summary>
    public static SecurityScanResult Create(ScanTarget targetType, Guid targetId, ScanProvider provider)
    {
        Guard.Against.Default(targetId);
        return new SecurityScanResult
        {
            Id = SecurityScanResultId.New(),
            TargetType = targetType,
            TargetId = targetId,
            ScannedAt = DateTimeOffset.UtcNow,
            ScanProvider = provider,
            OverallRisk = SecurityRiskLevel.Clean,
            PassedGate = true,
            Summary = SecurityScanSummary.Empty
        };
    }

    /// <summary>
    /// Adiciona achados ao resultado e recalcula OverallRisk e PassedGate.
    /// PassedGate = false se houver qualquer achado Critical ou mais de 3 achados High.
    /// </summary>
    public void AddFindings(IReadOnlyList<SecurityFinding> findings, int maxHighFindings = 3)
    {
        Guard.Against.Null(findings);
        _findings.AddRange(findings);
        Summary = SecurityScanSummary.Create(_findings);
        OverallRisk = CalculateOverallRisk(_findings);
        PassedGate = Summary.CriticalCount == 0 && Summary.HighCount <= maxHighFindings;
    }

    /// <summary>Actualiza os thresholds do gate sem adicionar novos achados.</summary>
    public void ReEvaluateGate(int maxCritical, int maxHighFindings)
    {
        PassedGate = Summary.CriticalCount <= maxCritical && Summary.HighCount <= maxHighFindings;
    }

    private static SecurityRiskLevel CalculateOverallRisk(IReadOnlyList<SecurityFinding> findings)
    {
        if (findings.Count == 0) return SecurityRiskLevel.Clean;
        if (findings.Any(f => f.Severity == FindingSeverity.Critical)) return SecurityRiskLevel.Critical;
        if (findings.Any(f => f.Severity == FindingSeverity.High)) return SecurityRiskLevel.High;
        if (findings.Any(f => f.Severity == FindingSeverity.Medium)) return SecurityRiskLevel.Medium;
        if (findings.Any(f => f.Severity == FindingSeverity.Low)) return SecurityRiskLevel.Low;
        return SecurityRiskLevel.Clean;
    }
}

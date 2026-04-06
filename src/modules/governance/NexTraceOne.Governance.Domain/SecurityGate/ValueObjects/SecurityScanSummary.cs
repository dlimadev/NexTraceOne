using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Domain.SecurityGate.ValueObjects;

/// <summary>
/// Resumo agregado dos achados de um scan de segurança.
/// Calculado a partir da lista de achados no momento em que são adicionados.
/// </summary>
public sealed record SecurityScanSummary
{
    /// <summary>Total de achados.</summary>
    public int TotalFindings { get; init; }

    /// <summary>Número de achados críticos.</summary>
    public int CriticalCount { get; init; }

    /// <summary>Número de achados de alta severidade.</summary>
    public int HighCount { get; init; }

    /// <summary>Número de achados de média severidade.</summary>
    public int MediumCount { get; init; }

    /// <summary>Número de achados de baixa severidade.</summary>
    public int LowCount { get; init; }

    /// <summary>Número de achados informativos.</summary>
    public int InfoCount { get; init; }

    /// <summary>Top categorias de achados por frequência.</summary>
    public IReadOnlyList<string> TopCategories { get; init; } = [];

    /// <summary>Cria um resumo vazio (sem achados).</summary>
    public static SecurityScanSummary Empty => new();

    /// <summary>Cria um resumo a partir de uma lista de achados.</summary>
    public static SecurityScanSummary Create(IReadOnlyList<Entities.SecurityFinding> findings)
    {
        if (findings.Count == 0)
            return Empty;

        var topCategories = findings
            .GroupBy(f => f.Category.ToString())
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToList();

        return new SecurityScanSummary
        {
            TotalFindings = findings.Count,
            CriticalCount = findings.Count(f => f.Severity == FindingSeverity.Critical),
            HighCount = findings.Count(f => f.Severity == FindingSeverity.High),
            MediumCount = findings.Count(f => f.Severity == FindingSeverity.Medium),
            LowCount = findings.Count(f => f.Severity == FindingSeverity.Low),
            InfoCount = findings.Count(f => f.Severity == FindingSeverity.Info),
            TopCategories = topCategories
        };
    }
}

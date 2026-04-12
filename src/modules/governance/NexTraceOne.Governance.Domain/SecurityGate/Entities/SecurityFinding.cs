using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Domain.SecurityGate.Entities;

/// <summary>
/// Achado individual de um scan de segurança.
/// Representa uma vulnerabilidade ou problema de segurança detectado no código, contrato ou template.
/// </summary>
public sealed class SecurityFinding
{
    private SecurityFinding() { }

    /// <summary>Identificador único do achado.</summary>
    public Guid FindingId { get; private set; }

    /// <summary>Identificador do scan ao qual pertence.</summary>
    public SecurityScanResultId ScanResultId { get; private set; } = null!;

    /// <summary>Identificador da regra que detectou o achado (ex: "SAST-001", "CWE-89").</summary>
    public string RuleId { get; private set; } = string.Empty;

    /// <summary>Categoria de segurança do achado.</summary>
    public SecurityCategory Category { get; private set; }

    /// <summary>Severidade do achado.</summary>
    public FindingSeverity Severity { get; private set; }

    /// <summary>Caminho do ficheiro onde o achado foi detectado.</summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>Número de linha aproximado do achado (null se não aplicável).</summary>
    public int? LineNumber { get; private set; }

    /// <summary>Descrição do problema de segurança.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Sugestão de remediação.</summary>
    public string Remediation { get; private set; } = string.Empty;

    /// <summary>Referência CWE (ex: "CWE-89").</summary>
    public string? CweId { get; private set; }

    /// <summary>Categoria OWASP correspondente (ex: "A03:2021").</summary>
    public string? OwaspCategory { get; private set; }

    /// <summary>Indica se o achado foi detectado por IA.</summary>
    public bool IsAiGenerated { get; private set; }

    /// <summary>Estado atual do achado.</summary>
    public FindingStatus Status { get; private set; }

    /// <summary>Cria um novo achado de segurança.</summary>
    public static SecurityFinding Create(
        Guid scanResultId,
        string ruleId,
        SecurityCategory category,
        FindingSeverity severity,
        string filePath,
        string description,
        string remediation,
        int? lineNumber = null,
        string? cweId = null,
        string? owaspCategory = null,
        bool isAiGenerated = false)
    {
        return new SecurityFinding
        {
            FindingId = Guid.NewGuid(),
            ScanResultId = new SecurityScanResultId(scanResultId),
            RuleId = ruleId,
            Category = category,
            Severity = severity,
            FilePath = filePath,
            LineNumber = lineNumber,
            Description = description,
            Remediation = remediation,
            CweId = cweId,
            OwaspCategory = owaspCategory,
            IsAiGenerated = isAiGenerated,
            Status = FindingStatus.Open
        };
    }

    /// <summary>Marca o achado como reconhecido pela equipa.</summary>
    public void Acknowledge()
    {
        Status = FindingStatus.Acknowledged;
    }

    /// <summary>Marca o achado como falso positivo.</summary>
    public void MarkAsFalsePositive()
    {
        Status = FindingStatus.FalsePositive;
    }

    /// <summary>Marca o achado como mitigado.</summary>
    public void MarkAsMitigated()
    {
        Status = FindingStatus.Mitigated;
    }
}

namespace NexTraceOne.Governance.Domain.SecurityGate.Enums;

/// <summary>Nível de risco de segurança de um scan.</summary>
public enum SecurityRiskLevel
{
    /// <summary>Sem achados de segurança.</summary>
    Clean = 0,

    /// <summary>Achados de baixo risco.</summary>
    Low = 1,

    /// <summary>Achados de risco médio.</summary>
    Medium = 2,

    /// <summary>Achados de risco elevado.</summary>
    High = 3,

    /// <summary>Achados críticos — gate falha automaticamente.</summary>
    Critical = 4
}

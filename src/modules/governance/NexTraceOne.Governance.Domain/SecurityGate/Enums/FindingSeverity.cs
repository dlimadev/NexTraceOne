namespace NexTraceOne.Governance.Domain.SecurityGate.Enums;

/// <summary>Severidade de um achado de segurança.</summary>
public enum FindingSeverity
{
    /// <summary>Informativo — sem risco direto.</summary>
    Info = 0,

    /// <summary>Baixo risco.</summary>
    Low = 1,

    /// <summary>Risco médio.</summary>
    Medium = 2,

    /// <summary>Risco elevado.</summary>
    High = 3,

    /// <summary>Risco crítico — requer ação imediata.</summary>
    Critical = 4
}

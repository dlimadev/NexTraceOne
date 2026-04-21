namespace NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

/// <summary>Estado de conformidade de um controlo NIS2.</summary>
public enum Nis2ControlStatus
{
    /// <summary>Controlo ainda não avaliado — dados insuficientes.</summary>
    NotAssessed = 0,
    /// <summary>Controlo em conformidade.</summary>
    Compliant = 1,
    /// <summary>Conformidade parcial — alguns critérios não satisfeitos.</summary>
    PartiallyCompliant = 2,
    /// <summary>Controlo fora de conformidade — acção correctiva necessária.</summary>
    NonCompliant = 3,
}

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Âmbito de aplicação de um gate de compliance contratual.
/// Define a que nível o gate se aplica: organização, equipa ou ambiente.
/// </summary>
public enum ComplianceGateScope
{
    /// <summary>Gate aplicado a toda a organização.</summary>
    Organization = 0,

    /// <summary>Gate aplicado a uma equipa específica.</summary>
    Team = 1,

    /// <summary>Gate aplicado a um ambiente específico.</summary>
    Environment = 2
}

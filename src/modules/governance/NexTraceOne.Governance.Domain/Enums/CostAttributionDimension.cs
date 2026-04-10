namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Dimensão de atribuição de custo operacional.
/// Define a que tipo de entidade o custo é atribuído.
/// </summary>
public enum CostAttributionDimension
{
    /// <summary>Custo atribuído a um serviço individual.</summary>
    Service = 1,

    /// <summary>Custo atribuído a uma equipa.</summary>
    Team = 2,

    /// <summary>Custo atribuído a um domínio de negócio.</summary>
    Domain = 3,

    /// <summary>Custo atribuído a um contrato (API, evento, etc.).</summary>
    Contract = 4,

    /// <summary>Custo atribuído a uma mudança/release específica.</summary>
    Change = 5
}

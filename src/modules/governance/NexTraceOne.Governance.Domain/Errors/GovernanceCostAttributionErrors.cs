using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros de atribuição de custo operacional.
/// </summary>
public static class GovernanceCostAttributionErrors
{
    /// <summary>Atribuição de custo não encontrada pelo identificador.</summary>
    public static Error AttributionNotFound(string id) => Error.NotFound(
        "Governance.CostAttribution.NotFound",
        "Cost attribution '{0}' was not found.", id);
}

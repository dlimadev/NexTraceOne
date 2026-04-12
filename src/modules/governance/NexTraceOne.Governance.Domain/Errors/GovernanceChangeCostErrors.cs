using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros de impacto de custo por mudança (FinOps).
/// </summary>
public static class GovernanceChangeCostErrors
{
    /// <summary>Impacto de custo não encontrado pelo identificador.</summary>
    public static Error NotFound(string id) => Error.NotFound(
        "Governance.ChangeCostImpact.NotFound",
        "Change cost impact '{0}' was not found.", id);

    /// <summary>Nenhum impacto de custo encontrado para a release indicada.</summary>
    public static Error ReleaseNotFound(string releaseId) => Error.NotFound(
        "Governance.ChangeCostImpact.ReleaseNotFound",
        "No cost impact found for release '{0}'.", releaseId);
}

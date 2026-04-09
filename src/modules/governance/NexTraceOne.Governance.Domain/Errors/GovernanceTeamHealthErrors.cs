using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros de saúde de equipas.
/// </summary>
public static class GovernanceTeamHealthErrors
{
    /// <summary>Snapshot de saúde não encontrado pelo identificador.</summary>
    public static Error SnapshotNotFound(string id) => Error.NotFound(
        "Governance.TeamHealthSnapshot.NotFound",
        "Team health snapshot '{0}' was not found.", id);

    /// <summary>Nenhum snapshot de saúde encontrado para a equipa indicada.</summary>
    public static Error SnapshotNotFoundForTeam(string teamId) => Error.NotFound(
        "Governance.TeamHealthSnapshot.TeamNotFound",
        "No health snapshot found for team '{0}'.", teamId);
}

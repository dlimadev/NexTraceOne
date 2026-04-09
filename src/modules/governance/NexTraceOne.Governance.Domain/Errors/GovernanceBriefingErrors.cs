using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros de executive briefings.
/// </summary>
public static class GovernanceBriefingErrors
{
    /// <summary>Executive briefing não encontrado pelo identificador.</summary>
    public static Error BriefingNotFound(string id) => Error.NotFound(
        "Governance.ExecutiveBriefing.NotFound",
        "Executive briefing '{0}' was not found.", id);

    /// <summary>Transição de estado inválida para o executive briefing.</summary>
    public static Error InvalidTransition(string id, string currentStatus, string targetStatus) => Error.Validation(
        "Governance.ExecutiveBriefing.InvalidTransition",
        "Cannot transition briefing '{0}' from '{1}' to '{2}'.", id, currentStatus, targetStatus);
}

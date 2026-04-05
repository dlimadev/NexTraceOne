using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Catalog.Domain.DeveloperExperience;

/// <summary>Erros do domínio de Developer Experience.</summary>
public static class DeveloperExperienceErrors
{
    public static Error TeamNotFound(string id) =>
        Error.NotFound("TEAM_NOT_FOUND_DX", $"Team '{id}' not found.");

    public static Error InvalidPeriod() =>
        Error.Validation("INVALID_DX_PERIOD", "Valid periods: weekly, monthly, quarterly.");
}

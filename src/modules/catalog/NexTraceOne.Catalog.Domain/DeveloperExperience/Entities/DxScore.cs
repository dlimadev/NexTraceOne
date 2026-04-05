using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

/// <summary>
/// Score de experiência do desenvolvedor (DX Score) para uma equipa, calculado a partir
/// de métricas DORA: frequência de deploy, cycle time, cognitive load e toil.
/// </summary>
public sealed class DxScore : Entity<DxScoreId>
{
    private DxScore() { }

    public string TeamId { get; private set; } = string.Empty;
    public string TeamName { get; private set; } = string.Empty;
    public string? ServiceId { get; private set; }
    public string Period { get; private set; } = string.Empty;
    public decimal CycleTimeHours { get; private set; }
    public decimal DeploymentFrequencyPerWeek { get; private set; }
    public decimal CognitiveLoadScore { get; private set; }
    public decimal ToilPercentage { get; private set; }
    public decimal OverallScore { get; private set; }
    public string ScoreLevel { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    private static readonly string[] ValidPeriods = ["weekly", "monthly", "quarterly"];

    /// <summary>Cria um novo DX Score para uma equipa.</summary>
    public static Result<DxScore> Create(
        string teamId,
        string teamName,
        string? serviceId,
        string period,
        decimal cycleTimeHours,
        decimal deploymentFrequencyPerWeek,
        decimal cognitiveLoadScore,
        decimal toilPercentage,
        string? notes,
        DateTimeOffset computedAt)
    {
        if (string.IsNullOrWhiteSpace(teamId) || teamId.Length > 200)
            return Error.Validation("INVALID_TEAM_ID", "TeamId is required.");
        if (string.IsNullOrWhiteSpace(teamName) || teamName.Length > 200)
            return Error.Validation("INVALID_TEAM_NAME", "TeamName is required.");
        if (!ValidPeriods.Contains(period))
            return Error.Validation("INVALID_DX_PERIOD", "Valid periods: weekly, monthly, quarterly.");
        if (cycleTimeHours <= 0m)
            return Error.Validation("INVALID_CYCLE_TIME", "CycleTimeHours must be greater than 0.");
        if (cognitiveLoadScore < 0m || cognitiveLoadScore > 10m)
            return Error.Validation("INVALID_COGNITIVE_LOAD", "CognitiveLoadScore must be between 0 and 10.");
        if (toilPercentage < 0m || toilPercentage > 100m)
            return Error.Validation("INVALID_TOIL", "ToilPercentage must be between 0 and 100.");

        var deployScore = Math.Min(deploymentFrequencyPerWeek * 10m, 30m);
        var cycleScore = cycleTimeHours <= 1m ? 30m
            : cycleTimeHours <= 24m ? 25m
            : cycleTimeHours <= 168m ? 15m
            : 5m;
        var loadScore = (10m - cognitiveLoadScore) * 2m;
        var toilScore = (100m - toilPercentage) * 0.2m;
        var overallScore = Math.Min(deployScore + cycleScore + loadScore + toilScore, 100m);
        var scoreLevel = overallScore >= 80m ? "Elite"
            : overallScore >= 60m ? "High"
            : overallScore >= 40m ? "Medium"
            : "Low";

        return Result<DxScore>.Success(new DxScore
        {
            Id = DxScoreId.New(),
            TeamId = teamId,
            TeamName = teamName,
            ServiceId = serviceId,
            Period = period,
            CycleTimeHours = cycleTimeHours,
            DeploymentFrequencyPerWeek = deploymentFrequencyPerWeek,
            CognitiveLoadScore = cognitiveLoadScore,
            ToilPercentage = toilPercentage,
            OverallScore = overallScore,
            ScoreLevel = scoreLevel,
            Notes = notes,
            ComputedAt = computedAt
        });
    }
}

/// <summary>Identificador fortemente tipado de DxScore.</summary>
public sealed record DxScoreId(Guid Value) : TypedIdBase(Value)
{
    public static DxScoreId New() => new(Guid.NewGuid());
    public static DxScoreId From(Guid id) => new(id);
}

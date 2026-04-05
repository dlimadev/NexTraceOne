using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

/// <summary>
/// Survey de experiência do desenvolvedor (NPS + satisfação por dimensão) submetido por um membro de equipa.
/// Usado para calcular o NPS agregado da equipa num período.
/// </summary>
public sealed class DeveloperSurvey : Entity<DeveloperSurveyId>
{
    private DeveloperSurvey() { }

    public string TeamId { get; private set; } = string.Empty;
    public string TeamName { get; private set; } = string.Empty;
    public string? ServiceId { get; private set; }
    public string RespondentId { get; private set; } = string.Empty;
    public string Period { get; private set; } = string.Empty;
    public int NpsScore { get; private set; }
    public decimal ToolSatisfaction { get; private set; }
    public decimal ProcessSatisfaction { get; private set; }
    public decimal PlatformSatisfaction { get; private set; }
    public string? Comments { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public string NpsCategory { get; private set; } = string.Empty;

    private static readonly string[] ValidPeriods = ["weekly", "monthly", "quarterly"];

    /// <summary>Cria um novo survey de NPS para um respondente de equipa.</summary>
    public static Result<DeveloperSurvey> Create(
        string teamId,
        string teamName,
        string? serviceId,
        string respondentId,
        string period,
        int npsScore,
        decimal toolSatisfaction,
        decimal processSatisfaction,
        decimal platformSatisfaction,
        string? comments,
        DateTimeOffset submittedAt)
    {
        if (string.IsNullOrWhiteSpace(teamId) || teamId.Length > 200)
            return Error.Validation("INVALID_TEAM_ID", "TeamId is required.");
        if (string.IsNullOrWhiteSpace(teamName) || teamName.Length > 200)
            return Error.Validation("INVALID_TEAM_NAME", "TeamName is required.");
        if (string.IsNullOrWhiteSpace(respondentId) || respondentId.Length > 200)
            return Error.Validation("INVALID_RESPONDENT_ID", "RespondentId is required.");
        if (!ValidPeriods.Contains(period))
            return Error.Validation("INVALID_SURVEY_PERIOD", "Valid periods: weekly, monthly, quarterly.");
        if (npsScore < 0 || npsScore > 10)
            return Error.Validation("INVALID_NPS_SCORE", "NpsScore must be between 0 and 10.");
        if (toolSatisfaction < 0m || toolSatisfaction > 5m)
            return Error.Validation("INVALID_TOOL_SATISFACTION", "ToolSatisfaction must be between 0 and 5.");
        if (processSatisfaction < 0m || processSatisfaction > 5m)
            return Error.Validation("INVALID_PROCESS_SATISFACTION", "ProcessSatisfaction must be between 0 and 5.");
        if (platformSatisfaction < 0m || platformSatisfaction > 5m)
            return Error.Validation("INVALID_PLATFORM_SATISFACTION", "PlatformSatisfaction must be between 0 and 5.");
        if (comments is not null && comments.Length > 2000)
            return Error.Validation("INVALID_COMMENTS", "Comments must not exceed 2000 characters.");

        var npsCategory = npsScore >= 9 ? "Promoter"
            : npsScore >= 7 ? "Passive"
            : "Detractor";

        return Result<DeveloperSurvey>.Success(new DeveloperSurvey
        {
            Id = DeveloperSurveyId.New(),
            TeamId = teamId,
            TeamName = teamName,
            ServiceId = serviceId,
            RespondentId = respondentId,
            Period = period,
            NpsScore = npsScore,
            ToolSatisfaction = toolSatisfaction,
            ProcessSatisfaction = processSatisfaction,
            PlatformSatisfaction = platformSatisfaction,
            Comments = comments,
            SubmittedAt = submittedAt,
            NpsCategory = npsCategory
        });
    }
}

/// <summary>Identificador fortemente tipado de DeveloperSurvey.</summary>
public sealed record DeveloperSurveyId(Guid Value) : TypedIdBase(Value)
{
    public static DeveloperSurveyId New() => new(Guid.NewGuid());
    public static DeveloperSurveyId From(Guid id) => new(id);
}

using System.Text.Json;
using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Score de confiança calculado por AI antes de um deployment.
/// Componentes ponderados: blast radius (25%), test coverage (20%), incident history (20%),
/// time of day risk (10%), deployer experience (10%), change size (10%), dependency stability (5%).
/// </summary>
public sealed class ChangeConfidenceScore : AuditableEntity<ChangeConfidenceScoreId>
{
    private ChangeConfidenceScore() { }

    public string ChangeId { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public int Score { get; private set; }
    public string Verdict { get; private set; } = string.Empty;

    public double BlastRadiusScore { get; private set; }
    public double TestCoverageScore { get; private set; }
    public double IncidentHistoryScore { get; private set; }
    public double TimeOfDayScore { get; private set; }
    public double DeployerExperienceScore { get; private set; }
    public double ChangeSizeScore { get; private set; }
    public double DependencyStabilityScore { get; private set; }

    public string ScoreBreakdownJson { get; private set; } = "{}";
    public string RecommendationText { get; private set; } = string.Empty;
    public string CalculatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CalculatedAt { get; private set; }

    public static ChangeConfidenceScore Calculate(
        string changeId,
        string serviceName,
        Guid tenantId,
        double blastRadiusScore,
        double testCoverageScore,
        double incidentHistoryScore,
        double timeOfDayScore,
        double deployerExperienceScore,
        double changeSizeScore,
        double dependencyStabilityScore,
        string calculatedBy,
        DateTimeOffset calculatedAt)
    {
        Guard.Against.NullOrWhiteSpace(changeId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(calculatedBy);

        var score = (int)Math.Round(
            blastRadiusScore * 25 + testCoverageScore * 20 + incidentHistoryScore * 20 +
            timeOfDayScore * 10 + deployerExperienceScore * 10 + changeSizeScore * 10 +
            dependencyStabilityScore * 5);

        var verdict = score >= 80 ? "SAFE"
            : score >= 65 ? "CAUTION"
            : score >= 40 ? "REVIEW_NEEDED"
            : "BLOCK";

        var recommendation = verdict switch
        {
            "SAFE" => "Change is safe to deploy. All components show healthy indicators.",
            "CAUTION" => "Proceed with caution. Monitor closely after deployment.",
            "REVIEW_NEEDED" => "Review required before deployment. Address identified risks.",
            _ => "Deployment blocked. Critical risks detected. Do not proceed."
        };

        var breakdown = JsonSerializer.Serialize(new
        {
            blastRadius = new { score = blastRadiusScore, weight = 0.25 },
            testCoverage = new { score = testCoverageScore, weight = 0.20 },
            incidentHistory = new { score = incidentHistoryScore, weight = 0.20 },
            timeOfDay = new { score = timeOfDayScore, weight = 0.10 },
            deployerExperience = new { score = deployerExperienceScore, weight = 0.10 },
            changeSize = new { score = changeSizeScore, weight = 0.10 },
            dependencyStability = new { score = dependencyStabilityScore, weight = 0.05 }
        });

        return new ChangeConfidenceScore
        {
            Id = ChangeConfidenceScoreId.New(),
            ChangeId = changeId,
            ServiceName = serviceName,
            TenantId = tenantId,
            Score = score,
            Verdict = verdict,
            BlastRadiusScore = blastRadiusScore,
            TestCoverageScore = testCoverageScore,
            IncidentHistoryScore = incidentHistoryScore,
            TimeOfDayScore = timeOfDayScore,
            DeployerExperienceScore = deployerExperienceScore,
            ChangeSizeScore = changeSizeScore,
            DependencyStabilityScore = dependencyStabilityScore,
            ScoreBreakdownJson = breakdown,
            RecommendationText = recommendation,
            CalculatedBy = calculatedBy,
            CalculatedAt = calculatedAt,
        };
    }
}

/// <summary>Identificador fortemente tipado de ChangeConfidenceScore.</summary>
public sealed record ChangeConfidenceScoreId(Guid Value) : TypedIdBase(Value)
{
    public static ChangeConfidenceScoreId New() => new(Guid.NewGuid());
    public static ChangeConfidenceScoreId From(Guid id) => new(id);
}

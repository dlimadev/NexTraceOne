using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetMaturityScorecards;

/// <summary>
/// Feature: GetMaturityScorecards — scorecards de maturidade por equipa ou domínio.
/// Avalia maturidade em 8 dimensões: ownership, contract, documentation, runbook,
/// dependencyMapping, changeValidation, operationalReadiness, aiGovernance.
/// </summary>
public static class GetMaturityScorecards
{
    /// <summary>Query de scorecards de maturidade. Dimensão: team ou domain.</summary>
    public sealed record Query(
        string? Dimension = null) : IQuery<Response>;

    /// <summary>Handler que computa scorecards de maturidade por grupo.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dimension = request.Dimension ?? "team";

            var scorecards = new List<MaturityScorecardDto>
            {
                new("team-payments", "Team Payments", MaturityLevel.Managed, new List<MaturityDimensionScoreDto>
                {
                    new("ownership", MaturityLevel.Optimizing, 10.0m, 10.0m, "Full ownership coverage with clear escalation paths"),
                    new("contract", MaturityLevel.Managed, 8.0m, 10.0m, "Contracts defined and versioned, minor gaps in event schemas"),
                    new("documentation", MaturityLevel.Defined, 6.5m, 10.0m, "Core documentation present, some services lack detail"),
                    new("runbook", MaturityLevel.Managed, 7.5m, 10.0m, "Runbooks for critical services, missing for auxiliary ones"),
                    new("dependencyMapping", MaturityLevel.Optimizing, 9.5m, 10.0m, "All dependencies mapped and monitored"),
                    new("changeValidation", MaturityLevel.Managed, 8.0m, 10.0m, "Automated validation in CI/CD, rollback procedures defined"),
                    new("operationalReadiness", MaturityLevel.Managed, 7.5m, 10.0m, "Monitoring and alerting in place, SLOs defined"),
                    new("aiGovernance", MaturityLevel.Developing, 4.0m, 10.0m, "Basic AI usage, no formal governance policies")
                }),
                new("team-commerce", "Team Commerce", MaturityLevel.Developing, new List<MaturityDimensionScoreDto>
                {
                    new("ownership", MaturityLevel.Defined, 6.0m, 10.0m, "Most services have owners, some gaps during transitions"),
                    new("contract", MaturityLevel.Developing, 4.5m, 10.0m, "Contracts partially defined, no versioning for event streams"),
                    new("documentation", MaturityLevel.Developing, 4.0m, 10.0m, "Sparse documentation, key flows undocumented"),
                    new("runbook", MaturityLevel.Initial, 2.0m, 10.0m, "Very few runbooks, incident response ad-hoc"),
                    new("dependencyMapping", MaturityLevel.Defined, 6.0m, 10.0m, "Major dependencies known, minor ones unmapped"),
                    new("changeValidation", MaturityLevel.Developing, 5.0m, 10.0m, "Manual validation, no automated blast radius analysis"),
                    new("operationalReadiness", MaturityLevel.Developing, 4.5m, 10.0m, "Basic monitoring, no SLOs defined"),
                    new("aiGovernance", MaturityLevel.Initial, 1.5m, 10.0m, "No AI governance framework in place")
                }),
                new("team-identity", "Team Identity", MaturityLevel.Defined, new List<MaturityDimensionScoreDto>
                {
                    new("ownership", MaturityLevel.Managed, 8.0m, 10.0m, "Clear ownership with documented responsibilities"),
                    new("contract", MaturityLevel.Defined, 7.0m, 10.0m, "REST contracts defined, SOAP contracts outdated"),
                    new("documentation", MaturityLevel.Defined, 6.0m, 10.0m, "Good documentation for core, gaps in edge cases"),
                    new("runbook", MaturityLevel.Developing, 4.5m, 10.0m, "Runbook for main service, missing for dependencies"),
                    new("dependencyMapping", MaturityLevel.Managed, 8.0m, 10.0m, "Dependencies well mapped with health checks"),
                    new("changeValidation", MaturityLevel.Defined, 6.5m, 10.0m, "Validation present but not fully automated"),
                    new("operationalReadiness", MaturityLevel.Defined, 6.5m, 10.0m, "Monitoring in place, SLOs partially defined"),
                    new("aiGovernance", MaturityLevel.Developing, 3.5m, 10.0m, "Experimenting with AI tools, no formal policies")
                }),
                new("team-integration", "Team Integration", MaturityLevel.Initial, new List<MaturityDimensionScoreDto>
                {
                    new("ownership", MaturityLevel.Developing, 3.5m, 10.0m, "Ownership unclear for legacy adapters"),
                    new("contract", MaturityLevel.Initial, 1.5m, 10.0m, "Most services lack formal contracts"),
                    new("documentation", MaturityLevel.Initial, 1.0m, 10.0m, "Minimal documentation, mostly tribal knowledge"),
                    new("runbook", MaturityLevel.Initial, 0.5m, 10.0m, "No runbooks available"),
                    new("dependencyMapping", MaturityLevel.Developing, 3.0m, 10.0m, "Major dependencies known but not formalized"),
                    new("changeValidation", MaturityLevel.Initial, 1.5m, 10.0m, "No automated validation, manual deployment"),
                    new("operationalReadiness", MaturityLevel.Initial, 2.0m, 10.0m, "Basic monitoring only, no alerting or SLOs"),
                    new("aiGovernance", MaturityLevel.Initial, 0.5m, 10.0m, "No AI governance awareness")
                }),
                new("team-messaging", "Team Messaging", MaturityLevel.Managed, new List<MaturityDimensionScoreDto>
                {
                    new("ownership", MaturityLevel.Optimizing, 9.5m, 10.0m, "Full ownership with rotation and backup defined"),
                    new("contract", MaturityLevel.Managed, 8.5m, 10.0m, "Event contracts well defined and versioned"),
                    new("documentation", MaturityLevel.Managed, 8.0m, 10.0m, "Comprehensive documentation with examples"),
                    new("runbook", MaturityLevel.Managed, 7.5m, 10.0m, "Runbooks for all services with regular reviews"),
                    new("dependencyMapping", MaturityLevel.Managed, 8.5m, 10.0m, "Dependencies mapped with impact analysis"),
                    new("changeValidation", MaturityLevel.Managed, 8.0m, 10.0m, "Automated validation with canary deployments"),
                    new("operationalReadiness", MaturityLevel.Optimizing, 9.0m, 10.0m, "Full observability, SLOs and error budgets defined"),
                    new("aiGovernance", MaturityLevel.Defined, 5.5m, 10.0m, "AI usage policies defined, audit in progress")
                })
            };

            var response = new Response(
                Dimension: dimension,
                Scorecards: scorecards,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta dos scorecards de maturidade por grupo.</summary>
    public sealed record Response(
        string Dimension,
        IReadOnlyList<MaturityScorecardDto> Scorecards,
        DateTimeOffset GeneratedAt);

    /// <summary>Scorecard de maturidade para um grupo com avaliação por dimensão.</summary>
    public sealed record MaturityScorecardDto(
        string GroupId,
        string GroupName,
        MaturityLevel OverallMaturity,
        IReadOnlyList<MaturityDimensionScoreDto> Dimensions);

    /// <summary>Pontuação de uma dimensão de maturidade com explicação.</summary>
    public sealed record MaturityDimensionScoreDto(
        string Dimension,
        MaturityLevel Level,
        decimal Score,
        decimal MaxScore,
        string Explanation);
}

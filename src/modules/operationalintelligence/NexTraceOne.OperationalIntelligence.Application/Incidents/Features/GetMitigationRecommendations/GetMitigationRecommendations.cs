using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendations;

/// <summary>
/// Feature: GetMitigationRecommendations — retorna recomendações de mitigação para um incidente,
/// incluindo tipo de ação sugerida, nível de risco, evidências e passos de validação.
/// </summary>
public static class GetMitigationRecommendations
{
    /// <summary>Query para obter recomendações de mitigação de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe as recomendações de mitigação do incidente.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = FindRecommendations(request.IncidentId);
            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static Response? FindRecommendations(string incidentId)
        {
            if (incidentId.Equals("a1b2c3d4-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    IncidentId: Guid.Parse(incidentId),
                    Recommendations: new[]
                    {
                        new MitigationRecommendationDto(
                            RecommendationId: Guid.Parse("bec00001-0001-0000-0000-000000000001"),
                            Title: "Rollback deployment to v2.13.2",
                            Summary: "Revert the latest deployment that introduced the payment processing regression.",
                            RecommendedActionType: MitigationActionType.RollbackCandidate,
                            RationaleSummary: "Deployment v2.14.0 correlates with error rate spike. Previous version was stable.",
                            EvidenceSummary: "Error rate increased from 0.1% to 12.4% within 15 minutes of deployment.",
                            RequiresApproval: true,
                            RiskLevel: RiskLevel.Medium,
                            LinkedRunbookIds: new[] { Guid.Parse("bb000001-0001-0000-0000-000000000001") },
                            SuggestedValidationSteps: new[] { "Monitor error rate for 30 minutes post-rollback", "Verify payment success rate returns to baseline" }),
                        new MitigationRecommendationDto(
                            RecommendationId: Guid.Parse("bec00001-0002-0000-0000-000000000002"),
                            Title: "Notify downstream teams of degradation",
                            Summary: "Alert downstream consumers of the payment service about current issues.",
                            RecommendedActionType: MitigationActionType.Escalate,
                            RationaleSummary: "Multiple downstream services depend on payment processing. Early notification reduces blast radius.",
                            EvidenceSummary: null,
                            RequiresApproval: false,
                            RiskLevel: RiskLevel.Low,
                            LinkedRunbookIds: Array.Empty<Guid>(),
                            SuggestedValidationSteps: new[] { "Confirm notification received by all downstream teams" }),
                        new MitigationRecommendationDto(
                            RecommendationId: Guid.Parse("bec00001-0003-0000-0000-000000000003"),
                            Title: "Investigate contract compatibility impact",
                            Summary: "Review if the deployment introduced breaking changes in the payment API contract.",
                            RecommendedActionType: MitigationActionType.ContractImpactReview,
                            RationaleSummary: "API contract changes in v2.14.0 may affect consumers using the legacy schema.",
                            EvidenceSummary: "Schema diff detected between v2.13.2 and v2.14.0 contracts.",
                            RequiresApproval: false,
                            RiskLevel: RiskLevel.Low,
                            LinkedRunbookIds: Array.Empty<Guid>(),
                            SuggestedValidationSteps: new[] { "Run contract compatibility validation", "Check consumer error logs" }),
                    });
            }

            if (incidentId.Equals("a1b2c3d4-0002-0000-0000-000000000002", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    IncidentId: Guid.Parse(incidentId),
                    Recommendations: new[]
                    {
                        new MitigationRecommendationDto(
                            RecommendationId: Guid.Parse("bec00002-0001-0000-0000-000000000001"),
                            Title: "Verify external dependency health",
                            Summary: "Check the status of the external catalog sync provider.",
                            RecommendedActionType: MitigationActionType.VerifyDependency,
                            RationaleSummary: "External dependency failure detected. Manual verification needed before further action.",
                            EvidenceSummary: "Connection timeout errors observed since 14:30 UTC.",
                            RequiresApproval: false,
                            RiskLevel: RiskLevel.Low,
                            LinkedRunbookIds: new[] { Guid.Parse("bb000002-0001-0000-0000-000000000001") },
                            SuggestedValidationSteps: new[] { "Check vendor status page", "Attempt manual sync request" }),
                        new MitigationRecommendationDto(
                            RecommendationId: Guid.Parse("bec00002-0002-0000-0000-000000000002"),
                            Title: "Enable manual sync fallback",
                            Summary: "Activate the manual sync fallback to maintain catalog availability.",
                            RecommendedActionType: MitigationActionType.ExecuteRunbook,
                            RationaleSummary: "Manual fallback can restore partial functionality while external dependency is unavailable.",
                            EvidenceSummary: null,
                            RequiresApproval: true,
                            RiskLevel: RiskLevel.Medium,
                            LinkedRunbookIds: new[] { Guid.Parse("bb000002-0001-0000-0000-000000000001") },
                            SuggestedValidationSteps: new[] { "Verify catalog data freshness after manual sync", "Monitor sync error rate" }),
                    });
            }

            return null;
        }
    }

    /// <summary>Resposta com recomendações de mitigação do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        IReadOnlyList<MitigationRecommendationDto> Recommendations);

    /// <summary>Recomendação individual de mitigação.</summary>
    public sealed record MitigationRecommendationDto(
        Guid RecommendationId,
        string Title,
        string Summary,
        MitigationActionType RecommendedActionType,
        string RationaleSummary,
        string? EvidenceSummary,
        bool RequiresApproval,
        RiskLevel RiskLevel,
        IReadOnlyList<Guid> LinkedRunbookIds,
        IReadOnlyList<string> SuggestedValidationSteps);
}

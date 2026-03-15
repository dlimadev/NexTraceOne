using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;

/// <summary>
/// Feature: GetIncidentMitigation — retorna informações de mitigação e runbooks de um incidente.
/// Inclui ações sugeridas, runbooks recomendados, status de mitigação,
/// orientação de rollback e recomendação de escalonamento.
/// </summary>
public static class GetIncidentMitigation
{
    /// <summary>Query para obter a mitigação de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe a mitigação do incidente.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var mitigation = FindMitigation(request.IncidentId);
            if (mitigation is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(mitigation));
        }

        private static Response? FindMitigation(string incidentId)
        {
            if (incidentId.Equals("a1b2c3d4-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    IncidentId: Guid.Parse(incidentId),
                    MitigationStatus: MitigationStatus.InProgress,
                    SuggestedActions: new[]
                    {
                        new SuggestedAction("Rollback to v2.13.2", "Applied", true),
                        new SuggestedAction("Monitor error rate recovery for 30 minutes", "In progress", false),
                        new SuggestedAction("Notify affected downstream teams", "Completed", true),
                        new SuggestedAction("Create post-incident review ticket", "Pending", false),
                    },
                    RecommendedRunbooks: new[]
                    {
                        new RecommendedRunbook("Payment Gateway Rollback Procedure", "https://docs.internal/runbooks/payment-rollback", "Service-specific rollback guide"),
                        new RecommendedRunbook("Payment Error Rate Troubleshooting", "https://docs.internal/runbooks/payment-errors", "Diagnostic steps for payment errors"),
                    },
                    RollbackGuidance: "Rollback to v2.13.2 is the primary mitigation. Deployment pipeline supports one-click rollback.",
                    RollbackRelevant: true,
                    EscalationGuidance: "Escalate to payments-lead if error rate does not recover within 30 minutes post-rollback.");
            }

            if (incidentId.Equals("a1b2c3d4-0002-0000-0000-000000000002", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    IncidentId: Guid.Parse(incidentId),
                    MitigationStatus: MitigationStatus.NotStarted,
                    SuggestedActions: new[]
                    {
                        new SuggestedAction("Contact vendor support", "Pending", false),
                        new SuggestedAction("Enable manual sync fallback", "Available", false),
                    },
                    RecommendedRunbooks: new[]
                    {
                        new RecommendedRunbook("Catalog Sync Manual Recovery", "https://docs.internal/runbooks/catalog-sync-recovery", "Steps for manual catalog sync"),
                    },
                    RollbackGuidance: "Not applicable — external dependency failure.",
                    RollbackRelevant: false,
                    EscalationGuidance: "Escalate to platform-lead if vendor does not respond within 2 hours.");
            }

            return null;
        }
    }

    /// <summary>Resposta de mitigação do incidente.</summary>
    public sealed record Response(
        Guid IncidentId,
        MitigationStatus MitigationStatus,
        IReadOnlyList<SuggestedAction> SuggestedActions,
        IReadOnlyList<RecommendedRunbook> RecommendedRunbooks,
        string? RollbackGuidance,
        bool RollbackRelevant,
        string? EscalationGuidance);

    /// <summary>Ação sugerida de mitigação.</summary>
    public sealed record SuggestedAction(string Description, string Status, bool Completed);

    /// <summary>Runbook recomendado.</summary>
    public sealed record RecommendedRunbook(string Title, string? Url, string? Description);
}

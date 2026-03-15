using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRunbookDetail;

/// <summary>
/// Feature: GetRunbookDetail — retorna os detalhes completos de um runbook operacional,
/// incluindo passos, pré-condições, orientação pós-validação e metadados.
/// </summary>
public static class GetRunbookDetail
{
    /// <summary>Query para obter o detalhe de um runbook.</summary>
    public sealed record Query(string RunbookId) : IQuery<Response>;

    /// <summary>Valida o identificador do runbook.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.RunbookId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe o detalhe do runbook.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = FindRunbook(request.RunbookId);
            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.RunbookNotFound(request.RunbookId));

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static Response? FindRunbook(string runbookId)
        {
            if (runbookId.Equals("bb000001-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    RunbookId: Guid.Parse(runbookId),
                    Title: "Payment Gateway Rollback Procedure",
                    Summary: "Step-by-step guide for rolling back the payment-service deployment to a known stable version.",
                    LinkedServiceId: "payment-service",
                    LinkedIncidentType: "ServiceDegradation",
                    Steps: new[]
                    {
                        new RunbookStepDto(1, "Confirm rollback target version", "Identify the last known stable version from deployment history.", false),
                        new RunbookStepDto(2, "Notify affected teams", "Send notification to downstream consumers before rollback.", false),
                        new RunbookStepDto(3, "Trigger rollback pipeline", "Use the CI/CD one-click rollback to deploy the target version.", false),
                        new RunbookStepDto(4, "Validate deployment health", "Check health endpoints and error rates post-deployment.", false),
                        new RunbookStepDto(5, "Monitor for 30 minutes", "Observe error rate and payment success metrics for stability.", false),
                        new RunbookStepDto(6, "Update incident status", "Mark the incident as mitigated and document the outcome.", true),
                    },
                    Preconditions: new[]
                    {
                        "CI/CD pipeline access for payment-service",
                        "Previous stable version identified",
                        "Downstream teams notified",
                    },
                    PostValidationGuidance: "After rollback, monitor error rate and payment success rate for at least 30 minutes. If metrics do not return to baseline, escalate to payments-lead.",
                    CreatedBy: "platform-team@nextraceone.io",
                    CreatedAt: DateTimeOffset.Parse("2024-01-15T09:00:00Z"),
                    UpdatedAt: DateTimeOffset.Parse("2024-05-20T14:30:00Z"));
            }

            if (runbookId.Equals("bb000002-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    RunbookId: Guid.Parse(runbookId),
                    Title: "Catalog Sync Manual Recovery",
                    Summary: "Steps for manually recovering catalog synchronization when the external provider is unavailable.",
                    LinkedServiceId: "catalog-service",
                    LinkedIncidentType: "DependencyFailure",
                    Steps: new[]
                    {
                        new RunbookStepDto(1, "Check vendor status page", "Verify the current status of the external catalog provider.", false),
                        new RunbookStepDto(2, "Attempt manual sync request", "Send a manual sync request to test connectivity.", false),
                        new RunbookStepDto(3, "Enable fallback mode", "Activate the manual sync fallback configuration.", false),
                        new RunbookStepDto(4, "Verify catalog data freshness", "Confirm catalog data is within acceptable freshness threshold.", false),
                    },
                    Preconditions: new[]
                    {
                        "Access to catalog-service configuration",
                        "Manual sync endpoint credentials",
                    },
                    PostValidationGuidance: "Monitor catalog data freshness and sync error rate. Disable fallback mode once vendor connectivity is restored.",
                    CreatedBy: "platform-team@nextraceone.io",
                    CreatedAt: DateTimeOffset.Parse("2024-02-10T11:00:00Z"),
                    UpdatedAt: null);
            }

            if (runbookId.Equals("bb000003-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    RunbookId: Guid.Parse(runbookId),
                    Title: "Generic Service Restart Procedure",
                    Summary: "Standard procedure for performing a controlled restart of a service with minimal impact.",
                    LinkedServiceId: null,
                    LinkedIncidentType: null,
                    Steps: new[]
                    {
                        new RunbookStepDto(1, "Notify dependent teams", "Alert teams that depend on this service about the planned restart.", true),
                        new RunbookStepDto(2, "Drain active connections", "Gracefully drain active connections before restart.", false),
                        new RunbookStepDto(3, "Trigger controlled restart", "Initiate the restart via orchestrator or deployment tool.", false),
                        new RunbookStepDto(4, "Verify service health", "Confirm the service is healthy post-restart.", false),
                    },
                    Preconditions: new[]
                    {
                        "Orchestrator or deployment tool access",
                        "Service health endpoint available",
                    },
                    PostValidationGuidance: "Monitor service health and downstream error rates for 15 minutes post-restart.",
                    CreatedBy: "sre-team@nextraceone.io",
                    CreatedAt: DateTimeOffset.Parse("2024-03-01T08:00:00Z"),
                    UpdatedAt: DateTimeOffset.Parse("2024-04-10T16:00:00Z"));
            }

            return null;
        }
    }

    /// <summary>Resposta com os detalhes completos do runbook.</summary>
    public sealed record Response(
        Guid RunbookId,
        string Title,
        string Summary,
        string? LinkedServiceId,
        string? LinkedIncidentType,
        IReadOnlyList<RunbookStepDto> Steps,
        IReadOnlyList<string> Preconditions,
        string? PostValidationGuidance,
        string CreatedBy,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    /// <summary>Passo individual do runbook.</summary>
    public sealed record RunbookStepDto(
        int StepOrder,
        string Title,
        string? Description,
        bool IsOptional);
}

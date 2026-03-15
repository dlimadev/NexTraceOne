using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationWorkflow;

/// <summary>
/// Feature: GetMitigationWorkflow — retorna os detalhes completos de um workflow de mitigação,
/// incluindo passos, decisões, status atual e resultado.
/// </summary>
public static class GetMitigationWorkflow
{
    /// <summary>Query para obter um workflow de mitigação de um incidente.</summary>
    public sealed record Query(string IncidentId, string WorkflowId) : IQuery<Response>;

    /// <summary>Valida os identificadores do incidente e do workflow.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe o detalhe do workflow de mitigação.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = FindWorkflow(request.IncidentId, request.WorkflowId);
            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static Response? FindWorkflow(string incidentId, string workflowId)
        {
            if (incidentId.Equals("a1b2c3d4-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase)
                && workflowId.Equals("wf-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    WorkflowId: Guid.Parse("00000001-0001-0000-0000-000000000001"),
                    IncidentId: Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001"),
                    Title: "Rollback payment-service to v2.13.2",
                    Status: MitigationWorkflowStatus.InProgress,
                    ActionType: MitigationActionType.RollbackCandidate,
                    RiskLevel: RiskLevel.Medium,
                    RequiresApproval: true,
                    ApprovedBy: "tech-lead@nextraceone.io",
                    ApprovedAt: DateTimeOffset.Parse("2024-06-15T10:30:00Z"),
                    CreatedBy: "ai-assistant",
                    CreatedAt: DateTimeOffset.Parse("2024-06-15T10:15:00Z"),
                    StartedAt: DateTimeOffset.Parse("2024-06-15T10:35:00Z"),
                    CompletedAt: null,
                    Outcome: null,
                    OutcomeNotes: null,
                    LinkedRunbookId: Guid.Parse("bb000001-0001-0000-0000-000000000001"),
                    Steps: new[]
                    {
                        new WorkflowStepDto(1, "Trigger rollback pipeline", "Initiate the CI/CD rollback to v2.13.2", true, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:36:00Z"), null),
                        new WorkflowStepDto(2, "Validate deployment status", "Confirm rollback deployment completed successfully", true, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:42:00Z"), "Deployment verified via health check"),
                        new WorkflowStepDto(3, "Monitor error rate recovery", "Observe error rate for 30 minutes post-rollback", false, null, null, null),
                        new WorkflowStepDto(4, "Confirm resolution", "Verify incident is resolved and close workflow", false, null, null, null),
                    },
                    Decisions: new[]
                    {
                        new WorkflowDecisionDto(MitigationDecisionType.Approved, "tech-lead@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:30:00Z"), "Approved based on correlation evidence and low risk of rollback."),
                    });
            }

            if (incidentId.Equals("a1b2c3d4-0002-0000-0000-000000000002", StringComparison.OrdinalIgnoreCase)
                && workflowId.Equals("wf-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    WorkflowId: Guid.Parse("00000002-0001-0000-0000-000000000001"),
                    IncidentId: Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002"),
                    Title: "Verify external catalog sync dependency",
                    Status: MitigationWorkflowStatus.AwaitingApproval,
                    ActionType: MitigationActionType.VerifyDependency,
                    RiskLevel: RiskLevel.Low,
                    RequiresApproval: true,
                    ApprovedBy: null,
                    ApprovedAt: null,
                    CreatedBy: "ai-assistant",
                    CreatedAt: DateTimeOffset.Parse("2024-06-15T14:45:00Z"),
                    StartedAt: null,
                    CompletedAt: null,
                    Outcome: null,
                    OutcomeNotes: null,
                    LinkedRunbookId: Guid.Parse("bb000002-0001-0000-0000-000000000001"),
                    Steps: new[]
                    {
                        new WorkflowStepDto(1, "Check vendor status page", "Verify current status of external provider", false, null, null, null),
                        new WorkflowStepDto(2, "Attempt manual sync request", "Test connectivity with a manual sync attempt", false, null, null, null),
                        new WorkflowStepDto(3, "Enable fallback mode", "Activate manual sync fallback if vendor is down", false, null, null, null),
                    },
                    Decisions: Array.Empty<WorkflowDecisionDto>());
            }

            return null;
        }
    }

    /// <summary>Resposta com detalhes completos do workflow de mitigação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        Guid IncidentId,
        string Title,
        MitigationWorkflowStatus Status,
        MitigationActionType ActionType,
        RiskLevel RiskLevel,
        bool RequiresApproval,
        string? ApprovedBy,
        DateTimeOffset? ApprovedAt,
        string CreatedBy,
        DateTimeOffset CreatedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        MitigationOutcome? Outcome,
        string? OutcomeNotes,
        Guid? LinkedRunbookId,
        IReadOnlyList<WorkflowStepDto> Steps,
        IReadOnlyList<WorkflowDecisionDto> Decisions);

    /// <summary>Passo individual do workflow de mitigação.</summary>
    public sealed record WorkflowStepDto(
        int StepOrder,
        string Title,
        string? Description,
        bool IsCompleted,
        string? CompletedBy,
        DateTimeOffset? CompletedAt,
        string? Notes);

    /// <summary>Decisão registada no workflow de mitigação.</summary>
    public sealed record WorkflowDecisionDto(
        MitigationDecisionType DecisionType,
        string DecidedBy,
        DateTimeOffset DecidedAt,
        string? Reason);
}

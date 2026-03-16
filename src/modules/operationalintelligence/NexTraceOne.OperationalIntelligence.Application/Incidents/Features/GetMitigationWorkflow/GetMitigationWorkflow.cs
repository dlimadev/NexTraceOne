using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
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
    public sealed class Handler(IIncidentStore store) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = store.GetMitigationWorkflow(request.IncidentId, request.WorkflowId);
            if (response is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(response));
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

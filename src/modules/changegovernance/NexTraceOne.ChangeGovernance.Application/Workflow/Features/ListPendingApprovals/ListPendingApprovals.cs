using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.ListPendingApprovals;

/// <summary>
/// Feature: ListPendingApprovals — lista instâncias de workflow em revisão com paginação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListPendingApprovals
{
    /// <summary>Query para listar instâncias de workflow pendentes de aprovação.</summary>
    public sealed record Query(
        string ApproverUserId,
        int Page,
        int PageSize) : IQuery<Response>;

    /// <summary>Valida a entrada da query de aprovações pendentes.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista instâncias de workflow em status InReview com paginação.</summary>
    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instances = await instanceRepository.ListByStatusAsync(
                WorkflowStatus.InReview, request.Page, request.PageSize, cancellationToken);

            var totalCount = await instanceRepository.CountByStatusAsync(
                WorkflowStatus.InReview, cancellationToken);

            var items = instances
                .Select(i => new PendingApprovalItem(
                    i.Id.Value,
                    i.ReleaseId,
                    i.SubmittedBy,
                    i.Status.ToString(),
                    i.CurrentStageIndex,
                    i.SubmittedAt))
                .ToList();

            return new Response(items, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>Item de uma instância de workflow pendente de aprovação.</summary>
    public sealed record PendingApprovalItem(
        Guid InstanceId,
        Guid ReleaseId,
        string SubmittedBy,
        string Status,
        int CurrentStageIndex,
        DateTimeOffset SubmittedAt);

    /// <summary>Resposta paginada de aprovações pendentes.</summary>
    public sealed record Response(
        IReadOnlyList<PendingApprovalItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}

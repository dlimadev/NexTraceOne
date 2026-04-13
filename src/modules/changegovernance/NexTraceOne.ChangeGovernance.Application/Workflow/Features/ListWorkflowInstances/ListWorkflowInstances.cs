using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.ListWorkflowInstances;

/// <summary>
/// Feature: ListWorkflowInstances — lista instâncias de workflow com paginação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListWorkflowInstances
{
    /// <summary>Query para listar instâncias de workflow.</summary>
    public sealed record Query(int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida a entrada da query de instâncias.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista todas as instâncias de workflow com paginação.</summary>
    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instances = await instanceRepository.ListAsync(request.Page, request.PageSize, cancellationToken);
            var totalCount = await instanceRepository.CountAsync(cancellationToken);

            var items = instances
                .Select(i => new WorkflowInstanceDto(
                    i.Id.Value,
                    i.ReleaseId,
                    i.SubmittedBy,
                    i.Status.ToString(),
                    i.CurrentStageIndex,
                    i.SubmittedAt,
                    i.CompletedAt))
                .ToList();

            return new Response(items, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>DTO de instância de workflow.</summary>
    public sealed record WorkflowInstanceDto(
        Guid InstanceId,
        Guid ReleaseId,
        string SubmittedBy,
        string Status,
        int CurrentStageIndex,
        DateTimeOffset SubmittedAt,
        DateTimeOffset? CompletedAt);

    /// <summary>Resposta paginada de instâncias de workflow.</summary>
    public sealed record Response(
        IReadOnlyList<WorkflowInstanceDto> Items,
        int TotalCount,
        int Page,
        int PageSize);
}

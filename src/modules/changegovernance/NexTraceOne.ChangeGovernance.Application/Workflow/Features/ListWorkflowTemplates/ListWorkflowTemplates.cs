using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.ListWorkflowTemplates;

/// <summary>
/// Feature: ListWorkflowTemplates — lista templates ativos de workflow com paginação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListWorkflowTemplates
{
    /// <summary>Query para listar templates ativos de workflow.</summary>
    public sealed record Query(int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida a entrada da query de templates.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista templates ativos com paginação.</summary>
    public sealed class Handler(
        IWorkflowTemplateRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var templates = await repository.ListActiveAsync(request.Page, request.PageSize, cancellationToken);
            var totalCount = await repository.CountActiveAsync(cancellationToken);

            var items = templates
                .Select(t => new WorkflowTemplateDto(
                    t.Id.Value,
                    t.Name,
                    t.Description,
                    t.ChangeType,
                    t.ApiCriticality,
                    t.TargetEnvironment,
                    t.MinimumApprovers,
                    t.IsActive,
                    t.CreatedAt))
                .ToList();

            return new Response(items, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>DTO de template de workflow.</summary>
    public sealed record WorkflowTemplateDto(
        Guid TemplateId,
        string Name,
        string Description,
        string ChangeType,
        string ApiCriticality,
        string TargetEnvironment,
        int MinimumApprovers,
        bool IsActive,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta paginada de templates de workflow.</summary>
    public sealed record Response(
        IReadOnlyList<WorkflowTemplateDto> Items,
        int TotalCount,
        int Page,
        int PageSize);
}

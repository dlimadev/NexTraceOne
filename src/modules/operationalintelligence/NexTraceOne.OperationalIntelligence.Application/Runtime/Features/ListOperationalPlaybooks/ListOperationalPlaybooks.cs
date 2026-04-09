using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListOperationalPlaybooks;

/// <summary>
/// Feature: ListOperationalPlaybooks — lista playbooks operacionais com filtro opcional por status.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListOperationalPlaybooks
{
    /// <summary>Query para listar playbooks operacionais com filtro opcional por status.</summary>
    public sealed record Query(string? Status = null) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Status).MaximumLength(50).When(x => x.Status is not null);
        }
    }

    /// <summary>Handler que lista playbooks operacionais.</summary>
    public sealed class Handler(
        IOperationalPlaybookRepository repository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var tenantId = currentTenant.Id.ToString();

            IReadOnlyList<Domain.Runtime.Entities.OperationalPlaybook> playbooks;

            if (request.Status is not null && Enum.TryParse<PlaybookStatus>(request.Status, true, out var statusFilter))
                playbooks = await repository.ListByStatusAsync(tenantId, statusFilter, cancellationToken);
            else
                playbooks = await repository.ListAsync(tenantId, cancellationToken);

            var items = playbooks
                .Select(p => new PlaybookSummaryItem(
                    p.Id.Value,
                    p.Name,
                    p.Version,
                    p.Status.ToString(),
                    p.ExecutionCount,
                    p.LastExecutedAt,
                    p.CreatedAt))
                .ToList();

            return Result<Response>.Success(new Response(items, items.Count));
        }
    }

    /// <summary>Item de resumo do playbook para listagem.</summary>
    public sealed record PlaybookSummaryItem(
        Guid PlaybookId,
        string Name,
        int Version,
        string Status,
        int ExecutionCount,
        DateTimeOffset? LastExecutedAt,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta com a lista de playbooks.</summary>
    public sealed record Response(
        IReadOnlyList<PlaybookSummaryItem> Items,
        int TotalCount);
}

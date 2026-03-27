using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;

namespace NexTraceOne.Knowledge.Application.Features.GetKnowledgeRelationsBySource;

/// <summary>
/// Lista as relações ligadas a um documento/nota para navegação contextual mínima.
/// </summary>
public static class GetKnowledgeRelationsBySource
{
    public sealed record Query(Guid SourceEntityId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SourceEntityId).NotEqual(Guid.Empty);
        }
    }

    public sealed class Handler(
        IKnowledgeRelationRepository relationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var relations = await relationRepository.ListBySourceAsync(request.SourceEntityId, cancellationToken);
            var items = relations
                .Select(r => new RelationItem(
                    r.Id.Value,
                    r.SourceEntityId,
                    r.SourceEntityType.ToString(),
                    r.TargetEntityId,
                    r.TargetType.ToString(),
                    r.Description,
                    r.Context,
                    r.CreatedById,
                    r.CreatedAt))
                .ToArray();

            return new Response(items);
        }
    }

    public sealed record Response(IReadOnlyList<RelationItem> Items);

    public sealed record RelationItem(
        Guid RelationId,
        Guid SourceEntityId,
        string SourceEntityType,
        Guid TargetEntityId,
        string TargetType,
        string? Description,
        string? Context,
        Guid CreatedById,
        DateTimeOffset CreatedAt);
}

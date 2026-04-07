using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListEntityTags;

/// <summary>Feature: ListEntityTags — lista as tags de uma entidade.</summary>
public static class ListEntityTags
{
    public sealed record Query(string TenantId, string EntityType, string EntityId) : IQuery<Response>;

    public sealed class Handler(IEntityTagRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var tags = await repository.ListByEntityAsync(request.TenantId, request.EntityType, request.EntityId, cancellationToken);
            var items = tags.Select(t => new TagSummary(t.Id.Value, t.Key, t.Value, t.CreatedBy, t.CreatedAt)).ToList();
            return Result<Response>.Success(new Response(items));
        }
    }

    public sealed record TagSummary(Guid TagId, string Key, string Value, string CreatedBy, DateTimeOffset CreatedAt);
    public sealed record Response(IReadOnlyList<TagSummary> Items);
}

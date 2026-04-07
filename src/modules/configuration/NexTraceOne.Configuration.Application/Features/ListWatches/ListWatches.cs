using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListWatches;

/// <summary>Feature: ListWatches — lista as entidades seguidas pelo utilizador.</summary>
public static class ListWatches
{
    public sealed record Query(string? EntityType) : IQuery<Response>;

    public sealed class Handler(
        IUserWatchRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var tenantId = currentTenant.Id.ToString();
            var watches = await repository.ListByUserAsync(currentUser.Id, tenantId, request.EntityType, cancellationToken);

            var items = watches
                .Select(w => new WatchSummary(w.Id.Value, w.EntityType, w.EntityId, w.NotifyLevel, w.CreatedAt))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record WatchSummary(Guid WatchId, string EntityType, string EntityId, string NotifyLevel, DateTimeOffset CreatedAt);
    public sealed record Response(IReadOnlyList<WatchSummary> Items, int TotalCount);
}

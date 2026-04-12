using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.ListUserSavedViews;

public static class ListUserSavedViews
{
    public sealed record Query(string? Context) : IQuery<Response>;

    public sealed class Handler(
        IUserSavedViewRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var ownViews = await repository.ListByUserAsync(currentUser.Id, request.Context, cancellationToken);

            var sharedViews = request.Context is not null
                ? await repository.ListSharedByContextAsync(request.Context, currentTenant.Id.ToString(), cancellationToken)
                : [];

            var items = ownViews
                .Select(v => new SavedViewItem(v.Id.Value, v.UserId, v.Context, v.Name, v.Description, v.IsShared, true, v.SortOrder, v.CreatedAt))
                .Concat(sharedViews
                    .Where(v => v.UserId != currentUser.Id)
                    .Select(v => new SavedViewItem(v.Id.Value, v.UserId, v.Context, v.Name, v.Description, v.IsShared, false, v.SortOrder, v.CreatedAt)))
                .DistinctBy(v => v.Id)
                .ToList();

            return new Response(items);
        }
    }

    public sealed record SavedViewItem(
        Guid Id,
        string UserId,
        string Context,
        string Name,
        string? Description,
        bool IsShared,
        bool IsOwn,
        int SortOrder,
        DateTimeOffset CreatedAt);

    public sealed record Response(List<SavedViewItem> Items);
}

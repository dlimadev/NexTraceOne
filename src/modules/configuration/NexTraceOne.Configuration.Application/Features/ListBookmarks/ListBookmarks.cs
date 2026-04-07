using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.ListBookmarks;

public static class ListBookmarks
{
    public sealed record Query(BookmarkEntityType? EntityType) : IQuery<Response>;

    public sealed class Handler(
        IUserBookmarkRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var tenantId = currentTenant.Id.ToString();
            var bookmarks = await repository.ListByUserAsync(currentUser.Id, tenantId, request.EntityType, cancellationToken);

            var items = bookmarks
                .Select(b => new BookmarkItem(b.Id.Value, b.EntityType, b.EntityId, b.DisplayName, b.Url, b.CreatedAt))
                .ToList();

            return new Response(items);
        }
    }

    public sealed record BookmarkItem(Guid Id, BookmarkEntityType EntityType, string EntityId, string DisplayName, string? Url, DateTimeOffset CreatedAt);
    public sealed record Response(List<BookmarkItem> Items);
}

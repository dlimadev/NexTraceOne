using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.RemoveBookmark;

public static class RemoveBookmark
{
    public sealed record Command(Guid Id) : ICommand<Response>;

    public sealed class Handler(
        IUserBookmarkRepository repository,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var bookmark = await repository.GetByIdAsync(new UserBookmarkId(request.Id), cancellationToken);
            if (bookmark is null)
                return Error.NotFound("Bookmark.NotFound", $"Bookmark '{request.Id}' not found.");

            if (bookmark.UserId != currentUser.Id)
                return Error.Forbidden("Bookmark.Forbidden", "You do not own this bookmark.");

            await repository.DeleteAsync(bookmark, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(request.Id, DateTimeOffset.UtcNow);
        }
    }

    public sealed record Response(Guid Id, DateTimeOffset RemovedAt);
}

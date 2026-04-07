using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.DeleteUserSavedView;

public static class DeleteUserSavedView
{
    public sealed record Command(Guid Id) : ICommand<Response>;

    public sealed class Handler(
        IUserSavedViewRepository repository,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var view = await repository.GetByIdAsync(new UserSavedViewId(request.Id), cancellationToken);
            if (view is null)
                return Error.NotFound("SavedView.NotFound", $"Saved view '{request.Id}' not found.");

            if (view.UserId != currentUser.Id)
                return Error.Forbidden("SavedView.Forbidden", "You do not own this saved view.");

            await repository.DeleteAsync(view, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(request.Id, DateTimeOffset.UtcNow);
        }
    }

    public sealed record Response(Guid Id, DateTimeOffset DeletedAt);
}

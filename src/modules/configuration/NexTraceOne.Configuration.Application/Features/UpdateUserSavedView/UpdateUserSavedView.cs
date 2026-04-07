using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.UpdateUserSavedView;

public static class UpdateUserSavedView
{
    public sealed record Command(Guid Id, string Name, string? Description, string FiltersJson, bool IsShared) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.FiltersJson).NotEmpty().MaximumLength(8000);
        }
    }

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

            view.Update(request.Name, request.Description, request.FiltersJson, request.IsShared);
            await repository.UpdateAsync(view, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(view.Id.Value, view.Name, view.Context, view.IsShared, view.UpdatedAt ?? DateTimeOffset.UtcNow);
        }
    }

    public sealed record Response(Guid Id, string Name, string Context, bool IsShared, DateTimeOffset UpdatedAt);
}

using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.CreateUserSavedView;

public static class CreateUserSavedView
{
    public sealed record Command(string Context, string Name, string? Description, string FiltersJson, bool IsShared) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Context).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.FiltersJson).NotEmpty().MaximumLength(8000);
        }
    }

    public sealed class Handler(
        IUserSavedViewRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var view = UserSavedView.Create(
                userId: currentUser.Id,
                tenantId: currentTenant.Id.ToString(),
                context: request.Context,
                name: request.Name,
                filtersJson: request.FiltersJson,
                description: request.Description,
                isShared: request.IsShared);

            await repository.AddAsync(view, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(view.Id.Value, view.Name, view.Context, view.IsShared, view.CreatedAt);
        }
    }

    public sealed record Response(Guid Id, string Name, string Context, bool IsShared, DateTimeOffset CreatedAt);
}

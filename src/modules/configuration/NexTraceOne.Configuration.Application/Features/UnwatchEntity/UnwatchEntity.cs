using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.UnwatchEntity;

/// <summary>Feature: UnwatchEntity — remove uma entidade da watch list do utilizador.</summary>
public static class UnwatchEntity
{
    public sealed record Command(Guid WatchId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WatchId).NotEmpty();
        }
    }

    public sealed class Handler(
        IUserWatchRepository repository,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var watch = await repository.GetByIdAsync(new UserWatchId(request.WatchId), cancellationToken);
            if (watch is null)
                return Error.NotFound("UserWatch.NotFound", $"Watch '{request.WatchId}' not found.");

            if (watch.UserId != currentUser.Id)
                return Error.Forbidden("UserWatch.Forbidden", "You do not own this watch.");

            await repository.DeleteAsync(watch, cancellationToken);
            return new Response(request.WatchId);
        }
    }

    public sealed record Response(Guid WatchId);
}

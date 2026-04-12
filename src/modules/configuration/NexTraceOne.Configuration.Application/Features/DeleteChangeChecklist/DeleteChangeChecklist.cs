using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.DeleteChangeChecklist;

/// <summary>Feature: DeleteChangeChecklist — remove uma checklist de mudança.</summary>
public static class DeleteChangeChecklist
{
    public sealed record Command(Guid Id) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public sealed class Handler(
        IChangeChecklistRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var checklist = await repository.GetByIdAsync(
                new ChangeChecklistId(request.Id),
                currentTenant.Id.ToString(),
                cancellationToken);

            if (checklist is null)
                return Error.NotFound("ChangeChecklist.NotFound", $"Checklist '{request.Id}' not found.");

            await repository.DeleteAsync(new ChangeChecklistId(request.Id), cancellationToken);

            return new Response(request.Id);
        }
    }

    public sealed record Response(Guid Id);
}

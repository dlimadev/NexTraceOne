using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.DeleteSavedPrompt;

/// <summary>Feature: DeleteSavedPrompt — remove um prompt guardado pelo utilizador.</summary>
public static class DeleteSavedPrompt
{
    public sealed record Command(Guid PromptId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PromptId).NotEmpty();
        }
    }

    public sealed class Handler(
        ISavedPromptRepository repository,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var prompt = await repository.GetByIdAsync(new SavedPromptId(request.PromptId), cancellationToken);
            if (prompt is null)
                return Error.NotFound("SavedPrompt.NotFound", $"Saved prompt '{request.PromptId}' not found.");

            if (prompt.UserId != currentUser.Id)
                return Error.Forbidden("SavedPrompt.Forbidden", "You do not own this saved prompt.");

            await repository.DeleteAsync(new SavedPromptId(request.PromptId), cancellationToken);
            return new Response(request.PromptId);
        }
    }

    public sealed record Response(Guid PromptId);
}

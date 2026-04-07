using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.ShareSavedPrompt;

/// <summary>Feature: ShareSavedPrompt — altera o estado de partilha de um prompt guardado.</summary>
public static class ShareSavedPrompt
{
    public sealed record Command(Guid PromptId, bool IsShared) : ICommand<Response>;

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

            prompt.SetShared(request.IsShared);
            await repository.UpdateAsync(prompt, cancellationToken);
            return new Response(request.PromptId, prompt.IsShared);
        }
    }

    public sealed record Response(Guid PromptId, bool IsShared);
}

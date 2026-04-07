using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.SavePrompt;

/// <summary>Feature: SavePrompt — guarda um novo prompt de IA personalizado para o utilizador.</summary>
public static class SavePrompt
{
    private static readonly string[] ValidContextTypes = ["general", "incident", "contract", "change", "service"];

    public sealed record Command(
        string Name,
        string PromptText,
        string ContextType,
        string? TagsCsv,
        bool IsShared) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PromptText).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.ContextType).NotEmpty()
                .Must(c => ValidContextTypes.Contains(c, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"ContextType must be one of: {string.Join(", ", ValidContextTypes)}");
        }
    }

    public sealed class Handler(
        ISavedPromptRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var prompt = SavedPrompt.Create(
                currentUser.Id,
                currentTenant.Id.ToString(),
                request.Name,
                request.PromptText,
                request.ContextType,
                request.TagsCsv,
                request.IsShared,
                clock.UtcNow);

            await repository.AddAsync(prompt, cancellationToken);

            return new Response(prompt.Id.Value, prompt.Name, prompt.ContextType, prompt.IsShared, prompt.CreatedAt);
        }
    }

    public sealed record Response(Guid PromptId, string Name, string ContextType, bool IsShared, DateTimeOffset CreatedAt);
}

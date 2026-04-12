using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.CreateChangeChecklist;

/// <summary>Feature: CreateChangeChecklist — cria uma checklist personalizada para um tipo de mudança.</summary>
public static class CreateChangeChecklist
{
    public sealed record Command(
        string Name,
        string ChangeType,
        string? Environment,
        bool IsRequired,
        IReadOnlyList<string> Items) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ChangeType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Items).NotNull();
        }
    }

    public sealed class Handler(
        IChangeChecklistRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var checklist = ChangeChecklist.Create(
                currentTenant.Id.ToString(),
                request.Name,
                request.ChangeType,
                request.Environment,
                request.IsRequired,
                request.Items,
                clock.UtcNow);

            await repository.AddAsync(checklist, cancellationToken);

            return new Response(
                checklist.Id.Value,
                checklist.Name,
                checklist.ChangeType,
                checklist.Environment,
                checklist.IsRequired,
                checklist.Items,
                checklist.CreatedAt);
        }
    }

    public sealed record Response(
        Guid ChecklistId,
        string Name,
        string ChangeType,
        string? Environment,
        bool IsRequired,
        IReadOnlyList<string> Items,
        DateTimeOffset CreatedAt);
}

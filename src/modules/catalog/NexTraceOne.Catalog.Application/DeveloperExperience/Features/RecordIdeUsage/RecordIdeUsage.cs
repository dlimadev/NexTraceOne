using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using static NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions.IIDEUsageRepository;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Features.RecordIdeUsage;

/// <summary>
/// Feature: RecordIdeUsage — regista um evento de uso da extensão IDE por utilizador.
/// Alimenta GetDeveloperActivityReport e métricas de adopção de IA.
/// Wave AK.1 — IDE Context API (Catalog / DeveloperExperience).
/// </summary>
public static class RecordIdeUsage
{
    public sealed record Command(
        string UserId,
        string TenantId,
        string EventType,
        string? ResourceName) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.EventType)
                .NotEmpty()
                .Must(e => Enum.TryParse<IdeEventType>(e, ignoreCase: true, out _))
                .WithMessage("Invalid IDE event type.");
        }
    }

    public sealed record Response(Guid RecordId, DateTimeOffset RecordedAt);

    public sealed class Handler(
        IIDEUsageRepository repository,
        IDeveloperExperienceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.UserId);
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var eventType = Enum.Parse<IdeEventType>(request.EventType, ignoreCase: true);
            var id = Guid.NewGuid();
            var now = clock.UtcNow;

            var record = new IdeUsageRecord(id, request.UserId, request.TenantId, eventType, request.ResourceName, now);
            await repository.AddAsync(record, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(id, now));
        }
    }
}

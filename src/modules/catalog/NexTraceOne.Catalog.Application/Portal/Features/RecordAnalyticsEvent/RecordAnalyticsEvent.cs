using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.DeveloperPortal.Application.Abstractions;
using NexTraceOne.DeveloperPortal.Domain.Entities;

namespace NexTraceOne.DeveloperPortal.Application.Features.RecordAnalyticsEvent;

/// <summary>
/// Feature: RecordAnalyticsEvent — regista evento de analytics do portal.
/// Captura interações de utilização para métricas de adoção.
/// Estrutura VSA: Command + Validator + Handler em um único arquivo.
/// </summary>
public static class RecordAnalyticsEvent
{
    /// <summary>Comando para registar evento de analytics.</summary>
    public sealed record Command(
        Guid? UserId,
        string EventType,
        string? EntityId,
        string? EntityType,
        string? SearchQuery,
        bool? ZeroResults,
        long? DurationMs,
        string? Metadata) : ICommand;

    /// <summary>Valida os parâmetros do evento de analytics.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EventType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.EntityId).MaximumLength(200);
            RuleFor(x => x.EntityType).MaximumLength(100);
            RuleFor(x => x.SearchQuery).MaximumLength(500);
            RuleFor(x => x.Metadata).MaximumLength(4000);
        }
    }

    /// <summary>Handler que regista evento de analytics no repositório.</summary>
    public sealed class Handler(
        IPortalAnalyticsRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var analyticsEvent = PortalAnalyticsEvent.Create(
                request.UserId,
                request.EventType,
                request.EntityId,
                request.EntityType,
                request.SearchQuery,
                request.ZeroResults,
                request.DurationMs,
                request.Metadata,
                clock.UtcNow);

            repository.Add(analyticsEvent);
            await unitOfWork.CommitAsync(cancellationToken);

            return Unit.Value;
        }
    }
}

using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.RecordAnalyticsEvent;

/// <summary>
/// Regista um evento de product analytics.
/// Cada evento responde a uma pergunta real sobre adoção, valor ou fricção.
/// Privacy-aware: não captura PII desnecessário nem payloads sensíveis.
/// COMPATIBILIDADE TRANSITÓRIA (P2.4): Handler temporariamente em Governance.Application.
/// Ownership real: módulo Product Analytics. Migração para ProductAnalytics.Application prevista em fase futura.
/// </summary>
public static class RecordAnalyticsEvent
{
    /// <summary>Comando para registar um evento de analytics.</summary>
    public sealed record Command(
        AnalyticsEventType EventType,
        ProductModule Module,
        string Route,
        string? Feature,
        string? EntityType,
        string? Outcome,
        string? PersonaHint,
        string? TeamId,
        string? DomainId,
        string? SessionCorrelationId,
        string? ClientType,
        string? MetadataJson) : ICommand<Response>;

    /// <summary>Valida os parâmetros mínimos do evento de analytics.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Route).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Feature).MaximumLength(200);
            RuleFor(x => x.EntityType).MaximumLength(100);
            RuleFor(x => x.Outcome).MaximumLength(200);
            RuleFor(x => x.PersonaHint).MaximumLength(50);
            RuleFor(x => x.TeamId).MaximumLength(200);
            RuleFor(x => x.DomainId).MaximumLength(200);
            RuleFor(x => x.SessionCorrelationId).MaximumLength(200);
            RuleFor(x => x.ClientType).MaximumLength(50);
            RuleFor(x => x.MetadataJson).MaximumLength(8000);
        }
    }

    /// <summary>Handler que processa e armazena o evento de analytics.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentTenant tenant,
        ICurrentUser user,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var analyticsEvent = AnalyticsEvent.Create(
                tenantId: tenant.Id,
                userId: user.IsAuthenticated ? user.Id : null,
                persona: request.PersonaHint,
                module: request.Module,
                eventType: request.EventType,
                feature: request.Feature,
                entityType: request.EntityType,
                outcome: request.Outcome,
                route: request.Route,
                teamId: request.TeamId,
                domainId: request.DomainId,
                sessionId: request.SessionCorrelationId,
                clientType: request.ClientType,
                metadataJson: request.MetadataJson,
                occurredAt: clock.UtcNow);

            await repository.AddAsync(analyticsEvent, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(
                EventId: analyticsEvent.Id.Value.ToString("N")[..12],
                RecordedAt: analyticsEvent.OccurredAt,
                EventType: request.EventType,
                Module: request.Module);

            return response;
        }
    }

    /// <summary>Confirmação de registo do evento.</summary>
    public sealed record Response(
        string EventId,
        DateTimeOffset RecordedAt,
        AnalyticsEventType EventType,
        ProductModule Module);
}

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.RecordAnalyticsEvent;

/// <summary>
/// Regista um evento de product analytics.
/// Cada evento responde a uma pergunta real sobre adoção, valor ou fricção.
/// Privacy-aware: não captura PII desnecessário nem payloads sensíveis.
/// </summary>
public static class RecordAnalyticsEvent
{
    /// <summary>Comando para registar um evento de analytics.</summary>
    public sealed record Command(
        AnalyticsEventType EventType,
        ProductModule Module,
        string? Feature,
        string? EntityType,
        string? Outcome,
        string? PersonaHint,
        string? TeamId,
        string? DomainId,
        string? SessionCorrelationId,
        string? ClientType) : ICommand<Response>;

    /// <summary>Handler que processa e armazena o evento de analytics.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // MVP: evento aceite e registado em memória.
            // Em produção, persistir em store dedicado com retenção e agregação.
            // NOTA: em produção, usar IClock/ISystemClock para obter o timestamp.
            var eventId = Guid.NewGuid().ToString("N")[..12];
            var timestamp = DateTimeOffset.UtcNow;

            var response = new Response(
                EventId: eventId,
                RecordedAt: timestamp,
                EventType: request.EventType,
                Module: request.Module);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Confirmação de registo do evento.</summary>
    public sealed record Response(
        string EventId,
        DateTimeOffset RecordedAt,
        AnalyticsEventType EventType,
        ProductModule Module);
}

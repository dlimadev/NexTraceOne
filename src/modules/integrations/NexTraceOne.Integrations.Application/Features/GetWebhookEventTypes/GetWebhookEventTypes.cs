using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Integrations.Application.Features.GetWebhookEventTypes;

/// <summary>
/// Feature: GetWebhookEventTypes — retorna todos os tipos de eventos disponíveis para subscrições de webhook.
/// Dados estáticos que descrevem cada evento possível com categoria e descrição.
/// Handler nativo do módulo Integrations.
/// Ownership: módulo Integrations.
/// </summary>
public static class GetWebhookEventTypes
{
    /// <summary>Query para obter os tipos de eventos disponíveis para webhook.</summary>
    public sealed record Query : IQuery<Response>;

    /// <summary>Handler que retorna a lista estática de tipos de eventos para webhook outbound.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private static readonly IReadOnlyList<EventTypeDto> _eventTypes = new[]
        {
            new EventTypeDto("incident.created",    "Incidents",  "Triggered when a new incident is created in the platform."),
            new EventTypeDto("incident.resolved",   "Incidents",  "Triggered when an incident is marked as resolved."),
            new EventTypeDto("change.deployed",     "Changes",    "Triggered when a change is deployed to any environment."),
            new EventTypeDto("change.promoted",     "Changes",    "Triggered when a change is promoted to a higher environment."),
            new EventTypeDto("contract.published",  "Contracts",  "Triggered when an API or event contract is published."),
            new EventTypeDto("contract.deprecated", "Contracts",  "Triggered when a contract version is deprecated."),
            new EventTypeDto("service.registered",  "Services",   "Triggered when a new service is registered in the catalog."),
            new EventTypeDto("alert.triggered",     "Alerts",     "Triggered when a monitoring or reliability alert fires."),
        };

        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<Response>.Success(new Response(_eventTypes)));
        }
    }

    /// <summary>Resposta da query GetWebhookEventTypes.</summary>
    public sealed record Response(IReadOnlyList<EventTypeDto> EventTypes);

    /// <summary>DTO representando um tipo de evento de webhook disponível.</summary>
    public sealed record EventTypeDto(string Code, string Category, string Description);
}

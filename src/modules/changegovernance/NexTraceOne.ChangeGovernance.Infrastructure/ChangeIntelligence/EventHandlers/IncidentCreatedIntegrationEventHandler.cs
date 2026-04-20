using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.EventHandlers;

/// <summary>
/// Consome incidentes operacionais e adiciona marcadores no contexto de mudanças
/// para suportar investigação e evidência de correlação incident↔change.
/// </summary>
internal sealed class IncidentCreatedIntegrationEventHandler(
    IReleaseRepository releaseRepository,
    IChangeEventRepository changeEventRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<IncidentCreatedIntegrationEventHandler> logger) : IIntegrationEventHandler<IncidentCreatedIntegrationEvent>
{
    public async Task HandleAsync(IncidentCreatedIntegrationEvent @event, CancellationToken ct = default)
    {
        var releases = await releaseRepository.ListByServiceNameAsync(
            @event.ServiceName,
            page: 1,
            pageSize: 1,
            cancellationToken: ct);

        var latestRelease = releases.Count > 0 ? releases[0] : null;
        if (latestRelease is null)
        {
            logger.LogDebug(
                "No release found for service {ServiceName}; skipping incident marker for incident {IncidentId}",
                @event.ServiceName,
                @event.IncidentId);
            return;
        }

        var markerEvent = ChangeEvent.Create(
            latestRelease.Id,
            eventType: "incident_created",
            description: $"Incident {@event.IncidentId} created for {@event.ServiceName} with severity {@event.IncidentSeverity}.",
            source: "OperationalIntelligence",
            occurredAt: clock.UtcNow);

        changeEventRepository.Add(markerEvent);
        await unitOfWork.CommitAsync(ct);

        logger.LogInformation(
            "Change marker created for incident {IncidentId} on release {ReleaseId}",
            @event.IncidentId,
            latestRelease.Id.Value);
    }
}

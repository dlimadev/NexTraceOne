using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.Integrations.Contracts;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.EventHandlers;

/// <summary>
/// Consome o evento de processamento de payload de ingestão externa e regista
/// um marcador na timeline da release correspondente.
///
/// Permite que o módulo Change Intelligence enriqueça releases com sinais
/// provenientes de pipelines CI/CD que passaram pelo módulo Integrations.
/// </summary>
internal sealed class IngestionPayloadProcessedIntegrationEventHandler(
    IReleaseRepository releaseRepository,
    IChangeEventRepository changeEventRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<IngestionPayloadProcessedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<IntegrationEvents.IngestionPayloadProcessedIntegrationEvent>
{
    public async Task HandleAsync(
        IntegrationEvents.IngestionPayloadProcessedIntegrationEvent @event,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(@event.ServiceName) || string.IsNullOrWhiteSpace(@event.Environment))
        {
            logger.LogDebug(
                "IngestionPayloadProcessed event {ExecutionId} has no service/environment context; skipping.",
                @event.ExecutionId);
            return;
        }

        var release = @event.Version is not null
            ? await releaseRepository.GetByServiceNameVersionEnvironmentAsync(
                @event.ServiceName, @event.Version, @event.Environment, ct)
            : null;

        if (release is null)
        {
            logger.LogDebug(
                "No release found for service {ServiceName} v{Version} in {Environment}; skipping ingestion marker.",
                @event.ServiceName, @event.Version, @event.Environment);
            return;
        }

        var changeType = @event.ChangeType ?? "ingestion";
        var description = string.IsNullOrWhiteSpace(@event.CommitSha)
            ? $"Ingestion payload processed for {changeType} in {release.Environment}."
            : $"Ingestion payload processed for {changeType} in {release.Environment} (commit {@event.CommitSha}).";

        var marker = ChangeEvent.Create(
            release.Id,
            eventType: "ingestion_payload_processed",
            description: description,
            source: "Integrations",
            occurredAt: @event.ProcessedAt);

        changeEventRepository.Add(marker);
        await unitOfWork.CommitAsync(ct);

        logger.LogInformation(
            "Change marker created for ingestion payload on release {ReleaseId} (execution {ExecutionId})",
            release.Id.Value,
            @event.ExecutionId);
    }
}

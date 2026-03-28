using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Events;

namespace NexTraceOne.Catalog.Infrastructure.Graph.EventHandlers;

/// <summary>
/// Consome publicação de release para enriquecer o Source of Truth do catálogo com changelog.
/// </summary>
internal sealed class ReleasePublishedEventHandler(
    ILinkedReferenceRepository linkedReferenceRepository,
    IUnitOfWork unitOfWork,
    ILogger<ReleasePublishedEventHandler> logger) : IIntegrationEventHandler<ReleasePublishedEvent>
{
    public async Task HandleAsync(ReleasePublishedEvent @event, CancellationToken ct = default)
    {
        var reference = LinkedReference.Create(
            assetId: @event.ReleaseId,
            assetType: LinkedAssetType.Service,
            referenceType: LinkedReferenceType.Changelog,
            title: $"Release {@event.Version} published",
            description: $"Release published in environment {@event.Environment}.",
            metadata: $$"""
                        {"source":"ChangeGovernance","releaseId":"{{@event.ReleaseId}}","publishedAt":"{{@event.PublishedAt:O}}"}
                        """);

        linkedReferenceRepository.Add(reference);
        await unitOfWork.CommitAsync(ct);

        logger.LogInformation(
            "Catalog changelog linked reference created for release {ReleaseId}",
            @event.ReleaseId);
    }
}

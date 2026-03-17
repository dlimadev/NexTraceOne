using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.ChangeGovernance.Domain.Promotion.Events;

/// <summary>
/// Evento emitido quando uma promoção de release entre ambientes é registrada.
/// Consumidores típicos: Audit, Workflow, OperationalIntelligence.
/// </summary>
public sealed record PromotionRegisteredEvent(
    Guid PromotionId,
    Guid ReleaseId,
    string FromEnvironment,
    string ToEnvironment,
    DateTimeOffset RegisteredAt) : IntegrationEventBase("ChangeGovernance");

using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para AnalyticsEvent.
/// </summary>
public sealed record AnalyticsEventId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Evento mínimo de Product Analytics.
/// Base para resumo e adoção por módulo.
/// </summary>
public sealed class AnalyticsEvent : Entity<AnalyticsEventId>
{
    private AnalyticsEvent() { }

    public Guid TenantId { get; private init; }
    public string? UserId { get; private init; }
    public string? Persona { get; private init; }

    public ProductModule Module { get; private init; }
    public AnalyticsEventType EventType { get; private init; }

    public string? Feature { get; private init; }
    public string? EntityType { get; private init; }
    public string? Outcome { get; private init; }

    public string Route { get; private init; } = string.Empty;

    public string? TeamId { get; private init; }
    public string? DomainId { get; private init; }

    public string? SessionId { get; private init; }
    public string? ClientType { get; private init; }

    /// <summary>Metadados adicionais serializados como JSON.</summary>
    public string? MetadataJson { get; private init; }

    public DateTimeOffset OccurredAt { get; private init; }

    public static AnalyticsEvent Create(
        Guid tenantId,
        string? userId,
        string? persona,
        ProductModule module,
        AnalyticsEventType eventType,
        string? feature,
        string? entityType,
        string? outcome,
        string route,
        string? teamId,
        string? domainId,
        string? sessionId,
        string? clientType,
        string? metadataJson,
        DateTimeOffset occurredAt)
    {
        Guard.Against.Default(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(route, nameof(route));
        Guard.Against.StringTooLong(route, 500, nameof(route));

        if (feature is not null)
            Guard.Against.StringTooLong(feature, 200, nameof(feature));

        if (entityType is not null)
            Guard.Against.StringTooLong(entityType, 100, nameof(entityType));

        if (outcome is not null)
            Guard.Against.StringTooLong(outcome, 200, nameof(outcome));

        if (persona is not null)
            Guard.Against.StringTooLong(persona, 50, nameof(persona));

        if (teamId is not null)
            Guard.Against.StringTooLong(teamId, 200, nameof(teamId));

        if (domainId is not null)
            Guard.Against.StringTooLong(domainId, 200, nameof(domainId));

        if (sessionId is not null)
            Guard.Against.StringTooLong(sessionId, 200, nameof(sessionId));

        if (clientType is not null)
            Guard.Against.StringTooLong(clientType, 50, nameof(clientType));

        if (metadataJson is not null)
            Guard.Against.StringTooLong(metadataJson, 8000, nameof(metadataJson));

        if (userId is not null)
            Guard.Against.StringTooLong(userId, 200, nameof(userId));

        return new AnalyticsEvent
        {
            Id = new AnalyticsEventId(Guid.NewGuid()),
            TenantId = tenantId,
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
            Persona = string.IsNullOrWhiteSpace(persona) ? null : persona,
            Module = module,
            EventType = eventType,
            Feature = string.IsNullOrWhiteSpace(feature) ? null : feature,
            EntityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType,
            Outcome = string.IsNullOrWhiteSpace(outcome) ? null : outcome,
            Route = route.Trim(),
            TeamId = string.IsNullOrWhiteSpace(teamId) ? null : teamId,
            DomainId = string.IsNullOrWhiteSpace(domainId) ? null : domainId,
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId,
            ClientType = string.IsNullOrWhiteSpace(clientType) ? null : clientType,
            MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson,
            OccurredAt = occurredAt
        };
    }
}

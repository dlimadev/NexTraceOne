using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Audit.Domain.Entities;

/// <summary>
/// Aggregate Root que representa um evento de auditoria na trilha da plataforma.
/// Imutável após criação para garantir integridade da cadeia.
/// </summary>
public sealed class AuditEvent : AggregateRoot<AuditEventId>
{
    private AuditEvent() { }

    /// <summary>Módulo que originou o evento.</summary>
    public string SourceModule { get; private set; } = string.Empty;

    /// <summary>Tipo da ação registrada.</summary>
    public string ActionType { get; private set; } = string.Empty;

    /// <summary>Identificador do recurso afetado.</summary>
    public string ResourceId { get; private set; } = string.Empty;

    /// <summary>Tipo do recurso afetado.</summary>
    public string ResourceType { get; private set; } = string.Empty;

    /// <summary>Usuário responsável pela ação.</summary>
    public string PerformedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC do evento.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Tenant onde a ação ocorreu.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Payload serializado com detalhes adicionais do evento.</summary>
    public string? Payload { get; private set; }

    /// <summary>Link da cadeia de hash associado a este evento.</summary>
    public AuditChainLink? ChainLink { get; private set; }

    /// <summary>Cria um novo evento de auditoria.</summary>
    public static AuditEvent Record(
        string sourceModule,
        string actionType,
        string resourceId,
        string resourceType,
        string performedBy,
        DateTimeOffset occurredAt,
        Guid tenantId,
        string? payload = null)
    {
        return new AuditEvent
        {
            Id = AuditEventId.New(),
            SourceModule = Guard.Against.NullOrWhiteSpace(sourceModule),
            ActionType = Guard.Against.NullOrWhiteSpace(actionType),
            ResourceId = Guard.Against.NullOrWhiteSpace(resourceId),
            ResourceType = Guard.Against.NullOrWhiteSpace(resourceType),
            PerformedBy = Guard.Against.NullOrWhiteSpace(performedBy),
            OccurredAt = occurredAt,
            TenantId = tenantId,
            Payload = payload
        };
    }

    /// <summary>Vincula o evento à cadeia de hash para garantir integridade criptográfica.</summary>
    public void LinkToChain(AuditChainLink chainLink)
    {
        ChainLink = Guard.Against.Null(chainLink);
    }
}

/// <summary>Identificador fortemente tipado de AuditEvent.</summary>
public sealed record AuditEventId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AuditEventId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AuditEventId From(Guid id) => new(id);
}

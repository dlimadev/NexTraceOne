using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Entidade que registra um evento ocorrido durante o ciclo de vida de uma release,
/// como início de deployment, alteração de contrato ou mudança de status.
/// </summary>
public sealed class ChangeEvent : AuditableEntity<ChangeEventId>
{
    private ChangeEvent() { }

    /// <summary>Identificador da release à qual este evento pertence.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Tipo do evento (ex: "deployment_started", "contract_changed").</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>Descrição legível do evento ocorrido.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Momento em que o evento ocorreu.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Origem do evento, como nome ou URL do pipeline de CI/CD.</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>
    /// Cria um novo evento de mudança associado a uma release.
    /// </summary>
    public static ChangeEvent Create(
        ReleaseId releaseId,
        string eventType,
        string description,
        string source,
        DateTimeOffset occurredAt)
    {
        Guard.Against.Null(releaseId);
        Guard.Against.NullOrWhiteSpace(eventType);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NullOrWhiteSpace(source);

        return new ChangeEvent
        {
            Id = ChangeEventId.New(),
            ReleaseId = releaseId,
            EventType = eventType,
            Description = description,
            Source = source,
            OccurredAt = occurredAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ChangeEvent.</summary>
public sealed record ChangeEventId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ChangeEventId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ChangeEventId From(Guid id) => new(id);
}

using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Entidade imutável (append-only) que registra um evento na timeline de confiança
/// de uma mudança. Cada evento captura o score antes e depois, permitindo
/// visualizar a evolução da confiança ao longo do ciclo de vida da release.
/// </summary>
public sealed class ChangeConfidenceEvent : Entity<ChangeConfidenceEventId>
{
    private ChangeConfidenceEvent() { }

    /// <summary>Identificador da release à qual este evento pertence.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Tipo do evento que alterou a confiança.</summary>
    public ConfidenceEventType EventType { get; private set; }

    /// <summary>Score de confiança antes do evento (0–100).</summary>
    public int ConfidenceBefore { get; private set; }

    /// <summary>Score de confiança depois do evento (0–100).</summary>
    public int ConfidenceAfter { get; private set; }

    /// <summary>Razão legível que justifica a alteração no score.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Detalhes adicionais em formato livre (JSONB no banco).</summary>
    public string? Details { get; private set; }

    /// <summary>Momento em que o evento ocorreu.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Identificador do utilizador ou sistema que originou o evento.</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>
    /// Cria um novo evento de confiança para uma release.
    /// </summary>
    public static ChangeConfidenceEvent Create(
        ReleaseId releaseId,
        ConfidenceEventType eventType,
        int confidenceBefore,
        int confidenceAfter,
        string reason,
        string? details,
        string source,
        DateTimeOffset occurredAt)
    {
        Guard.Against.Null(releaseId);
        Guard.Against.OutOfRange(confidenceBefore, nameof(confidenceBefore), 0, 100);
        Guard.Against.OutOfRange(confidenceAfter, nameof(confidenceAfter), 0, 100);
        Guard.Against.NullOrWhiteSpace(reason);
        Guard.Against.StringTooLong(reason, 2000);
        Guard.Against.NullOrWhiteSpace(source);
        Guard.Against.StringTooLong(source, 500);

        if (details is not null)
            Guard.Against.StringTooLong(details, 8000);

        return new ChangeConfidenceEvent
        {
            Id = ChangeConfidenceEventId.New(),
            ReleaseId = releaseId,
            EventType = eventType,
            ConfidenceBefore = confidenceBefore,
            ConfidenceAfter = confidenceAfter,
            Reason = reason,
            Details = details,
            Source = source,
            OccurredAt = occurredAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ChangeConfidenceEvent.</summary>
public sealed record ChangeConfidenceEventId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ChangeConfidenceEventId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ChangeConfidenceEventId From(Guid id) => new(id);
}

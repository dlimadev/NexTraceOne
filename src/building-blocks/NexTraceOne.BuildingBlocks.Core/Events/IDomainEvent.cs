using MediatR;

namespace NexTraceOne.BuildingBlocks.Core;

/// <summary>
/// Marcador para todos os Domain Events da plataforma.
/// Domain Events representam algo relevante que aconteceu dentro do domínio.
/// São emitidos pelo Aggregate Root e processados de forma assíncrona
/// pelo pipeline do Outbox após o commit da transação.
/// REGRA: Domain Events são intra-módulo. Para comunicação entre módulos,
/// use Integration Events.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>Identificador único deste evento (para idempotência e rastreio).</summary>
    Guid EventId { get; }

    /// <summary>Data/hora UTC em que o evento ocorreu.</summary>
    DateTimeOffset OccurredAt { get; }
}

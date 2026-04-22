using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Repositório para registos de dead letter do event consumer worker.
/// Permite adicionar, listar e resolver registos de eventos que falharam o processamento.
/// </summary>
public interface IEventConsumerDeadLetterRepository
{
    /// <summary>Adiciona um novo registo de dead letter.</summary>
    Task AddAsync(EventConsumerDeadLetterRecord record, CancellationToken ct);

    /// <summary>
    /// Lista registos não resolvidos, opcionalmente filtrados por tenant.
    /// </summary>
    Task<IReadOnlyList<EventConsumerDeadLetterRecord>> ListUnresolvedAsync(Guid? tenantId, CancellationToken ct);

    /// <summary>Marca um registo como resolvido.</summary>
    Task ResolveAsync(EventConsumerDeadLetterRecordId id, CancellationToken ct);
}

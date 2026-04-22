using System.Collections.Concurrent;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Services;

/// <summary>
/// Implementação nula do repositório de dead letters do event consumer.
/// Armazena registos em memória via ConcurrentDictionary enquanto nenhuma
/// infraestrutura de persistência real estiver configurada.
/// </summary>
public sealed class NullEventConsumerDeadLetterRepository : IEventConsumerDeadLetterRepository
{
    private readonly ConcurrentDictionary<Guid, EventConsumerDeadLetterRecord> _store = new();

    /// <inheritdoc />
    public Task AddAsync(EventConsumerDeadLetterRecord record, CancellationToken ct)
    {
        _store.TryAdd(record.Id.Value, record);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EventConsumerDeadLetterRecord>> ListUnresolvedAsync(Guid? tenantId, CancellationToken ct)
    {
        IReadOnlyList<EventConsumerDeadLetterRecord> result = _store.Values
            .Where(r => !r.IsResolved && (tenantId == null || r.TenantId == tenantId))
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task ResolveAsync(EventConsumerDeadLetterRecordId id, CancellationToken ct)
    {
        if (_store.TryGetValue(id.Value, out var record))
            record.MarkResolved();

        return Task.CompletedTask;
    }
}

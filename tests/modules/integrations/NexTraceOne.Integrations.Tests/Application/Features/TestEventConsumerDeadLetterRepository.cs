using System.Linq;

using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Implementação em memória do repositório de dead letters.
/// Usada exclusivamente em testes unitários.
/// </summary>
public sealed class TestEventConsumerDeadLetterRepository : IEventConsumerDeadLetterRepository
{
    private readonly List<EventConsumerDeadLetterRecord> _records = [];

    public Task AddAsync(EventConsumerDeadLetterRecord record, CancellationToken ct)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EventConsumerDeadLetterRecord>> ListUnresolvedAsync(Guid? tenantId, CancellationToken ct)
    {
        IReadOnlyList<EventConsumerDeadLetterRecord> result = _records.Where(r => !r.IsResolved).ToList();
        return Task.FromResult(result);
    }

    public Task ResolveAsync(EventConsumerDeadLetterRecordId id, CancellationToken ct)
    {
        var record = _records.FirstOrDefault(r => r.Id == id);
        record?.MarkResolved();
        return Task.CompletedTask;
    }
}

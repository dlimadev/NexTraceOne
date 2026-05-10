using Microsoft.EntityFrameworkCore;

using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de dead letters do event consumer.
/// Substitui o NullEventConsumerDeadLetterRepository que armazenava em memória (perdido ao reiniciar).
/// </summary>
internal sealed class EfEventConsumerDeadLetterRepository(IntegrationsDbContext context)
    : IEventConsumerDeadLetterRepository
{
    public async Task AddAsync(EventConsumerDeadLetterRecord record, CancellationToken ct)
        => await context.EventConsumerDeadLetters.AddAsync(record, ct);

    public async Task<IReadOnlyList<EventConsumerDeadLetterRecord>> ListUnresolvedAsync(
        Guid? tenantId, CancellationToken ct)
        => await context.EventConsumerDeadLetters
            .Where(r => !r.IsResolved && (tenantId == null || r.TenantId == tenantId))
            .OrderByDescending(r => r.LastAttemptAt)
            .ToListAsync(ct);

    public async Task ResolveAsync(EventConsumerDeadLetterRecordId id, CancellationToken ct)
    {
        var record = await context.EventConsumerDeadLetters
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (record is not null)
            record.MarkResolved();
    }
}

using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Outbox;

internal sealed class DeadLetterRepository(BuildingBlocksDbContext dbContext) : IDeadLetterRepository
{
    public async Task SaveAsync(DeadLetterMessage message, CancellationToken ct = default)
    {
        dbContext.DeadLetterMessages.Add(message);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<DeadLetterPage> ListAsync(
        Guid? tenantId,
        DlqMessageStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = dbContext.DeadLetterMessages.AsNoTracking();

        if (tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.ExhaustedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new DeadLetterPage(items, total, page, pageSize);
    }

    public Task<DeadLetterMessage?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => dbContext.DeadLetterMessages.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task UpdateAsync(DeadLetterMessage message, CancellationToken ct = default)
    {
        dbContext.DeadLetterMessages.Update(message);
        await dbContext.SaveChangesAsync(ct);
    }
}

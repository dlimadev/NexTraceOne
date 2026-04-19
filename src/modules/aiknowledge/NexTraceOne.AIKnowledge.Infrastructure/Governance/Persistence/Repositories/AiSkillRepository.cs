using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiSkillRepository(AiGovernanceDbContext context) : IAiSkillRepository
{
    public async Task<AiSkill?> GetByIdAsync(AiSkillId id, CancellationToken ct)
        => await context.Skills.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<AiSkill?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct)
        => await context.Skills.SingleOrDefaultAsync(
            s => s.Name == name && (s.TenantId == tenantId || s.OwnershipType == SkillOwnershipType.System), ct);

    public async Task<IReadOnlyList<AiSkill>> ListAsync(
        SkillStatus? status,
        SkillOwnershipType? ownershipType,
        Guid? tenantId,
        CancellationToken ct)
    {
        var query = context.Skills.AsQueryable();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (ownershipType.HasValue)
            query = query.Where(s => s.OwnershipType == ownershipType.Value);

        if (tenantId.HasValue)
            query = query.Where(s => s.TenantId == tenantId.Value || s.OwnershipType == SkillOwnershipType.System);

        return await query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid tenantId, CancellationToken ct)
        => await context.Skills.AnyAsync(
            s => s.Name == name && (s.TenantId == tenantId || s.OwnershipType == SkillOwnershipType.System), ct);

    public void Add(AiSkill skill)
        => context.Skills.Add(skill);

    public async Task<int> CountBySkillIdAsync(AiSkillId id, CancellationToken ct)
        => await context.SkillExecutions.CountAsync(e => e.SkillId == id, ct);
}

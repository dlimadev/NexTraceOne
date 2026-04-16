using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiGuardrailRepository(AiGovernanceDbContext context) : IAiGuardrailRepository
{
    public async Task<AiGuardrail?> GetByIdAsync(AiGuardrailId id, CancellationToken ct)
        => await context.Guardrails.SingleOrDefaultAsync(g => g.Id == id, ct);

    public async Task<AiGuardrail?> GetByNameAsync(string name, CancellationToken ct)
        => await context.Guardrails.SingleOrDefaultAsync(g => g.Name == name, ct);

    public async Task<IReadOnlyList<AiGuardrail>> GetAllActiveAsync(CancellationToken ct)
        => await context.Guardrails.Where(g => g.IsActive).OrderBy(g => g.Priority).ToListAsync(ct);

    public async Task<IReadOnlyList<AiGuardrail>> GetByCategoryAsync(string category, CancellationToken ct)
    {
        if (!Enum.TryParse<GuardrailCategory>(category, ignoreCase: true, out var categoryEnum))
            return Array.Empty<AiGuardrail>();

        return await context.Guardrails.Where(g => g.Category == categoryEnum).OrderBy(g => g.Priority).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AiGuardrail>> GetByGuardTypeAsync(string guardType, CancellationToken ct)
    {
        if (!Enum.TryParse<GuardrailType>(guardType, ignoreCase: true, out var guardTypeEnum))
            return Array.Empty<AiGuardrail>();

        return await context.Guardrails.Where(g => g.GuardType == guardTypeEnum).OrderBy(g => g.Priority).ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => await context.Guardrails.AnyAsync(g => g.Name == name, ct);

    public async Task AddAsync(AiGuardrail entity, CancellationToken ct)
    {
        await context.Guardrails.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }
}

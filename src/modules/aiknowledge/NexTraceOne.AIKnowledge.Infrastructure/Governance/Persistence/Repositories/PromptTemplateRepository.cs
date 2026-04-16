using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class PromptTemplateRepository(AiGovernanceDbContext context) : IPromptTemplateRepository
{
    public async Task<PromptTemplate?> GetByIdAsync(PromptTemplateId id, CancellationToken ct)
        => await context.PromptTemplates.SingleOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PromptTemplate?> GetActiveByNameAsync(string name, CancellationToken ct)
        => await context.PromptTemplates.Where(t => t.Name == name && t.IsActive).OrderByDescending(t => t.Version).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<PromptTemplate>> GetAllActiveAsync(CancellationToken ct)
        => await context.PromptTemplates.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<PromptTemplate>> GetByCategoryAsync(string category, CancellationToken ct)
        => await context.PromptTemplates.Where(t => t.Category == category).OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<PromptTemplate>> GetByPersonaAsync(string persona, CancellationToken ct)
        => await context.PromptTemplates
            .Where(t => t.TargetPersonas.Contains(persona))
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => await context.PromptTemplates.AnyAsync(t => t.Name == name, ct);

    public async Task AddAsync(PromptTemplate entity, CancellationToken ct)
    {
        await context.PromptTemplates.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class SavedPromptRepository(ConfigurationDbContext context) : ISavedPromptRepository
{
    public async Task<SavedPrompt?> GetByIdAsync(SavedPromptId id, CancellationToken cancellationToken)
        => await context.SavedPrompts.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SavedPrompt>> ListByUserAsync(string userId, string tenantId, CancellationToken cancellationToken)
        => await context.SavedPrompts
            .Where(p => p.UserId == userId && p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(SavedPrompt prompt, CancellationToken cancellationToken)
        => await context.SavedPrompts.AddAsync(prompt, cancellationToken);

    public Task UpdateAsync(SavedPrompt prompt, CancellationToken cancellationToken)
    {
        context.SavedPrompts.Update(prompt);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(SavedPromptId id, CancellationToken cancellationToken)
    {
        var entity = await context.SavedPrompts.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is not null) context.SavedPrompts.Remove(entity);
    }
}

using System.Collections.Concurrent;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>
/// Implementação em memória do repositório de prompts guardados.
/// Para uso no MVP1 enquanto a persistência PostgreSQL não for adicionada.
/// </summary>
public sealed class InMemorySavedPromptRepository : ISavedPromptRepository
{
    private readonly ConcurrentDictionary<Guid, SavedPrompt> _store = new();

    public Task<SavedPrompt?> GetByIdAsync(SavedPromptId id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var prompt);
        return Task.FromResult(prompt);
    }

    public Task<IReadOnlyList<SavedPrompt>> ListByUserAsync(string userId, string tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<SavedPrompt> result = _store.Values
            .Where(p => p.UserId == userId && p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(SavedPrompt prompt, CancellationToken cancellationToken)
    {
        _store[prompt.Id.Value] = prompt;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SavedPrompt prompt, CancellationToken cancellationToken)
    {
        _store[prompt.Id.Value] = prompt;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(SavedPromptId id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}

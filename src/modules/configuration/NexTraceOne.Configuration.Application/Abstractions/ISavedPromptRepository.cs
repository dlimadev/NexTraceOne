using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de prompts guardados pelo utilizador.</summary>
public interface ISavedPromptRepository
{
    Task<SavedPrompt?> GetByIdAsync(SavedPromptId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<SavedPrompt>> ListByUserAsync(string userId, string tenantId, CancellationToken cancellationToken);
    Task AddAsync(SavedPrompt prompt, CancellationToken cancellationToken);
    Task UpdateAsync(SavedPrompt prompt, CancellationToken cancellationToken);
    Task DeleteAsync(SavedPromptId id, CancellationToken cancellationToken);
}

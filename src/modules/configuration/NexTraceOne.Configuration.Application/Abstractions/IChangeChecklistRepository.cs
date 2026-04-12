using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de checklists de mudanças.</summary>
public interface IChangeChecklistRepository
{
    Task<ChangeChecklist?> GetByIdAsync(ChangeChecklistId id, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChangeChecklist>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChangeChecklist>> GetForChangeAsync(string tenantId, string changeType, string? environment, CancellationToken cancellationToken);
    Task AddAsync(ChangeChecklist checklist, CancellationToken cancellationToken);
    Task UpdateAsync(ChangeChecklist checklist, CancellationToken cancellationToken);
    Task DeleteAsync(ChangeChecklistId id, CancellationToken cancellationToken);
}

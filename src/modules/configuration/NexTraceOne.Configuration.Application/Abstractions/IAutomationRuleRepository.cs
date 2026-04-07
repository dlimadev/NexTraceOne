using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de regras de automação.</summary>
public interface IAutomationRuleRepository
{
    Task<AutomationRule?> GetByIdAsync(AutomationRuleId id, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AutomationRule>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AutomationRule>> GetByTriggerAsync(string tenantId, string trigger, CancellationToken cancellationToken);
    Task AddAsync(AutomationRule rule, CancellationToken cancellationToken);
    Task UpdateAsync(AutomationRule rule, CancellationToken cancellationToken);
    Task DeleteAsync(AutomationRuleId id, CancellationToken cancellationToken);
}

using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de regras de alerta personalizadas.</summary>
public interface IUserAlertRuleRepository
{
    Task<UserAlertRule?> GetByIdAsync(UserAlertRuleId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserAlertRule>> ListByUserAsync(string userId, string tenantId, CancellationToken cancellationToken);
    Task AddAsync(UserAlertRule rule, CancellationToken cancellationToken);
    Task UpdateAsync(UserAlertRule rule, CancellationToken cancellationToken);
    Task DeleteAsync(UserAlertRule rule, CancellationToken cancellationToken);
}

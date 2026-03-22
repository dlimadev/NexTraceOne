using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;

/// <summary>
/// Repositório para workflows de automação persistidos.
/// </summary>
public interface IAutomationWorkflowRepository
{
    Task<AutomationWorkflowRecord?> GetByIdAsync(AutomationWorkflowRecordId id, CancellationToken ct);
    Task<IReadOnlyList<AutomationWorkflowRecord>> ListAsync(string? serviceId, AutomationWorkflowStatus? status, int page, int pageSize, CancellationToken ct);
    Task<int> CountAsync(string? serviceId, AutomationWorkflowStatus? status, CancellationToken ct);
    Task AddAsync(AutomationWorkflowRecord workflow, CancellationToken ct);
    Task UpdateAsync(AutomationWorkflowRecord workflow, CancellationToken ct);
}

/// <summary>
/// Repositório para registos de validação de automação.
/// </summary>
public interface IAutomationValidationRepository
{
    Task<AutomationValidationRecord?> GetByWorkflowIdAsync(AutomationWorkflowRecordId workflowId, CancellationToken ct);
    Task AddAsync(AutomationValidationRecord validation, CancellationToken ct);
}

/// <summary>
/// Repositório para registos de auditoria de automação.
/// </summary>
public interface IAutomationAuditRepository
{
    Task<IReadOnlyList<AutomationAuditRecord>> GetByWorkflowIdAsync(AutomationWorkflowRecordId workflowId, CancellationToken ct);
    Task<IReadOnlyList<AutomationAuditRecord>> GetByServiceIdAsync(string serviceId, CancellationToken ct);
    Task<IReadOnlyList<AutomationAuditRecord>> GetByTeamIdAsync(string teamId, CancellationToken ct);
    Task AddAsync(AutomationAuditRecord auditRecord, CancellationToken ct);
}

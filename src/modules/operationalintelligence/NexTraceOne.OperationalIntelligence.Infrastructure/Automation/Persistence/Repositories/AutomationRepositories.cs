using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de workflows de automação.
/// </summary>
internal sealed class AutomationWorkflowRepository(AutomationDbContext context) : IAutomationWorkflowRepository
{
    public async Task<AutomationWorkflowRecord?> GetByIdAsync(AutomationWorkflowRecordId id, CancellationToken ct)
        => await context.Workflows.SingleOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IReadOnlyList<AutomationWorkflowRecord>> ListAsync(
        string? serviceId, AutomationWorkflowStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = context.Workflows.AsQueryable();

        if (!string.IsNullOrEmpty(serviceId))
            query = query.Where(w => w.ServiceId == serviceId);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(string? serviceId, AutomationWorkflowStatus? status, CancellationToken ct)
    {
        var query = context.Workflows.AsQueryable();

        if (!string.IsNullOrEmpty(serviceId))
            query = query.Where(w => w.ServiceId == serviceId);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        return await query.CountAsync(ct);
    }

    public async Task AddAsync(AutomationWorkflowRecord workflow, CancellationToken ct)
        => await context.Workflows.AddAsync(workflow, ct);

    public Task UpdateAsync(AutomationWorkflowRecord workflow, CancellationToken ct)
    {
        context.Workflows.Update(workflow);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Implementação EF Core do repositório de validações de automação.
/// </summary>
internal sealed class AutomationValidationRepository(AutomationDbContext context) : IAutomationValidationRepository
{
    public async Task<AutomationValidationRecord?> GetByWorkflowIdAsync(
        AutomationWorkflowRecordId workflowId, CancellationToken ct)
        => await context.Validations.SingleOrDefaultAsync(v => v.WorkflowId == workflowId, ct);

    public async Task AddAsync(AutomationValidationRecord validation, CancellationToken ct)
        => await context.Validations.AddAsync(validation, ct);
}

/// <summary>
/// Implementação EF Core do repositório de registos de auditoria de automação.
/// </summary>
internal sealed class AutomationAuditRepository(AutomationDbContext context) : IAutomationAuditRepository
{
    public async Task<IReadOnlyList<AutomationAuditRecord>> GetByWorkflowIdAsync(
        AutomationWorkflowRecordId workflowId, CancellationToken ct)
        => await context.AuditRecords
            .Where(a => a.WorkflowId == workflowId)
            .OrderBy(a => a.OccurredAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AutomationAuditRecord>> GetByServiceIdAsync(string serviceId, CancellationToken ct)
        => await context.AuditRecords
            .Where(a => a.ServiceId == serviceId)
            .OrderByDescending(a => a.OccurredAt)
            .Take(100)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AutomationAuditRecord>> GetByTeamIdAsync(string teamId, CancellationToken ct)
        => await context.AuditRecords
            .Where(a => a.TeamId == teamId)
            .OrderByDescending(a => a.OccurredAt)
            .Take(100)
            .ToListAsync(ct);

    public async Task AddAsync(AutomationAuditRecord auditRecord, CancellationToken ct)
        => await context.AuditRecords.AddAsync(auditRecord, ct);
}

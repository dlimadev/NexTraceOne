using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Contracts.Automation.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Services;

/// <summary>
/// Implementação do contrato cross-module <see cref="IAutomationModule"/>.
/// Expõe dados de automação para outros módulos (ChangeGovernance, Governance)
/// sem permitir acesso directo ao DbContext ou repositórios internos.
/// Leituras apenas — sem tracking para melhor performance.
/// </summary>
internal sealed class AutomationModuleService(AutomationDbContext context) : IAutomationModule
{
    /// <summary>
    /// Statuses considered as "terminal" — workflows in these states are no longer active.
    /// </summary>
    private static readonly AutomationWorkflowStatus[] TerminalStatuses =
    [
        AutomationWorkflowStatus.Completed,
        AutomationWorkflowStatus.Failed,
        AutomationWorkflowStatus.Cancelled,
        AutomationWorkflowStatus.Rejected
    ];

    /// <summary>
    /// Statuses considered as "blocking" — workflows that prevent safe change promotion.
    /// </summary>
    private static readonly AutomationWorkflowStatus[] BlockingStatuses =
    [
        AutomationWorkflowStatus.Executing,
        AutomationWorkflowStatus.AwaitingApproval,
        AutomationWorkflowStatus.AwaitingValidation,
        AutomationWorkflowStatus.PendingPreconditions
    ];

    /// <inheritdoc />
    public async Task<string?> GetWorkflowStatusAsync(
        string workflowId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(workflowId, out var guid))
            return null;

        var typedId = new Domain.Automation.Entities.AutomationWorkflowRecordId(guid);

        var workflow = await context.Workflows
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == typedId, cancellationToken);

        return workflow?.Status.ToString();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AutomationWorkflowSummary>> GetActiveWorkflowsAsync(
        string serviceName, CancellationToken cancellationToken = default)
    {
        var workflows = await context.Workflows
            .AsNoTracking()
            .Where(w => w.ServiceId == serviceName && !TerminalStatuses.Contains(w.Status))
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        return workflows
            .Select(w => new AutomationWorkflowSummary(
                WorkflowId: w.Id.Value.ToString(),
                ServiceName: w.ServiceId ?? string.Empty,
                WorkflowStatus: w.Status.ToString(),
                ActionType: w.ActionId,
                CreatedAt: w.CreatedAt))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<bool> HasBlockingWorkflowsAsync(
        string serviceName, string environment, CancellationToken cancellationToken = default)
    {
        return await context.Workflows
            .AsNoTracking()
            .AnyAsync(
                w => w.ServiceId == serviceName
                     && w.TargetEnvironment == environment
                     && BlockingStatuses.Contains(w.Status),
                cancellationToken);
    }
}

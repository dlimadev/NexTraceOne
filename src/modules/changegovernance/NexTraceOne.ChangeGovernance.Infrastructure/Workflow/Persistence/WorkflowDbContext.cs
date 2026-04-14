using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence;

/// <summary>
/// DbContext do módulo Workflow.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class WorkflowDbContext(
    DbContextOptions<WorkflowDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, IWorkflowUnitOfWork
{
    /// <summary>Templates de workflow persistidos no módulo Workflow.</summary>
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();

    /// <summary>Instâncias de workflow persistidas no módulo Workflow.</summary>
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();

    /// <summary>Estágios de workflow persistidos no módulo Workflow.</summary>
    public DbSet<WorkflowStage> WorkflowStages => Set<WorkflowStage>();

    /// <summary>Pacotes de evidência persistidos no módulo Workflow.</summary>
    public DbSet<EvidencePack> EvidencePacks => Set<EvidencePack>();

    /// <summary>Políticas de SLA persistidas no módulo Workflow.</summary>
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();

    /// <summary>Decisões de aprovação persistidas no módulo Workflow.</summary>
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(WorkflowDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "chg_wf_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}

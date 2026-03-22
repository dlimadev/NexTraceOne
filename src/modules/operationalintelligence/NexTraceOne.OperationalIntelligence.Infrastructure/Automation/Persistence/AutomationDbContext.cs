using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence;

/// <summary>
/// DbContext do subdomínio Automation do módulo OperationalIntelligence.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class AutomationDbContext(
    DbContextOptions<AutomationDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IAutomationUnitOfWork
{
    /// <summary>Workflows de automação operacional registados.</summary>
    public DbSet<AutomationWorkflowRecord> Workflows => Set<AutomationWorkflowRecord>();

    /// <summary>Registos de validação pós-execução de workflows.</summary>
    public DbSet<AutomationValidationRecord> Validations => Set<AutomationValidationRecord>();

    /// <summary>Trilha de auditoria de eventos de automação.</summary>
    public DbSet<AutomationAuditRecord> AuditRecords => Set<AutomationAuditRecord>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(AutomationDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence.Configurations";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}

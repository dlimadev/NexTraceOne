using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

/// <summary>
/// DbContext do subdomínio Incidents do módulo OperationalIntelligence.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// Base de dados isolada — cada módulo possui sua própria connection string.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class IncidentDbContext(
    DbContextOptions<IncidentDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Incidentes operacionais registados.</summary>
    public DbSet<IncidentRecord> Incidents => Set<IncidentRecord>();

    /// <summary>Workflows de mitigação associados a incidentes.</summary>
    public DbSet<MitigationWorkflowRecord> MitigationWorkflows => Set<MitigationWorkflowRecord>();

    /// <summary>Logs de ações executadas sobre workflows de mitigação.</summary>
    public DbSet<MitigationWorkflowActionLog> MitigationWorkflowActions => Set<MitigationWorkflowActionLog>();

    /// <summary>Logs de validação pós-mitigação.</summary>
    public DbSet<MitigationValidationLog> MitigationValidations => Set<MitigationValidationLog>();

    /// <summary>Runbooks operacionais.</summary>
    public DbSet<RunbookRecord> Runbooks => Set<RunbookRecord>();

    /// <summary>Correlações dinâmicas incidente↔mudança geradas pelo motor de correlação.</summary>
    public DbSet<IncidentChangeCorrelation> ChangeCorrelations => Set<IncidentChangeCorrelation>();

    /// <summary>Post-Incident Reviews (PIR) formais.</summary>
    public DbSet<PostIncidentReview> PostIncidentReviews => Set<PostIncidentReview>();

    /// <summary>Narrativas de incidentes geradas por IA.</summary>
    public DbSet<IncidentNarrative> IncidentNarratives => Set<IncidentNarrative>();

    /// <summary>Execuções de passos de runbooks operacionais.</summary>
    public DbSet<RunbookStepExecution> RunbookStepExecutions => Set<RunbookStepExecution>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(IncidentDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "ops_inc_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}

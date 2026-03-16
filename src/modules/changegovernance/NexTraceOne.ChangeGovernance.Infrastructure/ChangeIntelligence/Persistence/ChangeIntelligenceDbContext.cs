using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo ChangeIntelligence.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ChangeIntelligenceDbContext(
    DbContextOptions<ChangeIntelligenceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    /// <summary>Releases de serviços/APIs persistidas no módulo ChangeIntelligence.</summary>
    public DbSet<Release> Releases => Set<Release>();

    /// <summary>Relatórios de blast radius persistidos no módulo ChangeIntelligence.</summary>
    public DbSet<BlastRadiusReport> BlastRadiusReports => Set<BlastRadiusReport>();

    /// <summary>Scores de risco de mudança persistidos no módulo ChangeIntelligence.</summary>
    public DbSet<ChangeIntelligenceScore> ChangeScores => Set<ChangeIntelligenceScore>();

    /// <summary>Eventos de mudança persistidos no módulo ChangeIntelligence.</summary>
    public DbSet<ChangeEvent> ChangeEvents => Set<ChangeEvent>();

    /// <summary>Marcadores externos de ferramentas CI/CD persistidos no módulo ChangeIntelligence.</summary>
    public DbSet<ExternalMarker> ExternalMarkers => Set<ExternalMarker>();

    /// <summary>Janelas de freeze persistidas no módulo ChangeIntelligence.</summary>
    public DbSet<FreezeWindow> FreezeWindows => Set<FreezeWindow>();

    /// <summary>Baselines de indicadores pré-release persistidos no módulo ChangeIntelligence.</summary>
    public DbSet<ReleaseBaseline> ReleaseBaselines => Set<ReleaseBaseline>();

    /// <summary>Janelas de observação pós-release persistidas no módulo ChangeIntelligence.</summary>
    public DbSet<ObservationWindow> ObservationWindows => Set<ObservationWindow>();

    /// <summary>Reviews automáticas pós-release persistidas no módulo ChangeIntelligence.</summary>
    public DbSet<PostReleaseReview> PostReleaseReviews => Set<PostReleaseReview>();

    /// <summary>Avaliações de viabilidade de rollback persistidas no módulo ChangeIntelligence.</summary>
    public DbSet<RollbackAssessment> RollbackAssessments => Set<RollbackAssessment>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ChangeIntelligenceDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.ChangeIntelligence.Infrastructure";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}

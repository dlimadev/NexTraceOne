using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;

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
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork, IChangeIntelligenceUnitOfWork
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

    /// <summary>Estados de feature flags de releases persistidos no módulo ChangeIntelligence.</summary>
    public DbSet<ReleaseFeatureFlagState> FeatureFlagStates => Set<ReleaseFeatureFlagState>();

    /// <summary>Registos de canary rollout de releases persistidos no módulo ChangeIntelligence.</summary>
    public DbSet<CanaryRollout> CanaryRollouts => Set<CanaryRollout>();

    /// <summary>Breakdowns detalhados do Change Confidence Score 2.0 persistidos no módulo ChangeIntelligence.</summary>
    public DbSet<ChangeConfidenceBreakdown> ConfidenceBreakdowns => Set<ChangeConfidenceBreakdown>();

    /// <summary>Eventos de confiança de mudanças (append-only) persistidos no módulo ChangeIntelligence.</summary>
    public DbSet<ChangeConfidenceEvent> ChangeConfidenceEvents => Set<ChangeConfidenceEvent>();

    /// <summary>Release notes geradas por IA persistidas no módulo ChangeIntelligence.</summary>
    public DbSet<ReleaseNotes> ReleaseNotes => Set<ReleaseNotes>();

    /// <summary>Gates de promoção configuráveis persistidos no módulo ChangeIntelligence.</summary>
    public DbSet<PromotionGate> PromotionGates => Set<PromotionGate>();

    /// <summary>Avaliações de gates de promoção persistidas no módulo ChangeIntelligence.</summary>
    public DbSet<PromotionGateEvaluation> PromotionGateEvaluations => Set<PromotionGateEvaluation>();

    /// <summary>Commit pool — associações de commits ao ciclo de vida de releases.</summary>
    public DbSet<CommitAssociation> CommitAssociations => Set<CommitAssociation>();

    /// <summary>Associações de work items externos a releases.</summary>
    public DbSet<WorkItemAssociation> WorkItemAssociations => Set<WorkItemAssociation>();

    /// <summary>Pedidos de aprovação de releases — internos e externos (via webhook outbound/callback).</summary>
    public DbSet<ReleaseApprovalRequest> ApprovalRequests => Set<ReleaseApprovalRequest>();

    /// <summary>Políticas de aprovação de releases configuráveis por ambiente e serviço.</summary>
    public DbSet<ReleaseApprovalPolicy> ApprovalPolicies => Set<ReleaseApprovalPolicy>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ChangeIntelligenceDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "chg_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}

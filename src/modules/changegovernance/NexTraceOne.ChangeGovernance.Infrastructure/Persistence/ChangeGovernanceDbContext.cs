using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using CiPromotionGate = NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities.PromotionGate;
using PrmPromotionGate = NexTraceOne.ChangeGovernance.Domain.Promotion.Entities.PromotionGate;
using PrmDeploymentEnvironment = NexTraceOne.ChangeGovernance.Domain.Promotion.Entities.DeploymentEnvironment;
using PrmPromotionRequest = NexTraceOne.ChangeGovernance.Domain.Promotion.Entities.PromotionRequest;
using PrmGateEvaluation = NexTraceOne.ChangeGovernance.Domain.Promotion.Entities.GateEvaluation;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Persistence;

/// <summary>
/// DbContext consolidado do módulo ChangeGovernance.
/// Unifica ChangeIntelligenceDbContext + WorkflowDbContext + PromotionDbContext + RulesetGovernanceDbContext.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ChangeGovernanceDbContext(
    DbContextOptions<ChangeGovernanceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock),
      IUnitOfWork,
      IChangeIntelligenceUnitOfWork,
      IWorkflowUnitOfWork,
      IPromotionUnitOfWork,
      IRulesetGovernanceUnitOfWork
{
    // ── ChangeIntelligence ────────────────────────────────────────────────────
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<BlastRadiusReport> BlastRadiusReports => Set<BlastRadiusReport>();
    public DbSet<ChangeIntelligenceScore> ChangeScores => Set<ChangeIntelligenceScore>();
    public DbSet<ChangeEvent> ChangeEvents => Set<ChangeEvent>();
    public DbSet<ExternalMarker> ExternalMarkers => Set<ExternalMarker>();
    public DbSet<FreezeWindow> FreezeWindows => Set<FreezeWindow>();
    public DbSet<ReleaseBaseline> ReleaseBaselines => Set<ReleaseBaseline>();
    public DbSet<ObservationWindow> ObservationWindows => Set<ObservationWindow>();
    public DbSet<PostReleaseReview> PostReleaseReviews => Set<PostReleaseReview>();
    public DbSet<RollbackAssessment> RollbackAssessments => Set<RollbackAssessment>();
    public DbSet<ReleaseFeatureFlagState> FeatureFlagStates => Set<ReleaseFeatureFlagState>();
    public DbSet<CanaryRollout> CanaryRollouts => Set<CanaryRollout>();
    public DbSet<ChangeConfidenceBreakdown> ConfidenceBreakdowns => Set<ChangeConfidenceBreakdown>();
    public DbSet<ChangeConfidenceEvent> ChangeConfidenceEvents => Set<ChangeConfidenceEvent>();
    public DbSet<ReleaseNotes> ReleaseNotes => Set<ReleaseNotes>();
    public DbSet<CiPromotionGate> CiPromotionGates => Set<CiPromotionGate>();
    public DbSet<PromotionGateEvaluation> PromotionGateEvaluations => Set<PromotionGateEvaluation>();
    public DbSet<CommitAssociation> CommitAssociations => Set<CommitAssociation>();
    public DbSet<WorkItemAssociation> WorkItemAssociations => Set<WorkItemAssociation>();
    public DbSet<ReleaseApprovalRequest> ApprovalRequests => Set<ReleaseApprovalRequest>();
    public DbSet<ReleaseApprovalPolicy> ApprovalPolicies => Set<ReleaseApprovalPolicy>();
    public DbSet<ExternalChangeRequest> ExternalChangeRequests => Set<ExternalChangeRequest>();
    public DbSet<TenantBenchmarkConsent> BenchmarkConsents => Set<TenantBenchmarkConsent>();
    public DbSet<BenchmarkSnapshotRecord> BenchmarkSnapshots => Set<BenchmarkSnapshotRecord>();
    public DbSet<ReleaseCalendarEntry> ReleaseCalendarEntries => Set<ReleaseCalendarEntry>();
    public DbSet<ServiceRiskProfile> ServiceRiskProfiles => Set<ServiceRiskProfile>();

    // ── Workflow ──────────────────────────────────────────────────────────────
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowStage> WorkflowStages => Set<WorkflowStage>();
    public DbSet<EvidencePack> EvidencePacks => Set<EvidencePack>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();

    // ── Promotion ─────────────────────────────────────────────────────────────
    public DbSet<PrmDeploymentEnvironment> DeploymentEnvironments => Set<PrmDeploymentEnvironment>();
    public DbSet<PrmPromotionRequest> PromotionRequests => Set<PrmPromotionRequest>();
    public DbSet<PrmPromotionGate> PromotionGates => Set<PrmPromotionGate>();
    public DbSet<PrmGateEvaluation> GateEvaluations => Set<PrmGateEvaluation>();

    // ── RulesetGovernance ─────────────────────────────────────────────────────
    public DbSet<Ruleset> Rulesets => Set<Ruleset>();
    public DbSet<RulesetBinding> RulesetBindings => Set<RulesetBinding>();
    public DbSet<LintResult> LintResults => Set<LintResult>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ChangeGovernanceDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace => null;

    /// <inheritdoc />
    protected override string OutboxTableName => "chg_hub_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}

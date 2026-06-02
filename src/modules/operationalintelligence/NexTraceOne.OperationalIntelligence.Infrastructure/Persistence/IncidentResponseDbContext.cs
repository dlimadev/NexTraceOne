using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Persistence;

/// <summary>
/// DbContext consolidado do módulo OperationalIntelligence (IncidentResponse).
/// Unifica IncidentDbContext + ReliabilityDbContext + AutomationDbContext + RuntimeIntelligenceDbContext.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class IncidentResponseDbContext(
    DbContextOptions<IncidentResponseDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock),
      IUnitOfWork,
      IReliabilityUnitOfWork,
      IAutomationUnitOfWork,
      IRuntimeIntelligenceUnitOfWork
{
    // ── Incidents ─────────────────────────────────────────────────────────────
    public DbSet<IncidentRecord> Incidents => Set<IncidentRecord>();
    public DbSet<MitigationWorkflowRecord> MitigationWorkflows => Set<MitigationWorkflowRecord>();
    public DbSet<MitigationWorkflowActionLog> MitigationWorkflowActions => Set<MitigationWorkflowActionLog>();
    public DbSet<MitigationValidationLog> MitigationValidations => Set<MitigationValidationLog>();
    public DbSet<RunbookRecord> Runbooks => Set<RunbookRecord>();
    public DbSet<IncidentChangeCorrelation> ChangeCorrelations => Set<IncidentChangeCorrelation>();
    public DbSet<PostIncidentReview> PostIncidentReviews => Set<PostIncidentReview>();
    public DbSet<IncidentNarrative> IncidentNarratives => Set<IncidentNarrative>();
    public DbSet<RunbookStepExecution> RunbookStepExecutions => Set<RunbookStepExecution>();

    // ── Reliability ───────────────────────────────────────────────────────────
    public DbSet<ReliabilitySnapshot> ReliabilitySnapshots => Set<ReliabilitySnapshot>();
    public DbSet<SloDefinition> SloDefinitions => Set<SloDefinition>();
    public DbSet<SlaDefinition> SlaDefinitions => Set<SlaDefinition>();
    public DbSet<ErrorBudgetSnapshot> ErrorBudgetSnapshots => Set<ErrorBudgetSnapshot>();
    public DbSet<BurnRateSnapshot> BurnRateSnapshots => Set<BurnRateSnapshot>();
    public DbSet<ServiceFailurePrediction> ServiceFailurePredictions => Set<ServiceFailurePrediction>();
    public DbSet<CapacityForecast> CapacityForecasts => Set<CapacityForecast>();
    public DbSet<IncidentPredictionPattern> IncidentPredictionPatterns => Set<IncidentPredictionPattern>();
    public DbSet<HealingRecommendation> HealingRecommendations => Set<HealingRecommendation>();

    // ── Automation ────────────────────────────────────────────────────────────
    public DbSet<AutomationWorkflowRecord> AutomationWorkflows => Set<AutomationWorkflowRecord>();
    public DbSet<AutomationValidationRecord> AutomationValidations => Set<AutomationValidationRecord>();
    public DbSet<AutomationAuditRecord> AutomationAuditRecords => Set<AutomationAuditRecord>();

    // ── Runtime Intelligence ──────────────────────────────────────────────────
    public DbSet<RuntimeSnapshot> RuntimeSnapshots => Set<RuntimeSnapshot>();
    public DbSet<RuntimeBaseline> RuntimeBaselines => Set<RuntimeBaseline>();
    public DbSet<DriftFinding> DriftFindings => Set<DriftFinding>();
    public DbSet<ObservabilityProfile> ObservabilityProfiles => Set<ObservabilityProfile>();
    public DbSet<CustomChart> CustomCharts => Set<CustomChart>();
    public DbSet<ChaosExperiment> ChaosExperiments => Set<ChaosExperiment>();
    public DbSet<AnomalyNarrative> AnomalyNarratives => Set<AnomalyNarrative>();
    public DbSet<EnvironmentDriftReport> EnvironmentDriftReports => Set<EnvironmentDriftReport>();
    public DbSet<OperationalPlaybook> OperationalPlaybooks => Set<OperationalPlaybook>();
    public DbSet<PlaybookExecution> PlaybookExecutions => Set<PlaybookExecution>();
    public DbSet<ResilienceReport> ResilienceReports => Set<ResilienceReport>();
    public DbSet<ProfilingSession> ProfilingSessions => Set<ProfilingSession>();
    public DbSet<SloObservation> SloObservations => Set<SloObservation>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(IncidentResponseDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace => null;

    /// <inheritdoc />
    protected override string OutboxTableName => "ops_incident_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}

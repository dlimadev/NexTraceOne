using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence;

/// <summary>
/// DbContext consolidado do módulo AIHub (AIKnowledge).
/// Unifica AiGovernanceDbContext + ExternalAiDbContext + AiOrchestrationDbContext num único bounded context.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class AiHubDbContext(
    DbContextOptions<AiHubDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    // ── AiGovernance ──────────────────────────────────────────────────────────
    public DbSet<AIAccessPolicy> AccessPolicies => Set<AIAccessPolicy>();
    public DbSet<AIModel> Models => Set<AIModel>();
    public DbSet<AIBudget> Budgets => Set<AIBudget>();
    public DbSet<AiAssistantConversation> Conversations => Set<AiAssistantConversation>();
    public DbSet<AiMessage> Messages => Set<AiMessage>();
    public DbSet<AIKnowledgeSource> KnowledgeSources => Set<AIKnowledgeSource>();
    public DbSet<AIIDEClientRegistration> IdeClientRegistrations => Set<AIIDEClientRegistration>();
    public DbSet<AIIDECapabilityPolicy> IdeCapabilityPolicies => Set<AIIDECapabilityPolicy>();
    public DbSet<AIRoutingDecision> RoutingDecisions => Set<AIRoutingDecision>();
    public DbSet<AIRoutingStrategy> RoutingStrategies => Set<AIRoutingStrategy>();
    public DbSet<AiProvider> Providers => Set<AiProvider>();
    public DbSet<AiSource> Sources => Set<AiSource>();
    public DbSet<AiTokenQuotaPolicy> TokenQuotaPolicies => Set<AiTokenQuotaPolicy>();
    public DbSet<AiTokenUsageLedger> TokenUsageLedger => Set<AiTokenUsageLedger>();
    public DbSet<AiExternalInferenceRecord> ExternalInferenceRecords => Set<AiExternalInferenceRecord>();
    public DbSet<AiAgent> Agents => Set<AiAgent>();
    public DbSet<AiAgentExecution> AgentExecutions => Set<AiAgentExecution>();
    public DbSet<AiAgentArtifact> AgentArtifacts => Set<AiAgentArtifact>();
    public DbSet<AIKnowledgeSourceWeight> SourceWeights => Set<AIKnowledgeSourceWeight>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<AiToolDefinition> ToolDefinitions => Set<AiToolDefinition>();
    public DbSet<AiGuardrail> Guardrails => Set<AiGuardrail>();
    public DbSet<AiEvaluation> Evaluations => Set<AiEvaluation>();
    public DbSet<AiFeedback> Feedbacks => Set<AiFeedback>();
    public DbSet<OnboardingSession> OnboardingSessions => Set<OnboardingSession>();
    public DbSet<IdeQuerySession> IdeQuerySessions => Set<IdeQuerySession>();
    public DbSet<AIExecutionPlan> ExecutionPlans => Set<AIExecutionPlan>();
    public DbSet<AiSkill> Skills => Set<AiSkill>();
    public DbSet<AiSkillExecution> SkillExecutions => Set<AiSkillExecution>();
    public DbSet<AiSkillFeedback> SkillFeedbacks => Set<AiSkillFeedback>();
    public DbSet<AiAgentTrajectoryFeedback> AgentTrajectoryFeedbacks => Set<AiAgentTrajectoryFeedback>();
    public DbSet<AiAgentPerformanceMetric> AgentPerformanceMetrics => Set<AiAgentPerformanceMetric>();
    public DbSet<WarRoomSession> WarRooms => Set<WarRoomSession>();
    public DbSet<ChangeConfidenceScore> ChangeConfidenceScores => Set<ChangeConfidenceScore>();
    public DbSet<GuardianAlert> GuardianAlerts => Set<GuardianAlert>();
    public DbSet<OrganizationalMemoryNode> MemoryNodes => Set<OrganizationalMemoryNode>();
    public DbSet<SelfHealingAction> SelfHealingActions => Set<SelfHealingAction>();
    public DbSet<EvaluationSuite> EvaluationSuites => Set<EvaluationSuite>();
    public DbSet<EvaluationCase> EvaluationCases => Set<EvaluationCase>();
    public DbSet<EvaluationRun> EvaluationRuns => Set<EvaluationRun>();
    public DbSet<EvaluationDataset> EvaluationDatasets => Set<EvaluationDataset>();
    public DbSet<AiEvalDataset> EvalDatasets => Set<AiEvalDataset>();
    public DbSet<AiEvalRun> EvalRuns => Set<AiEvalRun>();
    public DbSet<ExternalDataSource> ExternalDataSources => Set<ExternalDataSource>();
    public DbSet<PromptAsset> PromptAssets => Set<PromptAsset>();
    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();
    public DbSet<ModelRoutingPolicy> ModelRoutingPolicies => Set<ModelRoutingPolicy>();
    public DbSet<AgentExecutionPlan> AgentExecutionPlans => Set<AgentExecutionPlan>();
    public DbSet<AiFeatureModelBinding> FeatureModelBindings => Set<AiFeatureModelBinding>();
    public DbSet<ModelPredictionSample> ModelPredictionSamples => Set<ModelPredictionSample>();

    // ── ExternalAI — prefixo "ExternalAi" evita conflito com AiProvider.Providers ──
    public DbSet<ExternalAiProvider> ExternalAiProviders => Set<ExternalAiProvider>();
    public DbSet<ExternalAiPolicy> ExternalAiPolicies => Set<ExternalAiPolicy>();
    public DbSet<ExternalAiConsultation> Consultations => Set<ExternalAiConsultation>();
    public DbSet<KnowledgeCapture> KnowledgeCaptures => Set<KnowledgeCapture>();

    // ── Orchestration — prefixo "Orchestration" evita conflito com AiAssistantConversation.Conversations ──
    public DbSet<AiContext> Contexts => Set<AiContext>();
    public DbSet<AiConversation> OrchestrationConversations => Set<AiConversation>();
    public DbSet<GeneratedTestArtifact> TestArtifacts => Set<GeneratedTestArtifact>();
    public DbSet<KnowledgeCaptureEntry> KnowledgeCaptureEntries => Set<KnowledgeCaptureEntry>();
    public DbSet<AgentWorkflowExecution> WorkflowExecutions => Set<AgentWorkflowExecution>();

    /// <inheritdoc />
    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(AiHubDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace => null;

    /// <inheritdoc />
    protected override string OutboxTableName => "aik_hub_outbox_messages";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Ignore<AgentStep>();
    }

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}

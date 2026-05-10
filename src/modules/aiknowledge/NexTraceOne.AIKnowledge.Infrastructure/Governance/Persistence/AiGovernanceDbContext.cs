using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence;

/// <summary>
/// DbContext do módulo AiGovernance.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class AiGovernanceDbContext(
    DbContextOptions<AiGovernanceDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    public DbSet<AIAccessPolicy> AccessPolicies => Set<AIAccessPolicy>();
    public DbSet<AIModel> Models => Set<AIModel>();
    public DbSet<AIBudget> Budgets => Set<AIBudget>();
    public DbSet<AiAssistantConversation> Conversations => Set<AiAssistantConversation>();
    public DbSet<AiMessage> Messages => Set<AiMessage>();
    public DbSet<AIUsageEntry> UsageEntries => Set<AIUsageEntry>();
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

    // ── Phase 4: Prompt Templates & Tool Definitions ────────────
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<AiToolDefinition> ToolDefinitions => Set<AiToolDefinition>();

    // ── Phase 5: Guardrails & Evaluations ───────────────────────
    public DbSet<AiGuardrail> Guardrails => Set<AiGuardrail>();
    public DbSet<AiEvaluation> Evaluations => Set<AiEvaluation>();

    // ── Phase 6: Feedback Loop ──────────────────────────────────
    public DbSet<AiFeedback> Feedbacks => Set<AiFeedback>();

    // ── Phase 7: Onboarding Companion ───────────────────────────
    public DbSet<OnboardingSession> OnboardingSessions => Set<OnboardingSession>();

    // ── Phase 8: IDE Query Sessions (AI Pair Programming) ───────
    public DbSet<IdeQuerySession> IdeQuerySessions => Set<IdeQuerySession>();

    // ── E-A04: Execution Plans ───────────────────────────────────
    public DbSet<AIExecutionPlan> ExecutionPlans => Set<AIExecutionPlan>();

    // ── Phase 9: Skills System ───────────────────────────────────────────────
    public DbSet<AiSkill> Skills => Set<AiSkill>();
    public DbSet<AiSkillExecution> SkillExecutions => Set<AiSkillExecution>();
    public DbSet<AiSkillFeedback> SkillFeedbacks => Set<AiSkillFeedback>();

    // ── Phase 10: Agent Lightning (Reinforcement Learning) ───────────────────
    public DbSet<AiAgentTrajectoryFeedback> AgentTrajectoryFeedbacks => Set<AiAgentTrajectoryFeedback>();
    public DbSet<AiAgentPerformanceMetric> AgentPerformanceMetrics => Set<AiAgentPerformanceMetric>();

    // ── Phase 11: Enterprise Capabilities ────────────────────────────────────
    public DbSet<WarRoomSession> WarRooms => Set<WarRoomSession>();
    public DbSet<ChangeConfidenceScore> ChangeConfidenceScores => Set<ChangeConfidenceScore>();
    public DbSet<GuardianAlert> GuardianAlerts => Set<GuardianAlert>();
    public DbSet<OrganizationalMemoryNode> MemoryNodes => Set<OrganizationalMemoryNode>();
    public DbSet<SelfHealingAction> SelfHealingActions => Set<SelfHealingAction>();

    // ── ADR-009: AI Evaluation Harness ───────────────────────────────────────
    public DbSet<EvaluationSuite> EvaluationSuites => Set<EvaluationSuite>();
    public DbSet<EvaluationCase> EvaluationCases => Set<EvaluationCase>();
    public DbSet<EvaluationRun> EvaluationRuns => Set<EvaluationRun>();
    public DbSet<EvaluationDataset> EvaluationDatasets => Set<EvaluationDataset>();

    // ── CC-05: AI Eval Harness — model comparison datasets & runs ────────────
    public DbSet<AiEvalDataset> EvalDatasets => Set<AiEvalDataset>();
    public DbSet<AiEvalRun> EvalRuns => Set<AiEvalRun>();

    // ── External Data Sources (Extensible RAG) ───────────────────────────────
    public DbSet<ExternalDataSource> ExternalDataSources => Set<ExternalDataSource>();

    // ── AI-5.2: Prompt Asset Registry ────────────────────────────────────────
    public DbSet<PromptAsset> PromptAssets => Set<PromptAsset>();
    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();

    // ── Routing Policies & Agentic Execution Plans ────────────────────────────
    public DbSet<ModelRoutingPolicy> ModelRoutingPolicies => Set<ModelRoutingPolicy>();
    public DbSet<AgentExecutionPlan> AgentExecutionPlans => Set<AgentExecutionPlan>();

    // ── AI Model Quality & Drift Governance (Wave AT.1) ───────────────────────
    public DbSet<ModelPredictionSample> ModelPredictionSamples => Set<ModelPredictionSample>();

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(AiGovernanceDbContext).Assembly;

    /// <inheritdoc />
    protected override string? ConfigurationsNamespace
        => "NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations";

    /// <inheritdoc />
    protected override string OutboxTableName => "aik_gov_outbox_messages";

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);
}

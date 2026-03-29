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

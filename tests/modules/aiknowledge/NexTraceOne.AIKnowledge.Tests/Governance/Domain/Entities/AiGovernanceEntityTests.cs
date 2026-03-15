using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Tests.Governance.Domain.Entities;

/// <summary>Testes unitários das entidades de AI Governance.</summary>
public sealed class AiGovernanceEntityTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── AIModel ────────────────────────────────────────────────────────────

    [Fact]
    public void Model_Register_ShouldSetProperties()
    {
        var model = AIModel.Register(
            "gpt-4o", "GPT-4o", "OpenAI", ModelType.Chat,
            isInternal: false, "chat,code,reasoning", 3, FixedNow);

        model.Name.Should().Be("gpt-4o");
        model.DisplayName.Should().Be("GPT-4o");
        model.Provider.Should().Be("OpenAI");
        model.ModelType.Should().Be(ModelType.Chat);
        model.IsInternal.Should().BeFalse();
        model.IsExternal.Should().BeTrue();
        model.Status.Should().Be(ModelStatus.Active);
        model.Capabilities.Should().Be("chat,code,reasoning");
        model.SensitivityLevel.Should().Be(3);
        model.RegisteredAt.Should().Be(FixedNow);
        model.DefaultUseCases.Should().BeEmpty();
    }

    [Fact]
    public void Model_Register_Internal_ShouldDeriveIsExternal()
    {
        var model = AIModel.Register(
            "local-llm", "Local LLM", "Internal", ModelType.Completion,
            isInternal: true, "completion", 1, FixedNow);

        model.IsInternal.Should().BeTrue();
        model.IsExternal.Should().BeFalse();
    }

    [Fact]
    public void Model_UpdateDetails_ShouldModifyProperties()
    {
        var model = AIModel.Register(
            "gpt-4o", "GPT-4o", "OpenAI", ModelType.Chat,
            false, "chat", 2, FixedNow);

        var result = model.UpdateDetails("GPT-4o Updated", "chat,code", "change-analysis", 4);

        result.IsSuccess.Should().BeTrue();
        model.DisplayName.Should().Be("GPT-4o Updated");
        model.Capabilities.Should().Be("chat,code");
        model.DefaultUseCases.Should().Be("change-analysis");
        model.SensitivityLevel.Should().Be(4);
    }

    [Fact]
    public void Model_Deactivate_ShouldSetInactive()
    {
        var model = AIModel.Register(
            "m", "M", "P", ModelType.Chat, false, "chat", 1, FixedNow);

        model.Deactivate();

        model.Status.Should().Be(ModelStatus.Inactive);
    }

    [Fact]
    public void Model_Activate_ShouldSetActive()
    {
        var model = AIModel.Register(
            "m", "M", "P", ModelType.Chat, false, "chat", 1, FixedNow);
        model.Deactivate();

        model.Activate();

        model.Status.Should().Be(ModelStatus.Active);
    }

    [Fact]
    public void Model_Deprecate_ShouldSetDeprecated()
    {
        var model = AIModel.Register(
            "m", "M", "P", ModelType.Chat, false, "chat", 1, FixedNow);

        model.Deprecate();

        model.Status.Should().Be(ModelStatus.Deprecated);
    }

    [Fact]
    public void Model_Block_ShouldSetBlocked()
    {
        var model = AIModel.Register(
            "m", "M", "P", ModelType.Chat, false, "chat", 1, FixedNow);

        model.Block();

        model.Status.Should().Be(ModelStatus.Blocked);
    }

    // ── AIAccessPolicy ─────────────────────────────────────────────────────

    [Fact]
    public void Policy_Create_ShouldSetProperties()
    {
        var policy = AIAccessPolicy.Create(
            "Default Policy", "Standard access policy", "team", "platform-team",
            allowExternalAI: true, internalOnly: false, maxTokensPerRequest: 8000, FixedNow);

        policy.Name.Should().Be("Default Policy");
        policy.Description.Should().Be("Standard access policy");
        policy.Scope.Should().Be("team");
        policy.ScopeValue.Should().Be("platform-team");
        policy.AllowExternalAI.Should().BeTrue();
        policy.InternalOnly.Should().BeFalse();
        policy.MaxTokensPerRequest.Should().Be(8000);
        policy.IsActive.Should().BeTrue();
        policy.AllowedModelIds.Should().BeEmpty();
        policy.BlockedModelIds.Should().BeEmpty();
    }

    [Fact]
    public void Policy_Update_ShouldModifyProperties()
    {
        var policy = AIAccessPolicy.Create(
            "P", "D", "user", "u1", true, false, 4000, FixedNow);

        var result = policy.Update("New desc", false, true, 16000, "production");

        result.IsSuccess.Should().BeTrue();
        policy.Description.Should().Be("New desc");
        policy.AllowExternalAI.Should().BeFalse();
        policy.InternalOnly.Should().BeTrue();
        policy.MaxTokensPerRequest.Should().Be(16000);
        policy.EnvironmentRestrictions.Should().Be("production");
    }

    [Fact]
    public void Policy_Deactivate_ShouldSetInactive()
    {
        var policy = AIAccessPolicy.Create(
            "P", "D", "role", "admin", true, false, 4000, FixedNow);

        policy.Deactivate();

        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Policy_Activate_AfterDeactivation_ShouldSetActive()
    {
        var policy = AIAccessPolicy.Create(
            "P", "D", "role", "admin", true, false, 4000, FixedNow);
        policy.Deactivate();

        policy.Activate();

        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Policy_IsModelAllowed_WhenNoLists_ShouldReturnTrue()
    {
        var policy = AIAccessPolicy.Create(
            "P", "D", "team", "t1", true, false, 4000, FixedNow);
        var modelId = Guid.NewGuid();

        policy.IsModelAllowed(modelId).Should().BeTrue();
    }

    [Fact]
    public void Policy_IsModelAllowed_WhenInBlockedList_ShouldReturnFalse()
    {
        var policy = AIAccessPolicy.Create(
            "P", "D", "team", "t1", true, false, 4000, FixedNow);
        var modelId = Guid.NewGuid();
        policy.SetBlockedModels(modelId.ToString());

        policy.IsModelAllowed(modelId).Should().BeFalse();
    }

    [Fact]
    public void Policy_IsModelAllowed_WhenInAllowedList_ShouldReturnTrue()
    {
        var policy = AIAccessPolicy.Create(
            "P", "D", "team", "t1", true, false, 4000, FixedNow);
        var modelId = Guid.NewGuid();
        policy.SetAllowedModels(modelId.ToString());

        policy.IsModelAllowed(modelId).Should().BeTrue();
    }

    [Fact]
    public void Policy_IsModelAllowed_WhenNotInAllowedList_ShouldReturnFalse()
    {
        var policy = AIAccessPolicy.Create(
            "P", "D", "team", "t1", true, false, 4000, FixedNow);
        policy.SetAllowedModels(Guid.NewGuid().ToString());

        policy.IsModelAllowed(Guid.NewGuid()).Should().BeFalse();
    }

    // ── AIBudget ───────────────────────────────────────────────────────────

    [Fact]
    public void Budget_Create_ShouldSetProperties()
    {
        var budget = AIBudget.Create(
            "Team Budget", "team", "platform-team",
            BudgetPeriod.Monthly, 500_000, 1000, FixedNow);

        budget.Name.Should().Be("Team Budget");
        budget.Scope.Should().Be("team");
        budget.ScopeValue.Should().Be("platform-team");
        budget.Period.Should().Be(BudgetPeriod.Monthly);
        budget.MaxTokens.Should().Be(500_000);
        budget.MaxRequests.Should().Be(1000);
        budget.CurrentTokensUsed.Should().Be(0);
        budget.CurrentRequestCount.Should().Be(0);
        budget.IsActive.Should().BeTrue();
        budget.IsQuotaExceeded.Should().BeFalse();
    }

    [Fact]
    public void Budget_RecordUsage_ShouldIncrementCounters()
    {
        var budget = AIBudget.Create(
            "B", "user", "u1", BudgetPeriod.Daily, 100_000, 100, FixedNow);

        var result = budget.RecordUsage(500);

        result.IsSuccess.Should().BeTrue();
        budget.CurrentTokensUsed.Should().Be(500);
        budget.CurrentRequestCount.Should().Be(1);
    }

    [Fact]
    public void Budget_RecordUsage_WhenQuotaExceeded_ShouldReturnError()
    {
        var budget = AIBudget.Create(
            "B", "user", "u1", BudgetPeriod.Daily, 100, 2, FixedNow);

        budget.RecordUsage(50);
        budget.RecordUsage(60);

        var result = budget.RecordUsage(10);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AiGovernance.Budget.QuotaExceeded");
    }

    [Fact]
    public void Budget_IsQuotaExceeded_WhenTokensReachMax_ShouldBeTrue()
    {
        var budget = AIBudget.Create(
            "B", "user", "u1", BudgetPeriod.Daily, 100, 1000, FixedNow);

        budget.RecordUsage(100);

        budget.IsQuotaExceeded.Should().BeTrue();
    }

    [Fact]
    public void Budget_ResetPeriod_ShouldZeroCounters()
    {
        var budget = AIBudget.Create(
            "B", "user", "u1", BudgetPeriod.Weekly, 100_000, 100, FixedNow);
        budget.RecordUsage(5000);

        var newStart = FixedNow.AddDays(7);
        budget.ResetPeriod(newStart);

        budget.CurrentTokensUsed.Should().Be(0);
        budget.CurrentRequestCount.Should().Be(0);
        budget.PeriodStartDate.Should().Be(newStart);
        budget.IsQuotaExceeded.Should().BeFalse();
    }

    [Fact]
    public void Budget_Update_ShouldModifyLimits()
    {
        var budget = AIBudget.Create(
            "B", "team", "t1", BudgetPeriod.Daily, 100_000, 100, FixedNow);

        var result = budget.Update(200_000, 500, BudgetPeriod.Monthly);

        result.IsSuccess.Should().BeTrue();
        budget.MaxTokens.Should().Be(200_000);
        budget.MaxRequests.Should().Be(500);
        budget.Period.Should().Be(BudgetPeriod.Monthly);
    }

    // ── AIUsageEntry ───────────────────────────────────────────────────────

    [Fact]
    public void UsageEntry_Record_ShouldSetProperties()
    {
        var modelId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var entry = AIUsageEntry.Record(
            "user-1", "Alice", modelId, "gpt-4o", "OpenAI",
            isInternal: false, FixedNow,
            promptTokens: 200, completionTokens: 300,
            policyId, "Default Policy", UsageResult.Allowed,
            "change-analysis", AIClientType.Web, "corr-001",
            conversationId);

        entry.UserId.Should().Be("user-1");
        entry.UserDisplayName.Should().Be("Alice");
        entry.ModelId.Should().Be(modelId);
        entry.ModelName.Should().Be("gpt-4o");
        entry.Provider.Should().Be("OpenAI");
        entry.IsInternal.Should().BeFalse();
        entry.IsExternal.Should().BeTrue();
        entry.Timestamp.Should().Be(FixedNow);
        entry.PromptTokens.Should().Be(200);
        entry.CompletionTokens.Should().Be(300);
        entry.TotalTokens.Should().Be(500);
        entry.PolicyId.Should().Be(policyId);
        entry.PolicyName.Should().Be("Default Policy");
        entry.Result.Should().Be(UsageResult.Allowed);
        entry.ContextScope.Should().Be("change-analysis");
        entry.ClientType.Should().Be(AIClientType.Web);
        entry.CorrelationId.Should().Be("corr-001");
        entry.ConversationId.Should().Be(conversationId);
    }

    [Fact]
    public void UsageEntry_Record_Internal_ShouldDeriveIsExternal()
    {
        var entry = AIUsageEntry.Record(
            "user-2", "Bob", Guid.NewGuid(), "local-llm", "Internal",
            isInternal: true, FixedNow,
            100, 150, null, null, UsageResult.Allowed,
            "contract-generation", AIClientType.VsCode, "corr-002");

        entry.IsInternal.Should().BeTrue();
        entry.IsExternal.Should().BeFalse();
    }

    [Fact]
    public void UsageEntry_Record_ShouldCalculateTotalTokens()
    {
        var entry = AIUsageEntry.Record(
            "user-3", "Carol", Guid.NewGuid(), "claude-3", "Anthropic",
            false, FixedNow,
            promptTokens: 1000, completionTokens: 2500,
            null, null, UsageResult.Allowed,
            "error-diagnosis", AIClientType.Api, "corr-003");

        entry.TotalTokens.Should().Be(3500);
    }

    [Fact]
    public void UsageEntry_Record_WithoutConversation_ShouldBeNull()
    {
        var entry = AIUsageEntry.Record(
            "user-4", "Dave", Guid.NewGuid(), "gpt-4o", "OpenAI",
            false, FixedNow,
            50, 100, null, null, UsageResult.Blocked,
            "general", AIClientType.VisualStudio, "corr-004");

        entry.ConversationId.Should().BeNull();
        entry.PolicyId.Should().BeNull();
        entry.PolicyName.Should().BeNull();
    }

    // ── AIKnowledgeSource ──────────────────────────────────────────────────

    [Fact]
    public void KnowledgeSource_Register_ShouldSetProperties()
    {
        var source = AIKnowledgeSource.Register(
            "Service Catalog", "Service information for grounding",
            KnowledgeSourceType.Service, "/api/services", 1, FixedNow);

        source.Name.Should().Be("Service Catalog");
        source.Description.Should().Be("Service information for grounding");
        source.SourceType.Should().Be(KnowledgeSourceType.Service);
        source.EndpointOrPath.Should().Be("/api/services");
        source.IsActive.Should().BeTrue();
        source.Priority.Should().Be(1);
        source.RegisteredAt.Should().Be(FixedNow);
    }

    [Fact]
    public void KnowledgeSource_Deactivate_ShouldSetInactive()
    {
        var source = AIKnowledgeSource.Register(
            "S", "D", KnowledgeSourceType.Contract, "/api/contracts", 0, FixedNow);

        source.Deactivate();

        source.IsActive.Should().BeFalse();
    }

    [Fact]
    public void KnowledgeSource_Activate_AfterDeactivation_ShouldSetActive()
    {
        var source = AIKnowledgeSource.Register(
            "S", "D", KnowledgeSourceType.Incident, "/api/incidents", 0, FixedNow);
        source.Deactivate();

        source.Activate();

        source.IsActive.Should().BeTrue();
    }

    [Fact]
    public void KnowledgeSource_UpdatePriority_ShouldChangePriority()
    {
        var source = AIKnowledgeSource.Register(
            "S", "D", KnowledgeSourceType.Change, "/api/changes", 1, FixedNow);

        source.UpdatePriority(5);

        source.Priority.Should().Be(5);
    }

    [Fact]
    public void KnowledgeSource_Update_ShouldModifyDescriptionAndPath()
    {
        var source = AIKnowledgeSource.Register(
            "S", "Old desc", KnowledgeSourceType.Runbook, "/api/old", 0, FixedNow);

        var result = source.Update("New description", "/api/new-endpoint");

        result.IsSuccess.Should().BeTrue();
        source.Description.Should().Be("New description");
        source.EndpointOrPath.Should().Be("/api/new-endpoint");
    }
}

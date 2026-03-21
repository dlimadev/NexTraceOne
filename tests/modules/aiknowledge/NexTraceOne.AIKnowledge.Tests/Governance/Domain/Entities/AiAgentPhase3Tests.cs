using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

/// <summary>
/// Testes unitários para Phase 3 Agent Runtime Foundation:
/// AiAgent (CreateCustom, UpdateDefinition, lifecycle, IsModelAllowed, IsAccessibleBy),
/// AiAgentExecution (Start, Complete, Fail, Cancel),
/// AiAgentArtifact (Create, Approve, Reject, Supersede).
/// </summary>
public sealed class AiAgentPhase3Tests
{
    // ── CreateCustom ────────────────────────────────────────────────────

    [Fact]
    public void CreateCustom_WithValidData_ShouldCreateDraftAgent()
    {
        var agent = AiAgent.CreateCustom(
            "my-agent",
            "My Agent",
            "Custom description",
            AgentCategory.ApiDesign,
            "You are a custom agent.",
            "Help design APIs",
            AgentOwnershipType.Tenant,
            AgentVisibility.Team,
            "user-1",
            ownerTeamId: "team-1");

        agent.Name.Should().Be("my-agent");
        agent.DisplayName.Should().Be("My Agent");
        agent.Slug.Should().Be("my-agent");
        agent.Category.Should().Be(AgentCategory.ApiDesign);
        agent.IsOfficial.Should().BeFalse();
        agent.IsActive.Should().BeFalse();
        agent.OwnershipType.Should().Be(AgentOwnershipType.Tenant);
        agent.Visibility.Should().Be(AgentVisibility.Team);
        agent.PublicationStatus.Should().Be(AgentPublicationStatus.Draft);
        agent.OwnerId.Should().Be("user-1");
        agent.OwnerTeamId.Should().Be("team-1");
        agent.Objective.Should().Be("Help design APIs");
        agent.AllowModelOverride.Should().BeTrue();
        agent.Version.Should().Be(1);
        agent.ExecutionCount.Should().Be(0);
    }

    [Fact]
    public void CreateCustom_WithSystemOwnership_ShouldThrow()
    {
        var act = () => AiAgent.CreateCustom(
            "name", "Display", "desc", AgentCategory.General,
            "prompt", "obj", AgentOwnershipType.System,
            AgentVisibility.Tenant, "user-1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateCustom_WithNullOwner_ShouldThrow()
    {
        var act = () => AiAgent.CreateCustom(
            "name", "Display", "desc", AgentCategory.General,
            "prompt", "obj", AgentOwnershipType.User,
            AgentVisibility.Private, null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateCustom_WithAllowedModels_ShouldStore()
    {
        var modelId = Guid.NewGuid();
        var agent = AiAgent.CreateCustom(
            "name", "Display", "desc", AgentCategory.General,
            "prompt", "obj", AgentOwnershipType.User,
            AgentVisibility.Private, "user-1",
            allowedModelIds: modelId.ToString());

        agent.AllowedModelIds.Should().Be(modelId.ToString());
    }

    // ── UpdateDefinition ────────────────────────────────────────────────

    [Fact]
    public void UpdateDefinition_OnCustomAgent_ShouldSuccess()
    {
        var agent = AiAgent.CreateCustom(
            "name", "Display", "desc", AgentCategory.General,
            "prompt", "obj", AgentOwnershipType.Tenant,
            AgentVisibility.Team, "user-1");

        var result = agent.UpdateDefinition(
            "New Display", "New Desc", "New Prompt", "New Obj",
            "caps", "eng", "icon", null, null, null, null, null,
            AgentVisibility.Tenant, true, 5);

        result.IsSuccess.Should().BeTrue();
        agent.DisplayName.Should().Be("New Display");
        agent.Description.Should().Be("New Desc");
        agent.SystemPrompt.Should().Be("New Prompt");
        agent.Objective.Should().Be("New Obj");
        agent.Visibility.Should().Be(AgentVisibility.Tenant);
        agent.SortOrder.Should().Be(5);
    }

    [Fact]
    public void UpdateDefinition_OnSystemAgent_ShouldFail()
    {
        var agent = AiAgent.Register(
            "system-agent", "System Agent", "desc",
            AgentCategory.General, true, "prompt");

        var result = agent.UpdateDefinition(
            "Changed", "desc", null, null, null, null, null,
            null, null, null, null, null, null, null, null);

        result.IsFailure.Should().BeTrue();
    }

    // ── Lifecycle ───────────────────────────────────────────────────────

    [Fact]
    public void Activate_ShouldSetActiveAndPromoteDraft()
    {
        var agent = AiAgent.CreateCustom(
            "n", "D", "d", AgentCategory.General,
            "p", "o", AgentOwnershipType.User,
            AgentVisibility.Private, "u1");

        agent.IsActive.Should().BeFalse();
        agent.PublicationStatus.Should().Be(AgentPublicationStatus.Draft);

        agent.Activate();

        agent.IsActive.Should().BeTrue();
        agent.PublicationStatus.Should().Be(AgentPublicationStatus.Active);
    }

    [Fact]
    public void Deactivate_ShouldSetInactive()
    {
        var agent = AiAgent.Register(
            "n", "D", "d", AgentCategory.General, true, "p");

        agent.IsActive.Should().BeTrue();
        agent.Deactivate();
        agent.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Publish_ShouldIncrementVersion()
    {
        var agent = AiAgent.CreateCustom(
            "n", "D", "d", AgentCategory.General,
            "p", "o", AgentOwnershipType.Tenant,
            AgentVisibility.Tenant, "u1");

        agent.Activate();
        var v1 = agent.Version;
        agent.Publish();

        agent.PublicationStatus.Should().Be(AgentPublicationStatus.Published);
        agent.Version.Should().Be(v1 + 1);
        agent.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Publish_WhenBlocked_ShouldFail()
    {
        var agent = AiAgent.CreateCustom(
            "n", "D", "d", AgentCategory.General,
            "p", "o", AgentOwnershipType.Tenant,
            AgentVisibility.Tenant, "u1");

        agent.Block();
        var result = agent.Publish();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Archive_ShouldSetArchivedAndInactive()
    {
        var agent = AiAgent.Register(
            "n", "D", "d", AgentCategory.General, true, "p");

        agent.Archive();

        agent.PublicationStatus.Should().Be(AgentPublicationStatus.Archived);
        agent.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Block_ShouldSetBlockedAndInactive()
    {
        var agent = AiAgent.Register(
            "n", "D", "d", AgentCategory.General, true, "p");

        agent.Block();

        agent.PublicationStatus.Should().Be(AgentPublicationStatus.Blocked);
        agent.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SubmitForReview_FromDraft_ShouldSucceed()
    {
        var agent = AiAgent.CreateCustom(
            "n", "D", "d", AgentCategory.General,
            "p", "o", AgentOwnershipType.User,
            AgentVisibility.Private, "u1");

        var result = agent.SubmitForReview();

        result.IsSuccess.Should().BeTrue();
        agent.PublicationStatus.Should().Be(AgentPublicationStatus.PendingReview);
    }

    [Fact]
    public void SubmitForReview_FromPublished_ShouldFail()
    {
        var agent = AiAgent.CreateCustom(
            "n", "D", "d", AgentCategory.General,
            "p", "o", AgentOwnershipType.Tenant,
            AgentVisibility.Tenant, "u1");

        agent.Activate();
        agent.Publish();

        var result = agent.SubmitForReview();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void IncrementExecutionCount_ShouldIncrement()
    {
        var agent = AiAgent.Register(
            "n", "D", "d", AgentCategory.General, true, "p");

        agent.ExecutionCount.Should().Be(0);
        agent.IncrementExecutionCount();
        agent.ExecutionCount.Should().Be(1);
    }

    // ── IsModelAllowed ──────────────────────────────────────────────────

    [Fact]
    public void IsModelAllowed_WhenNoRestrictions_ShouldReturnTrue()
    {
        var agent = AiAgent.Register(
            "n", "D", "d", AgentCategory.General, true, "p");

        agent.IsModelAllowed(Guid.NewGuid()).Should().BeTrue();
    }

    [Fact]
    public void IsModelAllowed_WhenModelInList_ShouldReturnTrue()
    {
        var modelId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var agent = AiAgent.CreateCustom(
            "n", "D", "d", AgentCategory.General,
            "p", "o", AgentOwnershipType.User,
            AgentVisibility.Private, "u1",
            allowedModelIds: $"{modelId},{otherId}");

        agent.IsModelAllowed(modelId).Should().BeTrue();
    }

    [Fact]
    public void IsModelAllowed_WhenModelNotInList_ShouldReturnFalse()
    {
        var allowedId = Guid.NewGuid();
        var agent = AiAgent.CreateCustom(
            "n", "D", "d", AgentCategory.General,
            "p", "o", AgentOwnershipType.User,
            AgentVisibility.Private, "u1",
            allowedModelIds: allowedId.ToString());

        agent.IsModelAllowed(Guid.NewGuid()).Should().BeFalse();
    }

    // ── IsAccessibleBy ──────────────────────────────────────────────────

    [Fact]
    public void IsAccessibleBy_SystemAgent_ShouldReturnTrue()
    {
        var agent = AiAgent.Register(
            "n", "D", "d", AgentCategory.General, true, "p");

        agent.IsAccessibleBy("any-user", null).Should().BeTrue();
    }

    [Fact]
    public void IsAccessibleBy_TenantVisibility_ShouldReturnTrue()
    {
        var agent = AiAgent.CreateCustom(
            "n", "D", "d", AgentCategory.General,
            "p", "o", AgentOwnershipType.Tenant,
            AgentVisibility.Tenant, "u1");

        agent.IsAccessibleBy("any-user", null).Should().BeTrue();
    }

    [Fact]
    public void IsAccessibleBy_PrivateAgent_OwnerShouldAccess()
    {
        var agent = AiAgent.CreateCustom(
            "n", "D", "d", AgentCategory.General,
            "p", "o", AgentOwnershipType.User,
            AgentVisibility.Private, "user-1");

        agent.IsAccessibleBy("user-1", null).Should().BeTrue();
        agent.IsAccessibleBy("user-2", null).Should().BeFalse();
    }

    [Fact]
    public void IsAccessibleBy_TeamVisibility_TeamMemberShouldAccess()
    {
        var agent = AiAgent.CreateCustom(
            "n", "D", "d", AgentCategory.General,
            "p", "o", AgentOwnershipType.Tenant,
            AgentVisibility.Team, "u1",
            ownerTeamId: "team-alpha");

        agent.IsAccessibleBy("any-user", "team-alpha").Should().BeTrue();
        agent.IsAccessibleBy("any-user", "team-beta").Should().BeFalse();
        agent.IsAccessibleBy("any-user", null).Should().BeFalse();
    }
}

/// <summary>Testes unitários da entidade AiAgentExecution — ciclo de vida da execução.</summary>
public sealed class AiAgentExecutionTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_ShouldCreateRunningExecution()
    {
        var agentId = AiAgentId.New();
        var modelId = Guid.NewGuid();

        var execution = AiAgentExecution.Start(
            agentId, "user-1", modelId, "ollama",
            "{\"input\":\"test\"}", null, FixedNow);

        execution.AgentId.Should().Be(agentId);
        execution.ExecutedBy.Should().Be("user-1");
        execution.Status.Should().Be(AgentExecutionStatus.Running);
        execution.ModelIdUsed.Should().Be(modelId);
        execution.ProviderUsed.Should().Be("ollama");
        execution.StartedAt.Should().Be(FixedNow);
        execution.CompletedAt.Should().BeNull();
        execution.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Complete_ShouldSetCompletedStatusAndTokens()
    {
        var execution = AiAgentExecution.Start(
            AiAgentId.New(), "user-1", Guid.NewGuid(),
            "ollama", "{}", null, FixedNow);

        var completedAt = FixedNow.AddSeconds(5);
        execution.Complete("output result", 100, 200, 5000, completedAt);

        execution.Status.Should().Be(AgentExecutionStatus.Completed);
        execution.OutputJson.Should().Be("output result");
        execution.PromptTokens.Should().Be(100);
        execution.CompletionTokens.Should().Be(200);
        execution.TotalTokens.Should().Be(300);
        execution.DurationMs.Should().Be(5000);
        execution.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void Fail_ShouldSetFailedStatusWithError()
    {
        var execution = AiAgentExecution.Start(
            AiAgentId.New(), "user-1", Guid.NewGuid(),
            "ollama", "{}", null, FixedNow);

        var failedAt = FixedNow.AddSeconds(2);
        execution.Fail("Provider timeout", failedAt, 2000);

        execution.Status.Should().Be(AgentExecutionStatus.Failed);
        execution.ErrorMessage.Should().Be("Provider timeout");
        execution.CompletedAt.Should().Be(failedAt);
        execution.DurationMs.Should().Be(2000);
    }

    [Fact]
    public void Cancel_ShouldSetCancelledStatus()
    {
        var execution = AiAgentExecution.Start(
            AiAgentId.New(), "user-1", Guid.NewGuid(),
            "ollama", "{}", null, FixedNow);

        var cancelledAt = FixedNow.AddSeconds(1);
        execution.Cancel(cancelledAt);

        execution.Status.Should().Be(AgentExecutionStatus.Cancelled);
        execution.CompletedAt.Should().Be(cancelledAt);
    }
}

/// <summary>Testes unitários da entidade AiAgentArtifact — criação e review de artefactos.</summary>
public sealed class AiAgentArtifactTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldCreatePendingArtifact()
    {
        var executionId = AiAgentExecutionId.New();
        var agentId = AiAgentId.New();

        var artifact = AiAgentArtifact.Create(
            executionId, agentId,
            AgentArtifactType.OpenApiDraft,
            "Generated API Contract",
            "openapi: 3.1.0\ninfo: ...",
            "yaml");

        artifact.ExecutionId.Should().Be(executionId);
        artifact.AgentId.Should().Be(agentId);
        artifact.ArtifactType.Should().Be(AgentArtifactType.OpenApiDraft);
        artifact.Title.Should().Be("Generated API Contract");
        artifact.Content.Should().StartWith("openapi");
        artifact.Format.Should().Be("yaml");
        artifact.ReviewStatus.Should().Be(ArtifactReviewStatus.Pending);
        artifact.Version.Should().Be(1);
    }

    [Fact]
    public void Approve_ShouldSetApproved()
    {
        var artifact = AiAgentArtifact.Create(
            AiAgentExecutionId.New(), AiAgentId.New(),
            AgentArtifactType.TestScenarios,
            "Test Scenarios", "[{\"name\":\"test1\"}]", "json");

        var result = artifact.Approve("reviewer-1", FixedNow, "Looks good");

        result.IsSuccess.Should().BeTrue();
        artifact.ReviewStatus.Should().Be(ArtifactReviewStatus.Approved);
        artifact.ReviewedBy.Should().Be("reviewer-1");
        artifact.ReviewedAt.Should().Be(FixedNow);
        artifact.ReviewNotes.Should().Be("Looks good");
    }

    [Fact]
    public void Reject_ShouldSetRejected()
    {
        var artifact = AiAgentArtifact.Create(
            AiAgentExecutionId.New(), AiAgentId.New(),
            AgentArtifactType.KafkaSchema,
            "Kafka Schema", "{}", "json");

        var result = artifact.Reject("reviewer-2", FixedNow, "Missing fields");

        result.IsSuccess.Should().BeTrue();
        artifact.ReviewStatus.Should().Be(ArtifactReviewStatus.Rejected);
    }

    [Fact]
    public void Approve_WhenAlreadyReviewed_ShouldFail()
    {
        var artifact = AiAgentArtifact.Create(
            AiAgentExecutionId.New(), AiAgentId.New(),
            AgentArtifactType.Documentation,
            "Doc", "content", "markdown");

        artifact.Approve("r1", FixedNow);
        var result = artifact.Approve("r2", FixedNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reject_WhenAlreadyReviewed_ShouldFail()
    {
        var artifact = AiAgentArtifact.Create(
            AiAgentExecutionId.New(), AiAgentId.New(),
            AgentArtifactType.Analysis,
            "Analysis", "content", "markdown");

        artifact.Reject("r1", FixedNow, "bad");
        var result = artifact.Reject("r2", FixedNow, "also bad");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Supersede_ShouldSetSuperseded()
    {
        var artifact = AiAgentArtifact.Create(
            AiAgentExecutionId.New(), AiAgentId.New(),
            AgentArtifactType.OpenApiDraft,
            "Old Draft", "content", "yaml");

        artifact.Supersede();

        artifact.ReviewStatus.Should().Be(ArtifactReviewStatus.Superseded);
    }
}

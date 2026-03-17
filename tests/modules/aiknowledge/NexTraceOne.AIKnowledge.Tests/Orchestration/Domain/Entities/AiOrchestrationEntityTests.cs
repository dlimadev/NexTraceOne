using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Orchestration.Domain.Entities;

/// <summary>Testes unitários das entidades GeneratedTestArtifact, AiContext e KnowledgeCaptureEntry.</summary>
public sealed class AiOrchestrationEntityTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── GeneratedTestArtifact ─────────────────────────────────────────────

    [Fact]
    public void Artifact_Generate_ShouldInitializeAsDraft()
    {
        var result = GeneratedTestArtifact.Generate(
            Guid.NewGuid(), "OrderService", "xunit",
            "public class OrderTests { [Fact] public void Test() { } }",
            0.85m, FixedNow);

        result.IsSuccess.Should().BeTrue();
        var artifact = result.Value;
        artifact.Status.Should().Be(ArtifactStatus.Draft);
        artifact.TestFramework.Should().Be("xunit");
        artifact.Confidence.Should().Be(0.85m);
        artifact.ReviewedBy.Should().BeNull();
    }

    [Fact]
    public void Artifact_Accept_ShouldTransitionToAccepted()
    {
        var artifact = GeneratedTestArtifact.Generate(Guid.NewGuid(), "Svc", "nunit", "code", 0.9m, FixedNow).Value;

        var result = artifact.Accept("reviewer@co.com", FixedNow.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        artifact.Status.Should().Be(ArtifactStatus.Accepted);
        artifact.ReviewedBy.Should().Be("reviewer@co.com");
    }

    [Fact]
    public void Artifact_Reject_ShouldTransitionToRejected()
    {
        var artifact = GeneratedTestArtifact.Generate(Guid.NewGuid(), "Svc", "robot-framework", "code", 0.5m, FixedNow).Value;

        var result = artifact.Reject("reviewer@co.com", FixedNow.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        artifact.Status.Should().Be(ArtifactStatus.Rejected);
    }

    [Fact]
    public void Artifact_AcceptTwice_ShouldFail()
    {
        var artifact = GeneratedTestArtifact.Generate(Guid.NewGuid(), "Svc", "xunit", "code", 0.9m, FixedNow).Value;
        artifact.Accept("r@co.com", FixedNow.AddHours(1));

        var result = artifact.Accept("other@co.com", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyReviewed");
    }

    // ── AiContext ──────────────────────────────────────────────────────────

    [Fact]
    public void Context_Assemble_ShouldSetProperties()
    {
        var releaseId = Guid.NewGuid();
        var context = AiContext.Assemble(
            "OrderService", "change-analysis",
            "{\"diff\":[{\"path\":\"/orders\",\"change\":\"removed field\"}]}",
            FixedNow, releaseId);

        context.ServiceName.Should().Be("OrderService");
        context.ContextType.Should().Be("change-analysis");
        context.ReleaseId.Should().Be(releaseId);
    }

    [Fact]
    public void Context_EstimateTokens_ShouldReturnPositive()
    {
        var context = AiContext.Assemble("Svc", "error-diagnosis", new string('x', 1000), FixedNow);

        context.TokenEstimate.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Context_Assemble_WithoutRelease_ShouldHaveNullReleaseId()
    {
        var context = AiContext.Assemble("Svc", "test-generation", "{}", FixedNow);

        context.ReleaseId.Should().BeNull();
    }

    // ── KnowledgeCaptureEntry ─────────────────────────────────────────────

    [Fact]
    public void Entry_Suggest_ShouldInitializeAsSuggested()
    {
        var result = KnowledgeCaptureEntry.Suggest(
            AiConversationId.New(),
            "Best practice: field removal requires deprecation",
            "When removing fields from REST APIs, always deprecate first with a 90-day notice.",
            "change-analysis", 0.85m, FixedNow);

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value;
        entry.Status.Should().Be(KnowledgeEntryStatus.Suggested);
        entry.Relevance.Should().Be(0.85m);
        entry.ValidatedBy.Should().BeNull();
    }

    [Fact]
    public void Entry_Validate_ShouldTransitionToValidated()
    {
        var entry = KnowledgeCaptureEntry.Suggest(
            AiConversationId.New(), "Title", "Content", "source", 0.8m, FixedNow).Value;

        var result = entry.Validate("validator@co.com", FixedNow.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(KnowledgeEntryStatus.Validated);
    }

    [Fact]
    public void Entry_Discard_ShouldTransitionToDiscarded()
    {
        var entry = KnowledgeCaptureEntry.Suggest(
            AiConversationId.New(), "Title", "Content", "source", 0.3m, FixedNow).Value;

        var result = entry.Discard("validator@co.com", FixedNow.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(KnowledgeEntryStatus.Discarded);
    }

    [Fact]
    public void Entry_ValidateTwice_ShouldFail()
    {
        var entry = KnowledgeCaptureEntry.Suggest(
            AiConversationId.New(), "Title", "Content", "source", 0.9m, FixedNow).Value;
        entry.Validate("v@co.com", FixedNow.AddHours(1));

        var result = entry.Validate("v2@co.com", FixedNow.AddHours(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Entry.AlreadyProcessed");
    }

    [Fact]
    public void Entry_Suggest_WithInvalidRelevance_ShouldReturnFailure()
    {
        var result = KnowledgeCaptureEntry.Suggest(
            AiConversationId.New(), "Title", "Content", "source", 1.5m, FixedNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidRelevance");
    }
}

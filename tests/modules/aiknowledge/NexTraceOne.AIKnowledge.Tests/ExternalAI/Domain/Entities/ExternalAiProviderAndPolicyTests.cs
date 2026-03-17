using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Domain.Entities;

/// <summary>Testes unitários das entidades ExternalAiProvider e ExternalAiPolicy.</summary>
public sealed class ExternalAiProviderAndPolicyTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── ExternalAiProvider ────────────────────────────────────────────────

    [Fact]
    public void Provider_Register_ShouldSetProperties()
    {
        var provider = ExternalAiProvider.Register(
            "Azure OpenAI", "https://api.openai.azure.com", "gpt-4o",
            4096, 0.005m, 1, FixedNow);

        provider.Name.Should().Be("Azure OpenAI");
        provider.ModelName.Should().Be("gpt-4o");
        provider.IsActive.Should().BeTrue();
        provider.Priority.Should().Be(1);
    }

    [Fact]
    public void Provider_Deactivate_ShouldSetInactive()
    {
        var provider = ExternalAiProvider.Register("P", "https://x", "m", 4096, 0.01m, 1, FixedNow);

        provider.Deactivate();

        provider.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Provider_Activate_ShouldSetActive()
    {
        var provider = ExternalAiProvider.Register("P", "https://x", "m", 4096, 0.01m, 1, FixedNow);
        provider.Deactivate();

        provider.Activate();

        provider.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Provider_UpdatePriority_ShouldChangePriority()
    {
        var provider = ExternalAiProvider.Register("P", "https://x", "m", 4096, 0.01m, 1, FixedNow);

        provider.UpdatePriority(5);

        provider.Priority.Should().Be(5);
    }

    // ── ExternalAiPolicy ──────────────────────────────────────────────────

    [Fact]
    public void Policy_Create_ShouldSetProperties()
    {
        var policy = ExternalAiPolicy.Create(
            "Default AI Policy", "Standard AI usage policy",
            100, 50000, false, "change-analysis,error-diagnosis", FixedNow);

        policy.Name.Should().Be("Default AI Policy");
        policy.MaxDailyQueries.Should().Be(100);
        policy.RequiresApproval.Should().BeFalse();
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Policy_IsContextAllowed_WhenAllowed_ShouldReturnTrue()
    {
        var policy = ExternalAiPolicy.Create(
            "P", "D", 100, 50000, false, "change-analysis,error-diagnosis", FixedNow);

        policy.IsContextAllowed("change-analysis").Should().BeTrue();
    }

    [Fact]
    public void Policy_IsContextAllowed_WhenNotAllowed_ShouldReturnFalse()
    {
        var policy = ExternalAiPolicy.Create(
            "P", "D", 100, 50000, false, "change-analysis", FixedNow);

        policy.IsContextAllowed("test-generation").Should().BeFalse();
    }

    [Fact]
    public void Policy_Deactivate_ShouldSetInactive()
    {
        var policy = ExternalAiPolicy.Create("P", "D", 100, 50000, false, "all", FixedNow);

        policy.Deactivate();

        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Policy_Update_ShouldModifyProperties()
    {
        var policy = ExternalAiPolicy.Create("P", "D", 100, 50000, false, "all", FixedNow);

        policy.Update("New description", 200, 100000, true, "change-analysis,test-generation");

        policy.Description.Should().Be("New description");
        policy.MaxDailyQueries.Should().Be(200);
        policy.MaxTokensPerDay.Should().Be(100000);
        policy.RequiresApproval.Should().BeTrue();
    }
}

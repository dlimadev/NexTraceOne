using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Domain;

/// <summary>Testes unitários da entidade AiProvider.</summary>
public sealed class AiProviderTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_WithValidData_ShouldCreateProvider()
    {
        var provider = AiProvider.Register(
            "ollama-local", "Ollama Local", "Ollama",
            "http://localhost:11434", true, "chat,embeddings",
            1, "Local Ollama instance", FixedNow);

        provider.Name.Should().Be("ollama-local");
        provider.DisplayName.Should().Be("Ollama Local");
        provider.ProviderType.Should().Be("Ollama");
        provider.BaseUrl.Should().Be("http://localhost:11434");
        provider.IsLocal.Should().BeTrue();
        provider.IsExternal.Should().BeFalse();
        provider.IsEnabled.Should().BeTrue();
        provider.SupportedCapabilities.Should().Be("chat,embeddings");
        provider.Priority.Should().Be(1);
        provider.Description.Should().Be("Local Ollama instance");
        provider.RegisteredAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Register_WithNullName_ShouldThrow()
    {
        var act = () => AiProvider.Register(
            null!, "Display", "Type",
            "http://localhost", true, "chat",
            1, "desc", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Register_External_ShouldDeriveIsExternal()
    {
        var provider = AiProvider.Register(
            "openai-prod", "OpenAI", "OpenAI",
            "https://api.openai.com", false, "chat,vision",
            2, "Production OpenAI", FixedNow);

        provider.IsLocal.Should().BeFalse();
        provider.IsExternal.Should().BeTrue();
    }

    [Fact]
    public void Enable_ShouldSetIsEnabled()
    {
        var provider = AiProvider.Register(
            "p", "P", "T", "http://url", true, "chat", 1, "d", FixedNow);
        provider.Disable();

        var result = provider.Enable();

        result.IsSuccess.Should().BeTrue();
        provider.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabled()
    {
        var provider = AiProvider.Register(
            "p", "P", "T", "http://url", true, "chat", 1, "d", FixedNow);

        var result = provider.Disable();

        result.IsSuccess.Should().BeTrue();
        provider.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void UpdateConfiguration_WithValidData_ShouldUpdateFields()
    {
        var provider = AiProvider.Register(
            "p", "Old Name", "T", "http://old-url", true, "chat", 1, "old desc", FixedNow);

        var result = provider.UpdateConfiguration(
            "New Name", "http://new-url", "chat,vision", 5, "new desc");

        result.IsSuccess.Should().BeTrue();
        provider.DisplayName.Should().Be("New Name");
        provider.BaseUrl.Should().Be("http://new-url");
        provider.SupportedCapabilities.Should().Be("chat,vision");
        provider.Priority.Should().Be(5);
        provider.Description.Should().Be("new desc");
    }

    // ── New fields (Phase 1: AI Runtime Foundation) ─────────────────────

    [Fact]
    public void Register_ShouldSetSlugFromName_WhenNotProvided()
    {
        var provider = AiProvider.Register(
            "Ollama Local", "Ollama", "ollama",
            "http://localhost:11434", true, "chat", 1, "d", FixedNow);

        provider.Slug.Should().Be("ollama-local");
    }

    [Fact]
    public void Register_ShouldUseExplicitSlug_WhenProvided()
    {
        var provider = AiProvider.Register(
            "ollama", "Ollama", "ollama",
            "http://localhost:11434", true, "chat", 1, "d", FixedNow,
            slug: "my-custom-slug");

        provider.Slug.Should().Be("my-custom-slug");
    }

    [Fact]
    public void Register_ShouldSetDefaultValues_ForNewFields()
    {
        var provider = AiProvider.Register(
            "p", "P", "T", "http://url", true, "chat", 1, "d", FixedNow);

        provider.AuthenticationMode.Should().Be(AuthenticationMode.None);
        provider.SupportsChat.Should().BeFalse();
        provider.SupportsEmbeddings.Should().BeFalse();
        provider.SupportsTools.Should().BeFalse();
        provider.SupportsVision.Should().BeFalse();
        provider.SupportsStructuredOutput.Should().BeFalse();
        provider.HealthStatus.Should().Be(ProviderHealthStatus.Unknown);
        provider.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void Register_WithAllNewFields_ShouldSetCorrectly()
    {
        var provider = AiProvider.Register(
            "openai", "OpenAI", "openai",
            "https://api.openai.com/v1", false, "chat,vision,tools",
            10, "External provider", FixedNow,
            slug: "openai",
            authenticationMode: AuthenticationMode.ApiKey,
            supportsChat: true,
            supportsEmbeddings: true,
            supportsTools: true,
            supportsVision: true,
            supportsStructuredOutput: true,
            timeoutSeconds: 60);

        provider.Slug.Should().Be("openai");
        provider.AuthenticationMode.Should().Be(AuthenticationMode.ApiKey);
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsEmbeddings.Should().BeTrue();
        provider.SupportsTools.Should().BeTrue();
        provider.SupportsVision.Should().BeTrue();
        provider.SupportsStructuredOutput.Should().BeTrue();
        provider.HealthStatus.Should().Be(ProviderHealthStatus.Unknown);
        provider.TimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void Register_WithZeroTimeout_ShouldThrow()
    {
        var act = () => AiProvider.Register(
            "p", "P", "T", "http://url", true, "chat", 1, "d", FixedNow,
            timeoutSeconds: 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateCapabilityFlags_ShouldUpdateAllFlags()
    {
        var provider = AiProvider.Register(
            "p", "P", "T", "http://url", true, "chat", 1, "d", FixedNow);

        var result = provider.UpdateCapabilityFlags(
            supportsChat: true,
            supportsEmbeddings: true,
            supportsTools: false,
            supportsVision: true,
            supportsStructuredOutput: false);

        result.IsSuccess.Should().BeTrue();
        provider.SupportsChat.Should().BeTrue();
        provider.SupportsEmbeddings.Should().BeTrue();
        provider.SupportsTools.Should().BeFalse();
        provider.SupportsVision.Should().BeTrue();
        provider.SupportsStructuredOutput.Should().BeFalse();
    }

    [Fact]
    public void RecordHealthStatus_ShouldUpdateStatus()
    {
        var provider = AiProvider.Register(
            "p", "P", "T", "http://url", true, "chat", 1, "d", FixedNow);

        provider.RecordHealthStatus(ProviderHealthStatus.Healthy);

        provider.HealthStatus.Should().Be(ProviderHealthStatus.Healthy);
    }

    [Fact]
    public void RecordHealthStatus_ShouldTransitionThroughStates()
    {
        var provider = AiProvider.Register(
            "p", "P", "T", "http://url", true, "chat", 1, "d", FixedNow);
        provider.HealthStatus.Should().Be(ProviderHealthStatus.Unknown);

        provider.RecordHealthStatus(ProviderHealthStatus.Healthy);
        provider.HealthStatus.Should().Be(ProviderHealthStatus.Healthy);

        provider.RecordHealthStatus(ProviderHealthStatus.Degraded);
        provider.HealthStatus.Should().Be(ProviderHealthStatus.Degraded);

        provider.RecordHealthStatus(ProviderHealthStatus.Unhealthy);
        provider.HealthStatus.Should().Be(ProviderHealthStatus.Unhealthy);
    }
}

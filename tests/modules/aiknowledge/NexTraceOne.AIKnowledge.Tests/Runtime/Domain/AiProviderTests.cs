using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

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
}

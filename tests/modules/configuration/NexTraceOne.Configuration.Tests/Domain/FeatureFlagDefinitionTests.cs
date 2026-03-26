using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade FeatureFlagDefinition.
/// Valida criação, guard clauses, atualização, ativação e desativação.
/// </summary>
public sealed class FeatureFlagDefinitionTests
{
    private static readonly ConfigurationModuleId SampleModuleId =
        new(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    // ── Create — happy path ────────────────────────────────────────────

    [Fact]
    public void Create_WithValidParameters_ShouldCreateFlagDefinition()
    {
        // Act
        var flag = FeatureFlagDefinition.Create(
            key: "ai.assistant.enabled",
            displayName: "AI Assistant Enabled",
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant, ConfigurationScope.Environment],
            description: "Controls whether the AI assistant is available.",
            defaultEnabled: true,
            moduleId: SampleModuleId,
            isEditable: true);

        // Assert
        flag.Should().NotBeNull();
        flag.Id.Should().NotBeNull();
        flag.Id.Value.Should().NotBeEmpty();
        flag.Key.Should().Be("ai.assistant.enabled");
        flag.DisplayName.Should().Be("AI Assistant Enabled");
        flag.Description.Should().Be("Controls whether the AI assistant is available.");
        flag.DefaultEnabled.Should().BeTrue();
        flag.AllowedScopes.Should().BeEquivalentTo(new[]
        {
            ConfigurationScope.System,
            ConfigurationScope.Tenant,
            ConfigurationScope.Environment
        });
        flag.ModuleId.Should().Be(SampleModuleId);
        flag.IsActive.Should().BeTrue();
        flag.IsEditable.Should().BeTrue();
        flag.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        flag.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutOptionalParameters_ShouldUseDefaults()
    {
        var flag = FeatureFlagDefinition.Create(
            key: "feature.test",
            displayName: "Test Flag",
            allowedScopes: [ConfigurationScope.System]);

        flag.Description.Should().BeNull();
        flag.DefaultEnabled.Should().BeFalse();
        flag.ModuleId.Should().BeNull();
        flag.IsEditable.Should().BeTrue();
        flag.IsActive.Should().BeTrue();
    }

    // ── Create — validation ────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyKey_ShouldThrow(string? key)
    {
        var act = () => FeatureFlagDefinition.Create(
            key: key!,
            displayName: "Test",
            allowedScopes: [ConfigurationScope.System]);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDisplayName_ShouldThrow(string? displayName)
    {
        var act = () => FeatureFlagDefinition.Create(
            key: "feature.test",
            displayName: displayName!,
            allowedScopes: [ConfigurationScope.System]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyAllowedScopes_ShouldThrow()
    {
        var act = () => FeatureFlagDefinition.Create(
            key: "feature.test",
            displayName: "Test",
            allowedScopes: []);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithKeyTooLong_ShouldThrow()
    {
        var act = () => FeatureFlagDefinition.Create(
            key: new string('a', 257),
            displayName: "Test",
            allowedScopes: [ConfigurationScope.System]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithDescriptionTooLong_ShouldThrow()
    {
        var act = () => FeatureFlagDefinition.Create(
            key: "feature.test",
            displayName: "Test",
            allowedScopes: [ConfigurationScope.System],
            description: new string('a', 1001));
        act.Should().Throw<ArgumentException>();
    }

    // ── Update ─────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateMutableFields()
    {
        var flag = FeatureFlagDefinition.Create(
            key: "feature.test",
            displayName: "Test",
            allowedScopes: [ConfigurationScope.System],
            defaultEnabled: false);

        var newModuleId = new ConfigurationModuleId(Guid.NewGuid());
        flag.Update(
            displayName: "Updated Flag",
            description: "New description.",
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            defaultEnabled: true,
            moduleId: newModuleId,
            isEditable: false);

        flag.DisplayName.Should().Be("Updated Flag");
        flag.Description.Should().Be("New description.");
        flag.AllowedScopes.Should().BeEquivalentTo(new[]
        {
            ConfigurationScope.System, ConfigurationScope.Tenant
        });
        flag.DefaultEnabled.Should().BeTrue();
        flag.ModuleId.Should().Be(newModuleId);
        flag.IsEditable.Should().BeFalse();
        flag.UpdatedAt.Should().NotBeNull();
        flag.Key.Should().Be("feature.test"); // key is immutable
    }

    [Fact]
    public void Update_WithEmptyDisplayName_ShouldThrow()
    {
        var flag = FeatureFlagDefinition.Create(
            key: "feature.test",
            displayName: "Test",
            allowedScopes: [ConfigurationScope.System]);

        var act = () => flag.Update(
            displayName: "",
            description: null,
            allowedScopes: [ConfigurationScope.System],
            defaultEnabled: false,
            moduleId: null,
            isEditable: true);
        act.Should().Throw<ArgumentException>();
    }

    // ── Activate / Deactivate ──────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var flag = FeatureFlagDefinition.Create(
            key: "feature.test",
            displayName: "Test",
            allowedScopes: [ConfigurationScope.System]);
        flag.Deactivate();
        flag.IsActive.Should().BeFalse();
        flag.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var flag = FeatureFlagDefinition.Create(
            key: "feature.test",
            displayName: "Test",
            allowedScopes: [ConfigurationScope.System]);
        flag.Deactivate();
        flag.Activate();
        flag.IsActive.Should().BeTrue();
    }
}

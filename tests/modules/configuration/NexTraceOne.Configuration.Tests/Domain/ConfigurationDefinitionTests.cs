using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade ConfigurationDefinition.
/// Valida criação, guard clauses, atualização e cenários representativos do Phase 1.
/// </summary>
public sealed class ConfigurationDefinitionTests
{
    // ── Create — happy path ────────────────────────────────────────────

    [Fact]
    public void Create_WithValidParameters_ShouldCreateDefinition()
    {
        // Act
        var definition = ConfigurationDefinition.Create(
            key: "platform.notifications.enabled",
            displayName: "Notifications Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Controls whether the notification system is active.",
            defaultValue: "true",
            uiEditorType: "toggle",
            sortOrder: 100);

        // Assert
        definition.Should().NotBeNull();
        definition.Id.Should().NotBeNull();
        definition.Id.Value.Should().NotBeEmpty();
        definition.Key.Should().Be("platform.notifications.enabled");
        definition.DisplayName.Should().Be("Notifications Enabled");
        definition.Category.Should().Be(ConfigurationCategory.Functional);
        definition.ValueType.Should().Be(ConfigurationValueType.Boolean);
        definition.AllowedScopes.Should().BeEquivalentTo(
            new[] { ConfigurationScope.System, ConfigurationScope.Tenant });
        definition.Description.Should().Be("Controls whether the notification system is active.");
        definition.DefaultValue.Should().Be("true");
        definition.IsSensitive.Should().BeFalse();
        definition.IsEditable.Should().BeTrue();
        definition.IsInheritable.Should().BeTrue();
        definition.UiEditorType.Should().Be("toggle");
        definition.SortOrder.Should().Be(100);
        definition.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        definition.UpdatedAt.Should().BeNull();
    }

    // ── Create — validation ────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyKey_ShouldThrow(string? key)
    {
        var act = () => ConfigurationDefinition.Create(
            key: key!,
            displayName: "Test",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyAllowedScopes_ShouldThrow()
    {
        var act = () => ConfigurationDefinition.Create(
            key: "test.key",
            displayName: "Test",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: []);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDisplayName_ShouldThrow(string? displayName)
    {
        var act = () => ConfigurationDefinition.Create(
            key: "test.key",
            displayName: displayName!,
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System]);

        act.Should().Throw<ArgumentException>();
    }

    // ── Create — Phase 1 patterns ──────────────────────────────────────

    [Fact]
    public void Create_WithInstanceSettingsKey_ShouldHaveCorrectDefaults()
    {
        var definition = ConfigurationDefinition.Create(
            key: "instance.name",
            displayName: "Instance Name",
            category: ConfigurationCategory.Bootstrap,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "Display name of the platform instance",
            defaultValue: "NexTraceOne",
            uiEditorType: "text",
            sortOrder: 1000);

        definition.Key.Should().StartWith("instance.");
        definition.Category.Should().Be(ConfigurationCategory.Bootstrap);
        definition.AllowedScopes.Should().Contain(ConfigurationScope.System);
        definition.DefaultValue.Should().Be("NexTraceOne");
        definition.IsEditable.Should().BeTrue();
        definition.IsInheritable.Should().BeTrue();
    }

    [Fact]
    public void Create_WithTenantSettingsKey_ShouldAllowTenantScope()
    {
        var definition = ConfigurationDefinition.Create(
            key: "tenant.display_name",
            displayName: "Tenant Display Name",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.Tenant],
            description: "Custom display name for the tenant",
            uiEditorType: "text",
            sortOrder: 1100);

        definition.Key.Should().StartWith("tenant.");
        definition.AllowedScopes.Should().Contain(ConfigurationScope.Tenant);
    }

    [Fact]
    public void Create_WithEnvironmentSettingsKey_ShouldAllowEnvironmentScope()
    {
        var definition = ConfigurationDefinition.Create(
            key: "environment.is_production",
            displayName: "Environment Is Production",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.Environment],
            description: "Whether this environment is the production environment.",
            defaultValue: "false",
            isInheritable: false,
            uiEditorType: "toggle",
            sortOrder: 1210);

        definition.Key.Should().StartWith("environment.");
        definition.AllowedScopes.Should().Contain(ConfigurationScope.Environment);
        definition.IsInheritable.Should().BeFalse();
    }

    [Fact]
    public void Create_SensitiveDefinition_ShouldMarkAsSensitive()
    {
        var definition = ConfigurationDefinition.Create(
            key: "integration.webhook_secret",
            displayName: "Webhook Secret",
            category: ConfigurationCategory.SensitiveOperational,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            description: "Shared secret for webhook signature verification.",
            isSensitive: true,
            uiEditorType: "text",
            sortOrder: 600);

        definition.IsSensitive.Should().BeTrue();
        definition.Category.Should().Be(ConfigurationCategory.SensitiveOperational);
    }

    // ── Update ─────────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldModifyMutableFields()
    {
        var definition = ConfigurationDefinition.Create(
            key: "test.updateable",
            displayName: "Original Name",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System],
            description: "Original description",
            defaultValue: "original",
            sortOrder: 1);

        // Act
        definition.Update(
            displayName: "Updated Name",
            description: "Updated description",
            allowedScopes: [ConfigurationScope.System, ConfigurationScope.Tenant],
            defaultValue: "updated",
            isSensitive: true,
            isEditable: false,
            isInheritable: false,
            validationRules: """{"min": 1}""",
            uiEditorType: "select",
            sortOrder: 99);

        // Assert — mutable fields changed
        definition.DisplayName.Should().Be("Updated Name");
        definition.Description.Should().Be("Updated description");
        definition.AllowedScopes.Should().BeEquivalentTo(
            new[] { ConfigurationScope.System, ConfigurationScope.Tenant });
        definition.DefaultValue.Should().Be("updated");
        definition.IsSensitive.Should().BeTrue();
        definition.IsEditable.Should().BeFalse();
        definition.IsInheritable.Should().BeFalse();
        definition.ValidationRules.Should().Be("""{"min": 1}""");
        definition.UiEditorType.Should().Be("select");
        definition.SortOrder.Should().Be(99);
        definition.UpdatedAt.Should().NotBeNull();
        definition.UpdatedAt!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        // Assert — immutable key preserved
        definition.Key.Should().Be("test.updateable");
    }

    [Fact]
    public void Update_WithEmptyDisplayName_ShouldThrow()
    {
        var definition = ConfigurationDefinition.Create(
            key: "test.key",
            displayName: "Name",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System]);

        var act = () => definition.Update(
            displayName: "",
            description: null,
            allowedScopes: [ConfigurationScope.System],
            defaultValue: null,
            isSensitive: false,
            isEditable: true,
            isInheritable: true,
            validationRules: null,
            uiEditorType: null,
            sortOrder: 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithEmptyAllowedScopes_ShouldThrow()
    {
        var definition = ConfigurationDefinition.Create(
            key: "test.key",
            displayName: "Name",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System]);

        var act = () => definition.Update(
            displayName: "Name",
            description: null,
            allowedScopes: [],
            defaultValue: null,
            isSensitive: false,
            isEditable: true,
            isInheritable: true,
            validationRules: null,
            uiEditorType: null,
            sortOrder: 0);

        act.Should().Throw<ArgumentException>();
    }

    // ── Create — trims whitespace ──────────────────────────────────────

    [Fact]
    public void Create_ShouldTrimKeyAndDisplayName()
    {
        var definition = ConfigurationDefinition.Create(
            key: "  test.trimmed  ",
            displayName: "  Trimmed Name  ",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.String,
            allowedScopes: [ConfigurationScope.System]);

        definition.Key.Should().Be("test.trimmed");
        definition.DisplayName.Should().Be("Trimmed Name");
    }

    // ── ModuleId — hierarchy expansion P3.1 ───────────────────────────

    [Fact]
    public void Create_WithModuleId_ShouldAssignModuleId()
    {
        var moduleId = new ConfigurationModuleId(Guid.NewGuid());
        var definition = ConfigurationDefinition.Create(
            key: "notifications.enabled",
            displayName: "Notifications Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System],
            moduleId: moduleId);

        definition.ModuleId.Should().Be(moduleId);
    }

    [Fact]
    public void Create_WithoutModuleId_ShouldHaveNullModuleId()
    {
        var definition = ConfigurationDefinition.Create(
            key: "notifications.enabled",
            displayName: "Notifications Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System]);

        definition.ModuleId.Should().BeNull();
    }

    [Fact]
    public void Update_WithModuleId_ShouldUpdateModuleId()
    {
        var definition = ConfigurationDefinition.Create(
            key: "notifications.enabled",
            displayName: "Notifications Enabled",
            category: ConfigurationCategory.Functional,
            valueType: ConfigurationValueType.Boolean,
            allowedScopes: [ConfigurationScope.System]);

        var moduleId = new ConfigurationModuleId(Guid.NewGuid());
        definition.Update(
            displayName: "Notifications Enabled",
            description: null,
            allowedScopes: [ConfigurationScope.System],
            defaultValue: null,
            isSensitive: false,
            isEditable: true,
            isInheritable: true,
            validationRules: null,
            uiEditorType: null,
            sortOrder: 0,
            moduleId: moduleId);

        definition.ModuleId.Should().Be(moduleId);
    }
}

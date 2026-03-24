using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade ConfigurationEntry.
/// Valida criação, atualização de valor, versionamento, ativação/desativação.
/// </summary>
public sealed class ConfigurationEntryTests
{
    private static readonly ConfigurationDefinitionId SampleDefinitionId = new(Guid.NewGuid());

    // ── Create — happy path ────────────────────────────────────────────

    [Fact]
    public void Create_WithValidParameters_ShouldCreateEntry()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "platform.notifications.enabled",
            scope: ConfigurationScope.System,
            createdBy: "admin@platform.io",
            value: "true",
            changeReason: "Initial setup");

        entry.Should().NotBeNull();
        entry.Id.Should().NotBeNull();
        entry.Id.Value.Should().NotBeEmpty();
        entry.DefinitionId.Should().Be(SampleDefinitionId);
        entry.Key.Should().Be("platform.notifications.enabled");
        entry.Scope.Should().Be(ConfigurationScope.System);
        entry.ScopeReferenceId.Should().BeNull();
        entry.Value.Should().Be("true");
        entry.IsSensitive.Should().BeFalse();
        entry.IsEncrypted.Should().BeFalse();
        entry.Version.Should().Be(1);
        entry.IsActive.Should().BeTrue();
        entry.ChangeReason.Should().Be("Initial setup");
        entry.CreatedBy.Should().Be("admin@platform.io");
        entry.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        entry.UpdatedAt.Should().BeNull();
        entry.UpdatedBy.Should().BeNull();
    }

    // ── Create — scope reference rules ─────────────────────────────────

    [Fact]
    public void Create_ForSystemScope_ShouldNotRequireScopeReferenceId()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "platform.maintenance_mode",
            scope: ConfigurationScope.System,
            createdBy: "system");

        entry.Scope.Should().Be(ConfigurationScope.System);
        entry.ScopeReferenceId.Should().BeNull();
    }

    [Fact]
    public void Create_ForTenantScope_ShouldSetScopeReferenceId()
    {
        var tenantId = Guid.NewGuid().ToString();

        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "tenant.display_name",
            scope: ConfigurationScope.Tenant,
            createdBy: "admin@tenant.io",
            scopeReferenceId: tenantId,
            value: "Acme Corp");

        entry.Scope.Should().Be(ConfigurationScope.Tenant);
        entry.ScopeReferenceId.Should().Be(tenantId);
        entry.Value.Should().Be("Acme Corp");
    }

    [Fact]
    public void Create_ForEnvironmentScope_ShouldSetScopeReferenceId()
    {
        var envId = Guid.NewGuid().ToString();

        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "environment.is_production",
            scope: ConfigurationScope.Environment,
            createdBy: "admin@platform.io",
            scopeReferenceId: envId,
            value: "true");

        entry.Scope.Should().Be(ConfigurationScope.Environment);
        entry.ScopeReferenceId.Should().Be(envId);
    }

    // ── UpdateValue ────────────────────────────────────────────────────

    [Fact]
    public void UpdateValue_ShouldIncrementVersion()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "ai.default_temperature",
            scope: ConfigurationScope.System,
            createdBy: "system",
            value: "0.7");

        entry.Version.Should().Be(1);

        entry.UpdateValue(
            value: "0.9",
            structuredValueJson: null,
            updatedBy: "admin@platform.io",
            changeReason: "Increased temperature");

        entry.Version.Should().Be(2);
        entry.Value.Should().Be("0.9");
    }

    [Fact]
    public void UpdateValue_ShouldUpdateTimestamps()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "security.session_timeout_minutes",
            scope: ConfigurationScope.System,
            createdBy: "system",
            value: "60");

        entry.UpdatedAt.Should().BeNull();
        entry.UpdatedBy.Should().BeNull();

        entry.UpdateValue(
            value: "120",
            structuredValueJson: null,
            updatedBy: "admin@platform.io",
            changeReason: "Extended session timeout");

        entry.UpdatedAt.Should().NotBeNull();
        entry.UpdatedAt!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        entry.UpdatedBy.Should().Be("admin@platform.io");
        entry.ChangeReason.Should().Be("Extended session timeout");
    }

    [Fact]
    public void UpdateValue_MultipleTimes_ShouldIncrementVersionEachTime()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "test.versioning",
            scope: ConfigurationScope.System,
            createdBy: "system",
            value: "v1");

        entry.UpdateValue("v2", null, "admin");
        entry.UpdateValue("v3", null, "admin");
        entry.UpdateValue("v4", null, "admin");

        entry.Version.Should().Be(4);
        entry.Value.Should().Be("v4");
    }

    // ── Activate / Deactivate ──────────────────────────────────────────

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.module.catalog.enabled",
            scope: ConfigurationScope.System,
            createdBy: "system",
            value: "true");

        // Deactivate first, then reactivate
        entry.Deactivate("admin@platform.io", "Temporary disable");
        entry.IsActive.Should().BeFalse();

        entry.Activate("admin@platform.io", "Re-enabled");
        entry.IsActive.Should().BeTrue();
        entry.ChangeReason.Should().Be("Re-enabled");
        entry.UpdatedBy.Should().Be("admin@platform.io");
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.module.ai.enabled",
            scope: ConfigurationScope.System,
            createdBy: "system",
            value: "true");

        entry.IsActive.Should().BeTrue();

        entry.Deactivate("admin@platform.io", "Module disabled for maintenance");

        entry.IsActive.Should().BeFalse();
        entry.ChangeReason.Should().Be("Module disabled for maintenance");
        entry.UpdatedAt.Should().NotBeNull();
    }

    // ── Create — validation ────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyKey_ShouldThrow(string? key)
    {
        var act = () => ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: key!,
            scope: ConfigurationScope.System,
            createdBy: "system");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCreatedBy_ShouldThrow(string? createdBy)
    {
        var act = () => ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "test.key",
            scope: ConfigurationScope.System,
            createdBy: createdBy!);

        act.Should().Throw<ArgumentException>();
    }

    // ── Create — sensitive and encrypted ───────────────────────────────

    [Fact]
    public void Create_WithSensitiveFlag_ShouldPreserveFlag()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "integration.webhook_secret",
            scope: ConfigurationScope.System,
            createdBy: "system",
            value: "secret-value",
            isSensitive: true,
            isEncrypted: true);

        entry.IsSensitive.Should().BeTrue();
        entry.IsEncrypted.Should().BeTrue();
    }

    // ── Create — trims whitespace ──────────────────────────────────────

    [Fact]
    public void Create_ShouldTrimValues()
    {
        var entry = ConfigurationEntry.Create(
            definitionId: SampleDefinitionId,
            key: "  test.trimmed  ",
            scope: ConfigurationScope.System,
            createdBy: "  admin  ",
            value: "  trimmed-value  ");

        entry.Key.Should().Be("test.trimmed");
        entry.CreatedBy.Should().Be("admin");
        entry.Value.Should().Be("trimmed-value");
    }
}

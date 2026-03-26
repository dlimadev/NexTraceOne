using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade FeatureFlagEntry.
/// Valida criação, guard clauses, atualização de valor, ativação e desativação.
/// </summary>
public sealed class FeatureFlagEntryTests
{
    private static readonly FeatureFlagDefinitionId SampleDefinitionId =
        new(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    // ── Create — happy path ────────────────────────────────────────────

    [Fact]
    public void Create_WithValidParameters_ShouldCreateEntry()
    {
        // Act
        var entry = FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "ai.assistant.enabled",
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "user-123",
            scopeReferenceId: "tenant-456",
            changeReason: "Enabling AI for beta tenants.");

        // Assert
        entry.Should().NotBeNull();
        entry.Id.Should().NotBeNull();
        entry.Id.Value.Should().NotBeEmpty();
        entry.DefinitionId.Should().Be(SampleDefinitionId);
        entry.Key.Should().Be("ai.assistant.enabled");
        entry.Scope.Should().Be(ConfigurationScope.Tenant);
        entry.IsEnabled.Should().BeTrue();
        entry.IsActive.Should().BeTrue();
        entry.CreatedBy.Should().Be("user-123");
        entry.ScopeReferenceId.Should().Be("tenant-456");
        entry.ChangeReason.Should().Be("Enabling AI for beta tenants.");
        entry.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        entry.UpdatedAt.Should().BeNull();
        entry.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutOptionalParameters_ShouldUseDefaults()
    {
        var entry = FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.test",
            scope: ConfigurationScope.System,
            isEnabled: false,
            createdBy: "system");

        entry.ScopeReferenceId.Should().BeNull();
        entry.ChangeReason.Should().BeNull();
        entry.IsActive.Should().BeTrue();
        entry.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithSystemScope_ShouldHaveNullScopeReference()
    {
        var entry = FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.test",
            scope: ConfigurationScope.System,
            isEnabled: true,
            createdBy: "system");

        entry.Scope.Should().Be(ConfigurationScope.System);
        entry.ScopeReferenceId.Should().BeNull();
    }

    // ── Create — validation ────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyKey_ShouldThrow(string? key)
    {
        var act = () => FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: key!,
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "user-123");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCreatedBy_ShouldThrow(string? createdBy)
    {
        var act = () => FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.test",
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: createdBy!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullDefinitionId_ShouldThrow()
    {
        var act = () => FeatureFlagEntry.Create(
            definitionId: null!,
            key: "feature.test",
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "user-123");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithKeyTooLong_ShouldThrow()
    {
        var act = () => FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: new string('a', 257),
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "user-123");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithChangeReasonTooLong_ShouldThrow()
    {
        var act = () => FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.test",
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "user-123",
            changeReason: new string('a', 501));
        act.Should().Throw<ArgumentException>();
    }

    // ── UpdateValue ────────────────────────────────────────────────────

    [Fact]
    public void UpdateValue_ShouldChangeIsEnabledAndSetUpdatedAt()
    {
        var entry = FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.test",
            scope: ConfigurationScope.Tenant,
            isEnabled: false,
            createdBy: "user-123");

        entry.UpdateValue(isEnabled: true, updatedBy: "admin", changeReason: "Rollout started.");

        entry.IsEnabled.Should().BeTrue();
        entry.UpdatedBy.Should().Be("admin");
        entry.ChangeReason.Should().Be("Rollout started.");
        entry.UpdatedAt.Should().NotBeNull();
        entry.UpdatedAt!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateValue_WithEmptyUpdatedBy_ShouldThrow(string? updatedBy)
    {
        var entry = FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.test",
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "user-123");

        var act = () => entry.UpdateValue(false, updatedBy!);
        act.Should().Throw<ArgumentException>();
    }

    // ── Activate / Deactivate ──────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var entry = FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.test",
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "user-123");

        entry.Deactivate(updatedBy: "admin", changeReason: "Override removed.");

        entry.IsActive.Should().BeFalse();
        entry.UpdatedBy.Should().Be("admin");
        entry.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var entry = FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.test",
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "user-123");

        entry.Deactivate("admin");
        entry.Activate("admin", "Re-enabling.");

        entry.IsActive.Should().BeTrue();
        entry.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Deactivate_WithEmptyUpdatedBy_ShouldThrow(string? updatedBy)
    {
        var entry = FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "feature.test",
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "user-123");

        var act = () => entry.Deactivate(updatedBy!);
        act.Should().Throw<ArgumentException>();
    }

    // ── Key trimming ───────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldTrimKeyAndScopeReference()
    {
        var entry = FeatureFlagEntry.Create(
            definitionId: SampleDefinitionId,
            key: "  feature.test  ",
            scope: ConfigurationScope.Tenant,
            isEnabled: true,
            createdBy: "  user-123  ",
            scopeReferenceId: "  tenant-456  ");

        entry.Key.Should().Be("feature.test");
        entry.CreatedBy.Should().Be("user-123");
        entry.ScopeReferenceId.Should().Be("tenant-456");
    }
}

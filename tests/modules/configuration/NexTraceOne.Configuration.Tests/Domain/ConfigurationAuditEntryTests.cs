using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade ConfigurationAuditEntry.
/// Valida criação, registo de valores anteriores/novos e flag de sensibilidade.
/// </summary>
public sealed class ConfigurationAuditEntryTests
{
    private static readonly ConfigurationEntryId SampleEntryId = new(Guid.NewGuid());

    [Fact]
    public void Create_WithValidParameters_ShouldCreateAuditEntry()
    {
        var audit = ConfigurationAuditEntry.Create(
            entryId: SampleEntryId,
            key: "platform.notifications.enabled",
            scope: ConfigurationScope.System,
            action: "Created",
            newVersion: 1,
            changedBy: "admin@platform.io",
            newValue: "true",
            changeReason: "Initial creation");

        audit.Should().NotBeNull();
        audit.Id.Should().NotBeNull();
        audit.Id.Value.Should().NotBeEmpty();
        audit.EntryId.Should().Be(SampleEntryId);
        audit.Key.Should().Be("platform.notifications.enabled");
        audit.Scope.Should().Be(ConfigurationScope.System);
        audit.ScopeReferenceId.Should().BeNull();
        audit.Action.Should().Be("Created");
        audit.PreviousValue.Should().BeNull();
        audit.NewValue.Should().Be("true");
        audit.PreviousVersion.Should().BeNull();
        audit.NewVersion.Should().Be(1);
        audit.ChangedBy.Should().Be("admin@platform.io");
        audit.ChangedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        audit.ChangeReason.Should().Be("Initial creation");
        audit.IsSensitive.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldRecordPreviousAndNewValues()
    {
        var audit = ConfigurationAuditEntry.Create(
            entryId: SampleEntryId,
            key: "ai.default_temperature",
            scope: ConfigurationScope.Tenant,
            action: "Updated",
            newVersion: 3,
            changedBy: "engineer@team.io",
            scopeReferenceId: "tenant-001",
            previousValue: "0.7",
            newValue: "0.9",
            previousVersion: 2,
            changeReason: "Adjusted temperature for better results");

        audit.PreviousValue.Should().Be("0.7");
        audit.NewValue.Should().Be("0.9");
        audit.PreviousVersion.Should().Be(2);
        audit.NewVersion.Should().Be(3);
        audit.ScopeReferenceId.Should().Be("tenant-001");
        audit.Scope.Should().Be(ConfigurationScope.Tenant);
    }

    [Fact]
    public void Create_ForSensitiveConfig_ShouldMarkAsSensitive()
    {
        var audit = ConfigurationAuditEntry.Create(
            entryId: SampleEntryId,
            key: "integration.webhook_secret",
            scope: ConfigurationScope.System,
            action: "Updated",
            newVersion: 2,
            changedBy: "admin@platform.io",
            previousValue: "old-secret",
            newValue: "new-secret",
            previousVersion: 1,
            isSensitive: true);

        audit.IsSensitive.Should().BeTrue();
    }

    // ── Validation ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyKey_ShouldThrow(string? key)
    {
        var act = () => ConfigurationAuditEntry.Create(
            entryId: SampleEntryId,
            key: key!,
            scope: ConfigurationScope.System,
            action: "Created",
            newVersion: 1,
            changedBy: "admin");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyAction_ShouldThrow(string? action)
    {
        var act = () => ConfigurationAuditEntry.Create(
            entryId: SampleEntryId,
            key: "test.key",
            scope: ConfigurationScope.System,
            action: action!,
            newVersion: 1,
            changedBy: "admin");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyChangedBy_ShouldThrow(string? changedBy)
    {
        var act = () => ConfigurationAuditEntry.Create(
            entryId: SampleEntryId,
            key: "test.key",
            scope: ConfigurationScope.System,
            action: "Created",
            newVersion: 1,
            changedBy: changedBy!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldTrimValues()
    {
        var audit = ConfigurationAuditEntry.Create(
            entryId: SampleEntryId,
            key: "  test.trimmed  ",
            scope: ConfigurationScope.System,
            action: "  Updated  ",
            newVersion: 1,
            changedBy: "  admin  ",
            newValue: "  value  ",
            changeReason: "  reason  ");

        audit.Key.Should().Be("test.trimmed");
        audit.Action.Should().Be("Updated");
        audit.ChangedBy.Should().Be("admin");
        audit.NewValue.Should().Be("value");
        audit.ChangeReason.Should().Be("reason");
    }
}

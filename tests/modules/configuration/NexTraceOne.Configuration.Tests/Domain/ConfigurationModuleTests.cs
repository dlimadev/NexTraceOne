using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade ConfigurationModule.
/// Valida criação, guard clauses, atualização, ativação e desativação.
/// </summary>
public sealed class ConfigurationModuleTests
{
    // ── Create — happy path ────────────────────────────────────────────

    [Fact]
    public void Create_WithValidParameters_ShouldCreateModule()
    {
        // Act
        var module = ConfigurationModule.Create(
            key: "notifications",
            displayName: "Notifications",
            description: "Notification system configuration.",
            sortOrder: 10);

        // Assert
        module.Should().NotBeNull();
        module.Id.Should().NotBeNull();
        module.Id.Value.Should().NotBeEmpty();
        module.Key.Should().Be("notifications");
        module.DisplayName.Should().Be("Notifications");
        module.Description.Should().Be("Notification system configuration.");
        module.SortOrder.Should().Be(10);
        module.IsActive.Should().BeTrue();
        module.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        module.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_KeyShouldBeNormalisedToLowercase()
    {
        var module = ConfigurationModule.Create(key: "Notifications", displayName: "Test");
        module.Key.Should().Be("notifications");
    }

    [Fact]
    public void Create_WithoutOptionalParameters_ShouldUseDefaults()
    {
        var module = ConfigurationModule.Create(key: "ai", displayName: "AI");

        module.Description.Should().BeNull();
        module.SortOrder.Should().Be(0);
        module.IsActive.Should().BeTrue();
    }

    // ── Create — validation ────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyKey_ShouldThrow(string? key)
    {
        var act = () => ConfigurationModule.Create(key: key!, displayName: "Test");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDisplayName_ShouldThrow(string? displayName)
    {
        var act = () => ConfigurationModule.Create(key: "test", displayName: displayName!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithKeyTooLong_ShouldThrow()
    {
        var act = () => ConfigurationModule.Create(key: new string('a', 101), displayName: "Test");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithDisplayNameTooLong_ShouldThrow()
    {
        var act = () => ConfigurationModule.Create(key: "test", displayName: new string('a', 201));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithDescriptionTooLong_ShouldThrow()
    {
        var act = () => ConfigurationModule.Create(
            key: "test",
            displayName: "Test",
            description: new string('a', 501));
        act.Should().Throw<ArgumentException>();
    }

    // ── Update ─────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateMutableFields()
    {
        var module = ConfigurationModule.Create(key: "notifications", displayName: "Notifications", sortOrder: 0);

        module.Update(displayName: "Notifications Module", description: "Updated.", sortOrder: 5);

        module.DisplayName.Should().Be("Notifications Module");
        module.Description.Should().Be("Updated.");
        module.SortOrder.Should().Be(5);
        module.UpdatedAt.Should().NotBeNull();
        module.Key.Should().Be("notifications"); // key immutable
    }

    [Fact]
    public void Update_WithEmptyDisplayName_ShouldThrow()
    {
        var module = ConfigurationModule.Create(key: "test", displayName: "Test");
        var act = () => module.Update(displayName: "", description: null, sortOrder: 0);
        act.Should().Throw<ArgumentException>();
    }

    // ── Activate / Deactivate ──────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var module = ConfigurationModule.Create(key: "test", displayName: "Test");
        module.Deactivate();
        module.IsActive.Should().BeFalse();
        module.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var module = ConfigurationModule.Create(key: "test", displayName: "Test");
        module.Deactivate();
        module.Activate();
        module.IsActive.Should().BeTrue();
    }
}

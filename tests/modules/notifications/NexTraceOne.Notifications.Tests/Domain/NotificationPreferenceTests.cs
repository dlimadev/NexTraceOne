using FluentAssertions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade NotificationPreference.
/// Valida criação e atualização de preferências.
/// </summary>
public sealed class NotificationPreferenceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_ValidParameters_ShouldCreatePreference()
    {
        var preference = NotificationPreference.Create(
            _tenantId, _userId,
            NotificationCategory.Incident, DeliveryChannel.Email, true);

        preference.Should().NotBeNull();
        preference.Id.Value.Should().NotBeEmpty();
        preference.TenantId.Should().Be(_tenantId);
        preference.UserId.Should().Be(_userId);
        preference.Category.Should().Be(NotificationCategory.Incident);
        preference.Channel.Should().Be(DeliveryChannel.Email);
        preference.Enabled.Should().BeTrue();
        preference.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_Disabled_ShouldSetEnabledFalse()
    {
        var preference = NotificationPreference.Create(
            _tenantId, _userId,
            NotificationCategory.FinOps, DeliveryChannel.MicrosoftTeams, false);

        preference.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldChangeEnabledAndTimestamp()
    {
        var preference = NotificationPreference.Create(
            _tenantId, _userId,
            NotificationCategory.Security, DeliveryChannel.Email, true);
        var originalUpdatedAt = preference.UpdatedAt;

        preference.Update(false);

        preference.Enabled.Should().BeFalse();
        preference.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void Update_SameValue_ShouldUpdateTimestamp()
    {
        var preference = NotificationPreference.Create(
            _tenantId, _userId,
            NotificationCategory.Approval, DeliveryChannel.InApp, true);

        preference.Update(true);

        preference.Enabled.Should().BeTrue();
    }

    [Theory]
    [InlineData(NotificationCategory.Incident, DeliveryChannel.Email)]
    [InlineData(NotificationCategory.Approval, DeliveryChannel.MicrosoftTeams)]
    [InlineData(NotificationCategory.Change, DeliveryChannel.InApp)]
    [InlineData(NotificationCategory.Contract, DeliveryChannel.Email)]
    [InlineData(NotificationCategory.Security, DeliveryChannel.MicrosoftTeams)]
    [InlineData(NotificationCategory.Compliance, DeliveryChannel.InApp)]
    [InlineData(NotificationCategory.FinOps, DeliveryChannel.Email)]
    [InlineData(NotificationCategory.AI, DeliveryChannel.InApp)]
    [InlineData(NotificationCategory.Integration, DeliveryChannel.Email)]
    [InlineData(NotificationCategory.Platform, DeliveryChannel.MicrosoftTeams)]
    [InlineData(NotificationCategory.Informational, DeliveryChannel.InApp)]
    public void Create_AllCategoryAndChannelCombinations_ShouldWork(
        NotificationCategory category, DeliveryChannel channel)
    {
        var preference = NotificationPreference.Create(
            _tenantId, _userId, category, channel, true);

        preference.Category.Should().Be(category);
        preference.Channel.Should().Be(channel);
    }
}

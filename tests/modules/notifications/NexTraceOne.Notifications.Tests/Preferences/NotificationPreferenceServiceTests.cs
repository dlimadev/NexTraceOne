using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Preferences;

namespace NexTraceOne.Notifications.Tests.Preferences;

public sealed class NotificationPreferenceServiceTests
{
    private readonly INotificationPreferenceStore _store = Substitute.For<INotificationPreferenceStore>();

    private NotificationPreferenceService CreateService() =>
        new(_store, NullLoggerFactory.Instance.CreateLogger<NotificationPreferenceService>());

    [Fact]
    public async Task IsChannelEnabledAsync_ExplicitPreference_ReturnsPreferenceValue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(
            Guid.NewGuid(), userId, NotificationCategory.Incident, DeliveryChannel.Email, true);

        _store.GetAsync(userId, NotificationCategory.Incident, DeliveryChannel.Email, Arg.Any<CancellationToken>())
            .Returns(preference);

        var service = CreateService();

        // Act
        var result = await service.IsChannelEnabledAsync(
            userId, NotificationCategory.Incident, DeliveryChannel.Email, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsChannelEnabledAsync_ExplicitPreferenceDisabled_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(
            Guid.NewGuid(), userId, NotificationCategory.Incident, DeliveryChannel.Email, false);

        _store.GetAsync(userId, NotificationCategory.Incident, DeliveryChannel.Email, Arg.Any<CancellationToken>())
            .Returns(preference);

        var service = CreateService();

        // Act
        var result = await service.IsChannelEnabledAsync(
            userId, NotificationCategory.Incident, DeliveryChannel.Email, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(DeliveryChannel.InApp, true)]
    [InlineData(DeliveryChannel.Email, true)]
    [InlineData(DeliveryChannel.MicrosoftTeams, true)]
    public async Task IsChannelEnabledAsync_NoPreference_ReturnsSystemDefault(
        DeliveryChannel channel, bool expectedDefault)
    {
        // Arrange
        var userId = Guid.NewGuid();

        _store.GetAsync(userId, NotificationCategory.Incident, channel, Arg.Any<CancellationToken>())
            .Returns((NotificationPreference?)null);

        var service = CreateService();

        // Act
        var result = await service.IsChannelEnabledAsync(
            userId, NotificationCategory.Incident, channel, CancellationToken.None);

        // Assert
        result.Should().Be(expectedDefault);
    }

    [Fact]
    public async Task UpdatePreferenceAsync_NewPreference_CreatesAndSaves()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _store.GetAsync(userId, NotificationCategory.Change, DeliveryChannel.Email, Arg.Any<CancellationToken>())
            .Returns((NotificationPreference?)null);

        var service = CreateService();

        // Act
        await service.UpdatePreferenceAsync(
            tenantId, userId, NotificationCategory.Change, DeliveryChannel.Email, true, CancellationToken.None);

        // Assert
        await _store.Received(1).AddAsync(
            Arg.Is<NotificationPreference>(p =>
                p.UserId == userId &&
                p.Category == NotificationCategory.Change &&
                p.Channel == DeliveryChannel.Email &&
                p.Enabled),
            Arg.Any<CancellationToken>());

        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePreferenceAsync_ExistingPreference_UpdatesAndSaves()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existing = NotificationPreference.Create(
            tenantId, userId, NotificationCategory.Incident, DeliveryChannel.InApp, true);

        _store.GetAsync(userId, NotificationCategory.Incident, DeliveryChannel.InApp, Arg.Any<CancellationToken>())
            .Returns(existing);

        var service = CreateService();

        // Act
        await service.UpdatePreferenceAsync(
            tenantId, userId, NotificationCategory.Incident, DeliveryChannel.InApp, false, CancellationToken.None);

        // Assert
        existing.Enabled.Should().BeFalse();
        await _store.DidNotReceive().AddAsync(Arg.Any<NotificationPreference>(), Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPreferencesAsync_ReturnsUserPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = new List<NotificationPreference>
        {
            NotificationPreference.Create(Guid.NewGuid(), userId, NotificationCategory.Incident, DeliveryChannel.InApp, true),
            NotificationPreference.Create(Guid.NewGuid(), userId, NotificationCategory.Security, DeliveryChannel.Email, false),
        };

        _store.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(preferences.AsReadOnly());

        var service = CreateService();

        // Act
        var result = await service.GetPreferencesAsync(userId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(preferences);
    }
}

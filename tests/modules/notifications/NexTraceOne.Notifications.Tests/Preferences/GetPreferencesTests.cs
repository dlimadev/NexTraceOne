using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.GetPreferences;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Preferences;

public sealed class GetPreferencesTests
{
    private readonly INotificationPreferenceService _preferenceService = Substitute.For<INotificationPreferenceService>();
    private readonly IMandatoryNotificationPolicy _mandatoryPolicy = Substitute.For<IMandatoryNotificationPolicy>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetPreferences.Handler CreateHandler() =>
        new(_preferenceService, _mandatoryPolicy, _currentUser);

    [Fact]
    public async Task Handle_ValidUser_ReturnsAllCategoryChannelCombinations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        _preferenceService.GetPreferencesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<NotificationPreference>().AsReadOnly());

        _mandatoryPolicy.GetMandatoryChannels(
                Arg.Any<string>(), Arg.Any<NotificationCategory>(), NotificationSeverity.Critical)
            .Returns(new List<DeliveryChannel>());

        _preferenceService.IsChannelEnabledAsync(
                userId, Arg.Any<NotificationCategory>(), Arg.Any<DeliveryChannel>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = CreateHandler();
        var query = new GetPreferences.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var expectedCount = Enum.GetValues<NotificationCategory>().Length
                          * Enum.GetValues<DeliveryChannel>().Length;

        result.Value.Preferences.Should().HaveCount(expectedCount);
    }

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        _currentUser.Id.Returns("invalid");

        var handler = CreateHandler();
        var query = new GetPreferences.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Notification.InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithExplicitPreference_ReflectsInResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        var explicitPref = NotificationPreference.Create(
            tenantId, userId, NotificationCategory.Incident, DeliveryChannel.Email, false);

        _preferenceService.GetPreferencesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<NotificationPreference> { explicitPref }.AsReadOnly());

        _mandatoryPolicy.GetMandatoryChannels(
                Arg.Any<string>(), Arg.Any<NotificationCategory>(), NotificationSeverity.Critical)
            .Returns(new List<DeliveryChannel>());

        _preferenceService.IsChannelEnabledAsync(
                userId, Arg.Any<NotificationCategory>(), Arg.Any<DeliveryChannel>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = CreateHandler();
        var query = new GetPreferences.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var incidentEmail = result.Value.Preferences
            .Single(p => p.Category == "Incident" && p.Channel == "Email");

        incidentEmail.Enabled.Should().BeFalse();
        incidentEmail.UpdatedAt.Should().NotBeNull();
    }
}

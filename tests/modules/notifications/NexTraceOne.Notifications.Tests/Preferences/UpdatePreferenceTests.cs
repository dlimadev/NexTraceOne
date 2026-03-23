using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.UpdatePreference;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Preferences;

public sealed class UpdatePreferenceTests
{
    private readonly INotificationPreferenceService _preferenceService = Substitute.For<INotificationPreferenceService>();
    private readonly IMandatoryNotificationPolicy _mandatoryPolicy = Substitute.For<IMandatoryNotificationPolicy>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();

    private UpdatePreference.Handler CreateHandler() =>
        new(_preferenceService, _mandatoryPolicy, _currentUser, _currentTenant);

    [Fact]
    public async Task Handle_ValidPreference_UpdatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());
        _currentTenant.Id.Returns(tenantId);

        _mandatoryPolicy.GetMandatoryChannels(
                Arg.Any<string>(), Arg.Any<NotificationCategory>(), NotificationSeverity.Critical)
            .Returns(new List<DeliveryChannel>());

        var handler = CreateHandler();
        var command = new UpdatePreference.Command("Incident", "Email", Enabled: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();

        await _preferenceService.Received(1).UpdatePreferenceAsync(
            tenantId, userId,
            NotificationCategory.Incident, DeliveryChannel.Email,
            true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DisableMandatoryChannel_ReturnsValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());
        _currentTenant.Id.Returns(tenantId);

        _mandatoryPolicy.GetMandatoryChannels(
                Arg.Any<string>(), Arg.Any<NotificationCategory>(), NotificationSeverity.Critical)
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email]);

        var handler = CreateHandler();
        var command = new UpdatePreference.Command("Incident", "InApp", Enabled: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Notification.MandatoryChannel");

        await _preferenceService.DidNotReceive().UpdatePreferenceAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<NotificationCategory>(), Arg.Any<DeliveryChannel>(),
            Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        _currentUser.Id.Returns("not-a-guid");

        var handler = CreateHandler();
        var command = new UpdatePreference.Command("Incident", "Email", Enabled: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Notification.InvalidUserId");
    }

    [Fact]
    public async Task Handle_EnableNonMandatory_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());
        _currentTenant.Id.Returns(tenantId);

        _mandatoryPolicy.GetMandatoryChannels(
                Arg.Any<string>(), Arg.Any<NotificationCategory>(), NotificationSeverity.Critical)
            .Returns([DeliveryChannel.InApp]);

        var handler = CreateHandler();
        var command = new UpdatePreference.Command("Integration", "MicrosoftTeams", Enabled: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();

        await _preferenceService.Received(1).UpdatePreferenceAsync(
            tenantId, userId,
            NotificationCategory.Integration, DeliveryChannel.MicrosoftTeams,
            true, Arg.Any<CancellationToken>());
    }
}

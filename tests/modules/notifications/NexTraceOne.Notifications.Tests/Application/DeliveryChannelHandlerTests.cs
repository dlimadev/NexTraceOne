using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.ListDeliveryChannels;
using NexTraceOne.Notifications.Application.Features.UpsertDeliveryChannel;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Tests.Application;

/// <summary>
/// Testes de unidade para os handlers de canais de entrega:
/// ListDeliveryChannels e UpsertDeliveryChannel.
/// </summary>
public sealed class DeliveryChannelHandlerTests
{
    private readonly IDeliveryChannelConfigurationStore _store =
        Substitute.For<IDeliveryChannelConfigurationStore>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeliveryChannelHandlerTests()
    {
        _tenant.Id.Returns(_tenantId);
    }

    // ── ListDeliveryChannels ──────────────────────────────────────────────

    [Fact]
    public async Task ListDeliveryChannels_NoChannels_ReturnsEmptyList()
    {
        _store.ListAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<DeliveryChannelConfiguration>().AsReadOnly());

        var handler = new ListDeliveryChannels.Handler(_store, _tenant);
        var result = await handler.Handle(new ListDeliveryChannels.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListDeliveryChannels_WithChannels_ReturnsMappedDtos()
    {
        var emailChannel = DeliveryChannelConfiguration.Create(
            _tenantId, DeliveryChannel.Email, "Email Alerts", true, "{\"host\":\"smtp.test\"}");
        var teamsChannel = DeliveryChannelConfiguration.Create(
            _tenantId, DeliveryChannel.MicrosoftTeams, "Teams Webhook", false);

        _store.ListAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<DeliveryChannelConfiguration> { emailChannel, teamsChannel }.AsReadOnly());

        var handler = new ListDeliveryChannels.Handler(_store, _tenant);
        var result = await handler.Handle(new ListDeliveryChannels.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);

        var emailDto = result.Value.Items.First(c => c.ChannelType == "Email");
        emailDto.DisplayName.Should().Be("Email Alerts");
        emailDto.IsEnabled.Should().BeTrue();
        emailDto.ConfigurationJson.Should().Contain("smtp.test");

        var teamsDto = result.Value.Items.First(c => c.ChannelType == "MicrosoftTeams");
        teamsDto.IsEnabled.Should().BeFalse();
    }

    // ── UpsertDeliveryChannel (Create) ────────────────────────────────────

    [Fact]
    public async Task UpsertDeliveryChannel_Create_WhenNoExisting_AddsNewConfiguration()
    {
        _store.GetByChannelTypeAsync(_tenantId, DeliveryChannel.Email, Arg.Any<CancellationToken>())
            .Returns((DeliveryChannelConfiguration?)null);

        var handler = new UpsertDeliveryChannel.Handler(_store, _tenant);
        var command = new UpsertDeliveryChannel.Command(
            Id: null,
            ChannelType: "Email",
            DisplayName: "Email Channel",
            IsEnabled: true,
            ConfigurationJson: "{\"host\":\"smtp.corp.com\"}");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeTrue();
        await _store.Received(1).AddAsync(Arg.Any<DeliveryChannelConfiguration>(), Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertDeliveryChannel_Create_WhenExistsByType_UpdatesAndReturnsFalse()
    {
        var existing = DeliveryChannelConfiguration.Create(
            _tenantId, DeliveryChannel.Email, "Old Email", false);

        _store.GetByChannelTypeAsync(_tenantId, DeliveryChannel.Email, Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new UpsertDeliveryChannel.Handler(_store, _tenant);
        var command = new UpsertDeliveryChannel.Command(
            Id: null,
            ChannelType: "Email",
            DisplayName: "Updated Email",
            IsEnabled: true,
            ConfigurationJson: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeFalse();
        existing.DisplayName.Should().Be("Updated Email");
        existing.IsEnabled.Should().BeTrue();
        await _store.DidNotReceive().AddAsync(Arg.Any<DeliveryChannelConfiguration>(), Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── UpsertDeliveryChannel (Update by Id) ─────────────────────────────

    [Fact]
    public async Task UpsertDeliveryChannel_UpdateById_WhenFound_UpdatesSuccessfully()
    {
        var existing = DeliveryChannelConfiguration.Create(
            _tenantId, DeliveryChannel.MicrosoftTeams, "Teams Channel", true);

        _store.GetByIdAsync(
                Arg.Is<DeliveryChannelConfigurationId>(i => i.Value == existing.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new UpsertDeliveryChannel.Handler(_store, _tenant);
        var command = new UpsertDeliveryChannel.Command(
            Id: existing.Id.Value,
            ChannelType: "MicrosoftTeams",
            DisplayName: "Teams Updated",
            IsEnabled: false,
            ConfigurationJson: "{\"webhook\":\"https://teams.webhook.url\"}");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeFalse();
        existing.DisplayName.Should().Be("Teams Updated");
        existing.IsEnabled.Should().BeFalse();
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertDeliveryChannel_UpdateById_WhenNotFound_ReturnsNotFound()
    {
        _store.GetByIdAsync(Arg.Any<DeliveryChannelConfigurationId>(), Arg.Any<CancellationToken>())
            .Returns((DeliveryChannelConfiguration?)null);

        var handler = new UpsertDeliveryChannel.Handler(_store, _tenant);
        var command = new UpsertDeliveryChannel.Command(
            Id: Guid.NewGuid(),
            ChannelType: "Email",
            DisplayName: "Email",
            IsEnabled: false,
            ConfigurationJson: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(NexTraceOne.BuildingBlocks.Core.Results.ErrorType.NotFound);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertDeliveryChannel_UpdateById_WrongTenant_ReturnsForbidden()
    {
        var otherTenantId = Guid.NewGuid();
        var existing = DeliveryChannelConfiguration.Create(
            otherTenantId, DeliveryChannel.Email, "Email Channel", true);

        _store.GetByIdAsync(
                Arg.Is<DeliveryChannelConfigurationId>(i => i.Value == existing.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new UpsertDeliveryChannel.Handler(_store, _tenant);
        var command = new UpsertDeliveryChannel.Command(
            Id: existing.Id.Value,
            ChannelType: "Email",
            DisplayName: "Hacked",
            IsEnabled: false,
            ConfigurationJson: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(NexTraceOne.BuildingBlocks.Core.Results.ErrorType.Forbidden);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void UpsertDeliveryChannel_Validator_InvalidChannelType_ShouldFail()
    {
        var validator = new UpsertDeliveryChannel.Validator();
        var command = new UpsertDeliveryChannel.Command(
            Id: null,
            ChannelType: "InvalidChannel",
            DisplayName: "Some Channel",
            IsEnabled: true,
            ConfigurationJson: null);

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpsertDeliveryChannel_Validator_EmptyDisplayName_ShouldFail()
    {
        var validator = new UpsertDeliveryChannel.Validator();
        var command = new UpsertDeliveryChannel.Command(
            Id: null,
            ChannelType: "Email",
            DisplayName: string.Empty,
            IsEnabled: true,
            ConfigurationJson: null);

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}

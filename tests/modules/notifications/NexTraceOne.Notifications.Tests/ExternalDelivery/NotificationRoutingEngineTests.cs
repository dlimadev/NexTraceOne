using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;
using NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

namespace NexTraceOne.Notifications.Tests.ExternalDelivery;

public sealed class NotificationRoutingEngineTests
{
    private readonly ILogger<NotificationRoutingEngine> _logger =
        NullLoggerFactory.Instance.CreateLogger<NotificationRoutingEngine>();

    private readonly INotificationPreferenceService _preferenceService = Substitute.For<INotificationPreferenceService>();
    private readonly IMandatoryNotificationPolicy _mandatoryPolicy = Substitute.For<IMandatoryNotificationPolicy>();

    private NotificationRoutingEngine CreateEngine(bool emailEnabled = true, bool teamsEnabled = true)
    {
        var options = Options.Create(new NotificationChannelOptions
        {
            Email = new EmailChannelSettings { Enabled = emailEnabled },
            Teams = new TeamsChannelSettings { Enabled = teamsEnabled }
        });

        // Default: no mandatory channels, all user preferences enabled
        _mandatoryPolicy
            .GetMandatoryChannels(Arg.Any<string>(), Arg.Any<NotificationCategory>(), Arg.Any<NotificationSeverity>())
            .Returns([]);

        _preferenceService
            .IsChannelEnabledAsync(Arg.Any<Guid>(), Arg.Any<NotificationCategory>(), Arg.Any<DeliveryChannel>(), Arg.Any<CancellationToken>())
            .Returns(true);

        return new NotificationRoutingEngine(options, _preferenceService, _mandatoryPolicy, _logger);
    }

    [Fact]
    public async Task ResolveChannelsAsync_Info_ReturnsOnlyInApp()
    {
        var engine = CreateEngine();
        var channels = await engine.ResolveChannelsAsync(
            Guid.NewGuid(), NotificationCategory.Informational, NotificationSeverity.Info);

        channels.Should().ContainSingle()
            .Which.Should().Be(DeliveryChannel.InApp);
    }

    [Fact]
    public async Task ResolveChannelsAsync_ActionRequired_ReturnsInAppAndEmail()
    {
        var engine = CreateEngine();
        var channels = await engine.ResolveChannelsAsync(
            Guid.NewGuid(), NotificationCategory.Approval, NotificationSeverity.ActionRequired);

        channels.Should().HaveCount(2);
        channels.Should().Contain(DeliveryChannel.InApp);
        channels.Should().Contain(DeliveryChannel.Email);
    }

    [Fact]
    public async Task ResolveChannelsAsync_Warning_ReturnsAllChannels()
    {
        var engine = CreateEngine();
        var channels = await engine.ResolveChannelsAsync(
            Guid.NewGuid(), NotificationCategory.Compliance, NotificationSeverity.Warning);

        channels.Should().HaveCount(3);
        channels.Should().Contain(DeliveryChannel.InApp);
        channels.Should().Contain(DeliveryChannel.Email);
        channels.Should().Contain(DeliveryChannel.MicrosoftTeams);
    }

    [Fact]
    public async Task ResolveChannelsAsync_Critical_ReturnsAllChannels()
    {
        var engine = CreateEngine();
        var channels = await engine.ResolveChannelsAsync(
            Guid.NewGuid(), NotificationCategory.Incident, NotificationSeverity.Critical);

        channels.Should().HaveCount(3);
        channels.Should().Contain(DeliveryChannel.InApp);
        channels.Should().Contain(DeliveryChannel.Email);
        channels.Should().Contain(DeliveryChannel.MicrosoftTeams);
    }

    [Fact]
    public async Task ResolveChannelsAsync_EmailDisabled_ExcludesEmail()
    {
        var engine = CreateEngine(emailEnabled: false, teamsEnabled: true);
        var channels = await engine.ResolveChannelsAsync(
            Guid.NewGuid(), NotificationCategory.Incident, NotificationSeverity.Critical);

        channels.Should().HaveCount(2);
        channels.Should().Contain(DeliveryChannel.InApp);
        channels.Should().Contain(DeliveryChannel.MicrosoftTeams);
        channels.Should().NotContain(DeliveryChannel.Email);
    }

    [Fact]
    public async Task ResolveChannelsAsync_TeamsDisabled_ExcludesTeams()
    {
        var engine = CreateEngine(emailEnabled: true, teamsEnabled: false);
        var channels = await engine.ResolveChannelsAsync(
            Guid.NewGuid(), NotificationCategory.Incident, NotificationSeverity.Critical);

        channels.Should().HaveCount(2);
        channels.Should().Contain(DeliveryChannel.InApp);
        channels.Should().Contain(DeliveryChannel.Email);
        channels.Should().NotContain(DeliveryChannel.MicrosoftTeams);
    }

    [Fact]
    public async Task ResolveChannelsAsync_AllDisabled_ReturnsOnlyInApp()
    {
        var engine = CreateEngine(emailEnabled: false, teamsEnabled: false);
        var channels = await engine.ResolveChannelsAsync(
            Guid.NewGuid(), NotificationCategory.Incident, NotificationSeverity.Critical);

        channels.Should().ContainSingle()
            .Which.Should().Be(DeliveryChannel.InApp);
    }

    [Fact]
    public async Task ResolveChannelsAsync_ActionRequired_EmailDisabled_OnlyInApp()
    {
        var engine = CreateEngine(emailEnabled: false, teamsEnabled: true);
        var channels = await engine.ResolveChannelsAsync(
            Guid.NewGuid(), NotificationCategory.Approval, NotificationSeverity.ActionRequired);

        channels.Should().ContainSingle()
            .Which.Should().Be(DeliveryChannel.InApp);
    }

    [Fact]
    public async Task ResolveChannelsAsync_MandatoryChannels_AlwaysIncluded()
    {
        var options = Options.Create(new NotificationChannelOptions
        {
            Email = new EmailChannelSettings { Enabled = true },
            Teams = new TeamsChannelSettings { Enabled = true }
        });

        var mandatoryPolicy = Substitute.For<IMandatoryNotificationPolicy>();
        mandatoryPolicy
            .GetMandatoryChannels(Arg.Any<string>(), Arg.Any<NotificationCategory>(), Arg.Any<NotificationSeverity>())
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email, DeliveryChannel.MicrosoftTeams]);

        var prefService = Substitute.For<INotificationPreferenceService>();
        prefService
            .IsChannelEnabledAsync(Arg.Any<Guid>(), Arg.Any<NotificationCategory>(), Arg.Any<DeliveryChannel>(), Arg.Any<CancellationToken>())
            .Returns(false); // user disabled everything

        var engine = new NotificationRoutingEngine(options, prefService, mandatoryPolicy, _logger);
        var channels = await engine.ResolveChannelsAsync(
            Guid.NewGuid(), NotificationCategory.Security, NotificationSeverity.Critical);

        channels.Should().HaveCount(3);
        channels.Should().Contain(DeliveryChannel.InApp);
        channels.Should().Contain(DeliveryChannel.Email);
        channels.Should().Contain(DeliveryChannel.MicrosoftTeams);
    }

    [Fact]
    public async Task ResolveChannelsAsync_UserDisabledChannel_ExcludesNonMandatory()
    {
        var options = Options.Create(new NotificationChannelOptions
        {
            Email = new EmailChannelSettings { Enabled = true },
            Teams = new TeamsChannelSettings { Enabled = true }
        });

        var mandatoryPolicy = Substitute.For<IMandatoryNotificationPolicy>();
        mandatoryPolicy
            .GetMandatoryChannels(Arg.Any<string>(), Arg.Any<NotificationCategory>(), Arg.Any<NotificationSeverity>())
            .Returns([]);

        var prefService = Substitute.For<INotificationPreferenceService>();
        prefService
            .IsChannelEnabledAsync(Arg.Any<Guid>(), Arg.Any<NotificationCategory>(), DeliveryChannel.Email, Arg.Any<CancellationToken>())
            .Returns(false);
        prefService
            .IsChannelEnabledAsync(Arg.Any<Guid>(), Arg.Any<NotificationCategory>(), DeliveryChannel.MicrosoftTeams, Arg.Any<CancellationToken>())
            .Returns(true);

        var engine = new NotificationRoutingEngine(options, prefService, mandatoryPolicy, _logger);
        var channels = await engine.ResolveChannelsAsync(
            Guid.NewGuid(), NotificationCategory.Incident, NotificationSeverity.Warning);

        channels.Should().HaveCount(2);
        channels.Should().Contain(DeliveryChannel.InApp);
        channels.Should().Contain(DeliveryChannel.MicrosoftTeams);
        channels.Should().NotContain(DeliveryChannel.Email);
    }
}

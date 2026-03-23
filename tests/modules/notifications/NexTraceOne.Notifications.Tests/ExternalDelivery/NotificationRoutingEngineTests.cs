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

    private NotificationRoutingEngine CreateEngine(bool emailEnabled = true, bool teamsEnabled = true)
    {
        var options = Options.Create(new NotificationChannelOptions
        {
            Email = new EmailChannelSettings { Enabled = emailEnabled },
            Teams = new TeamsChannelSettings { Enabled = teamsEnabled }
        });
        return new NotificationRoutingEngine(options, _logger);
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
}

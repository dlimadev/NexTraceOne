using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Governance;
using NexTraceOne.Notifications.Infrastructure.Preferences;

namespace NexTraceOne.Notifications.Tests.Governance;

/// <summary>
/// Testes para o NotificationCatalogGovernance da Fase 7.
/// Valida governança do catálogo: tipos registados, templates, obrigatoriedade, validação.
/// </summary>
public sealed class NotificationCatalogGovernanceTests
{
    private readonly NotificationCatalogGovernance _governance;

    public NotificationCatalogGovernanceTests()
    {
        var templateResolver = new NexTraceOne.Notifications.Application.Engine.NotificationTemplateResolver();
        var mandatoryPolicy = new MandatoryNotificationPolicy();
        var logger = NullLoggerFactory.Instance.CreateLogger<NotificationCatalogGovernance>();
        _governance = new NotificationCatalogGovernance(templateResolver, mandatoryPolicy, logger);
    }

    [Fact]
    public async Task GetGovernanceSummary_ShouldReturnAllRegisteredTypes()
    {
        var summary = await _governance.GetGovernanceSummaryAsync();

        summary.TotalEventTypes.Should().Be(NotificationType.All.Count);
        summary.TotalEventTypes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetGovernanceSummary_ShouldHaveTypesWithTemplates()
    {
        var summary = await _governance.GetGovernanceSummaryAsync();

        summary.TypesWithTemplate.Should().BeGreaterThan(0);
        summary.TypesWithTemplate.Should().BeLessThan(summary.TotalEventTypes + 1);
    }

    [Fact]
    public async Task GetGovernanceSummary_ShouldIdentifyGaps()
    {
        var summary = await _governance.GetGovernanceSummaryAsync();

        // Some types don't have dedicated templates — they should be reported
        summary.TypesWithoutTemplate.Should().NotBeNull();
        (summary.TypesWithTemplate + summary.TypesWithoutTemplate.Count).Should().Be(summary.TotalEventTypes);
    }

    [Fact]
    public async Task GetGovernanceSummary_ShouldHaveMandatoryTypes()
    {
        var summary = await _governance.GetGovernanceSummaryAsync();

        summary.MandatoryTypes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetGovernanceSummary_ShouldReportChannelStatus()
    {
        var summary = await _governance.GetGovernanceSummaryAsync();

        summary.ChannelStatus.Should().ContainKey("InApp");
        summary.ChannelStatus.Should().ContainKey("Email");
        summary.ChannelStatus.Should().ContainKey("MicrosoftTeams");
    }

    [Fact]
    public async Task GetGovernanceSummary_ShouldReportCategories()
    {
        var summary = await _governance.GetGovernanceSummaryAsync();

        summary.TotalCategories.Should().Be(Enum.GetValues<NotificationCategory>().Length);
    }

    [Fact]
    public async Task ValidateEventType_RegisteredWithTemplate_ShouldBeValid()
    {
        var result = await _governance.ValidateEventTypeAsync(NotificationType.IncidentCreated);

        result.IsValid.Should().BeTrue();
        result.HasTemplate.Should().BeTrue();
        result.EventType.Should().Be(NotificationType.IncidentCreated);
    }

    [Fact]
    public async Task ValidateEventType_RegisteredWithoutTemplate_ShouldReportGap()
    {
        // IncidentResolved is registered but has no dedicated template
        var result = await _governance.ValidateEventTypeAsync(NotificationType.IncidentResolved);

        result.HasTemplate.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("dedicated template"));
    }

    [Fact]
    public async Task ValidateEventType_UnregisteredType_ShouldBeInvalid()
    {
        var result = await _governance.ValidateEventTypeAsync("NonExistentEventType");

        result.IsValid.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Contains("not registered"));
    }

    [Fact]
    public async Task ValidateEventType_MandatoryType_ShouldIndicateFlag()
    {
        var result = await _governance.ValidateEventTypeAsync(NotificationType.BreakGlassActivated);

        result.IsMandatory.Should().BeTrue();
        result.HasTemplate.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEventType_AllCatalogTypes_ShouldBeRegistered()
    {
        foreach (var eventType in NotificationType.All)
        {
            var result = await _governance.ValidateEventTypeAsync(eventType);
            result.EventType.Should().Be(eventType);
            result.Messages.Should().NotContain(m => m.Contains("not registered"),
                because: $"{eventType} is in the catalog and should be registered");
        }
    }
}

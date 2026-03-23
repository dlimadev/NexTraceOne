using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

namespace NexTraceOne.Notifications.Tests.ExternalDelivery;

public sealed class ExternalChannelTemplateResolverTests
{
    private readonly ExternalChannelTemplateResolver _resolver = new();
    private const string BaseUrl = "https://app.nextraceone.com";

    private static Notification CreateTestNotification(
        NotificationSeverity severity = NotificationSeverity.Critical,
        NotificationCategory category = NotificationCategory.Incident,
        string title = "Incident created — payments-api",
        string message = "A new critical incident has been created.",
        string sourceModule = "OperationalIntelligence",
        string? actionUrl = "/incidents/123",
        bool requiresAction = true)
    {
        return Notification.Create(
            tenantId: Guid.NewGuid(),
            recipientUserId: Guid.NewGuid(),
            eventType: "IncidentCreated",
            category: category,
            severity: severity,
            title: title,
            message: message,
            sourceModule: sourceModule,
            sourceEntityType: "Incident",
            sourceEntityId: Guid.NewGuid().ToString(),
            actionUrl: actionUrl,
            requiresAction: requiresAction);
    }

    // ── Email Template Tests ──

    [Fact]
    public void ResolveEmailTemplate_ReturnsNonEmptySubject()
    {
        var notification = CreateTestNotification();
        var template = _resolver.ResolveEmailTemplate(notification, BaseUrl);

        template.Subject.Should().NotBeNullOrWhiteSpace();
        template.Subject.Should().Contain("NexTraceOne");
        template.Subject.Should().Contain("Critical");
        template.Subject.Should().Contain("payments-api");
    }

    [Fact]
    public void ResolveEmailTemplate_ReturnsHtmlBody()
    {
        var notification = CreateTestNotification();
        var template = _resolver.ResolveEmailTemplate(notification, BaseUrl);

        template.HtmlBody.Should().NotBeNullOrWhiteSpace();
        template.HtmlBody.Should().Contain("<!DOCTYPE html>");
        template.HtmlBody.Should().Contain("NexTraceOne");
        template.HtmlBody.Should().Contain("payments-api");
    }

    [Fact]
    public void ResolveEmailTemplate_ReturnsPlainTextBody()
    {
        var notification = CreateTestNotification();
        var template = _resolver.ResolveEmailTemplate(notification, BaseUrl);

        template.PlainTextBody.Should().NotBeNullOrWhiteSpace();
        template.PlainTextBody.Should().Contain("payments-api");
        template.PlainTextBody.Should().Contain("Critical");
    }

    [Fact]
    public void ResolveEmailTemplate_IncludesDeepLink()
    {
        var notification = CreateTestNotification(actionUrl: "/incidents/abc123");
        var template = _resolver.ResolveEmailTemplate(notification, BaseUrl);

        template.HtmlBody.Should().Contain("https://app.nextraceone.com/incidents/abc123");
    }

    [Fact]
    public void ResolveEmailTemplate_WithoutActionUrl_FallsBackToNotificationsPage()
    {
        var notification = CreateTestNotification(actionUrl: null);
        var template = _resolver.ResolveEmailTemplate(notification, BaseUrl);

        template.HtmlBody.Should().Contain("https://app.nextraceone.com/notifications");
    }

    [Fact]
    public void ResolveEmailTemplate_ActionRequired_ShowsTakeActionButton()
    {
        var notification = CreateTestNotification(requiresAction: true);
        var template = _resolver.ResolveEmailTemplate(notification, BaseUrl);

        template.HtmlBody.Should().Contain("Take Action");
    }

    [Fact]
    public void ResolveEmailTemplate_NoActionRequired_ShowsViewDetailsButton()
    {
        var notification = CreateTestNotification(requiresAction: false);
        var template = _resolver.ResolveEmailTemplate(notification, BaseUrl);

        template.HtmlBody.Should().Contain("View Details");
    }

    [Theory]
    [InlineData(NotificationSeverity.Critical)]
    [InlineData(NotificationSeverity.Warning)]
    [InlineData(NotificationSeverity.ActionRequired)]
    [InlineData(NotificationSeverity.Info)]
    public void ResolveEmailTemplate_AllSeverities_ReturnValidTemplate(NotificationSeverity severity)
    {
        var notification = CreateTestNotification(severity: severity);
        var template = _resolver.ResolveEmailTemplate(notification, BaseUrl);

        template.Subject.Should().NotBeNullOrWhiteSpace();
        template.HtmlBody.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ResolveEmailTemplate_IncludesSourceModuleAndCategory()
    {
        var notification = CreateTestNotification(
            sourceModule: "Governance",
            category: NotificationCategory.Compliance);
        var template = _resolver.ResolveEmailTemplate(notification, BaseUrl);

        template.HtmlBody.Should().Contain("Governance");
        template.HtmlBody.Should().Contain("Compliance");
    }

    // ── Teams Template Tests ──

    [Fact]
    public void ResolveTeamsTemplate_ReturnsValidJson()
    {
        var notification = CreateTestNotification();
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        template.JsonPayload.Should().NotBeNullOrWhiteSpace();

        // Should be valid JSON
        var act = () => System.Text.Json.JsonDocument.Parse(template.JsonPayload);
        act.Should().NotThrow();
    }

    [Fact]
    public void ResolveTeamsTemplate_ContainsAdaptiveCard()
    {
        var notification = CreateTestNotification();
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        template.JsonPayload.Should().Contain("AdaptiveCard");
        template.JsonPayload.Should().Contain("application/vnd.microsoft.card.adaptive");
    }

    [Fact]
    public void ResolveTeamsTemplate_ContainsTitle()
    {
        var notification = CreateTestNotification(title: "Incident created — payments-api");
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        template.JsonPayload.Should().Contain("payments-api");
    }

    [Fact]
    public void ResolveTeamsTemplate_ContainsMessage()
    {
        var notification = CreateTestNotification(message: "A critical incident has been detected");
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        template.JsonPayload.Should().Contain("A critical incident has been detected");
    }

    [Fact]
    public void ResolveTeamsTemplate_ContainsActionUrl()
    {
        var notification = CreateTestNotification(actionUrl: "/incidents/test-123");
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        template.JsonPayload.Should().Contain("https://app.nextraceone.com/incidents/test-123");
    }

    [Fact]
    public void ResolveTeamsTemplate_ContainsSeverityEmoji_Critical()
    {
        var notification = CreateTestNotification(severity: NotificationSeverity.Critical);
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        // JSON serializer escapes emojis as unicode escapes, verify via deserialization
        var doc = System.Text.Json.JsonDocument.Parse(template.JsonPayload);
        var body = doc.RootElement.GetProperty("attachments")[0]
            .GetProperty("content").GetProperty("body");
        var titleBlock = body[0].GetProperty("text").GetString()!;
        titleBlock.Should().Contain("🔴");
    }

    [Fact]
    public void ResolveTeamsTemplate_ContainsSeverityEmoji_Warning()
    {
        var notification = CreateTestNotification(severity: NotificationSeverity.Warning);
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        var doc = System.Text.Json.JsonDocument.Parse(template.JsonPayload);
        var body = doc.RootElement.GetProperty("attachments")[0]
            .GetProperty("content").GetProperty("body");
        var titleBlock = body[0].GetProperty("text").GetString()!;
        titleBlock.Should().Contain("🟠");
    }

    [Theory]
    [InlineData(NotificationSeverity.Critical)]
    [InlineData(NotificationSeverity.Warning)]
    [InlineData(NotificationSeverity.ActionRequired)]
    [InlineData(NotificationSeverity.Info)]
    public void ResolveTeamsTemplate_AllSeverities_ReturnValidPayload(NotificationSeverity severity)
    {
        var notification = CreateTestNotification(severity: severity);
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        template.JsonPayload.Should().NotBeNullOrWhiteSpace();
        var act = () => System.Text.Json.JsonDocument.Parse(template.JsonPayload);
        act.Should().NotThrow();
    }

    [Fact]
    public void ResolveTeamsTemplate_ContainsSourceModule()
    {
        var notification = CreateTestNotification(sourceModule: "OperationalIntelligence");
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        template.JsonPayload.Should().Contain("OperationalIntelligence");
    }

    [Fact]
    public void ResolveTeamsTemplate_ActionRequired_TakeActionButton()
    {
        var notification = CreateTestNotification(requiresAction: true);
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        template.JsonPayload.Should().Contain("Take Action");
    }

    [Fact]
    public void ResolveTeamsTemplate_NoActionRequired_ViewButton()
    {
        var notification = CreateTestNotification(requiresAction: false);
        var template = _resolver.ResolveTeamsTemplate(notification, BaseUrl);

        template.JsonPayload.Should().Contain("View in NexTraceOne");
    }
}

using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para as entidades de integração: WebhookTemplate.
/// Valida criação, invariantes, toggle e normalização de EventType.
/// </summary>
public sealed class IntegrationEntitiesTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    // ── WebhookTemplate — Create happy path ──────────────────────────────────

    [Fact]
    public void WebhookTemplate_Create_WithValidData_ShouldReturn()
    {
        var template = WebhookTemplate.Create(
            "tenant1", "Change Notification", "change.created",
            """{"event":"{{eventType}}","service":"{{serviceName}}"}""",
            null, Now);

        Assert.NotNull(template);
        Assert.Equal("Change Notification", template.Name);
        Assert.Equal("change.created", template.EventType);
        Assert.True(template.IsEnabled);
        Assert.Equal("tenant1", template.TenantId);
        Assert.NotEqual(Guid.Empty, template.Id.Value);
    }

    // ── WebhookTemplate — EventType normalisation ─────────────────────────────

    [Theory]
    [InlineData("change.created")]
    [InlineData("incident.opened")]
    [InlineData("contract.published")]
    [InlineData("approval.expired")]
    public void WebhookTemplate_Create_WithAllValidEventTypes_ShouldSucceed(string eventType)
    {
        var template = WebhookTemplate.Create(
            "tenant1", "Template", eventType, "{}", null, Now);
        Assert.Equal(eventType, template.EventType);
    }

    [Fact]
    public void WebhookTemplate_Create_WithInvalidEventType_ShouldNormalizeToDefault()
    {
        var template = WebhookTemplate.Create(
            "tenant1", "Template", "unknown.event", "{}", null, Now);
        Assert.Equal("change.created", template.EventType);
    }

    // ── WebhookTemplate — Toggle ───────────────────────────────────────────────

    [Fact]
    public void WebhookTemplate_Toggle_False_ShouldDisable()
    {
        var template = WebhookTemplate.Create(
            "tenant1", "Template", "change.created", "{}", null, Now);

        template.Toggle(false);

        Assert.False(template.IsEnabled);
    }

    [Fact]
    public void WebhookTemplate_Toggle_TrueAfterFalse_ShouldEnable()
    {
        var template = WebhookTemplate.Create(
            "tenant1", "Template", "change.created", "{}", null, Now);
        template.Toggle(false);

        template.Toggle(true);

        Assert.True(template.IsEnabled);
    }

    // ── WebhookTemplate — Validation ─────────────────────────────────────────

    [Fact]
    public void WebhookTemplate_Create_WithEmptyName_ShouldThrow()
    {
        var act = () => WebhookTemplate.Create(
            "tenant1", "", "change.created", "{}", null, Now);
        Assert.ThrowsAny<Exception>(act);
    }

    [Fact]
    public void WebhookTemplate_Create_WithEmptyPayload_ShouldThrow()
    {
        var act = () => WebhookTemplate.Create(
            "tenant1", "Template", "change.created", "", null, Now);
        Assert.ThrowsAny<Exception>(act);
    }

    [Fact]
    public void WebhookTemplate_Create_WithHeadersJson_ShouldStore()
    {
        var headers = """{"X-Custom-Header":"value"}""";
        var template = WebhookTemplate.Create(
            "tenant1", "Template", "incident.opened", "{}", headers, Now);
        Assert.Equal(headers, template.HeadersJson);
    }
}

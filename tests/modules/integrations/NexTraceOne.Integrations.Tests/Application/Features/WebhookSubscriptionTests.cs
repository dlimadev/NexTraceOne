using System.Linq;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.GetWebhookEventTypes;
using NexTraceOne.Integrations.Application.Features.ListWebhookSubscriptions;
using NexTraceOne.Integrations.Application.Features.RegisterWebhookSubscription;
using NSubstitute;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de Webhook Subscriptions.
/// Verificam comportamento dos handlers e validadores para webhook outbound.
/// </summary>
public sealed class WebhookSubscriptionTests
{
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public WebhookSubscriptionTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    // ── RegisterWebhookSubscription ──

    [Fact]
    public async Task Register_ValidCommand_ShouldReturnSubscriptionId()
    {
        // Arrange
        var handler = new RegisterWebhookSubscription.Handler(_clock);
        var command = new RegisterWebhookSubscription.Command(
            TenantId: "tenant-1",
            Name: "Incident Alerts",
            TargetUrl: "https://hooks.example.com/incidents",
            EventTypes: new[] { "incident.created", "incident.resolved" },
            Secret: "my-secret",
            Description: "Sends incident events to external system");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SubscriptionId.Should().NotBeEmpty();
        result.Value.Name.Should().Be("Incident Alerts");
        result.Value.TargetUrl.Should().Be("https://hooks.example.com/incidents");
        result.Value.HasSecret.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.EventCount.Should().Be(2);
    }

    [Fact]
    public async Task Register_WithoutSecret_ShouldReturnHasSecretFalse()
    {
        // Arrange
        var handler = new RegisterWebhookSubscription.Handler(_clock);
        var command = new RegisterWebhookSubscription.Command(
            TenantId: "tenant-1",
            Name: "Deploy Notifications",
            TargetUrl: "https://hooks.example.com/deploys",
            EventTypes: new[] { "change.deployed" },
            Secret: null,
            Description: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasSecret.Should().BeFalse();
    }

    // ── ListWebhookSubscriptions ──

    [Fact]
    public async Task List_NoFilter_ShouldReturnDemoSubscriptions()
    {
        // Arrange
        var handler = new ListWebhookSubscriptions.Handler();
        var query = new ListWebhookSubscriptions.Query(TenantId: "tenant-1");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task List_FilterActiveOnly_ShouldReturnOnlyActiveSubscriptions()
    {
        // Arrange
        var handler = new ListWebhookSubscriptions.Handler();
        var query = new ListWebhookSubscriptions.Query(TenantId: "tenant-1", IsActive: true);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(x => x.IsActive);
    }

    // ── GetWebhookEventTypes ──

    [Fact]
    public async Task GetEventTypes_ShouldReturnAllEightEventTypes()
    {
        // Arrange
        var handler = new GetWebhookEventTypes.Handler();
        var query = new GetWebhookEventTypes.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EventTypes.Should().HaveCount(8);
        result.Value.EventTypes.Select(e => e.Code).Should().Contain(new[]
        {
            "incident.created", "incident.resolved",
            "change.deployed", "change.promoted",
            "contract.published", "contract.deprecated",
            "service.registered", "alert.triggered",
        });
    }

    // ── RegisterWebhookSubscription Validator ──

    [Fact]
    public async Task Validator_NonHttpsUrl_ShouldFail()
    {
        // Arrange
        var validator = new RegisterWebhookSubscription.Validator();
        var command = new RegisterWebhookSubscription.Command(
            TenantId: "tenant-1",
            Name: "Bad Webhook",
            TargetUrl: "http://insecure.example.com/hook",
            EventTypes: new[] { "incident.created" },
            Secret: null,
            Description: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetUrl");
    }

    [Fact]
    public async Task Validator_EmptyName_ShouldFail()
    {
        // Arrange
        var validator = new RegisterWebhookSubscription.Validator();
        var command = new RegisterWebhookSubscription.Command(
            TenantId: "tenant-1",
            Name: "",
            TargetUrl: "https://hooks.example.com/ok",
            EventTypes: new[] { "incident.created" },
            Secret: null,
            Description: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validator_TooManyEventTypes_ShouldFail()
    {
        // Arrange
        var validator = new RegisterWebhookSubscription.Validator();
        var tooManyEvents = Enumerable.Range(0, 11).Select(i => $"incident.created").ToList();
        var command = new RegisterWebhookSubscription.Command(
            TenantId: "tenant-1",
            Name: "Flood Webhook",
            TargetUrl: "https://hooks.example.com/flood",
            EventTypes: tooManyEvents,
            Secret: null,
            Description: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EventTypes");
    }

    [Fact]
    public async Task Validator_EmptyEventTypes_ShouldFail()
    {
        // Arrange
        var validator = new RegisterWebhookSubscription.Validator();
        var command = new RegisterWebhookSubscription.Command(
            TenantId: "tenant-1",
            Name: "Empty Events",
            TargetUrl: "https://hooks.example.com/empty",
            EventTypes: Array.Empty<string>(),
            Secret: null,
            Description: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EventTypes");
    }
}

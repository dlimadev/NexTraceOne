using System.Linq;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Application.Features.GetWebhookEventTypes;
using NexTraceOne.Integrations.Application.Features.ListWebhookSubscriptions;
using NexTraceOne.Integrations.Application.Features.RegisterWebhookSubscription;
using NexTraceOne.Integrations.Domain.Entities;
using NSubstitute;

namespace NexTraceOne.Integrations.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de Webhook Subscriptions.
/// Verificam comportamento dos handlers e validadores para webhook outbound.
/// </summary>
public sealed class WebhookSubscriptionTests
{
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IWebhookSubscriptionRepository _repository = Substitute.For<IWebhookSubscriptionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public WebhookSubscriptionTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    // ── RegisterWebhookSubscription ──

    [Fact]
    public async Task Register_ValidCommand_ShouldReturnSubscriptionId()
    {
        // Arrange
        var handler = new RegisterWebhookSubscription.Handler(_repository, _unitOfWork, _clock);
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
        await _repository.Received(1).AddAsync(
            Arg.Is<WebhookSubscription>(s =>
                s.TenantId == "tenant-1" &&
                s.Name == "Incident Alerts" &&
                s.TargetUrl == "https://hooks.example.com/incidents" &&
                s.EventTypes.Count == 2 &&
                s.SecretHash != null &&
                s.IsActive),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WithoutSecret_ShouldReturnHasSecretFalse()
    {
        // Arrange
        var handler = new RegisterWebhookSubscription.Handler(_repository, _unitOfWork, _clock);
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
        await _repository.Received(1).AddAsync(Arg.Any<WebhookSubscription>(), Arg.Any<CancellationToken>());
    }

    // ── ListWebhookSubscriptions ──

    [Fact]
    public async Task List_NoFilter_ShouldReturnPersistedSubscriptions()
    {
        // Arrange
        var sub1 = WebhookSubscription.Create("t1", "Sub1", "https://a.com/hook", new[] { "incident.created" }, null, null, true, _clock.UtcNow);
        var sub2 = WebhookSubscription.Create("t1", "Sub2", "https://b.com/hook", new[] { "change.deployed" }, "hash", null, false, _clock.UtcNow);
        _repository.ListAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<WebhookSubscription> { sub1, sub2 } as IReadOnlyList<WebhookSubscription>, 2));

        var handler = new ListWebhookSubscriptions.Handler(_repository);
        var query = new ListWebhookSubscriptions.Query(TenantId: "tenant-1");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task List_FilterActiveOnly_ShouldReturnOnlyActiveSubscriptions()
    {
        // Arrange
        var sub1 = WebhookSubscription.Create("t1", "Active Sub", "https://a.com/hook", new[] { "incident.created" }, null, null, true, _clock.UtcNow);
        _repository.ListAsync(true, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<WebhookSubscription> { sub1 } as IReadOnlyList<WebhookSubscription>, 1));

        var handler = new ListWebhookSubscriptions.Handler(_repository);
        var query = new ListWebhookSubscriptions.Query(TenantId: "tenant-1", IsActive: true);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(x => x.IsActive);
    }

    [Fact]
    public async Task List_EmptyRepository_ShouldReturnEmptyResult()
    {
        // Arrange
        _repository.ListAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<WebhookSubscription>() as IReadOnlyList<WebhookSubscription>, 0));

        var handler = new ListWebhookSubscriptions.Handler(_repository);
        var query = new ListWebhookSubscriptions.Query(TenantId: "tenant-1");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
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

    // ── WebhookSubscription Domain Entity ──

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange & Act
        var now = DateTimeOffset.UtcNow;
        var sub = WebhookSubscription.Create("t1", "Test", "https://example.com/hook",
            new[] { "incident.created" }, "hash123", "desc", true, now);

        // Assert
        sub.Id.Value.Should().NotBeEmpty();
        sub.TenantId.Should().Be("t1");
        sub.Name.Should().Be("Test");
        sub.TargetUrl.Should().Be("https://example.com/hook");
        sub.EventTypes.Should().ContainSingle("incident.created");
        sub.SecretHash.Should().Be("hash123");
        sub.Description.Should().Be("desc");
        sub.IsActive.Should().BeTrue();
        sub.DeliveryCount.Should().Be(0);
        sub.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void RecordDeliverySuccess_ShouldIncrementCounters()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var sub = WebhookSubscription.Create("t1", "Test", "https://example.com/hook",
            new[] { "incident.created" }, null, null, true, now);

        // Act
        sub.RecordDeliverySuccess(now.AddMinutes(1));

        // Assert
        sub.DeliveryCount.Should().Be(1);
        sub.SuccessCount.Should().Be(1);
        sub.FailureCount.Should().Be(0);
        sub.LastTriggeredAt.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public void RecordDeliveryFailure_ShouldIncrementFailureCounter()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var sub = WebhookSubscription.Create("t1", "Test", "https://example.com/hook",
            new[] { "incident.created" }, null, null, true, now);

        // Act
        sub.RecordDeliveryFailure(now.AddMinutes(1));

        // Assert
        sub.DeliveryCount.Should().Be(1);
        sub.SuccessCount.Should().Be(0);
        sub.FailureCount.Should().Be(1);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var sub = WebhookSubscription.Create("t1", "Test", "https://example.com/hook",
            new[] { "incident.created" }, null, null, true, now);

        // Act
        sub.Deactivate(now.AddMinutes(1));

        // Assert
        sub.IsActive.Should().BeFalse();
        sub.UpdatedAt.Should().Be(now.AddMinutes(1));
    }
}

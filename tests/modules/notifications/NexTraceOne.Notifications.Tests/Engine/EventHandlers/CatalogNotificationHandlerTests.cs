using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Catalog.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

/// <summary>
/// Testes para o handler de catálogo e contratos da Fase 5:
/// ContractPublished, BreakingChangeDetected, ContractValidationFailed.
/// </summary>
public sealed class CatalogNotificationHandlerTests
{
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly List<NotificationRequest> _captured = [];
    private readonly CatalogNotificationHandler _handler;

    public CatalogNotificationHandlerTests()
    {
        _module.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured.Add(ci.ArgAt<NotificationRequest>(0));
                return new NotificationResult(true) { NotificationIds = [Guid.NewGuid()] };
            });
        _handler = new CatalogNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<CatalogNotificationHandler>());
    }

    // ── ContractPublished ──

    [Fact]
    public async Task HandleAsync_ContractPublished_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var publisherUserId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var @event = new ContractPublishedIntegrationEvent(
            ContractId: contractId,
            ContractName: "Payments API",
            ServiceName: "payments-service",
            Version: "2.1.0",
            PublisherUserId: publisherUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.ContractPublished);
        r.Category.Should().Be(nameof(NotificationCategory.Contract));
        r.Severity.Should().Be(nameof(NotificationSeverity.Info));
        r.SourceModule.Should().Be("Catalog");
        r.SourceEntityType.Should().Be("Contract");
        r.SourceEntityId.Should().Be(contractId.ToString());
        r.ActionUrl.Should().Be($"/contracts/{contractId}");
        r.RequiresAction.Should().BeFalse();
        r.RecipientUserIds.Should().Contain(publisherUserId);
        r.Title.Should().Contain("Payments API").And.Contain("2.1.0");
        r.Message.Should().Contain("payments-service");
    }

    [Fact]
    public async Task HandleAsync_ContractPublished_MissingPublisher_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new ContractPublishedIntegrationEvent(
            ContractId: Guid.NewGuid(),
            ContractName: "API",
            ServiceName: "svc",
            Version: "1.0",
            PublisherUserId: null,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── BreakingChangeDetected ──

    [Fact]
    public async Task HandleAsync_BreakingChangeDetected_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var @event = new BreakingChangeDetectedIntegrationEvent(
            ContractId: contractId,
            ContractName: "Orders API",
            ServiceName: "orders-service",
            Description: "Removed required field 'customerId'",
            OwnerUserId: ownerUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.BreakingChangeDetected);
        r.Severity.Should().Be(nameof(NotificationSeverity.Critical));
        r.ActionUrl.Should().Be($"/contracts/{contractId}/changes");
        r.RequiresAction.Should().BeTrue();
        r.Title.Should().Contain("Orders API");
        r.Message.Should().Contain("Removed required field");
        r.Message.Should().Contain("orders-service");
    }

    [Fact]
    public async Task HandleAsync_BreakingChangeDetected_MissingOwner_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new BreakingChangeDetectedIntegrationEvent(
            ContractId: Guid.NewGuid(),
            ContractName: "API",
            ServiceName: "svc",
            Description: "Breaking",
            OwnerUserId: null,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── ContractValidationFailed ──

    [Fact]
    public async Task HandleAsync_ContractValidationFailed_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var @event = new ContractValidationFailedIntegrationEvent(
            ContractId: contractId,
            ContractName: "Billing API",
            ServiceName: "billing-service",
            ValidationError: "Schema invalid: missing required property 'amount'",
            OwnerUserId: ownerUserId,
            TenantId: tenantId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.ContractValidationFailed);
        r.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        r.ActionUrl.Should().Be($"/contracts/{contractId}/validation");
        r.RequiresAction.Should().BeTrue();
        r.Title.Should().Contain("Billing API");
        r.Message.Should().Contain("missing required property");
    }

    [Fact]
    public async Task HandleAsync_ContractValidationFailed_MissingTenant_Skips()
    {
        var @event = new ContractValidationFailedIntegrationEvent(
            ContractId: Guid.NewGuid(),
            ContractName: "API",
            ServiceName: "svc",
            ValidationError: "Error",
            OwnerUserId: Guid.NewGuid(),
            TenantId: null) { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }
}

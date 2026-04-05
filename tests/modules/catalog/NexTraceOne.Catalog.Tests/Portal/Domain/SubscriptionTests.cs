using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;

namespace NexTraceOne.Catalog.Tests.Portal.Domain;

/// <summary>
/// Testes de domínio para o aggregate Subscription.
/// Valida criação, desativação, reativação, atualização de preferências
/// e registo de notificação, incluindo cenários de sucesso e falha.
/// </summary>
public sealed class SubscriptionTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_ReturnSuccess_When_InputIsValid()
    {
        var result = Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiName.Should().Be("Payments API");
        result.Value.IsActive.Should().BeFalse();
        result.Value.Channel.Should().Be(NotificationChannel.Email);
        result.Value.CreatedAt.Should().Be(Now);
        result.Value.LastNotifiedAt.Should().BeNull();
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_WebhookChannelWithoutUrl()
    {
        var result = Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.BreakingChangesOnly,
            NotificationChannel.Webhook,
            webhookUrl: null,
            Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.InvalidWebhookUrl");
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_WebhookChannelWithEmptyUrl()
    {
        var result = Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.BreakingChangesOnly,
            NotificationChannel.Webhook,
            webhookUrl: "   ",
            Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.InvalidWebhookUrl");
    }

    [Fact]
    public void Create_Should_ReturnSuccess_When_EmailChannelWithoutWebhookUrl()
    {
        var result = Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.SecurityAdvisories,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Channel.Should().Be(NotificationChannel.Email);
        result.Value.WebhookUrl.Should().BeNull();
    }

    [Fact]
    public void Create_Should_ReturnSuccess_When_WebhookChannelWithValidUrl()
    {
        var result = Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.BreakingChangesOnly,
            NotificationChannel.Webhook,
            webhookUrl: "https://hooks.acme.com/notify",
            Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Channel.Should().Be(NotificationChannel.Webhook);
        result.Value.WebhookUrl.Should().Be("https://hooks.acme.com/notify");
    }

    [Fact]
    public void Deactivate_Should_ReturnSuccess_When_Active()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.Deactivate();

        result.IsSuccess.Should().BeTrue();
        subscription.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_Should_ReturnFailure_When_AlreadyInactive()
    {
        var subscription = CreateActiveSubscription();
        _ = subscription.Deactivate();

        var result = subscription.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.AlreadyInactive");
    }

    [Fact]
    public void Reactivate_Should_ReturnSuccess_When_Inactive()
    {
        var subscription = CreateActiveSubscription();
        _ = subscription.Deactivate();

        var result = subscription.Reactivate();

        result.IsSuccess.Should().BeTrue();
        subscription.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_Should_ReturnFailure_When_AlreadyActive()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.Reactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.AlreadyActive");
    }

    [Fact]
    public void UpdatePreferences_Should_ReturnSuccess_When_ValidData()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.UpdatePreferences(
            SubscriptionLevel.DeprecationNotices,
            NotificationChannel.Webhook,
            "https://hooks.acme.com/v2");

        result.IsSuccess.Should().BeTrue();
        subscription.Level.Should().Be(SubscriptionLevel.DeprecationNotices);
        subscription.Channel.Should().Be(NotificationChannel.Webhook);
        subscription.WebhookUrl.Should().Be("https://hooks.acme.com/v2");
    }

    [Fact]
    public void UpdatePreferences_Should_ReturnFailure_When_WebhookWithoutUrl()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.UpdatePreferences(
            SubscriptionLevel.AllChanges,
            NotificationChannel.Webhook,
            webhookUrl: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.InvalidWebhookUrl");
    }

    [Fact]
    public void MarkNotified_Should_UpdateLastNotifiedAt()
    {
        var subscription = CreateActiveSubscription();
        var notifiedAt = new DateTimeOffset(2025, 07, 01, 12, 0, 0, TimeSpan.Zero);

        subscription.MarkNotified(notifiedAt);

        subscription.LastNotifiedAt.Should().Be(notifiedAt);
    }

    /// <summary>Cria uma subscrição ativa válida para uso nos testes.</summary>
    private static Subscription CreateActiveSubscription()
    {
        var result = Subscription.Create(
            Guid.NewGuid(),
            "Payments API",
            Guid.NewGuid(),
            "dev@acme.com",
            "OrderService",
            "2.1.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        var subscription = result.Value;
        subscription.Approve("admin", Now);
        return subscription;
    }
}

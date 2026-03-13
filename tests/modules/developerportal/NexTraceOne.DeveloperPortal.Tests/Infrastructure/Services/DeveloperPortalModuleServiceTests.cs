using FluentAssertions;
using NexTraceOne.DeveloperPortal.Application.Abstractions;
using NexTraceOne.DeveloperPortal.Domain.Entities;
using NexTraceOne.DeveloperPortal.Domain.Enums;
using NexTraceOne.DeveloperPortal.Infrastructure.Services;
using NSubstitute;

namespace NexTraceOne.DeveloperPortal.Tests.Infrastructure.Services;

/// <summary>
/// Testes unitários para o serviço de contrato público cross-module DeveloperPortalModuleService.
/// Valida as queries expostas para outros módulos consultarem dados de subscrições
/// sem aceder diretamente ao DbContext do DeveloperPortal.
/// </summary>
public sealed class DeveloperPortalModuleServiceTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly DeveloperPortalModuleService _sut;

    public DeveloperPortalModuleServiceTests()
    {
        _subscriptionRepository = Substitute.For<ISubscriptionRepository>();
        _sut = new DeveloperPortalModuleService(_subscriptionRepository);
    }

    [Fact]
    public async Task HasActiveSubscriptionsAsync_Should_ReturnTrue_When_ActiveSubscriptionsExist()
    {
        // Arrange
        var apiAssetId = Guid.NewGuid();
        var subscriptions = CreateSubscriptions(apiAssetId, activeCount: 2, inactiveCount: 1);
        _subscriptionRepository.GetByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var result = await _sut.HasActiveSubscriptionsAsync(apiAssetId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasActiveSubscriptionsAsync_Should_ReturnFalse_When_NoActiveSubscriptions()
    {
        // Arrange
        var apiAssetId = Guid.NewGuid();
        var subscriptions = CreateSubscriptions(apiAssetId, activeCount: 0, inactiveCount: 2);
        _subscriptionRepository.GetByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var result = await _sut.HasActiveSubscriptionsAsync(apiAssetId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasActiveSubscriptionsAsync_Should_ReturnFalse_When_NoSubscriptionsExist()
    {
        // Arrange
        var apiAssetId = Guid.NewGuid();
        _subscriptionRepository.GetByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<Subscription>());

        // Act
        var result = await _sut.HasActiveSubscriptionsAsync(apiAssetId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveSubscriptionCountAsync_Should_ReturnCorrectCount()
    {
        // Arrange
        var apiAssetId = Guid.NewGuid();
        var subscriptions = CreateSubscriptions(apiAssetId, activeCount: 3, inactiveCount: 2);
        _subscriptionRepository.GetByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var result = await _sut.GetActiveSubscriptionCountAsync(apiAssetId, CancellationToken.None);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetActiveSubscriptionCountAsync_Should_ReturnZero_When_NoSubscriptions()
    {
        // Arrange
        var apiAssetId = Guid.NewGuid();
        _subscriptionRepository.GetByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<Subscription>());

        // Act
        var result = await _sut.GetActiveSubscriptionCountAsync(apiAssetId, CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetSubscriberIdsAsync_Should_ReturnOnlyActiveSubscriberIds()
    {
        // Arrange
        var apiAssetId = Guid.NewGuid();
        var subscriberId1 = Guid.NewGuid();
        var subscriberId2 = Guid.NewGuid();
        var subscriberInactive = Guid.NewGuid();

        var subscriptions = new List<Subscription>
        {
            CreateSubscription(apiAssetId, subscriberId1, isActive: true),
            CreateSubscription(apiAssetId, subscriberId2, isActive: true),
            CreateSubscription(apiAssetId, subscriberInactive, isActive: false)
        };

        _subscriptionRepository.GetByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var result = await _sut.GetSubscriberIdsAsync(apiAssetId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(subscriberId1);
        result.Should().Contain(subscriberId2);
        result.Should().NotContain(subscriberInactive);
    }

    [Fact]
    public async Task GetSubscriberIdsAsync_Should_ReturnDistinctIds_When_SameSubscriberHasMultipleSubscriptions()
    {
        // Arrange
        var apiAssetId = Guid.NewGuid();
        var subscriberId = Guid.NewGuid();

        // Mesmo subscritor com duas subscrições ativas (email + webhook)
        var subscriptions = new List<Subscription>
        {
            CreateSubscription(apiAssetId, subscriberId, isActive: true, channel: NotificationChannel.Email),
            CreateSubscription(apiAssetId, subscriberId, isActive: true, channel: NotificationChannel.Webhook)
        };

        _subscriptionRepository.GetByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var result = await _sut.GetSubscriberIdsAsync(apiAssetId, CancellationToken.None);

        // Assert — o resultado deve ser distinto (1 ID, não 2)
        result.Should().HaveCount(1);
        result.Should().Contain(subscriberId);
    }

    [Fact]
    public async Task GetSubscriberIdsAsync_Should_ReturnEmpty_When_NoActiveSubscriptions()
    {
        // Arrange
        var apiAssetId = Guid.NewGuid();
        _subscriptionRepository.GetByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(new List<Subscription>());

        // Act
        var result = await _sut.GetSubscriberIdsAsync(apiAssetId, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Cria uma lista mista de subscrições ativas e inativas para testes.
    /// </summary>
    private static List<Subscription> CreateSubscriptions(
        Guid apiAssetId, int activeCount, int inactiveCount)
    {
        var list = new List<Subscription>();

        for (var i = 0; i < activeCount; i++)
            list.Add(CreateSubscription(apiAssetId, Guid.NewGuid(), isActive: true));

        for (var i = 0; i < inactiveCount; i++)
            list.Add(CreateSubscription(apiAssetId, Guid.NewGuid(), isActive: false));

        return list;
    }

    /// <summary>
    /// Cria uma subscrição individual com os parâmetros especificados.
    /// </summary>
    private static Subscription CreateSubscription(
        Guid apiAssetId,
        Guid subscriberId,
        bool isActive,
        NotificationChannel channel = NotificationChannel.Email)
    {
        var webhookUrl = channel == NotificationChannel.Webhook
            ? "https://hooks.acme.com/notify"
            : null;

        var apiName = $"API-{apiAssetId:N}"[..20];
        var email = $"dev-{subscriberId:N}@acme.com";

        var result = Subscription.Create(
            apiAssetId,
            apiName,
            subscriberId,
            email,
            "TestService",
            "1.0.0",
            SubscriptionLevel.AllChanges,
            channel,
            webhookUrl,
            Now);

        var subscription = result.Value;

        if (!isActive)
            subscription.Deactivate();

        return subscription;
    }
}

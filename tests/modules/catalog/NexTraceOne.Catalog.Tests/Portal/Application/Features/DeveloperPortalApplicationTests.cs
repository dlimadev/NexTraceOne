using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;

using CreateSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.CreateSubscription.CreateSubscription;
using DeleteSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.DeleteSubscription.DeleteSubscription;
using GenerateCodeFeature = NexTraceOne.Catalog.Application.Portal.Features.GenerateCode.GenerateCode;

namespace NexTraceOne.Catalog.Tests.Portal.Application.Features;

/// <summary>
/// Testes de handlers da camada Application do módulo DeveloperPortal.
/// Valida criação e remoção de subscrições e geração de código,
/// incluindo cenários de sucesso e falha com mocks de repositórios.
/// </summary>
public sealed class DeveloperPortalApplicationTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    #region CreateSubscription

    [Fact]
    public async Task CreateSubscription_Should_ReturnSuccess_When_SubscriptionDoesNotExist()
    {
        var apiAssetId = Guid.NewGuid();
        var subscriberId = Guid.NewGuid();
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new CreateSubscriptionFeature.Handler(repository, unitOfWork, clock);

        Subscription? noSubscription = null;
        repository.GetByApiAndSubscriberAsync(apiAssetId, subscriberId, Arg.Any<CancellationToken>())
            .Returns(noSubscription);
        clock.UtcNow.Returns(Now);

        var command = new CreateSubscriptionFeature.Command(
            apiAssetId,
            "Payments API",
            subscriberId,
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            WebhookUrl: null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiName.Should().Be("Payments API");
        result.Value.IsActive.Should().BeFalse();
        result.Value.CreatedAt.Should().Be(Now);
        repository.Received(1).Add(Arg.Any<Subscription>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSubscription_Should_ReturnFailure_When_DuplicateExists()
    {
        var apiAssetId = Guid.NewGuid();
        var subscriberId = Guid.NewGuid();
        var existing = CreateActiveSubscription(apiAssetId, subscriberId);
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new CreateSubscriptionFeature.Handler(repository, unitOfWork, clock);

        repository.GetByApiAndSubscriberAsync(apiAssetId, subscriberId, Arg.Any<CancellationToken>())
            .Returns(existing);
        clock.UtcNow.Returns(Now);

        var command = new CreateSubscriptionFeature.Command(
            apiAssetId,
            "Payments API",
            subscriberId,
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            WebhookUrl: null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.AlreadyExists");
        repository.DidNotReceive().Add(Arg.Any<Subscription>());
    }

    [Fact]
    public async Task CreateSubscription_Should_ReturnSuccess_When_WebhookChannelWithValidUrl()
    {
        var apiAssetId = Guid.NewGuid();
        var subscriberId = Guid.NewGuid();
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new CreateSubscriptionFeature.Handler(repository, unitOfWork, clock);

        Subscription? noSubscription = null;
        repository.GetByApiAndSubscriberAsync(apiAssetId, subscriberId, Arg.Any<CancellationToken>())
            .Returns(noSubscription);
        clock.UtcNow.Returns(Now);

        var command = new CreateSubscriptionFeature.Command(
            apiAssetId,
            "Payments API",
            subscriberId,
            "dev@acme.com",
            "OrderService",
            "1.0.0",
            SubscriptionLevel.BreakingChangesOnly,
            NotificationChannel.Webhook,
            WebhookUrl: "https://hooks.acme.com/notify");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Channel.Should().Be("Webhook");
    }

    #endregion

    #region DeleteSubscription

    [Fact]
    public async Task DeleteSubscription_Should_ReturnSuccess_When_SubscriptionExists()
    {
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(Guid.NewGuid(), Guid.NewGuid());
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new DeleteSubscriptionFeature.Handler(repository, unitOfWork);

        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        var command = new DeleteSubscriptionFeature.Command(subscriptionId, Guid.NewGuid());

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Received(1).Remove(subscription);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSubscription_Should_ReturnFailure_When_SubscriptionNotFound()
    {
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new DeleteSubscriptionFeature.Handler(repository, unitOfWork);

        Subscription? noSubscription = null;
        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns(noSubscription);

        var command = new DeleteSubscriptionFeature.Command(Guid.NewGuid(), Guid.NewGuid());

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.NotFound");
        repository.DidNotReceive().Remove(Arg.Any<Subscription>());
    }

    #endregion

    #region GenerateCode

    [Fact]
    public async Task GenerateCode_Should_ReturnSuccess_When_ValidRequest()
    {
        var apiAssetId = Guid.NewGuid();
        var requestedById = Guid.NewGuid();
        var repository = Substitute.For<ICodeGenerationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new GenerateCodeFeature.Handler(repository, unitOfWork, clock);

        clock.UtcNow.Returns(Now);

        var command = new GenerateCodeFeature.Command(
            apiAssetId,
            "Payments API",
            "2.1.0",
            requestedById,
            "CSharp",
            GenerationType.SdkClient);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Language.Should().Be("CSharp");
        result.Value.GenerationType.Should().Be("SdkClient");
        result.Value.GeneratedCode.Should().Contain("PaymentsAPI");
        result.Value.IsAiGenerated.Should().BeFalse();
        result.Value.GeneratedAt.Should().Be(Now);
        repository.Received(1).Add(Arg.Any<CodeGenerationRecord>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateCode_Should_ReturnSuccess_When_TypeScriptSdkClient()
    {
        var repository = Substitute.For<ICodeGenerationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new GenerateCodeFeature.Handler(repository, unitOfWork, clock);

        clock.UtcNow.Returns(Now);

        var command = new GenerateCodeFeature.Command(
            Guid.NewGuid(),
            "Users API",
            "1.0.0",
            Guid.NewGuid(),
            "TypeScript",
            GenerationType.SdkClient);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Language.Should().Be("TypeScript");
        result.Value.GeneratedCode.Should().Contain("UsersAPI");
    }

    [Fact]
    public async Task GenerateCode_Should_ReturnSuccess_When_IntegrationExample()
    {
        var repository = Substitute.For<ICodeGenerationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new GenerateCodeFeature.Handler(repository, unitOfWork, clock);

        clock.UtcNow.Returns(Now);

        var command = new GenerateCodeFeature.Command(
            Guid.NewGuid(),
            "Catalog API",
            "3.0.0",
            Guid.NewGuid(),
            "Python",
            GenerationType.IntegrationExample);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GenerationType.Should().Be("IntegrationExample");
        result.Value.GeneratedCode.Should().Contain("Integration example");
    }

    #endregion

    /// <summary>
    /// Cria uma subscrição ativa válida para uso como fixture nos testes de handlers.
    /// </summary>
    private static Subscription CreateActiveSubscription(Guid apiAssetId, Guid subscriberId)
    {
        var result = Subscription.Create(
            apiAssetId,
            "Test API",
            subscriberId,
            "test@acme.com",
            "TestService",
            "1.0.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);

        return result.Value;
    }
}

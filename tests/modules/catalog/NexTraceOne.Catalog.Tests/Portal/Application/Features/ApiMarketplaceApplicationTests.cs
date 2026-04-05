using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;

using ApproveSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.ApproveSubscription.ApproveSubscription;
using CreateApiKeyFeature = NexTraceOne.Catalog.Application.Portal.Features.CreateApiKey.CreateApiKey;
using GetRateLimitPolicyFeature = NexTraceOne.Catalog.Application.Portal.Features.GetRateLimitPolicy.GetRateLimitPolicy;
using ListApiKeysFeature = NexTraceOne.Catalog.Application.Portal.Features.ListApiKeys.ListApiKeys;
using RejectSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.RejectSubscription.RejectSubscription;
using RevokeApiKeyFeature = NexTraceOne.Catalog.Application.Portal.Features.RevokeApiKey.RevokeApiKey;
using SetRateLimitPolicyFeature = NexTraceOne.Catalog.Application.Portal.Features.SetRateLimitPolicy.SetRateLimitPolicy;

namespace NexTraceOne.Catalog.Tests.Portal.Application.Features;

/// <summary>
/// Testes de handlers da camada Application do módulo API Marketplace.
/// Valida criação, revogação e listagem de API Keys, aprovação/rejeição de subscrições
/// e políticas de rate limiting.
/// </summary>
public sealed class ApiMarketplaceApplicationTests
{
    private static readonly DateTimeOffset Now = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── API Keys ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateApiKey_Should_ReturnSuccess_When_InputIsValid()
    {
        var ownerId = Guid.NewGuid();
        var repository = Substitute.For<IApiKeyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new CreateApiKeyFeature.Handler(repository, unitOfWork, clock);

        var command = new CreateApiKeyFeature.Command(ownerId, null, "My Key", "Test key", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Key");
        result.Value.RawKey.Should().NotBeNullOrEmpty();
        result.Value.KeyPrefix.Should().NotBeNullOrEmpty();
        result.Value.CreatedAt.Should().Be(Now);
        result.Value.ExpiresAt.Should().BeNull();
        repository.Received(1).Add(Arg.Any<ApiKey>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateApiKey_Should_ReturnSuccess_With_ApiAssetId()
    {
        var ownerId = Guid.NewGuid();
        var apiAssetId = Guid.NewGuid();
        var repository = Substitute.For<IApiKeyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new CreateApiKeyFeature.Handler(repository, unitOfWork, clock);

        var command = new CreateApiKeyFeature.Command(ownerId, apiAssetId, "API-Scoped Key", null, null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiKeyId.Should().NotBeEmpty();
        repository.Received(1).Add(Arg.Any<ApiKey>());
    }

    [Fact]
    public async Task CreateApiKey_Should_ReturnSuccess_With_ExpiryDate()
    {
        var ownerId = Guid.NewGuid();
        var expiry = Now.AddDays(30);
        var repository = Substitute.For<IApiKeyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new CreateApiKeyFeature.Handler(repository, unitOfWork, clock);

        var command = new CreateApiKeyFeature.Command(ownerId, null, "Expiring Key", null, expiry);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAt.Should().Be(expiry);
    }

    [Fact]
    public async Task CreateApiKey_RawKey_Should_BeHex64Characters()
    {
        var repository = Substitute.For<IApiKeyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new CreateApiKeyFeature.Handler(repository, unitOfWork, clock);

        var command = new CreateApiKeyFeature.Command(Guid.NewGuid(), null, "Key", null, null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RawKey.Should().HaveLength(64);
    }

    [Fact]
    public async Task RevokeApiKey_Should_ReturnSuccess_When_KeyExistsAndRequesterIsOwner()
    {
        var ownerId = Guid.NewGuid();
        var apiKey = CreateActiveApiKey(ownerId);
        var repository = Substitute.For<IApiKeyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new RevokeApiKeyFeature.Handler(repository, unitOfWork, clock);

        repository.GetByIdAsync(Arg.Any<ApiKeyId>(), Arg.Any<CancellationToken>()).Returns(apiKey);

        var command = new RevokeApiKeyFeature.Command(apiKey.Id.Value, ownerId);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiKeyId.Should().Be(apiKey.Id.Value);
        result.Value.RevokedAt.Should().Be(Now);
        repository.Received(1).Update(apiKey);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeApiKey_Should_ReturnFailure_When_KeyNotFound()
    {
        var repository = Substitute.For<IApiKeyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new RevokeApiKeyFeature.Handler(repository, unitOfWork, clock);

        ApiKey? noKey = null;
        repository.GetByIdAsync(Arg.Any<ApiKeyId>(), Arg.Any<CancellationToken>()).Returns(noKey);

        var command = new RevokeApiKeyFeature.Command(Guid.NewGuid(), Guid.NewGuid());

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("API_KEY_NOT_FOUND");
    }

    [Fact]
    public async Task RevokeApiKey_Should_ReturnFailure_When_RequesterIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var apiKey = CreateActiveApiKey(ownerId);
        var repository = Substitute.For<IApiKeyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new RevokeApiKeyFeature.Handler(repository, unitOfWork, clock);

        repository.GetByIdAsync(Arg.Any<ApiKeyId>(), Arg.Any<CancellationToken>()).Returns(apiKey);

        var command = new RevokeApiKeyFeature.Command(apiKey.Id.Value, otherUserId);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("API_KEY_ACCESS_DENIED");
        repository.DidNotReceive().Update(Arg.Any<ApiKey>());
    }

    [Fact]
    public async Task RevokeApiKey_Should_ReturnFailure_When_AlreadyRevoked()
    {
        var ownerId = Guid.NewGuid();
        var apiKey = CreateActiveApiKey(ownerId);
        apiKey.Revoke(ownerId.ToString(), Now);
        var repository = Substitute.For<IApiKeyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new RevokeApiKeyFeature.Handler(repository, unitOfWork, clock);

        repository.GetByIdAsync(Arg.Any<ApiKeyId>(), Arg.Any<CancellationToken>()).Returns(apiKey);

        var command = new RevokeApiKeyFeature.Command(apiKey.Id.Value, ownerId);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("API_KEY_ALREADY_REVOKED");
    }

    [Fact]
    public async Task ListApiKeys_Should_ReturnKeys_For_Owner()
    {
        var ownerId = Guid.NewGuid();
        var key1 = CreateActiveApiKey(ownerId);
        var key2 = CreateActiveApiKey(ownerId);
        var repository = Substitute.For<IApiKeyRepository>();
        var sut = new ListApiKeysFeature.Handler(repository);

        repository.GetByOwnerAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(new List<ApiKey> { key1, key2 });

        var query = new ListApiKeysFeature.Query(ownerId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(k => k.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task ListApiKeys_Should_ReturnEmptyList_When_NoKeys()
    {
        var ownerId = Guid.NewGuid();
        var repository = Substitute.For<IApiKeyRepository>();
        var sut = new ListApiKeysFeature.Handler(repository);

        repository.GetByOwnerAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(new List<ApiKey>());

        var result = await sut.Handle(new ListApiKeysFeature.Query(ownerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ListApiKeys_Should_Not_Expose_RawKey()
    {
        var ownerId = Guid.NewGuid();
        var apiKey = CreateActiveApiKey(ownerId);
        var repository = Substitute.For<IApiKeyRepository>();
        var sut = new ListApiKeysFeature.Handler(repository);

        repository.GetByOwnerAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(new List<ApiKey> { apiKey });

        var result = await sut.Handle(new ListApiKeysFeature.Query(ownerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value[0].KeyPrefix.Should().EndWith("...");
    }

    // ── Subscription Approval ─────────────────────────────────────────────────

    [Fact]
    public async Task ApproveSubscription_Should_ReturnSuccess_When_SubscriptionIsPending()
    {
        var subscription = CreatePendingSubscription();
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new ApproveSubscriptionFeature.Handler(repository, unitOfWork, clock);

        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        var command = new ApproveSubscriptionFeature.Command(subscription.Id.Value, "admin-user");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Active");
        result.Value.ApprovedAt.Should().Be(Now);
        subscription.IsActive.Should().BeTrue();
        repository.Received(1).Update(subscription);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveSubscription_Should_ReturnFailure_When_SubscriptionNotFound()
    {
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new ApproveSubscriptionFeature.Handler(repository, unitOfWork, clock);

        Subscription? noSub = null;
        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns(noSub);

        var result = await sut.Handle(
            new ApproveSubscriptionFeature.Command(Guid.NewGuid(), "admin"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.NotFound");
    }

    [Fact]
    public async Task ApproveSubscription_Should_ReturnFailure_When_AlreadyApproved()
    {
        var subscription = CreatePendingSubscription();
        subscription.Approve("admin", Now);
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new ApproveSubscriptionFeature.Handler(repository, unitOfWork, clock);

        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        var result = await sut.Handle(
            new ApproveSubscriptionFeature.Command(subscription.Id.Value, "admin"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SUBSCRIPTION_NOT_PENDING");
    }

    [Fact]
    public async Task RejectSubscription_Should_ReturnSuccess_When_SubscriptionIsPending()
    {
        var subscription = CreatePendingSubscription();
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new RejectSubscriptionFeature.Handler(repository, unitOfWork, clock);

        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        var command = new RejectSubscriptionFeature.Command(subscription.Id.Value, "Does not meet requirements.");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Rejected");
        result.Value.RejectionReason.Should().Be("Does not meet requirements.");
        subscription.IsActive.Should().BeFalse();
        repository.Received(1).Update(subscription);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectSubscription_Should_ReturnFailure_When_SubscriptionNotFound()
    {
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new RejectSubscriptionFeature.Handler(repository, unitOfWork, clock);

        Subscription? noSub = null;
        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns(noSub);

        var result = await sut.Handle(
            new RejectSubscriptionFeature.Command(Guid.NewGuid(), "Reason"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.NotFound");
    }

    [Fact]
    public async Task RejectSubscription_Should_ReturnFailure_When_AlreadyRejected()
    {
        var subscription = CreatePendingSubscription();
        subscription.Reject("Initial rejection", Now);
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new RejectSubscriptionFeature.Handler(repository, unitOfWork, clock);

        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        var result = await sut.Handle(
            new RejectSubscriptionFeature.Command(subscription.Id.Value, "Second rejection"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SUBSCRIPTION_ALREADY_REJECTED");
    }

    // ── Rate Limiting ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SetRateLimitPolicy_Should_CreateNew_When_NoPolicyExists()
    {
        var apiAssetId = Guid.NewGuid();
        var repository = Substitute.For<IApiRateLimitPolicyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new SetRateLimitPolicyFeature.Handler(repository, unitOfWork, clock);

        RateLimitPolicy? noPolicy = null;
        repository.GetByApiAssetIdAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(noPolicy);

        var command = new SetRateLimitPolicyFeature.Command(
            apiAssetId, 10, 100, 1000, 20, true, "admin", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiAssetId.Should().Be(apiAssetId);
        result.Value.RequestsPerMinute.Should().Be(10);
        result.Value.RequestsPerHour.Should().Be(100);
        result.Value.RequestsPerDay.Should().Be(1000);
        repository.Received(1).Add(Arg.Any<RateLimitPolicy>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetRateLimitPolicy_Should_Update_When_PolicyExists()
    {
        var apiAssetId = Guid.NewGuid();
        var existingPolicy = CreateRateLimitPolicy(apiAssetId);
        var repository = Substitute.For<IApiRateLimitPolicyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new SetRateLimitPolicyFeature.Handler(repository, unitOfWork, clock);

        repository.GetByApiAssetIdAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(existingPolicy);

        var command = new SetRateLimitPolicyFeature.Command(
            apiAssetId, 20, 200, 2000, 40, true, "admin", "Updated");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestsPerMinute.Should().Be(20);
        repository.Received(1).Update(existingPolicy);
        repository.DidNotReceive().Add(Arg.Any<RateLimitPolicy>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetRateLimitPolicy_Should_ReturnFailure_When_InvalidRatioValues()
    {
        var apiAssetId = Guid.NewGuid();
        var repository = Substitute.For<IApiRateLimitPolicyRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var sut = new SetRateLimitPolicyFeature.Handler(repository, unitOfWork, clock);

        RateLimitPolicy? noPolicy = null;
        repository.GetByApiAssetIdAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(noPolicy);

        // requestsPerHour (5) <= requestsPerMinute (10) → invalid
        var command = new SetRateLimitPolicyFeature.Command(
            apiAssetId, 10, 5, 1000, 20, true, "admin", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_RATE_LIMITS");
    }

    [Fact]
    public async Task GetRateLimitPolicy_Should_ReturnPolicy_When_Exists()
    {
        var apiAssetId = Guid.NewGuid();
        var policy = CreateRateLimitPolicy(apiAssetId);
        var repository = Substitute.For<IApiRateLimitPolicyRepository>();
        var sut = new GetRateLimitPolicyFeature.Handler(repository);

        repository.GetByApiAssetIdAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await sut.Handle(new GetRateLimitPolicyFeature.Query(apiAssetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiAssetId.Should().Be(apiAssetId);
        result.Value.RequestsPerMinute.Should().Be(10);
        result.Value.RequestsPerHour.Should().Be(100);
        result.Value.RequestsPerDay.Should().Be(1000);
        result.Value.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetRateLimitPolicy_Should_ReturnFailure_When_NotFound()
    {
        var apiAssetId = Guid.NewGuid();
        var repository = Substitute.For<IApiRateLimitPolicyRepository>();
        var sut = new GetRateLimitPolicyFeature.Handler(repository);

        RateLimitPolicy? noPolicy = null;
        repository.GetByApiAssetIdAsync(apiAssetId, Arg.Any<CancellationToken>()).Returns(noPolicy);

        var result = await sut.Handle(new GetRateLimitPolicyFeature.Query(apiAssetId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RATE_LIMIT_NOT_FOUND");
    }

    // ── Domain tests for ApiKey ───────────────────────────────────────────────

    [Fact]
    public void ApiKey_Create_Should_ReturnSuccess_When_ValidInput()
    {
        var ownerId = Guid.NewGuid();
        var result = ApiKey.Create(ownerId, null, "Test Key", "abc123hash", "abc12345...", null, null, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.RequestCount.Should().Be(0);
        result.Value.OwnerId.Should().Be(ownerId);
    }

    [Fact]
    public void ApiKey_Create_Should_ReturnFailure_When_EmptyOwnerId()
    {
        var result = ApiKey.Create(Guid.Empty, null, "Test Key", "abc123hash", "abc12345...", null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("API_KEY_INVALID_OWNER");
    }

    [Fact]
    public void ApiKey_Revoke_Should_SetIsActiveFalse()
    {
        var apiKey = CreateActiveApiKey(Guid.NewGuid());

        var result = apiKey.Revoke("user1", Now);

        result.IsSuccess.Should().BeTrue();
        apiKey.IsActive.Should().BeFalse();
        apiKey.RevokedAt.Should().Be(Now);
    }

    [Fact]
    public void ApiKey_Revoke_Should_ReturnFailure_When_AlreadyRevoked()
    {
        var apiKey = CreateActiveApiKey(Guid.NewGuid());
        apiKey.Revoke("user1", Now);

        var result = apiKey.Revoke("user1", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("API_KEY_ALREADY_REVOKED");
    }

    [Fact]
    public void ApiKey_RecordUsage_Should_IncrementCounterAndUpdateTimestamp()
    {
        var apiKey = CreateActiveApiKey(Guid.NewGuid());
        var usedAt = Now.AddHours(1);

        apiKey.RecordUsage(usedAt);

        apiKey.RequestCount.Should().Be(1);
        apiKey.LastUsedAt.Should().Be(usedAt);
    }

    // ── Domain tests for Subscription Approval ────────────────────────────────

    [Fact]
    public void Subscription_Create_Should_SetStatusPendingApproval()
    {
        var result = Subscription.Create(
            Guid.NewGuid(), "Test API", Guid.NewGuid(), "dev@acme.com",
            "ServiceA", "1.0.0", SubscriptionLevel.AllChanges, NotificationChannel.Email, null, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SubscriptionStatus.PendingApproval);
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Subscription_Approve_Should_SetStatusActive()
    {
        var subscription = CreatePendingSubscription();

        var result = subscription.Approve("admin", Now);

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.IsActive.Should().BeTrue();
        subscription.ApprovedBy.Should().Be("admin");
        subscription.ApprovedAt.Should().Be(Now);
    }

    [Fact]
    public void Subscription_Approve_Should_ReturnFailure_When_AlreadyApproved()
    {
        var subscription = CreatePendingSubscription();
        subscription.Approve("admin", Now);

        var result = subscription.Approve("admin2", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SUBSCRIPTION_NOT_PENDING");
    }

    [Fact]
    public void Subscription_Reject_Should_SetStatusRejected()
    {
        var subscription = CreatePendingSubscription();

        var result = subscription.Reject("Does not comply.", Now);

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Rejected);
        subscription.IsActive.Should().BeFalse();
        subscription.RejectionReason.Should().Be("Does not comply.");
    }

    [Fact]
    public void Subscription_Reject_Should_ReturnFailure_When_AlreadyRejected()
    {
        var subscription = CreatePendingSubscription();
        subscription.Reject("First reason", Now);

        var result = subscription.Reject("Second reason", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SUBSCRIPTION_ALREADY_REJECTED");
    }

    [Fact]
    public void Subscription_Approve_After_Reject_Should_ReturnFailure()
    {
        var subscription = CreatePendingSubscription();
        subscription.Reject("Rejected", Now);

        var result = subscription.Approve("admin", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SUBSCRIPTION_NOT_PENDING");
    }

    // ── Domain tests for RateLimitPolicy ─────────────────────────────────────

    [Fact]
    public void RateLimitPolicy_Create_Should_ReturnSuccess_When_ValidValues()
    {
        var result = RateLimitPolicy.Create(Guid.NewGuid(), 10, 100, 1000, 20, null, "admin", Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.RequestsPerMinute.Should().Be(10);
    }

    [Fact]
    public void RateLimitPolicy_Create_Should_ReturnFailure_When_HourLessThanMinute()
    {
        var result = RateLimitPolicy.Create(Guid.NewGuid(), 100, 50, 1000, 20, null, "admin", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_RATE_LIMITS");
    }

    [Fact]
    public void RateLimitPolicy_Create_Should_ReturnFailure_When_DayLessThanHour()
    {
        var result = RateLimitPolicy.Create(Guid.NewGuid(), 10, 100, 50, 20, null, "admin", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_RATE_LIMITS");
    }

    [Fact]
    public void RateLimitPolicy_Update_Should_ChangeValues()
    {
        var policy = CreateRateLimitPolicy(Guid.NewGuid());
        var updatedAt = Now.AddHours(1);

        policy.Update(20, 200, 2000, 40, false, "Updated notes", updatedAt);

        policy.RequestsPerMinute.Should().Be(20);
        policy.RequestsPerHour.Should().Be(200);
        policy.RequestsPerDay.Should().Be(2000);
        policy.IsEnabled.Should().BeFalse();
        policy.Notes.Should().Be("Updated notes");
        policy.UpdatedAt.Should().Be(updatedAt);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ApiKey CreateActiveApiKey(Guid ownerId)
    {
        var result = ApiKey.Create(
            ownerId,
            null,
            "Test Key",
            "a" + new string('b', 63),
            "aaaabbbb...",
            null,
            null,
            Now);
        return result.Value;
    }

    private static Subscription CreatePendingSubscription()
    {
        var result = Subscription.Create(
            Guid.NewGuid(),
            "Test API",
            Guid.NewGuid(),
            "dev@acme.com",
            "TestService",
            "1.0.0",
            SubscriptionLevel.AllChanges,
            NotificationChannel.Email,
            webhookUrl: null,
            Now);
        return result.Value;
    }

    private static RateLimitPolicy CreateRateLimitPolicy(Guid apiAssetId)
    {
        var result = RateLimitPolicy.Create(apiAssetId, 10, 100, 1000, 20, null, "admin", Now);
        return result.Value;
    }
}

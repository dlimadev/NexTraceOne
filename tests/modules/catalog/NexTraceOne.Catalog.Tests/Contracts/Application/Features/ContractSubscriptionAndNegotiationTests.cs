using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;

using CreateSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.CreateSubscription.CreateSubscription;
using ApproveSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.ApproveSubscription.ApproveSubscription;
using DeleteSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.DeleteSubscription.DeleteSubscription;
using CreateContractNegotiationFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateContractNegotiation.CreateContractNegotiation;
using AddNegotiationCommentFeature = NexTraceOne.Catalog.Application.Contracts.Features.AddNegotiationComment.AddNegotiationComment;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes dos handlers de subscrição de API e negociação cross-team de contratos
/// na camada Application dos módulos Portal e Contracts.
/// Cobre CreateSubscription, ApproveSubscription, DeleteSubscription,
/// CreateContractNegotiation e AddNegotiationComment.
/// </summary>
public sealed class ContractSubscriptionAndNegotiationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    private static IContractsUnitOfWork CreateContractsUnitOfWork() => Substitute.For<IContractsUnitOfWork>();

    // ── CreateSubscription ────────────────────────────────────────────

    [Fact]
    public async Task CreateSubscription_Should_ReturnResponse_When_NoDuplicate()
    {
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new CreateSubscriptionFeature.Handler(repository, unitOfWork, clock);

        var apiAssetId = Guid.NewGuid();
        var subscriberId = Guid.NewGuid();

        repository.GetByApiAndSubscriberAsync(apiAssetId, subscriberId, Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);
        clock.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new CreateSubscriptionFeature.Command(
                apiAssetId, "Payment API", subscriberId, "user@example.com",
                "checkout-service", "1.0.0",
                SubscriptionLevel.BreakingChangesOnly, NotificationChannel.Email, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ApiAssetId.Should().Be(apiAssetId);
        result.Value.ApiName.Should().Be("Payment API");
        result.Value.IsActive.Should().BeFalse();
        repository.Received(1).Add(Arg.Any<Subscription>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSubscription_Should_ReturnFailure_When_SubscriptionAlreadyExists()
    {
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new CreateSubscriptionFeature.Handler(repository, unitOfWork, clock);

        var apiAssetId = Guid.NewGuid();
        var subscriberId = Guid.NewGuid();
        clock.UtcNow.Returns(FixedNow);

        var existing = Subscription.Create(
            apiAssetId, "Payment API", subscriberId, "user@example.com",
            "checkout-service", "1.0.0",
            SubscriptionLevel.BreakingChangesOnly, NotificationChannel.Email, null, FixedNow).Value;

        repository.GetByApiAndSubscriberAsync(apiAssetId, subscriberId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await sut.Handle(
            new CreateSubscriptionFeature.Command(
                apiAssetId, "Payment API", subscriberId, "user@example.com",
                "checkout-service", "1.0.0",
                SubscriptionLevel.BreakingChangesOnly, NotificationChannel.Email, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.AlreadyExists");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSubscription_Validator_Should_Fail_When_EmailIsInvalid()
    {
        var validator = new CreateSubscriptionFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateSubscriptionFeature.Command(
                Guid.NewGuid(), "API", Guid.NewGuid(), "not-an-email",
                "svc", "1.0", SubscriptionLevel.BreakingChangesOnly, NotificationChannel.Email, null));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "SubscriberEmail");
    }

    [Fact]
    public async Task CreateSubscription_Validator_Should_Fail_When_WebhookUrlMissing_ForWebhookChannel()
    {
        var validator = new CreateSubscriptionFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateSubscriptionFeature.Command(
                Guid.NewGuid(), "API", Guid.NewGuid(), "user@example.com",
                "svc", "1.0", SubscriptionLevel.AllChanges, NotificationChannel.Webhook, null));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "WebhookUrl");
    }

    // ── ApproveSubscription ───────────────────────────────────────────

    [Fact]
    public async Task ApproveSubscription_Should_ReturnResponse_When_SubscriptionExists()
    {
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new ApproveSubscriptionFeature.Handler(repository, unitOfWork, clock);

        var subscription = Subscription.Create(
            Guid.NewGuid(), "API", Guid.NewGuid(), "user@example.com",
            "svc", "1.0", SubscriptionLevel.BreakingChangesOnly, NotificationChannel.Email, null, FixedNow).Value;

        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);
        clock.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new ApproveSubscriptionFeature.Command(subscription.Id.Value, "admin@example.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SubscriptionId.Should().Be(subscription.Id.Value);
        repository.Received(1).Update(Arg.Any<Subscription>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveSubscription_Should_ReturnNotFound_When_SubscriptionDoesNotExist()
    {
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new ApproveSubscriptionFeature.Handler(repository, unitOfWork, clock);

        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);

        var result = await sut.Handle(
            new ApproveSubscriptionFeature.Command(Guid.NewGuid(), "admin@example.com"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── DeleteSubscription ────────────────────────────────────────────

    [Fact]
    public async Task DeleteSubscription_Should_ReturnSuccess_When_SubscriptionExists()
    {
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new DeleteSubscriptionFeature.Handler(repository, unitOfWork);

        var subscription = Subscription.Create(
            Guid.NewGuid(), "API", Guid.NewGuid(), "user@example.com",
            "svc", "1.0", SubscriptionLevel.BreakingChangesOnly, NotificationChannel.Email, null, FixedNow).Value;

        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        var result = await sut.Handle(
            new DeleteSubscriptionFeature.Command(subscription.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Received(1).Remove(subscription);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSubscription_Should_ReturnNotFound_When_SubscriptionDoesNotExist()
    {
        var repository = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new DeleteSubscriptionFeature.Handler(repository, unitOfWork);

        repository.GetByIdAsync(Arg.Any<SubscriptionId>(), Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);

        var result = await sut.Handle(
            new DeleteSubscriptionFeature.Command(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeveloperPortal.Subscription.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── CreateContractNegotiation ─────────────────────────────────────

    [Fact]
    public async Task CreateContractNegotiation_Should_ReturnResponse_When_ValidCommand()
    {
        var repository = Substitute.For<IContractNegotiationRepository>();
        var unitOfWork = CreateContractsUnitOfWork();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new CreateContractNegotiationFeature.Handler(repository, unitOfWork, clock);

        clock.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new CreateContractNegotiationFeature.Command(
                ContractId: null,
                ProposedByTeamId: Guid.NewGuid(),
                ProposedByTeamName: "Payments Team",
                Title: "Payment API v2 Contract",
                Description: "Proposal for the new payment API v2 contract",
                Deadline: FixedNow.AddDays(30),
                Participants: "team-a,team-b",
                ParticipantCount: 2,
                ProposedContractSpec: "openapi: 3.0.0",
                InitiatedByUserId: "user-123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Payment API v2 Contract");
        result.Value.Status.Should().Be(NegotiationStatus.Draft);
        result.Value.ParticipantCount.Should().Be(2);
        await repository.Received(1).AddAsync(Arg.Any<ContractNegotiation>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateContractNegotiation_Validator_Should_Fail_When_TitleIsEmpty()
    {
        var validator = new CreateContractNegotiationFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateContractNegotiationFeature.Command(
                null, Guid.NewGuid(), "Team", "", "desc", null, "p", 1, null, "user"));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task CreateContractNegotiation_Validator_Should_Fail_When_ParticipantCountIsZero()
    {
        var validator = new CreateContractNegotiationFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new CreateContractNegotiationFeature.Command(
                null, Guid.NewGuid(), "Team", "Title", "desc", null, "p", 0, null, "user"));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "ParticipantCount");
    }

    // ── AddNegotiationComment ─────────────────────────────────────────

    [Fact]
    public async Task AddNegotiationComment_Should_ReturnResponse_When_NegotiationExists()
    {
        var negotiationRepository = Substitute.For<IContractNegotiationRepository>();
        var commentRepository = Substitute.For<INegotiationCommentRepository>();
        var unitOfWork = CreateContractsUnitOfWork();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new AddNegotiationCommentFeature.Handler(
            negotiationRepository, commentRepository, unitOfWork, clock);

        var negotiation = ContractNegotiation.Create(
            contractId: null,
            proposedByTeamId: Guid.NewGuid(),
            proposedByTeamName: "Team A",
            title: "Contract Negotiation",
            description: "Desc",
            deadline: null,
            participants: "team-a",
            participantCount: 1,
            proposedContractSpec: null,
            initiatedByUserId: "user-1",
            createdAt: FixedNow);

        negotiationRepository.GetByIdAsync(Arg.Any<ContractNegotiationId>(), Arg.Any<CancellationToken>())
            .Returns(negotiation);
        clock.UtcNow.Returns(FixedNow);

        var result = await sut.Handle(
            new AddNegotiationCommentFeature.Command(
                negotiation.Id.Value, "author-1", "Author Name", "This looks good.", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NegotiationId.Should().Be(negotiation.Id.Value);
        result.Value.AuthorId.Should().Be("author-1");
        result.Value.Content.Should().Be("This looks good.");
        await commentRepository.Received(1).AddAsync(Arg.Any<NegotiationComment>(), Arg.Any<CancellationToken>());
        await negotiationRepository.Received(1).UpdateAsync(negotiation, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddNegotiationComment_Should_ReturnNotFound_When_NegotiationDoesNotExist()
    {
        var negotiationRepository = Substitute.For<IContractNegotiationRepository>();
        var commentRepository = Substitute.For<INegotiationCommentRepository>();
        var unitOfWork = CreateContractsUnitOfWork();
        var clock = Substitute.For<IDateTimeProvider>();
        var sut = new AddNegotiationCommentFeature.Handler(
            negotiationRepository, commentRepository, unitOfWork, clock);

        negotiationRepository.GetByIdAsync(Arg.Any<ContractNegotiationId>(), Arg.Any<CancellationToken>())
            .Returns((ContractNegotiation?)null);

        var result = await sut.Handle(
            new AddNegotiationCommentFeature.Command(
                Guid.NewGuid(), "author-1", "Author Name", "Comment", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Negotiation.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddNegotiationComment_Validator_Should_Fail_When_ContentIsEmpty()
    {
        var validator = new AddNegotiationCommentFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new AddNegotiationCommentFeature.Command(Guid.NewGuid(), "author", "Name", "", null));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Content");
    }
}

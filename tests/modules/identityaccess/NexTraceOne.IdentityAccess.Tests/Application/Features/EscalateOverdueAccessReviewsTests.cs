using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.EscalateOverdueAccessReviews;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave C.2 — EscalateOverdueAccessReviews.
/// Cobre identificação de campanhas em atraso e envio de notificações de escalação.
/// </summary>
public sealed class EscalateOverdueAccessReviewsTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantGuid = Guid.NewGuid();

    private static IDateTimeProvider CreateClock() =>
        Substitute.For<IDateTimeProvider>() is { } c
            ? (c.UtcNow.Returns(FixedNow), c).Item2
            : null!;

    private static AccessReviewCampaign CreateCampaign(
        string name = "Q1 2025",
        DateTimeOffset? deadline = null,
        bool addPendingItem = true)
    {
        var tenantId = TenantId.From(TenantGuid);
        var campaign = AccessReviewCampaign.Create(tenantId, name, null, FixedNow, deadline.HasValue ? deadline.Value - FixedNow : null);

        if (addPendingItem)
        {
            campaign.AddItem(
                UserId.From(Guid.NewGuid()),
                RoleId.From(Guid.NewGuid()),
                "Admin",
                UserId.From(Guid.NewGuid()));
        }

        return campaign;
    }

    // ── Handler tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsZeroEscalated_WhenNoCampaignsApproachingDeadline()
    {
        var repo = Substitute.For<IAccessReviewRepository>();
        repo.ListOpenApproachingDeadlineAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AccessReviewCampaign>)[]);

        var notif = Substitute.For<INotificationModule>();
        var handler = new EscalateOverdueAccessReviews.Handler(repo, notif, CreateClock());

        var result = await handler.Handle(new EscalateOverdueAccessReviews.Command(3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EscalatedCount.Should().Be(0);
        await notif.DidNotReceive().SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EscalatesCampaignsWithPendingItems()
    {
        var campaign = CreateCampaign("Q1 2025", FixedNow.AddDays(2), addPendingItem: true);

        var repo = Substitute.For<IAccessReviewRepository>();
        repo.ListOpenApproachingDeadlineAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AccessReviewCampaign>)[campaign]);

        var notif = Substitute.For<INotificationModule>();
        notif.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(true));

        var handler = new EscalateOverdueAccessReviews.Handler(repo, notif, CreateClock());
        var result = await handler.Handle(new EscalateOverdueAccessReviews.Command(3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EscalatedCount.Should().Be(1);
        await notif.Received(1).SubmitAsync(
            Arg.Is<NotificationRequest>(r => r.EventType == "AccessReviewEscalation"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SkipsCampaigns_WithNoPendingItems()
    {
        var campaign = CreateCampaign("Q1 2025", FixedNow.AddDays(2), addPendingItem: false);

        var repo = Substitute.For<IAccessReviewRepository>();
        repo.ListOpenApproachingDeadlineAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AccessReviewCampaign>)[campaign]);

        var notif = Substitute.For<INotificationModule>();
        var handler = new EscalateOverdueAccessReviews.Handler(repo, notif, CreateClock());
        var result = await handler.Handle(new EscalateOverdueAccessReviews.Command(3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EscalatedCount.Should().Be(0);
        await notif.DidNotReceive().SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EscalatesMultipleCampaigns()
    {
        var campaigns = new[]
        {
            CreateCampaign("Q1 2025", FixedNow.AddDays(1)),
            CreateCampaign("Q2 2025", FixedNow.AddDays(2)),
            CreateCampaign("Q3 2025", FixedNow.AddDays(3)),
        };

        var repo = Substitute.For<IAccessReviewRepository>();
        repo.ListOpenApproachingDeadlineAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AccessReviewCampaign>)campaigns);

        var notif = Substitute.For<INotificationModule>();
        notif.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(true));

        var handler = new EscalateOverdueAccessReviews.Handler(repo, notif, CreateClock());
        var result = await handler.Handle(new EscalateOverdueAccessReviews.Command(5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EscalatedCount.Should().Be(3);
        await notif.Received(3).SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotCountFailed_NotificationSubmissions()
    {
        var campaign = CreateCampaign("Q1 2025", FixedNow.AddDays(2));

        var repo = Substitute.For<IAccessReviewRepository>();
        repo.ListOpenApproachingDeadlineAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AccessReviewCampaign>)[campaign]);

        var notif = Substitute.For<INotificationModule>();
        notif.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(false, "Channel unavailable"));

        var handler = new EscalateOverdueAccessReviews.Handler(repo, notif, CreateClock());
        var result = await handler.Handle(new EscalateOverdueAccessReviews.Command(3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EscalatedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectReviewedAt()
    {
        var repo = Substitute.For<IAccessReviewRepository>();
        repo.ListOpenApproachingDeadlineAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AccessReviewCampaign>)[]);

        var handler = new EscalateOverdueAccessReviews.Handler(repo, Substitute.For<INotificationModule>(), CreateClock());
        var result = await handler.Handle(new EscalateOverdueAccessReviews.Command(3), CancellationToken.None);

        result.Value.ReviewedAt.Should().Be(FixedNow);
    }

    // ── Validator tests ───────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Validator_RejectsDaysOutOfRange(int days)
    {
        var validator = new EscalateOverdueAccessReviews.Validator();
        var result = validator.Validate(new EscalateOverdueAccessReviews.Command(days));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(30)]
    public void Validator_AcceptsValidDaysRange(int days)
    {
        var validator = new EscalateOverdueAccessReviews.Validator();
        var result = validator.Validate(new EscalateOverdueAccessReviews.Command(days));
        result.IsValid.Should().BeTrue();
    }
}


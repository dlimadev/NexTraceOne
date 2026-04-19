using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CorrelateMqEvent;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetPostIncidentReview;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ProgressPostIncidentReview;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.StartPostIncidentReview;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes de unidade para as features de Post-Incident Review (PIR) e CorrelateMqEvent.
/// </summary>
public sealed class PostIncidentReviewTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid IncidentGuid = Guid.NewGuid();

    private static IDateTimeProvider CreateClock() =>
        CreateClock(FixedNow);

    private static IDateTimeProvider CreateClock(DateTimeOffset now)
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(now);
        return c;
    }

    private static PostIncidentReview BuildReview(Guid incidentId, bool completed = false)
    {
        var review = PostIncidentReview.Start(
            PostIncidentReviewId.New(),
            incidentId,
            "Platform Team",
            "Alice Facilitator",
            FixedNow);

        if (completed)
        {
            // Advance through all phases to reach Completed
            review.Progress(PostIncidentReviewPhase.RootCauseAnalysis, null, "RCA started", null, null, null, null);
            review.Progress(PostIncidentReviewPhase.PreventiveActions, null, "RCA done", null, null, null, null);
            review.Progress(PostIncidentReviewPhase.FinalReview, null, "RCA done", null, null, null, null);
            review.Progress(PostIncidentReviewPhase.Completed, PostIncidentReviewOutcome.RootCauseIdentified, "RCA done", "{}", "timeline", "summary", FixedNow.AddHours(2));
        }

        return review;
    }

    // ═══════════════════════════════════════════════════════════════════
    // StartPostIncidentReview
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task StartPostIncidentReview_Should_CreatePir_When_IncidentExistsAndNoPirYet()
    {
        var incidentStore = Substitute.For<IIncidentStore>();
        incidentStore.IncidentExists(IncidentGuid.ToString()).Returns(true);

        var reviewRepo = Substitute.For<IPostIncidentReviewRepository>();
        reviewRepo.GetByIncidentIdAsync(IncidentGuid, Arg.Any<CancellationToken>()).Returns((PostIncidentReview?)null);

        var handler = new StartPostIncidentReview.Handler(incidentStore, reviewRepo, CreateClock());
        var result = await handler.Handle(
            new StartPostIncidentReview.Command(IncidentGuid, "Platform Team", "Alice"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be(IncidentGuid);
        result.Value.CurrentPhase.Should().Be(PostIncidentReviewPhase.FactGathering.ToString());
        result.Value.Outcome.Should().Be(PostIncidentReviewOutcome.Pending.ToString());
        result.Value.ResponsibleTeam.Should().Be("Platform Team");
        result.Value.Facilitator.Should().Be("Alice");
        result.Value.StartedAt.Should().Be(FixedNow);

        await reviewRepo.Received(1).AddAsync(Arg.Any<PostIncidentReview>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartPostIncidentReview_Should_ReturnError_When_IncidentNotFound()
    {
        var incidentStore = Substitute.For<IIncidentStore>();
        incidentStore.IncidentExists(IncidentGuid.ToString()).Returns(false);

        var reviewRepo = Substitute.For<IPostIncidentReviewRepository>();

        var handler = new StartPostIncidentReview.Handler(incidentStore, reviewRepo, CreateClock());
        var result = await handler.Handle(
            new StartPostIncidentReview.Command(IncidentGuid, "Platform Team", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Incidents.Incident.NotFound");
    }

    [Fact]
    public async Task StartPostIncidentReview_Should_ReturnError_When_PirAlreadyExists()
    {
        var incidentStore = Substitute.For<IIncidentStore>();
        incidentStore.IncidentExists(IncidentGuid.ToString()).Returns(true);

        var existing = BuildReview(IncidentGuid);
        var reviewRepo = Substitute.For<IPostIncidentReviewRepository>();
        reviewRepo.GetByIncidentIdAsync(IncidentGuid, Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new StartPostIncidentReview.Handler(incidentStore, reviewRepo, CreateClock());
        var result = await handler.Handle(
            new StartPostIncidentReview.Command(IncidentGuid, "Platform Team", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Incidents.PIR.AlreadyExists");
    }

    [Fact]
    public void StartPostIncidentReview_Validator_Should_Reject_EmptyTeam()
    {
        var validator = new StartPostIncidentReview.Validator();
        var result = validator.Validate(new StartPostIncidentReview.Command(Guid.NewGuid(), "", null));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetPostIncidentReview
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPostIncidentReview_Should_ReturnPir_When_Exists()
    {
        var review = BuildReview(IncidentGuid);
        var reviewRepo = Substitute.For<IPostIncidentReviewRepository>();
        reviewRepo.GetByIncidentIdAsync(IncidentGuid, Arg.Any<CancellationToken>()).Returns(review);

        var handler = new GetPostIncidentReview.Handler(reviewRepo);
        var result = await handler.Handle(new GetPostIncidentReview.Query(IncidentGuid), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be(IncidentGuid);
        result.Value.CurrentPhase.Should().Be(PostIncidentReviewPhase.FactGathering.ToString());
        result.Value.IsCompleted.Should().BeFalse();
        result.Value.ResponsibleTeam.Should().Be("Platform Team");
        result.Value.Facilitator.Should().Be("Alice Facilitator");
    }

    [Fact]
    public async Task GetPostIncidentReview_Should_ReturnError_When_NotFound()
    {
        var reviewRepo = Substitute.For<IPostIncidentReviewRepository>();
        reviewRepo.GetByIncidentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PostIncidentReview?)null);

        var handler = new GetPostIncidentReview.Handler(reviewRepo);
        var result = await handler.Handle(new GetPostIncidentReview.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Incidents.PIR.NotFound");
    }

    [Fact]
    public void GetPostIncidentReview_Validator_Should_Reject_EmptyId()
    {
        var validator = new GetPostIncidentReview.Validator();
        var result = validator.Validate(new GetPostIncidentReview.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // ProgressPostIncidentReview
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProgressPostIncidentReview_Should_AdvancePhase_When_ReviewExists()
    {
        var review = BuildReview(IncidentGuid);
        var reviewRepo = Substitute.For<IPostIncidentReviewRepository>();
        reviewRepo.GetByIdAsync(review.Id.Value, Arg.Any<CancellationToken>()).Returns(review);

        var handler = new ProgressPostIncidentReview.Handler(reviewRepo);
        var result = await handler.Handle(
            new ProgressPostIncidentReview.Command(
                review.Id.Value,
                PostIncidentReviewPhase.RootCauseAnalysis,
                Outcome: null,
                RootCauseAnalysis: "Memory leak in OrderService",
                PreventiveActionsJson: null,
                TimelineNarrative: null,
                Summary: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReviewId.Should().Be(review.Id.Value);
        result.Value.CurrentPhase.Should().Be(PostIncidentReviewPhase.RootCauseAnalysis.ToString());
        result.Value.IsCompleted.Should().BeFalse();

        await reviewRepo.Received(1).UpdateAsync(Arg.Any<PostIncidentReview>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProgressPostIncidentReview_Should_ReturnError_When_ReviewNotFound()
    {
        var reviewRepo = Substitute.For<IPostIncidentReviewRepository>();
        reviewRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PostIncidentReview?)null);

        var handler = new ProgressPostIncidentReview.Handler(reviewRepo);
        var result = await handler.Handle(
            new ProgressPostIncidentReview.Command(
                Guid.NewGuid(),
                PostIncidentReviewPhase.RootCauseAnalysis,
                null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Incidents.PIR.NotFound");
    }

    [Fact]
    public async Task ProgressPostIncidentReview_Should_ReturnError_When_ReviewAlreadyCompleted()
    {
        var review = BuildReview(IncidentGuid, completed: true);
        var reviewRepo = Substitute.For<IPostIncidentReviewRepository>();
        reviewRepo.GetByIdAsync(review.Id.Value, Arg.Any<CancellationToken>()).Returns(review);

        var handler = new ProgressPostIncidentReview.Handler(reviewRepo);
        var result = await handler.Handle(
            new ProgressPostIncidentReview.Command(
                review.Id.Value,
                PostIncidentReviewPhase.FinalReview,
                null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Incidents.PIR.AlreadyCompleted");
    }

    [Fact]
    public void ProgressPostIncidentReview_Validator_Should_Reject_EmptyReviewId()
    {
        var validator = new ProgressPostIncidentReview.Validator();
        var result = validator.Validate(new ProgressPostIncidentReview.Command(
            Guid.Empty, PostIncidentReviewPhase.RootCauseAnalysis, null, null, null, null, null));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // CorrelateMqEvent
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CorrelateMqEvent_Should_CallCorrelatorByQueue()
    {
        var correlator = Substitute.For<ILegacyEventCorrelator>();
        correlator.CorrelateByQueueAsync("QM1", "ORDER.QUEUE", Arg.Any<CancellationToken>())
            .Returns(new CorrelationResult(true, "Service", "OrderService", Guid.NewGuid(), "order-service", "QueueName", null));

        var logger = Substitute.For<ILogger<CorrelateMqEvent.Handler>>();
        var handler = new CorrelateMqEvent.Handler(correlator, logger);

        var mqEvent = new LegacyMqEventIngestedEvent(
            "evt-001", "QM1", "ORDER.QUEUE", null, "QMGR.Q.DEPTH", 50, 1000,
            null, "Low", null, FixedNow);

        await handler.Handle(mqEvent, CancellationToken.None);

        await correlator.Received(1).CorrelateByQueueAsync("QM1", "ORDER.QUEUE", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CorrelateMqEvent_Should_HandleNonCorrelated_WithoutError()
    {
        var correlator = Substitute.For<ILegacyEventCorrelator>();
        correlator.CorrelateByQueueAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CorrelationResult(false, null, null, null, null, "NoMatch", null));

        var logger = Substitute.For<ILogger<CorrelateMqEvent.Handler>>();
        var handler = new CorrelateMqEvent.Handler(correlator, logger);

        var mqEvent = new LegacyMqEventIngestedEvent(
            "evt-002", null, "UNKNOWN.QUEUE", null, null, null, null, null, "Medium", null, FixedNow);

        // Should complete without throwing
        var exception = await Record.ExceptionAsync(() =>
            handler.Handle(mqEvent, CancellationToken.None));

        exception.Should().BeNull();
    }
}

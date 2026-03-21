using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ApproveKnowledgeCapture;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.CaptureExternalAIResponse;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ConfigureExternalAIPolicy;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.GetExternalAIUsage;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ListKnowledgeCaptures;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ReuseKnowledgeCapture;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Features;

/// <summary>
/// Testes unitários dos handlers ExternalAI implementados na Fase 2.
/// Valida: CaptureExternalAIResponse, ApproveKnowledgeCapture, ListKnowledgeCaptures,
/// GetExternalAIUsage, ReuseKnowledgeCapture, ConfigureExternalAIPolicy.
/// </summary>
public sealed class Phase2ExternalAiHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProviderId = Guid.NewGuid();
    private static readonly Guid CaptureId = Guid.NewGuid();
    private static readonly Guid ConsultationId = Guid.NewGuid();

    // ── CaptureExternalAIResponse ─────────────────────────────────────────

    [Fact]
    public async Task Capture_ShouldSucceed_WhenProviderExists()
    {
        var providerRepo = Substitute.For<IExternalAiProviderRepository>();
        var consultationRepo = Substitute.For<IExternalAiConsultationRepository>();
        var captureRepo = Substitute.For<IKnowledgeCaptureRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<CaptureExternalAIResponse.Handler>>();

        providerRepo.ExistsAsync(Arg.Any<ExternalAiProviderId>(), Arg.Any<CancellationToken>()).Returns(true);
        currentUser.Email.Returns("engineer@nextraceone.io");
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new CaptureExternalAIResponse.Handler(
            providerRepo, consultationRepo, captureRepo, currentUser, dateTimeProvider, logger);

        var command = new CaptureExternalAIResponse.Command(
            ProviderId, "change-analysis", "What is the blast radius?", "The blast radius is 3 services.",
            250, 0.85m, "Blast Radius Analysis", "change-analysis", "production,critical");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Blast Radius Analysis");
        result.Value.Category.Should().Be("change-analysis");
        result.Value.Status.Should().Be("Pending");
        result.Value.CapturedAt.Should().Be(FixedNow);
        await captureRepo.Received(1).AddAsync(Arg.Any<KnowledgeCapture>(), Arg.Any<CancellationToken>());
        await consultationRepo.Received(1).AddAsync(Arg.Any<ExternalAiConsultation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capture_ShouldFail_WhenProviderNotFound()
    {
        var providerRepo = Substitute.For<IExternalAiProviderRepository>();
        var consultationRepo = Substitute.For<IExternalAiConsultationRepository>();
        var captureRepo = Substitute.For<IKnowledgeCaptureRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<CaptureExternalAIResponse.Handler>>();

        providerRepo.ExistsAsync(Arg.Any<ExternalAiProviderId>(), Arg.Any<CancellationToken>()).Returns(false);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new CaptureExternalAIResponse.Handler(
            providerRepo, consultationRepo, captureRepo, currentUser, dateTimeProvider, logger);

        var command = new CaptureExternalAIResponse.Command(
            Guid.NewGuid(), "context", "query", "response", 100, 0.5m, "Title", "category", "tag");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await captureRepo.DidNotReceive().AddAsync(Arg.Any<KnowledgeCapture>(), Arg.Any<CancellationToken>());
    }

    // ── ApproveKnowledgeCapture ───────────────────────────────────────────

    [Fact]
    public async Task Approve_ShouldSucceed_WhenCaptureIsPending()
    {
        var captureRepo = Substitute.For<IKnowledgeCaptureRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<ApproveKnowledgeCapture.Handler>>();

        var capture = KnowledgeCapture.Capture(
            ExternalAiConsultationId.New(), "Test Title", "Test content for capture", "test", "unit-test", FixedNow);

        captureRepo.GetByIdAsync(Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);
        currentUser.Email.Returns("reviewer@nextraceone.io");
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new ApproveKnowledgeCapture.Handler(captureRepo, currentUser, dateTimeProvider, logger);
        var command = new ApproveKnowledgeCapture.Command(CaptureId, "Looks good.");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Approved");
        result.Value.ApprovedBy.Should().Be("reviewer@nextraceone.io");
        await captureRepo.Received(1).UpdateAsync(Arg.Any<KnowledgeCapture>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_ShouldFail_WhenCaptureNotFound()
    {
        var captureRepo = Substitute.For<IKnowledgeCaptureRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<ApproveKnowledgeCapture.Handler>>();

        captureRepo.GetByIdAsync(Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>()).Returns((KnowledgeCapture?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new ApproveKnowledgeCapture.Handler(captureRepo, currentUser, dateTimeProvider, logger);
        var command = new ApproveKnowledgeCapture.Command(Guid.NewGuid(), null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await captureRepo.DidNotReceive().UpdateAsync(Arg.Any<KnowledgeCapture>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_ShouldFail_WhenCaptureAlreadyApproved()
    {
        var captureRepo = Substitute.For<IKnowledgeCaptureRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<ApproveKnowledgeCapture.Handler>>();

        var capture = KnowledgeCapture.Capture(
            ExternalAiConsultationId.New(), "Title", "Content", "cat", "tag", FixedNow);
        capture.Approve("first-reviewer@test.io", FixedNow);

        captureRepo.GetByIdAsync(Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);
        currentUser.Email.Returns("second-reviewer@test.io");
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new ApproveKnowledgeCapture.Handler(captureRepo, currentUser, dateTimeProvider, logger);
        var result = await handler.Handle(new ApproveKnowledgeCapture.Command(CaptureId, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── ListKnowledgeCaptures ─────────────────────────────────────────────

    [Fact]
    public async Task List_ShouldReturnPaginatedCaptures()
    {
        var captureRepo = Substitute.For<IKnowledgeCaptureRepository>();

        var captures = new List<KnowledgeCapture>
        {
            KnowledgeCapture.Capture(ExternalAiConsultationId.New(), "Title 1", "Content 1", "cat1", "tag1", FixedNow),
            KnowledgeCapture.Capture(ExternalAiConsultationId.New(), "Title 2", "Content 2", "cat2", "tag2", FixedNow)
        };

        captureRepo.ListAsync(null, null, null, null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((captures, 2));

        var handler = new ListKnowledgeCaptures.Handler(captureRepo);
        var query = new ListKnowledgeCaptures.Query(null, null, null, null, null, null, 1, 20);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task List_ShouldReturnEmptyList_WhenNoCaptures()
    {
        var captureRepo = Substitute.For<IKnowledgeCaptureRepository>();
        captureRepo.ListAsync(Arg.Any<KnowledgeStatus?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<KnowledgeCapture>(), 0));

        var handler = new ListKnowledgeCaptures.Handler(captureRepo);
        var result = await handler.Handle(new ListKnowledgeCaptures.Query(null, null, null, null, null, null, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── GetExternalAIUsage ────────────────────────────────────────────────

    [Fact]
    public async Task GetUsage_ShouldReturnAggregatedMetrics()
    {
        var captureRepo = Substitute.For<IKnowledgeCaptureRepository>();
        var metrics = new ExternalAiUsageMetrics(
            TotalConsultations: 100,
            CompletedConsultations: 85,
            FailedConsultations: 5,
            TotalTokensUsed: 25_000L,
            ByProvider: new List<ProviderUsageMetric> { new("provider-1", 100, 25_000L) },
            TotalCaptures: 30,
            ApprovedCaptures: 20,
            RejectedCaptures: 5,
            PendingCaptures: 5,
            TotalReuses: 40L);

        captureRepo.GetUsageMetricsAsync(null, null, Arg.Any<CancellationToken>()).Returns(metrics);

        var handler = new GetExternalAIUsage.Handler(captureRepo);
        var result = await handler.Handle(new GetExternalAIUsage.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalConsultations.Should().Be(100);
        result.Value.TotalTokensUsed.Should().Be(25_000L);
        result.Value.ApprovalRatePct.Should().Be(Math.Round(20.0 / 30 * 100, 2));
        result.Value.ReuseRatePct.Should().Be(Math.Round(40.0 / 20 * 100, 2));
    }

    // ── ReuseKnowledgeCapture ─────────────────────────────────────────────

    [Fact]
    public async Task Reuse_ShouldSucceed_WhenCaptureIsApproved()
    {
        var captureRepo = Substitute.For<IKnowledgeCaptureRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<ReuseKnowledgeCapture.Handler>>();

        var capture = KnowledgeCapture.Capture(
            ExternalAiConsultationId.New(), "Reusable Title", "Content to reuse", "category", "tag", FixedNow);
        capture.Approve("approver@nextraceone.io", FixedNow);

        captureRepo.GetByIdAsync(Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);
        currentUser.Email.Returns("user@nextraceone.io");
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new ReuseKnowledgeCapture.Handler(captureRepo, currentUser, dateTimeProvider, logger);
        var command = new ReuseKnowledgeCapture.Command(CaptureId, "new context for reuse", "testing purpose");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UpdatedReuseCount.Should().Be(1);
        result.Value.NewContext.Should().Be("new context for reuse");
        await captureRepo.Received(1).UpdateAsync(Arg.Any<KnowledgeCapture>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reuse_ShouldFail_WhenCaptureIsPending()
    {
        var captureRepo = Substitute.For<IKnowledgeCaptureRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<ReuseKnowledgeCapture.Handler>>();

        var capture = KnowledgeCapture.Capture(
            ExternalAiConsultationId.New(), "Title", "Content", "cat", "tag", FixedNow);
        // No approval — capture is Pending

        captureRepo.GetByIdAsync(Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>()).Returns(capture);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new ReuseKnowledgeCapture.Handler(captureRepo, currentUser, dateTimeProvider, logger);
        var result = await handler.Handle(new ReuseKnowledgeCapture.Command(CaptureId, "ctx", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── ConfigureExternalAIPolicy ─────────────────────────────────────────

    [Fact]
    public async Task ConfigurePolicy_ShouldCreateNewPolicy_WhenNameNotExists()
    {
        var policyRepo = Substitute.For<IExternalAiPolicyRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<ConfigureExternalAIPolicy.Handler>>();

        policyRepo.GetByNameAsync("strict-external-ai", Arg.Any<CancellationToken>()).Returns((ExternalAiPolicy?)null);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new ConfigureExternalAIPolicy.Handler(policyRepo, dateTimeProvider, logger);
        var command = new ConfigureExternalAIPolicy.Command(
            "strict-external-ai", "Strict policy for production", 50, 100_000L, true, "change-analysis,incident");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Action.Should().Be("Created");
        result.Value.Name.Should().Be("strict-external-ai");
        result.Value.RequiresApproval.Should().BeTrue();
        await policyRepo.Received(1).AddAsync(Arg.Any<ExternalAiPolicy>(), Arg.Any<CancellationToken>());
        await policyRepo.DidNotReceive().UpdateAsync(Arg.Any<ExternalAiPolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfigurePolicy_ShouldUpdateExistingPolicy_WhenNameExists()
    {
        var policyRepo = Substitute.For<IExternalAiPolicyRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var logger = Substitute.For<ILogger<ConfigureExternalAIPolicy.Handler>>();

        var existing = ExternalAiPolicy.Create(
            "my-policy", "Old description", 10, 5_000L, false, "general", FixedNow);

        policyRepo.GetByNameAsync("my-policy", Arg.Any<CancellationToken>()).Returns(existing);
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var handler = new ConfigureExternalAIPolicy.Handler(policyRepo, dateTimeProvider, logger);
        var command = new ConfigureExternalAIPolicy.Command(
            "my-policy", "Updated description", 100, 500_000L, true, "change-analysis");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Action.Should().Be("Updated");
        await policyRepo.DidNotReceive().AddAsync(Arg.Any<ExternalAiPolicy>(), Arg.Any<CancellationToken>());
        await policyRepo.Received(1).UpdateAsync(Arg.Any<ExternalAiPolicy>(), Arg.Any<CancellationToken>());
    }
}

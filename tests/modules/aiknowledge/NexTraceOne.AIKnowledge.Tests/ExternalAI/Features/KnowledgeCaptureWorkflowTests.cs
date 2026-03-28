using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ApproveKnowledgeCapture;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.CaptureExternalAIResponse;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ListKnowledgeCaptures;
using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ReuseKnowledgeCapture;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Features;

/// <summary>
/// Testes unitários dos handlers de Knowledge Capture Workflow:
/// CaptureExternalAIResponse, ListKnowledgeCaptures, ApproveKnowledgeCapture, ReuseKnowledgeCapture.
/// </summary>
public sealed class KnowledgeCaptureWorkflowTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IExternalAiProviderRepository _providerRepository =
        Substitute.For<IExternalAiProviderRepository>();
    private readonly IExternalAiConsultationRepository _consultationRepository =
        Substitute.For<IExternalAiConsultationRepository>();
    private readonly IKnowledgeCaptureRepository _captureRepository =
        Substitute.For<IKnowledgeCaptureRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public KnowledgeCaptureWorkflowTests()
    {
        _currentUser.Id.Returns("user-001");
        _currentUser.Name.Returns("Test User");
        _currentUser.Email.Returns("test@example.com");
        _dateTimeProvider.UtcNow.Returns(FixedNow);
    }

    // ── CaptureExternalAIResponse ─────────────────────────────────────────

    [Fact]
    public async Task CaptureExternalAIResponse_ShouldPersistConsultationAndCapture_WhenProviderExists()
    {
        var providerId = Guid.NewGuid();
        _providerRepository.ExistsAsync(
            Arg.Is<ExternalAiProviderId>(id => id.Value == providerId),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CaptureExternalAIResponse.Command(
            ProviderId: providerId,
            Context: "Service payment-api had a deployment today.",
            Query: "What is the risk of this deployment?",
            AiResponse: "The deployment carries medium risk due to breaking change in v2 contract.",
            TokensUsed: 250,
            Confidence: 0.85m,
            Title: "Deployment risk classification — payment-api",
            Category: "change-analysis",
            Tags: "deployment,risk,payment-api");

        var handler = new CaptureExternalAIResponse.Handler(
            _providerRepository, _consultationRepository, _captureRepository,
            _currentUser, _dateTimeProvider);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(KnowledgeStatus.Pending.ToString());
        result.Value.Title.Should().Be(command.Title);
        result.Value.Category.Should().Be(command.Category);
        result.Value.CapturedAt.Should().Be(FixedNow);

        await _consultationRepository.Received(1).AddAsync(
            Arg.Any<ExternalAiConsultation>(), Arg.Any<CancellationToken>());
        await _captureRepository.Received(1).AddAsync(
            Arg.Any<KnowledgeCapture>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CaptureExternalAIResponse_ShouldFail_WhenProviderDoesNotExist()
    {
        var providerId = Guid.NewGuid();
        _providerRepository.ExistsAsync(Arg.Any<ExternalAiProviderId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CaptureExternalAIResponse.Command(
            ProviderId: providerId,
            Context: "ctx",
            Query: "query",
            AiResponse: "response",
            TokensUsed: 10,
            Confidence: 0.5m,
            Title: "title",
            Category: "category",
            Tags: "tag1");

        var handler = new CaptureExternalAIResponse.Handler(
            _providerRepository, _consultationRepository, _captureRepository,
            _currentUser, _dateTimeProvider);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Provider.NotFound");

        await _consultationRepository.DidNotReceive()
            .AddAsync(Arg.Any<ExternalAiConsultation>(), Arg.Any<CancellationToken>());
        await _captureRepository.DidNotReceive()
            .AddAsync(Arg.Any<KnowledgeCapture>(), Arg.Any<CancellationToken>());
    }

    // ── ListKnowledgeCaptures ─────────────────────────────────────────────

    [Fact]
    public async Task ListKnowledgeCaptures_ShouldReturnPagedItems_WhenCapturesExist()
    {
        var capture = KnowledgeCapture.Capture(
            ExternalAiConsultationId.New(),
            "Breaking change detection",
            "Content about breaking changes",
            "change-analysis",
            "breaking-change,rest",
            FixedNow);

        _captureRepository.ListAsync(
            Arg.Any<KnowledgeStatus?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            1, 20,
            Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<KnowledgeCapture>)[capture], 1));

        var query = new ListKnowledgeCaptures.Query(
            Status: null, Category: null, Tags: null, TextFilter: null,
            From: null, To: null, Page: 1, PageSize: 20);

        var handler = new ListKnowledgeCaptures.Handler(_captureRepository);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Title.Should().Be("Breaking change detection");
        result.Value.Items[0].Status.Should().Be(KnowledgeStatus.Pending.ToString());
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task ListKnowledgeCaptures_ShouldReturnEmpty_WhenNoCapturesExist()
    {
        _captureRepository.ListAsync(
            Arg.Any<KnowledgeStatus?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            1, 20,
            Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<KnowledgeCapture>)[], 0));

        var query = new ListKnowledgeCaptures.Query(
            Status: null, Category: null, Tags: null, TextFilter: null,
            From: null, To: null, Page: 1, PageSize: 20);

        var handler = new ListKnowledgeCaptures.Handler(_captureRepository);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalPages.Should().Be(0);
    }

    // ── ApproveKnowledgeCapture ───────────────────────────────────────────

    [Fact]
    public async Task ApproveKnowledgeCapture_ShouldApprove_WhenCapturePending()
    {
        var capture = KnowledgeCapture.Capture(
            ExternalAiConsultationId.New(),
            "Pattern for resilient retries",
            "Use exponential backoff with jitter.",
            "engineering",
            "retry,resilience",
            FixedNow);

        _captureRepository.GetByIdAsync(
            Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>())
            .Returns(capture);

        var command = new ApproveKnowledgeCapture.Command(
            CaptureId: capture.Id.Value,
            ReviewNotes: "Validated by principal engineer.");

        var handler = new ApproveKnowledgeCapture.Handler(
            _captureRepository, _currentUser, _dateTimeProvider);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(KnowledgeStatus.Approved.ToString());
        result.Value.ApprovedBy.Should().Be("user-001");
        result.Value.ApprovedAt.Should().Be(FixedNow);

        await _captureRepository.Received(1).UpdateAsync(capture, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveKnowledgeCapture_ShouldFail_WhenCaptureNotFound()
    {
        _captureRepository.GetByIdAsync(Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>())
            .Returns((KnowledgeCapture?)null);

        var command = new ApproveKnowledgeCapture.Command(
            CaptureId: Guid.NewGuid(),
            ReviewNotes: null);

        var handler = new ApproveKnowledgeCapture.Handler(
            _captureRepository, _currentUser, _dateTimeProvider);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("KnowledgeCapture.NotFound");
        await _captureRepository.DidNotReceive().UpdateAsync(
            Arg.Any<KnowledgeCapture>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveKnowledgeCapture_ShouldFail_WhenAlreadyReviewed()
    {
        var capture = KnowledgeCapture.Capture(
            ExternalAiConsultationId.New(),
            "Some insight",
            "Content",
            "category",
            "tag",
            FixedNow);
        capture.Approve("lead@co.com", FixedNow.AddHours(1));

        _captureRepository.GetByIdAsync(Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>())
            .Returns(capture);

        var command = new ApproveKnowledgeCapture.Command(
            CaptureId: capture.Id.Value,
            ReviewNotes: null);

        var handler = new ApproveKnowledgeCapture.Handler(
            _captureRepository, _currentUser, _dateTimeProvider);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyReviewed");
        await _captureRepository.DidNotReceive().UpdateAsync(
            Arg.Any<KnowledgeCapture>(), Arg.Any<CancellationToken>());
    }

    // ── ReuseKnowledgeCapture ─────────────────────────────────────────────

    [Fact]
    public async Task ReuseKnowledgeCapture_ShouldIncrementReuseCount_WhenCaptureApproved()
    {
        var capture = KnowledgeCapture.Capture(
            ExternalAiConsultationId.New(),
            "Retry pattern insight",
            "Exponential backoff details",
            "engineering",
            "retry",
            FixedNow);
        capture.Approve("lead@co.com", FixedNow.AddHours(1));

        _captureRepository.GetByIdAsync(Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>())
            .Returns(capture);

        var command = new ReuseKnowledgeCapture.Command(
            CaptureId: capture.Id.Value,
            NewContext: "Applying retry pattern to payment-api circuit breaker",
            Purpose: "Improve resilience of payment service");

        var handler = new ReuseKnowledgeCapture.Handler(
            _captureRepository, _currentUser, _dateTimeProvider);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UpdatedReuseCount.Should().Be(1);
        result.Value.ReusedBy.Should().Be("user-001");
        result.Value.ReusedAt.Should().Be(FixedNow);
        result.Value.NewContext.Should().Be(command.NewContext);
        result.Value.Purpose.Should().Be(command.Purpose);

        await _captureRepository.Received(1).UpdateAsync(capture, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReuseKnowledgeCapture_ShouldFail_WhenCaptureNotFound()
    {
        _captureRepository.GetByIdAsync(Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>())
            .Returns((KnowledgeCapture?)null);

        var command = new ReuseKnowledgeCapture.Command(
            CaptureId: Guid.NewGuid(),
            NewContext: "some context",
            Purpose: null);

        var handler = new ReuseKnowledgeCapture.Handler(
            _captureRepository, _currentUser, _dateTimeProvider);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("KnowledgeCapture.NotFound");
    }

    [Fact]
    public async Task ReuseKnowledgeCapture_ShouldFail_WhenCaptureNotApproved()
    {
        var capture = KnowledgeCapture.Capture(
            ExternalAiConsultationId.New(),
            "Pending capture",
            "Some content",
            "category",
            "tag",
            FixedNow);

        _captureRepository.GetByIdAsync(Arg.Any<KnowledgeCaptureId>(), Arg.Any<CancellationToken>())
            .Returns(capture);

        var command = new ReuseKnowledgeCapture.Command(
            CaptureId: capture.Id.Value,
            NewContext: "trying to reuse a pending capture",
            Purpose: null);

        var handler = new ReuseKnowledgeCapture.Handler(
            _captureRepository, _currentUser, _dateTimeProvider);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("KnowledgeCapture.NotApproved");
        await _captureRepository.DidNotReceive().UpdateAsync(
            Arg.Any<KnowledgeCapture>(), Arg.Any<CancellationToken>());
    }
}

using System.IO;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ExportPendingTrajectories;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentPerformanceDashboard;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitAgentExecutionFeedback;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários do Agent Lightning (Phase 10).
/// Cobre domínio, handlers e exportação de trajectórias para treino RL.
/// </summary>
public sealed class AgentLightningTests
{
    private readonly IAiAgentExecutionRepository _executionRepo = Substitute.For<IAiAgentExecutionRepository>();
    private readonly IAiAgentTrajectoryFeedbackRepository _feedbackRepo = Substitute.For<IAiAgentTrajectoryFeedbackRepository>();
    private readonly IAiAgentPerformanceMetricRepository _metricRepo = Substitute.For<IAiAgentPerformanceMetricRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly AiAgentId _agentId = AiAgentId.New();
    private static readonly AiAgentExecutionId _executionId = AiAgentExecutionId.New();

    // ── AiAgentTrajectoryFeedback.Submit ──────────────────────────────────

    [Fact]
    public void Submit_ValidInputs_CreatesFeedbackWithAllFields()
    {
        var submittedAt = DateTimeOffset.UtcNow;

        var feedback = AiAgentTrajectoryFeedback.Submit(
            executionId: _executionId,
            rating: 5,
            outcome: "resolved",
            comment: "Perfect outcome",
            actualOutcome: "incident resolved in 10 minutes",
            wasCorrect: true,
            timeToResolveMinutes: 10,
            submittedBy: "user-1",
            tenantId: _tenantId,
            submittedAt: submittedAt);

        feedback.ExecutionId.Should().Be(_executionId);
        feedback.Rating.Should().Be(5);
        feedback.Outcome.Should().Be("resolved");
        feedback.Comment.Should().Be("Perfect outcome");
        feedback.ActualOutcome.Should().Be("incident resolved in 10 minutes");
        feedback.WasCorrect.Should().BeTrue();
        feedback.TimeToResolveMinutes.Should().Be(10);
        feedback.SubmittedBy.Should().Be("user-1");
        feedback.TenantId.Should().Be(_tenantId);
        feedback.SubmittedAt.Should().Be(submittedAt);
        feedback.ExportedForTraining.Should().BeFalse();
        feedback.ExportedAt.Should().BeNull();
        feedback.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Submit_RatingOutOfRange_Throws()
    {
        var act = () => AiAgentTrajectoryFeedback.Submit(
            _executionId, 6, "resolved", null, null, false, null, "user-1", _tenantId, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Submit_RatingBelowMinimum_Throws()
    {
        var act = () => AiAgentTrajectoryFeedback.Submit(
            _executionId, 0, "resolved", null, null, false, null, "user-1", _tenantId, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Submit_EmptyTenantId_Throws()
    {
        var act = () => AiAgentTrajectoryFeedback.Submit(
            _executionId, 5, "resolved", null, null, true, null, "user-1", Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkExported_SetsFlagsCorrectly()
    {
        var feedback = AiAgentTrajectoryFeedback.Submit(
            _executionId, 4, "partial", null, null, false, null, "user-1", _tenantId, DateTimeOffset.UtcNow);

        var exportedAt = DateTimeOffset.UtcNow;
        feedback.MarkExported(exportedAt);

        feedback.ExportedForTraining.Should().BeTrue();
        feedback.ExportedAt.Should().Be(exportedAt);
    }

    // ── AiAgentPerformanceMetric.Create ───────────────────────────────────

    [Fact]
    public void Create_ValidInputs_CreatesMetricCorrectly()
    {
        var periodStart = DateTimeOffset.UtcNow.AddDays(-30);
        var periodEnd = DateTimeOffset.UtcNow;

        var metric = AiAgentPerformanceMetric.Create(
            agentId: _agentId,
            agentName: "incident-triage-agent",
            periodStart: periodStart,
            periodEnd: periodEnd,
            totalExecutions: 150,
            executionsWithFeedback: 80,
            averageRating: 4.2,
            accuracyRate: 0.85,
            tenantId: _tenantId);

        metric.AgentId.Should().Be(_agentId);
        metric.AgentName.Should().Be("incident-triage-agent");
        metric.PeriodStart.Should().Be(periodStart);
        metric.PeriodEnd.Should().Be(periodEnd);
        metric.TotalExecutions.Should().Be(150);
        metric.ExecutionsWithFeedback.Should().Be(80);
        metric.AverageRating.Should().Be(4.2);
        metric.AccuracyRate.Should().Be(0.85);
        metric.TenantId.Should().Be(_tenantId);
        metric.RlCyclesCompleted.Should().Be(0);
        metric.TrajectoriesExported.Should().Be(0);
        metric.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_EmptyTenantId_Throws()
    {
        var act = () => AiAgentPerformanceMetric.Create(
            _agentId, "agent", DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow, 100, 50, 4.0, 0.8, Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateRlCycles_SetsCorrectly()
    {
        var metric = CreateMetric();

        metric.UpdateRlCycles(5);

        metric.RlCyclesCompleted.Should().Be(5);
    }

    [Fact]
    public void IncrementTrajectoriesExported_AddsToCounter()
    {
        var metric = CreateMetric();

        metric.IncrementTrajectoriesExported(25);
        metric.IncrementTrajectoriesExported(15);

        metric.TrajectoriesExported.Should().Be(40);
    }

    // ── SubmitAgentExecutionFeedback handler ──────────────────────────────

    [Fact]
    public async Task SubmitFeedback_ValidExecution_CreatesFeedbackSuccessfully()
    {
        var execution = CreateExecution();
        _executionRepo.GetByIdAsync(
            Arg.Is<AiAgentExecutionId>(x => x == _executionId),
            Arg.Any<CancellationToken>())
            .Returns(execution);
        _feedbackRepo.ExistsByExecutionIdAsync(
            Arg.Any<AiAgentExecutionId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new SubmitAgentExecutionFeedback.Command(
            ExecutionId: _executionId.Value,
            Rating: 5,
            Outcome: "resolved",
            Comment: "Great job",
            ActualOutcome: null,
            WasCorrect: true,
            TimeToResolveMinutes: 8,
            SubmittedBy: "user-1",
            TenantId: _tenantId);

        var handler = new SubmitAgentExecutionFeedback.Handler(
            _executionRepo, _feedbackRepo, _unitOfWork);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(5);
        result.Value.WasCorrect.Should().BeTrue();
        result.Value.FeedbackId.Should().NotBeEmpty();
        _feedbackRepo.Received(1).Add(Arg.Any<AiAgentTrajectoryFeedback>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitFeedback_ExecutionNotFound_ReturnsError()
    {
        _executionRepo.GetByIdAsync(Arg.Any<AiAgentExecutionId>(), Arg.Any<CancellationToken>())
            .Returns((AiAgentExecution?)null);

        var command = new SubmitAgentExecutionFeedback.Command(
            Guid.NewGuid(), 5, "resolved", null, null, true, null, "user-1", _tenantId);

        var handler = new SubmitAgentExecutionFeedback.Handler(
            _executionRepo, _feedbackRepo, _unitOfWork);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AgentExecution.NotFound");
    }

    [Fact]
    public async Task SubmitFeedback_DuplicateFeedback_ReturnsError()
    {
        var execution = CreateExecution();
        _executionRepo.GetByIdAsync(
            Arg.Any<AiAgentExecutionId>(), Arg.Any<CancellationToken>())
            .Returns(execution);
        _feedbackRepo.ExistsByExecutionIdAsync(
            Arg.Any<AiAgentExecutionId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new SubmitAgentExecutionFeedback.Command(
            _executionId.Value, 5, "resolved", null, null, true, null, "user-1", _tenantId);

        var handler = new SubmitAgentExecutionFeedback.Handler(
            _executionRepo, _feedbackRepo, _unitOfWork);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FeedbackAlreadyExists");
        _feedbackRepo.DidNotReceive().Add(Arg.Any<AiAgentTrajectoryFeedback>());
    }

    // ── GetAgentPerformanceDashboard handler ──────────────────────────────

    [Fact]
    public async Task GetPerformanceDashboard_WithMetrics_ReturnsAggregatedData()
    {
        var metrics = new List<AiAgentPerformanceMetric>
        {
            CreateMetric(totalExecutions: 100, withFeedback: 60, accuracyRate: 0.85, rlCycles: 2, trajectories: 50),
            CreateMetric(totalExecutions: 80, withFeedback: 40, accuracyRate: 0.70, rlCycles: 1, trajectories: 30),
        };
        _metricRepo.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(metrics.AsReadOnly());

        var handler = new GetAgentPerformanceDashboard.Handler(_metricRepo);
        var result = await handler.Handle(new GetAgentPerformanceDashboard.Query(_tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AgentItems.Should().HaveCount(2);
        result.Value.TotalTrajectoriesCollected.Should().Be(80);
        result.Value.WithFeedbackConfirmed.Should().Be(100);
        result.Value.TotalRlCyclesCompleted.Should().Be(3);
    }

    [Fact]
    public async Task GetPerformanceDashboard_EmptyTenant_ReturnsEmptyResponse()
    {
        _metricRepo.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<AiAgentPerformanceMetric>().AsReadOnly());

        var handler = new GetAgentPerformanceDashboard.Handler(_metricRepo);
        var result = await handler.Handle(new GetAgentPerformanceDashboard.Query(_tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AgentItems.Should().BeEmpty();
        result.Value.TotalTrajectoriesCollected.Should().Be(0);
        result.Value.TotalRlCyclesCompleted.Should().Be(0);
    }

    [Fact]
    public async Task GetPerformanceDashboard_HighAccuracy_ShowsImprovingTrend()
    {
        var metrics = new List<AiAgentPerformanceMetric>
        {
            CreateMetric(accuracyRate: 0.9),
        };
        _metricRepo.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(metrics.AsReadOnly());

        var handler = new GetAgentPerformanceDashboard.Handler(_metricRepo);
        var result = await handler.Handle(new GetAgentPerformanceDashboard.Query(_tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AgentItems[0].AccuracyTrend.Should().Be("improving");
    }

    [Fact]
    public async Task GetPerformanceDashboard_LowAccuracy_ShowsDegradingTrend()
    {
        var metrics = new List<AiAgentPerformanceMetric>
        {
            CreateMetric(accuracyRate: 0.3),
        };
        _metricRepo.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(metrics.AsReadOnly());

        var handler = new GetAgentPerformanceDashboard.Handler(_metricRepo);
        var result = await handler.Handle(new GetAgentPerformanceDashboard.Query(_tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AgentItems[0].AccuracyTrend.Should().Be("degrading");
    }

    // ── ExportPendingTrajectories handler ─────────────────────────────────

    [Fact]
    public async Task ExportPendingTrajectories_WithPending_ExportsToFiles()
    {
        var exportDir = Path.Combine(
            Path.GetTempPath(), $"nextrace_test_{Guid.NewGuid():N}");

        try
        {
            var feedback = CreateFeedback();
            var execution = CreateExecution();

            _feedbackRepo.ListPendingExportAsync(50, Arg.Any<CancellationToken>())
                .Returns(new List<AiAgentTrajectoryFeedback> { feedback }.AsReadOnly());
            _executionRepo.GetByIdAsync(
                Arg.Is<AiAgentExecutionId>(x => x == feedback.ExecutionId),
                Arg.Any<CancellationToken>())
                .Returns(execution);

            var handler = new ExportPendingTrajectories.Handler(
                _feedbackRepo, _executionRepo, _unitOfWork);

            var result = await handler.Handle(
                new ExportPendingTrajectories.Command(50, exportDir, null),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.ExportedCount.Should().Be(1);
            result.Value.ExportedFiles.Should().HaveCount(1);

            var filePath = result.Value.ExportedFiles[0];
            File.Exists(filePath).Should().BeTrue();

            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Contain("trajectory_id");
            content.Should().Contain("feedback");

            await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        }
        finally
        {
            if (Directory.Exists(exportDir))
                Directory.Delete(exportDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExportPendingTrajectories_NoPending_ReturnsZeroCount()
    {
        _feedbackRepo.ListPendingExportAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<AiAgentTrajectoryFeedback>().AsReadOnly());

        var exportDir = Path.Combine(Path.GetTempPath(), $"nextrace_test_{Guid.NewGuid():N}");

        var handler = new ExportPendingTrajectories.Handler(
            _feedbackRepo, _executionRepo, _unitOfWork);

        var result = await handler.Handle(
            new ExportPendingTrajectories.Command(50, exportDir, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExportedCount.Should().Be(0);
        result.Value.ExportedFiles.Should().BeEmpty();
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExportPendingTrajectories_SkipsIfExecutionMissing()
    {
        var exportDir = Path.Combine(Path.GetTempPath(), $"nextrace_test_{Guid.NewGuid():N}");

        try
        {
            var feedback = CreateFeedback();

            _feedbackRepo.ListPendingExportAsync(50, Arg.Any<CancellationToken>())
                .Returns(new List<AiAgentTrajectoryFeedback> { feedback }.AsReadOnly());
            _executionRepo.GetByIdAsync(Arg.Any<AiAgentExecutionId>(), Arg.Any<CancellationToken>())
                .Returns((AiAgentExecution?)null);

            var handler = new ExportPendingTrajectories.Handler(
                _feedbackRepo, _executionRepo, _unitOfWork);

            var result = await handler.Handle(
                new ExportPendingTrajectories.Command(50, exportDir, null),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.ExportedCount.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(exportDir))
                Directory.Delete(exportDir, recursive: true);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AiAgentExecution CreateExecution()
    {
        var execution = AiAgentExecution.Start(
            agentId: _agentId,
            executedBy: "user-1",
            modelIdUsed: Guid.NewGuid(),
            providerUsed: "OpenAI",
            inputJson: """{"query": "investigate incident"}""",
            contextJson: null,
            startedAt: DateTimeOffset.UtcNow,
            correlationId: Guid.NewGuid().ToString("N"));

        return execution;
    }

    private static AiAgentTrajectoryFeedback CreateFeedback()
        => AiAgentTrajectoryFeedback.Submit(
            executionId: _executionId,
            rating: 4,
            outcome: "resolved",
            comment: "Good result",
            actualOutcome: null,
            wasCorrect: true,
            timeToResolveMinutes: 5,
            submittedBy: "user-1",
            tenantId: _tenantId,
            submittedAt: DateTimeOffset.UtcNow);

    private static AiAgentPerformanceMetric CreateMetric(
        long totalExecutions = 100,
        long withFeedback = 50,
        double accuracyRate = 0.75,
        int rlCycles = 0,
        long trajectories = 0)
    {
        var metric = AiAgentPerformanceMetric.Create(
            agentId: _agentId,
            agentName: "test-agent",
            periodStart: DateTimeOffset.UtcNow.AddDays(-30),
            periodEnd: DateTimeOffset.UtcNow,
            totalExecutions: totalExecutions,
            executionsWithFeedback: withFeedback,
            averageRating: 4.0,
            accuracyRate: accuracyRate,
            tenantId: _tenantId);

        if (rlCycles > 0) metric.UpdateRlCycles(rlCycles);
        if (trajectories > 0) metric.IncrementTrajectoriesExported(trajectories);

        return metric;
    }
}

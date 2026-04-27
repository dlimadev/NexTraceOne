using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateAiEvalDataset;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiEvalReport;
using NexTraceOne.AIKnowledge.Application.Governance.Features.RunAiEvaluation;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários do AI Evaluation Harness CC-05.
/// Cobre CreateAiEvalDataset, RunAiEvaluation e GetAiEvalReport.
/// </summary>
public sealed class AiEvalHarnessCC05Tests
{
    private readonly IAiEvalDatasetRepository _datasetRepo = Substitute.For<IAiEvalDatasetRepository>();
    private readonly IAiEvalRunRepository _runRepo = Substitute.For<IAiEvalRunRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private readonly DateTimeOffset _now = new(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

    public AiEvalHarnessCC05Tests() => _clock.UtcNow.Returns(_now);

    // ── CreateAiEvalDataset ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateAiEvalDataset_ValidInput_Persists()
    {
        var handler = new CreateAiEvalDataset.Handler(_datasetRepo, _uow, _clock);

        var cmd = new CreateAiEvalDataset.Command(
            "tenant-1", "contract-change-v1", "change-confidence",
            "Test dataset for change confidence use case",
            """[{"id":"tc-1","input":"query","expectedOutput":"result"}]""", 1);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("contract-change-v1");
        result.Value.TestCaseCount.Should().Be(1);
        await _datasetRepo.Received(1).AddAsync(Arg.Any<AiEvalDataset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAiEvalDataset_EmptyCases_ReturnsSuccessWithZeroCount()
    {
        var handler = new CreateAiEvalDataset.Handler(_datasetRepo, _uow, _clock);

        var cmd = new CreateAiEvalDataset.Command(
            "tenant-1", "empty-dataset", "incident-summary", null, "[]", 0);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TestCaseCount.Should().Be(0);
    }

    // ── RunAiEvaluation ──────────────────────────────────────────────────────

    [Fact]
    public async Task RunAiEvaluation_DatasetNotFound_ReturnsNotFound()
    {
        _datasetRepo.GetByIdAsync(Arg.Any<AiEvalDatasetId>(), Arg.Any<CancellationToken>())
            .Returns((AiEvalDataset?)null);

        var handler = new RunAiEvaluation.Handler(_datasetRepo, _runRepo, _uow, _clock);

        var result = await handler.Handle(
            new RunAiEvaluation.Command("tenant-1", Guid.NewGuid(), "claude-sonnet-4-6"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RunAiEvaluation_InactiveDataset_ReturnsFailure()
    {
        var dataset = AiEvalDataset.Create("tenant-1", "ds-1", "uc-1", null, "[]", 0, _now);
        dataset.Deactivate(_now);
        _datasetRepo.GetByIdAsync(Arg.Any<AiEvalDatasetId>(), Arg.Any<CancellationToken>())
            .Returns(dataset);

        var handler = new RunAiEvaluation.Handler(_datasetRepo, _runRepo, _uow, _clock);

        var result = await handler.Handle(
            new RunAiEvaluation.Command("tenant-1", dataset.Id.Value, "claude-opus-4-7"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RunAiEvaluation_ActiveDataset_ReturnsCompletedRun()
    {
        var dataset = AiEvalDataset.Create("tenant-1", "ds-1", "uc-1", null,
            """[{"id":"tc1","input":"q","expectedOutput":"a"}]""", 1, _now);
        _datasetRepo.GetByIdAsync(Arg.Any<AiEvalDatasetId>(), Arg.Any<CancellationToken>())
            .Returns(dataset);

        var handler = new RunAiEvaluation.Handler(_datasetRepo, _runRepo, _uow, _clock);

        var result = await handler.Handle(
            new RunAiEvaluation.Command("tenant-1", dataset.Id.Value, "claude-sonnet-4-6"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Completed");
        result.Value.CasesProcessed.Should().Be(1);
        result.Value.AverageSemanticSimilarity.Should().BeGreaterThan(0);
        result.Value.ToolCallAccuracy.Should().BeGreaterThan(0);
    }

    // ── GetAiEvalReport ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAiEvalReport_NoRuns_ReturnsEmptyReport()
    {
        _runRepo.ListByDatasetAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<AiEvalRun>());

        var handler = new GetAiEvalReport.Handler(_runRepo);

        var result = await handler.Handle(
            new GetAiEvalReport.Query("tenant-1", Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRuns.Should().Be(0);
        result.Value.ByModel.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAiEvalReport_WithCompletedRuns_GroupsByModel()
    {
        var datasetId = Guid.NewGuid();
        var run1 = AiEvalRun.Create("tenant-1", datasetId, "claude-sonnet-4-6", _now);
        run1.Start();
        run1.Complete(10, 8, 0.85m, 0.92m, 300, 900, 12000, _now);

        var run2 = AiEvalRun.Create("tenant-1", datasetId, "claude-opus-4-7", _now);
        run2.Start();
        run2.Complete(10, 9, 0.91m, 0.95m, 250, 800, 15000, _now);

        _runRepo.ListByDatasetAsync(datasetId, "tenant-1", CancellationToken.None)
            .Returns(new List<AiEvalRun> { run1, run2 });

        var handler = new GetAiEvalReport.Handler(_runRepo);
        var result = await handler.Handle(
            new GetAiEvalReport.Query("tenant-1", datasetId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRuns.Should().Be(2);
        result.Value.ByModel.Should().HaveCount(2);
        result.Value.ByModel[0].LatestSemanticSimilarity.Should().BeGreaterThanOrEqualTo(
            result.Value.ByModel[1].LatestSemanticSimilarity);
    }

    // ── Domain entity ────────────────────────────────────────────────────────

    [Fact]
    public void AiEvalDataset_Create_SetsPropertiesCorrectly()
    {
        var ds = AiEvalDataset.Create("t1", "test-dataset", "incident-summary",
            "desc", """[{"id":"1"}]""", 1, _now);

        ds.TenantId.Should().Be("t1");
        ds.Name.Should().Be("test-dataset");
        ds.IsActive.Should().BeTrue();
        ds.TestCaseCount.Should().Be(1);
        ds.CreatedAt.Should().Be(_now);
    }

    [Fact]
    public void AiEvalRun_Complete_SetsAllMetrics()
    {
        var run = AiEvalRun.Create("t1", Guid.NewGuid(), "claude-sonnet-4-6", _now);
        run.Start();
        run.Complete(20, 15, 0.80m, 0.90m, 400.0, 1100.0, 24000, _now);

        run.Status.Should().Be(AiEvalRunStatus.Completed);
        run.CasesProcessed.Should().Be(20);
        run.ExactMatchCount.Should().Be(15);
        run.AverageSemanticSimilarity.Should().Be(0.80m);
        run.ToolCallAccuracy.Should().Be(0.90m);
        run.LatencyP50Ms.Should().Be(400.0);
        run.LatencyP95Ms.Should().Be(1100.0);
        run.TotalTokenCost.Should().Be(24000);
        run.CompletedAt.Should().Be(_now);
    }

    [Fact]
    public void AiEvalRun_Fail_SetsErrorAndStatus()
    {
        var run = AiEvalRun.Create("t1", Guid.NewGuid(), "claude-opus-4-7", _now);
        run.Start();
        run.Fail("API rate limit exceeded", _now);

        run.Status.Should().Be(AiEvalRunStatus.Failed);
        run.ErrorMessage.Should().Be("API rate limit exceeded");
        run.CompletedAt.Should().Be(_now);
    }

    [Fact]
    public void AiEvalRun_SemanticSimilarity_ClampedTo01()
    {
        var run = AiEvalRun.Create("t1", Guid.NewGuid(), "model-x", _now);
        run.Start();
        run.Complete(1, 0, 1.5m, -0.1m, 100, 200, 1000, _now);

        run.AverageSemanticSimilarity.Should().Be(1.0m);
        run.ToolCallAccuracy.Should().Be(0.0m);
    }
}

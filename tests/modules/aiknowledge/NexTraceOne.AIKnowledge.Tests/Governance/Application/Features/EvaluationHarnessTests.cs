using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateEvaluationDataset;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateEvaluationRun;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateEvaluationSuite;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetEvaluationRun;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetEvaluationSuite;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListEvaluationSuites;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários do AI Evaluation Harness (ADR-009).
/// Cobre criação de suites, runs, datasets e respectivos casos de erro.
/// </summary>
public sealed class EvaluationHarnessTests
{
    private readonly IEvaluationSuiteRepository _suiteRepo = Substitute.For<IEvaluationSuiteRepository>();
    private readonly IEvaluationCaseRepository _caseRepo = Substitute.For<IEvaluationCaseRepository>();
    private readonly IEvaluationRunRepository _runRepo = Substitute.For<IEvaluationRunRepository>();
    private readonly IEvaluationDatasetRepository _datasetRepo = Substitute.For<IEvaluationDatasetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    public EvaluationHarnessTests()
    {
        _dateTime.UtcNow.Returns(_now);
    }

    // ── CreateEvaluationSuite ────────────────────────────────────────────────

    [Fact]
    public async Task CreateEvaluationSuite_ValidInput_ReturnsSuiteId()
    {
        var handler = new CreateEvaluationSuite.Handler(_suiteRepo, _unitOfWork, _dateTime);
        var command = new CreateEvaluationSuite.Command(
            "contract-review-v1", "Contract Review Suite", "Evaluates contract review",
            "contract-review", "1.0.0", _tenantId, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("contract-review-v1");
        result.Value.Status.Should().Be("Draft");
        _suiteRepo.Received(1).Add(Arg.Any<EvaluationSuite>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateEvaluationSuite_EmptyName_ReturnsValidationFailure()
    {
        var validator = new CreateEvaluationSuite.Validator();
        var command = new CreateEvaluationSuite.Command(
            "", "Display", "Desc", "contract-review", "1.0.0", _tenantId, null);

        var validation = await validator.ValidateAsync(command);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateEvaluationSuite_EmptyUseCase_ReturnsValidationFailure()
    {
        var validator = new CreateEvaluationSuite.Validator();
        var command = new CreateEvaluationSuite.Command(
            "suite-v1", "Display", "Desc", "", "1.0.0", _tenantId, null);

        var validation = await validator.ValidateAsync(command);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.PropertyName == "UseCase");
    }

    // ── GetEvaluationSuite ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEvaluationSuite_ExistingId_ReturnsMappedResponse()
    {
        var suite = EvaluationSuite.Create("s1", "Suite 1", "desc", "incident-summary", "1.0.0", _tenantId);
        _suiteRepo.GetByIdAsync(Arg.Any<EvaluationSuiteId>(), Arg.Any<CancellationToken>()).Returns(suite);
        _caseRepo.ListBySuiteAsync(Arg.Any<EvaluationSuiteId>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetEvaluationSuite.Handler(_suiteRepo, _caseRepo);
        var result = await handler.Handle(new GetEvaluationSuite.Query(suite.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("s1");
        result.Value.UseCase.Should().Be("incident-summary");
        result.Value.Status.Should().Be("Draft");
        result.Value.CaseCount.Should().Be(0);
    }

    [Fact]
    public async Task GetEvaluationSuite_NotFound_ReturnsError()
    {
        _suiteRepo.GetByIdAsync(Arg.Any<EvaluationSuiteId>(), Arg.Any<CancellationToken>()).Returns((EvaluationSuite?)null);

        var handler = new GetEvaluationSuite.Handler(_suiteRepo, _caseRepo);
        var result = await handler.Handle(new GetEvaluationSuite.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EvaluationSuiteNotFound");
    }

    // ── ListEvaluationSuites ─────────────────────────────────────────────────

    [Fact]
    public async Task ListEvaluationSuites_FilterByUseCase_ReturnsList()
    {
        var suite = EvaluationSuite.Create("s1", "Suite 1", "desc", "contract-review", "1.0.0", _tenantId);
        _suiteRepo.ListByTenantAsync(_tenantId, "contract-review", 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<EvaluationSuite> { suite });
        _suiteRepo.CountByTenantAsync(_tenantId, "contract-review", Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ListEvaluationSuites.Handler(_suiteRepo);
        var result = await handler.Handle(new ListEvaluationSuites.Query(_tenantId, "contract-review"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
    }

    // ── CreateEvaluationRun ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateEvaluationRun_ValidSuite_ReturnsRunId()
    {
        var suite = EvaluationSuite.Create("s1", "Suite 1", "desc", "incident-summary", "1.0.0", _tenantId);
        _suiteRepo.GetByIdAsync(Arg.Any<EvaluationSuiteId>(), Arg.Any<CancellationToken>()).Returns(suite);

        var handler = new CreateEvaluationRun.Handler(_suiteRepo, _runRepo, _unitOfWork, _dateTime);
        var command = new CreateEvaluationRun.Command(suite.Id.Value, Guid.NewGuid(), "v1.2.0", _tenantId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SuiteId.Should().Be(suite.Id.Value);
        result.Value.Status.Should().Be("Pending");
        _runRepo.Received(1).Add(Arg.Any<EvaluationRun>());
    }

    [Fact]
    public async Task CreateEvaluationRun_SuiteNotFound_ReturnsError()
    {
        _suiteRepo.GetByIdAsync(Arg.Any<EvaluationSuiteId>(), Arg.Any<CancellationToken>()).Returns((EvaluationSuite?)null);

        var handler = new CreateEvaluationRun.Handler(_suiteRepo, _runRepo, _unitOfWork, _dateTime);
        var command = new CreateEvaluationRun.Command(Guid.NewGuid(), Guid.NewGuid(), "v1", _tenantId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EvaluationSuiteNotFound");
    }

    // ── GetEvaluationRun ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetEvaluationRun_ExistingRun_ReturnsMappedResponse()
    {
        var suite = EvaluationSuite.Create("s1", "Suite 1", "desc", "change-impact", "1.0.0", _tenantId);
        var run = EvaluationRun.Create(suite.Id, Guid.NewGuid(), "v2.0.0", _tenantId);
        _runRepo.GetByIdAsync(Arg.Any<EvaluationRunId>(), Arg.Any<CancellationToken>()).Returns(run);

        var handler = new GetEvaluationRun.Handler(_runRepo);
        var result = await handler.Handle(new GetEvaluationRun.Query(run.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PromptVersion.Should().Be("v2.0.0");
        result.Value.Status.Should().Be("Pending");
        result.Value.TotalCases.Should().Be(0);
    }

    [Fact]
    public async Task GetEvaluationRun_NotFound_ReturnsError()
    {
        _runRepo.GetByIdAsync(Arg.Any<EvaluationRunId>(), Arg.Any<CancellationToken>()).Returns((EvaluationRun?)null);

        var handler = new GetEvaluationRun.Handler(_runRepo);
        var result = await handler.Handle(new GetEvaluationRun.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EvaluationRunNotFound");
    }

    // ── CreateEvaluationDataset ──────────────────────────────────────────────

    [Fact]
    public async Task CreateEvaluationDataset_ValidInput_ReturnsDatasetId()
    {
        var handler = new CreateEvaluationDataset.Handler(_datasetRepo, _unitOfWork, _dateTime);
        var command = new CreateEvaluationDataset.Command(
            "contract-cases-v1", "Contract review test cases", "contract-review", "Curated", _tenantId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("contract-cases-v1");
        result.Value.SourceType.Should().Be("Curated");
        _datasetRepo.Received(1).Add(Arg.Any<EvaluationDataset>());
    }

    // ── Domain behaviour ─────────────────────────────────────────────────────

    [Fact]
    public void EvaluationSuite_Activate_ChangesStatusToActive()
    {
        var suite = EvaluationSuite.Create("s1", "Suite 1", "desc", "contract-review", "1.0.0", _tenantId);
        suite.Status.Should().Be(EvaluationSuiteStatus.Draft);

        suite.Activate();

        suite.Status.Should().Be(EvaluationSuiteStatus.Active);
    }

    [Fact]
    public void EvaluationRun_Complete_UpdatesPassedFailedCount()
    {
        var suite = EvaluationSuite.Create("s1", "Suite 1", "desc", "change-impact", "1.0.0", _tenantId);
        var run = EvaluationRun.Create(suite.Id, Guid.NewGuid(), "v1.0.0", _tenantId);

        run.Start(_now);
        run.Complete(passed: 8, failed: 2, avgLatencyMs: 1234.5, tokenCost: 0.05m, _now.AddMinutes(5));

        run.Status.Should().Be(EvaluationRunStatus.Completed);
        run.PassedCases.Should().Be(8);
        run.FailedCases.Should().Be(2);
        run.TotalCases.Should().Be(10);
        run.AverageLatencyMs.Should().Be(1234.5);
        run.TotalTokenCost.Should().Be(0.05m);
    }
}

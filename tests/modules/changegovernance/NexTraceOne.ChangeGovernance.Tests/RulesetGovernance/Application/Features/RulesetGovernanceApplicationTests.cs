using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

using UploadRulesetFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.UploadRuleset.UploadRuleset;
using ListRulesetsFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ListRulesets.ListRulesets;
using ArchiveRulesetFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ArchiveRuleset.ArchiveRuleset;
using BindRulesetToAssetTypeFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.BindRulesetToAssetType.BindRulesetToAssetType;
using ExecuteLintForReleaseFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ExecuteLintForRelease.ExecuteLintForRelease;
using GetRulesetFindingsFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRulesetFindings.GetRulesetFindings;
using GetRulesetScoreFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRulesetScore.GetRulesetScore;
using InstallDefaultRulesetsFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.InstallDefaultRulesets.InstallDefaultRulesets;
using ComputeRulesetScoreFeature = NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ComputeRulesetScore.ComputeRulesetScore;

namespace NexTraceOne.ChangeGovernance.Tests.RulesetGovernance.Application.Features;

/// <summary>Testes de handlers da camada Application do módulo RulesetGovernance.</summary>
public sealed class RulesetGovernanceApplicationTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Ruleset CreateRuleset() =>
        Ruleset.Create("Test Ruleset", "Test Description", "{}", RulesetType.Custom, FixedNow);

    // -- UploadRuleset --

    [Fact]
    public async Task UploadRuleset_Should_CreateRuleset_AndReturnResponse()
    {
        var repository = Substitute.For<IRulesetRepository>();
        var unitOfWork = Substitute.For<IRulesetGovernanceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new UploadRulesetFeature.Handler(repository, unitOfWork, dateTimeProvider);

        var command = new UploadRulesetFeature.Command("My Ruleset", "Desc", "{\"rules\": {}}", RulesetType.Custom);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Ruleset");
        result.Value.IsActive.Should().BeTrue();
        repository.Received(1).Add(Arg.Any<Ruleset>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -- ListRulesets --

    [Fact]
    public async Task ListRulesets_Should_ReturnRulesets()
    {
        var repository = Substitute.For<IRulesetRepository>();
        var rulesets = new List<Ruleset> { CreateRuleset() };
        repository.ListAsync(1, 20, Arg.Any<CancellationToken>()).Returns(rulesets);

        var sut = new ListRulesetsFeature.Handler(repository);

        var result = await sut.Handle(new ListRulesetsFeature.Query(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rulesets.Should().HaveCount(1);
        result.Value.Rulesets[0].Name.Should().Be("Test Ruleset");
    }

    // -- ArchiveRuleset --

    [Fact]
    public async Task ArchiveRuleset_Should_ArchiveRuleset_WhenExists()
    {
        var ruleset = CreateRuleset();
        var repository = Substitute.For<IRulesetRepository>();
        var unitOfWork = Substitute.For<IRulesetGovernanceUnitOfWork>();
        repository.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns(ruleset);

        var sut = new ArchiveRulesetFeature.Handler(repository, unitOfWork);

        var result = await sut.Handle(new ArchiveRulesetFeature.Command(ruleset.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArchiveRuleset_Should_ReturnError_WhenNotFound()
    {
        var repository = Substitute.For<IRulesetRepository>();
        var unitOfWork = Substitute.For<IRulesetGovernanceUnitOfWork>();
        repository.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns((Ruleset?)null);

        var sut = new ArchiveRulesetFeature.Handler(repository, unitOfWork);

        var result = await sut.Handle(new ArchiveRulesetFeature.Command(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Ruleset.NotFound");
    }

    [Fact]
    public async Task ArchiveRuleset_Should_ReturnError_WhenAlreadyArchived()
    {
        var ruleset = CreateRuleset();
        ruleset.Archive();
        var repository = Substitute.For<IRulesetRepository>();
        var unitOfWork = Substitute.For<IRulesetGovernanceUnitOfWork>();
        repository.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns(ruleset);

        var sut = new ArchiveRulesetFeature.Handler(repository, unitOfWork);

        var result = await sut.Handle(new ArchiveRulesetFeature.Command(ruleset.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyArchived");
    }

    // -- BindRulesetToAssetType --

    [Fact]
    public async Task BindRulesetToAssetType_Should_CreateBinding()
    {
        var ruleset = CreateRuleset();
        var rulesetRepo = Substitute.For<IRulesetRepository>();
        var bindingRepo = Substitute.For<IRulesetBindingRepository>();
        var unitOfWork = Substitute.For<IRulesetGovernanceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);
        rulesetRepo.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns(ruleset);
        bindingRepo.GetByRulesetAndAssetTypeAsync(Arg.Any<RulesetId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RulesetBinding?)null);

        var sut = new BindRulesetToAssetTypeFeature.Handler(rulesetRepo, bindingRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new BindRulesetToAssetTypeFeature.Command(ruleset.Id.Value, "REST"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AssetType.Should().Be("REST");
        bindingRepo.Received(1).Add(Arg.Any<RulesetBinding>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BindRulesetToAssetType_Should_ReturnError_WhenBindingExists()
    {
        var ruleset = CreateRuleset();
        var rulesetRepo = Substitute.For<IRulesetRepository>();
        var bindingRepo = Substitute.For<IRulesetBindingRepository>();
        var unitOfWork = Substitute.For<IRulesetGovernanceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        rulesetRepo.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns(ruleset);
        var existingBinding = RulesetBinding.Create(ruleset.Id, "REST", FixedNow);
        bindingRepo.GetByRulesetAndAssetTypeAsync(Arg.Any<RulesetId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingBinding);

        var sut = new BindRulesetToAssetTypeFeature.Handler(rulesetRepo, bindingRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new BindRulesetToAssetTypeFeature.Command(ruleset.Id.Value, "REST"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyExists");
    }

    // -- ExecuteLintForRelease --

    [Fact]
    public async Task ExecuteLintForRelease_Should_CreateLintResult()
    {
        var ruleset = CreateRuleset();
        var rulesetRepo = Substitute.For<IRulesetRepository>();
        var lintResultRepo = Substitute.For<ILintResultRepository>();
        var unitOfWork = Substitute.For<IRulesetGovernanceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);
        rulesetRepo.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns(ruleset);

        var sut = new ExecuteLintForReleaseFeature.Handler(rulesetRepo, lintResultRepo, unitOfWork, dateTimeProvider);

        var findings = new List<ExecuteLintForReleaseFeature.FindingInput>
        {
            new("no-eval", FindingSeverity.Error, "Eval found", "/paths/test"),
            new("description-missing", FindingSeverity.Warning, "Missing description", "/info")
        };

        var result = await sut.Handle(
            new ExecuteLintForReleaseFeature.Command(ruleset.Id.Value, Guid.NewGuid(), Guid.NewGuid(), findings),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFindings.Should().Be(2);
        result.Value.Score.Should().Be(85m); // 100 - 10 - 5
        lintResultRepo.Received(1).Add(Arg.Any<LintResult>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteLintForRelease_Should_ReturnError_WhenRulesetNotFound()
    {
        var rulesetRepo = Substitute.For<IRulesetRepository>();
        var lintResultRepo = Substitute.For<ILintResultRepository>();
        var unitOfWork = Substitute.For<IRulesetGovernanceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        rulesetRepo.GetByIdAsync(Arg.Any<RulesetId>(), Arg.Any<CancellationToken>()).Returns((Ruleset?)null);

        var sut = new ExecuteLintForReleaseFeature.Handler(rulesetRepo, lintResultRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new ExecuteLintForReleaseFeature.Command(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), []),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Ruleset.NotFound");
    }

    // -- GetRulesetFindings --

    [Fact]
    public async Task GetRulesetFindings_Should_ReturnFindings_WhenExists()
    {
        var releaseId = Guid.NewGuid();
        var findings = new List<Finding>
        {
            Finding.Create("test-rule", FindingSeverity.Warning, "Test message", "/path")
        };
        var lintResult = LintResult.Create(RulesetId.New(), releaseId, Guid.NewGuid(), 95m, findings, FixedNow);
        var repository = Substitute.For<ILintResultRepository>();
        repository.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>()).Returns(lintResult);

        var sut = new GetRulesetFindingsFeature.Handler(repository);

        var result = await sut.Handle(new GetRulesetFindingsFeature.Query(releaseId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Findings.Should().HaveCount(1);
        result.Value.Score.Should().Be(95m);
    }

    [Fact]
    public async Task GetRulesetFindings_Should_ReturnError_WhenNotFound()
    {
        var repository = Substitute.For<ILintResultRepository>();
        repository.GetByReleaseIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LintResult?)null);

        var sut = new GetRulesetFindingsFeature.Handler(repository);

        var result = await sut.Handle(new GetRulesetFindingsFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("LintResult.NotFound");
    }

    // -- GetRulesetScore --

    [Fact]
    public async Task GetRulesetScore_Should_ReturnScore_WhenExists()
    {
        var releaseId = Guid.NewGuid();
        var lintResult = LintResult.Create(RulesetId.New(), releaseId, Guid.NewGuid(), 85m, [], FixedNow);
        var repository = Substitute.For<ILintResultRepository>();
        repository.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>()).Returns(lintResult);

        var sut = new GetRulesetScoreFeature.Handler(repository);

        var result = await sut.Handle(new GetRulesetScoreFeature.Query(releaseId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(85m);
    }

    [Fact]
    public async Task GetRulesetScore_Should_ReturnError_WhenNotFound()
    {
        var repository = Substitute.For<ILintResultRepository>();
        repository.GetByReleaseIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LintResult?)null);

        var sut = new GetRulesetScoreFeature.Handler(repository);

        var result = await sut.Handle(new GetRulesetScoreFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("LintResult.NotFound");
    }

    // -- InstallDefaultRulesets --

    [Fact]
    public async Task InstallDefaultRulesets_Should_CreateDefaultRuleset()
    {
        var repository = Substitute.For<IRulesetRepository>();
        var unitOfWork = Substitute.For<IRulesetGovernanceUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var sut = new InstallDefaultRulesetsFeature.Handler(repository, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(new InstallDefaultRulesetsFeature.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("OpenAPI Best Practices");
        result.Value.RulesetType.Should().Be("Default");
        repository.Received(1).Add(Arg.Any<Ruleset>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // -- ComputeRulesetScore --

    [Fact]
    public async Task ComputeRulesetScore_Should_ComputeCorrectScore()
    {
        var sut = new ComputeRulesetScoreFeature.Handler();

        var findings = new List<ComputeRulesetScoreFeature.FindingInput>
        {
            new(FindingSeverity.Error),
            new(FindingSeverity.Error),
            new(FindingSeverity.Warning),
            new(FindingSeverity.Info),
            new(FindingSeverity.Hint)
        };

        var result = await sut.Handle(new ComputeRulesetScoreFeature.Command(findings), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(74m); // 100 - 20 - 5 - 1
        result.Value.ErrorCount.Should().Be(2);
        result.Value.WarningCount.Should().Be(1);
        result.Value.InfoCount.Should().Be(1);
    }

    [Fact]
    public async Task ComputeRulesetScore_Should_ClampToZero_WhenManyErrors()
    {
        var sut = new ComputeRulesetScoreFeature.Handler();

        var findings = new List<ComputeRulesetScoreFeature.FindingInput>();
        for (int i = 0; i < 15; i++)
            findings.Add(new ComputeRulesetScoreFeature.FindingInput(FindingSeverity.Error));

        var result = await sut.Handle(new ComputeRulesetScoreFeature.Command(findings), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(0m);
    }

    [Fact]
    public async Task ComputeRulesetScore_Should_Return100_WhenNoFindings()
    {
        var sut = new ComputeRulesetScoreFeature.Handler();

        var result = await sut.Handle(
            new ComputeRulesetScoreFeature.Command([]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Score.Should().Be(100m);
    }
}

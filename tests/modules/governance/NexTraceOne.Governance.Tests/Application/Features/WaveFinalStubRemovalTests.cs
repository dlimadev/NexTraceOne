using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.ApplyGovernancePack;
using NexTraceOne.Governance.Application.Features.CreatePackVersion;
using NexTraceOne.Governance.Application.Features.GetBenchmarking;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para a Wave Final — validação da eliminação de MVP stubs,
/// placeholder scores e retornos vazios/fake.
/// </summary>
public sealed class WaveFinalStubRemovalTests
{
    // ── Test Infrastructure ──

    private static GovernancePack CreateTestPack() =>
        GovernancePack.Create("test-pack", "Test Pack", "Test pack for wave final", GovernanceRuleCategory.Contracts);

    private static GovernancePackVersion CreateTestVersion(GovernancePackId packId) =>
        GovernancePackVersion.Create(
            packId, "1.0.0", Array.Empty<GovernanceRuleBinding>(),
            EnforcementMode.Advisory, "Initial version", "admin@test.com");

    private static ICostIntelligenceModule CreateCostModuleMock()
    {
        var mock = Substitute.For<ICostIntelligenceModule>();
        mock.GetCostRecordsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CostRecordSummary[]
            {
                new("svc-api", "API Service", "Team Alpha", "Platform", "Production", 8500m, "EUR", "2026-03", "azure"),
                new("svc-worker", "Worker Service", "Team Alpha", "Platform", "Production", 12000m, "EUR", "2026-03", "azure"),
                new("svc-payments", "Payment Service", "Team Beta", "Commerce", "Production", 5200m, "EUR", "2026-03", "azure")
            });
        return mock;
    }

    // ── ApplyGovernancePack — Real Persistence ──

    [Fact]
    public async Task ApplyGovernancePack_ValidRequest_ShouldPersistRolloutRecord()
    {
        // Arrange
        var pack = CreateTestPack();
        var version = CreateTestVersion(pack.Id);

        var packRepo = Substitute.For<IGovernancePackRepository>();
        packRepo.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>()).Returns(pack);

        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        versionRepo.GetLatestByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>()).Returns(version);

        var rolloutRepo = Substitute.For<IGovernanceRolloutRecordRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

        var handler = new ApplyGovernancePack.Handler(packRepo, versionRepo, rolloutRepo, unitOfWork);
        var command = new ApplyGovernancePack.Command(
            pack.Id.Value.ToString(), "Team", "payments-squad", "Required", "admin@company.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RolloutId.Should().NotBeNullOrWhiteSpace();
        result.Value.PackId.Should().Be(pack.Id.Value.ToString());
        result.Value.VersionId.Should().Be(version.Id.Value.ToString());
        result.Value.Status.Should().Be("Completed");
        result.Value.InitiatedBy.Should().Be("admin@company.com");
        await rolloutRepo.Received(1).AddAsync(Arg.Any<GovernanceRolloutRecord>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyGovernancePack_InvalidPackId_ShouldReturnValidationError()
    {
        var packRepo = Substitute.For<IGovernancePackRepository>();
        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        var rolloutRepo = Substitute.For<IGovernanceRolloutRecordRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

        var handler = new ApplyGovernancePack.Handler(packRepo, versionRepo, rolloutRepo, unitOfWork);
        var command = new ApplyGovernancePack.Command("not-a-guid", "Team", "scope", "Advisory", "user@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PACK_ID");
    }

    [Fact]
    public async Task ApplyGovernancePack_PackNotFound_ShouldReturnNotFoundError()
    {
        var packRepo = Substitute.For<IGovernancePackRepository>();
        packRepo.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);

        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        var rolloutRepo = Substitute.For<IGovernanceRolloutRecordRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

        var handler = new ApplyGovernancePack.Handler(packRepo, versionRepo, rolloutRepo, unitOfWork);
        var command = new ApplyGovernancePack.Command(Guid.NewGuid().ToString(), "Team", "scope", "Advisory", "user@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PACK_NOT_FOUND");
    }

    [Fact]
    public async Task ApplyGovernancePack_NoVersionAvailable_ShouldReturnError()
    {
        var pack = CreateTestPack();
        var packRepo = Substitute.For<IGovernancePackRepository>();
        packRepo.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>()).Returns(pack);

        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        versionRepo.GetLatestByPackIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePackVersion?)null);

        var rolloutRepo = Substitute.For<IGovernanceRolloutRecordRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

        var handler = new ApplyGovernancePack.Handler(packRepo, versionRepo, rolloutRepo, unitOfWork);
        var command = new ApplyGovernancePack.Command(pack.Id.Value.ToString(), "Team", "scope", "Advisory", "user@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NO_VERSION_AVAILABLE");
    }

    [Fact]
    public async Task ApplyGovernancePack_InvalidScopeType_ShouldReturnValidationError()
    {
        var pack = CreateTestPack();
        var packRepo = Substitute.For<IGovernancePackRepository>();
        packRepo.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>()).Returns(pack);

        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        var rolloutRepo = Substitute.For<IGovernanceRolloutRecordRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

        var handler = new ApplyGovernancePack.Handler(packRepo, versionRepo, rolloutRepo, unitOfWork);
        var command = new ApplyGovernancePack.Command(
            pack.Id.Value.ToString(), "InvalidScope", "scope", "Advisory", "user@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_SCOPE_TYPE");
    }

    [Fact]
    public async Task ApplyGovernancePack_InvalidEnforcementMode_ShouldReturnValidationError()
    {
        var pack = CreateTestPack();
        var packRepo = Substitute.For<IGovernancePackRepository>();
        packRepo.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>()).Returns(pack);

        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        var rolloutRepo = Substitute.For<IGovernanceRolloutRecordRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

        var handler = new ApplyGovernancePack.Handler(packRepo, versionRepo, rolloutRepo, unitOfWork);
        var command = new ApplyGovernancePack.Command(
            pack.Id.Value.ToString(), "Team", "scope", "InvalidMode", "user@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_ENFORCEMENT_MODE");
    }

    // ── CreatePackVersion — Real Persistence ──

    [Fact]
    public async Task CreatePackVersion_ValidRequest_ShouldPersistVersion()
    {
        var pack = CreateTestPack();
        var packRepo = Substitute.For<IGovernancePackRepository>();
        packRepo.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>()).Returns(pack);

        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

        var handler = new CreatePackVersion.Handler(packRepo, versionRepo, unitOfWork);
        var command = new CreatePackVersion.Command(
            pack.Id.Value.ToString(), "2.0.0", "Blocking", "Major version with new rules", "architect@company.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.VersionId.Should().NotBeNullOrWhiteSpace();
        result.Value.PackId.Should().Be(pack.Id.Value.ToString());
        result.Value.Version.Should().Be("2.0.0");
        result.Value.DefaultEnforcementMode.Should().Be("Blocking");
        result.Value.CreatedBy.Should().Be("architect@company.com");
        result.Value.ChangeDescription.Should().Be("Major version with new rules");
        await versionRepo.Received(1).AddAsync(Arg.Any<GovernancePackVersion>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePackVersion_InvalidPackId_ShouldReturnValidationError()
    {
        var packRepo = Substitute.For<IGovernancePackRepository>();
        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

        var handler = new CreatePackVersion.Handler(packRepo, versionRepo, unitOfWork);
        var command = new CreatePackVersion.Command("not-a-guid", "1.0.0", "Advisory", null, "user@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PACK_ID");
    }

    [Fact]
    public async Task CreatePackVersion_PackNotFound_ShouldReturnNotFoundError()
    {
        var packRepo = Substitute.For<IGovernancePackRepository>();
        packRepo.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>())
            .Returns((GovernancePack?)null);

        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

        var handler = new CreatePackVersion.Handler(packRepo, versionRepo, unitOfWork);
        var command = new CreatePackVersion.Command(Guid.NewGuid().ToString(), "1.0.0", "Advisory", null, "user@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PACK_NOT_FOUND");
    }

    [Fact]
    public async Task CreatePackVersion_InvalidEnforcementMode_ShouldReturnValidationError()
    {
        var pack = CreateTestPack();
        var packRepo = Substitute.For<IGovernancePackRepository>();
        packRepo.GetByIdAsync(Arg.Any<GovernancePackId>(), Arg.Any<CancellationToken>()).Returns(pack);

        var versionRepo = Substitute.For<IGovernancePackVersionRepository>();
        var unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

        var handler = new CreatePackVersion.Handler(packRepo, versionRepo, unitOfWork);
        var command = new CreatePackVersion.Command(pack.Id.Value.ToString(), "1.0.0", "InvalidMode", null, "user@test.com");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_ENFORCEMENT_MODE");
    }

    // ── GetBenchmarking — No Placeholder Scores ──

    [Fact]
    public async Task GetBenchmarking_ShouldReturnNullScoresInsteadOfPlaceholders()
    {
        var handler = new GetBenchmarking.Handler(CreateCostModuleMock());
        var query = new GetBenchmarking.Query("teams");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Comparisons.Should().NotBeEmpty();
        result.Value.IsSimulated.Should().BeFalse();

        result.Value.Comparisons.Should().AllSatisfy(c =>
        {
            c.ReliabilityScore.Should().BeNull("reliability score should be null when not computable, not a placeholder");
            c.ChangeSafetyScore.Should().BeNull("change safety score should be null when not computable, not a placeholder");
            c.MaturityScore.Should().BeNull("maturity score should be null when not computable, not a placeholder");
            c.RiskScore.Should().BeNull("risk score should be null when not computable, not a placeholder");
            c.Criticality.Should().BeNull("criticality should be null when not computable, not a placeholder");
            c.ReliabilityTrend.Should().BeNull("reliability trend should be null when not computable, not a placeholder");
            c.IncidentRecurrenceRate.Should().BeNull("incident recurrence rate should be null when not computable, not a placeholder");
        });
    }

    [Fact]
    public async Task GetBenchmarking_ShouldComputeRealFinOpsEfficiency()
    {
        var handler = new GetBenchmarking.Handler(CreateCostModuleMock());
        var query = new GetBenchmarking.Query("teams");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.Comparisons.Should().AllSatisfy(c =>
        {
            c.FinopsEfficiency.Should().BeOneOf(
                CostEfficiency.Efficient,
                CostEfficiency.Acceptable,
                CostEfficiency.Inefficient,
                CostEfficiency.Wasteful);
        });
    }

    [Fact]
    public async Task GetBenchmarking_ShouldProvideRealContext()
    {
        var handler = new GetBenchmarking.Handler(CreateCostModuleMock());
        var query = new GetBenchmarking.Query("teams");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.Comparisons.Should().AllSatisfy(c =>
        {
            c.Context.Should().NotBeNullOrWhiteSpace("context should describe the data basis, not be empty");
            c.Context.Should().Contain("cost records", "context should reference actual data source");
        });
    }

    [Fact]
    public async Task GetBenchmarking_ShouldProvideStrengthsOrGaps()
    {
        var handler = new GetBenchmarking.Handler(CreateCostModuleMock());
        var query = new GetBenchmarking.Query("teams");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.Comparisons.Should().AllSatisfy(c =>
        {
            var hasInsights = c.Strengths.Count > 0 || c.Gaps.Count > 0;
            hasInsights.Should().BeTrue("each comparison should have at least strengths or gaps");
        });
    }

    [Fact]
    public async Task GetBenchmarking_DomainDimension_ShouldGroupByDomain()
    {
        var handler = new GetBenchmarking.Handler(CreateCostModuleMock());
        var query = new GetBenchmarking.Query("domains");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Dimension.Should().Be("domains");
        result.Value.Comparisons.Should().NotBeEmpty();
        result.Value.DataSource.Should().Be("cost-intelligence");
    }

    [Fact]
    public async Task GetBenchmarking_ShouldComputeCorrectEfficiencyForKnownData()
    {
        // Mock data: Team Alpha has avgCost = (8500+12000)/2 = 10250 → Inefficient
        //            Team Beta has avgCost = 5200 → Acceptable
        var handler = new GetBenchmarking.Handler(CreateCostModuleMock());
        var query = new GetBenchmarking.Query("teams");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var teamAlpha = result.Value.Comparisons.FirstOrDefault(c => c.GroupName == "Team Alpha");
        teamAlpha.Should().NotBeNull();
        teamAlpha!.FinopsEfficiency.Should().Be(CostEfficiency.Inefficient, "avg cost 10250 > 10000 is Inefficient");

        var teamBeta = result.Value.Comparisons.FirstOrDefault(c => c.GroupName == "Team Beta");
        teamBeta.Should().NotBeNull();
        teamBeta!.FinopsEfficiency.Should().Be(CostEfficiency.Acceptable, "avg cost 5200 > 5000 is Acceptable");
    }
}

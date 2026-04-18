using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using GetContractEnvironmentVersionDriftFeature =
    NexTraceOne.Catalog.Application.Contracts.Features.GetContractEnvironmentVersionDrift.GetContractEnvironmentVersionDrift;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes do handler GetContractEnvironmentVersionDrift.
/// Valida detecção de drift de versão de contrato entre ambientes.
/// Resolve gap INOVACAO-ROADMAP.md §1.1 — Contract Drift Detection entre ambientes.
/// </summary>
public sealed class GetContractEnvironmentVersionDriftTests
{
    private static readonly Guid ApiAssetId = Guid.NewGuid();

    private static ContractDeployment MakeDeployment(string environment, string semVer, DateTimeOffset? deployedAt = null) =>
        ContractDeployment.Create(
            contractVersionId: ContractVersionId.New(),
            apiAssetId: ApiAssetId,
            environment: environment,
            semVer: semVer,
            status: ContractDeploymentStatus.Success,
            deployedAt: deployedAt ?? DateTimeOffset.UtcNow.AddMinutes(-30),
            deployedBy: "ci-system",
            sourceSystem: "github-actions",
            notes: null);

    private static ContractVersion MakeContractVersion(string semVer) =>
        ContractVersion.Import(
            apiAssetId: ApiAssetId,
            semVer: semVer,
            specContent: """{"openapi":"3.1.0","paths":{"/health":{"get":{"responses":{"200":{"description":"OK"}}}}}}""",
            format: "json",
            importedFrom: "test",
            protocol: ContractProtocol.OpenApi).Value;

    // ── Cenário: todos os ambientes na mesma versão (Synchronized) ───────

    [Fact]
    public async Task Handle_Returns_Synchronized_When_AllEnvironmentsOnSameVersion()
    {
        var deploymentRepo = Substitute.For<IContractDeploymentRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();

        deploymentRepo.GetLatestSuccessfulByEnvironmentAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, ContractDeployment>(StringComparer.OrdinalIgnoreCase)
            {
                ["production"] = MakeDeployment("production", "1.2.0"),
                ["staging"] = MakeDeployment("staging", "1.2.0"),
            });

        versionRepo.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(MakeContractVersion("1.2.0"));

        var sut = new GetContractEnvironmentVersionDriftFeature.Handler(deploymentRepo, versionRepo);
        var result = await sut.Handle(new GetContractEnvironmentVersionDriftFeature.Query(ApiAssetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDrift.Should().BeFalse();
        result.Value.DriftStatus.Should().Be("Synchronized");
        result.Value.DistinctVersionCount.Should().Be(1);
        result.Value.LaggingEnvironments.Should().BeEmpty();
    }

    // ── Cenário: produção e staging em versões diferentes (CriticalDrift) ──

    [Fact]
    public async Task Handle_Returns_CriticalDrift_When_ProductionAndStagingHaveDifferentVersions()
    {
        var deploymentRepo = Substitute.For<IContractDeploymentRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();

        deploymentRepo.GetLatestSuccessfulByEnvironmentAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, ContractDeployment>(StringComparer.OrdinalIgnoreCase)
            {
                ["production"] = MakeDeployment("production", "1.0.0"),
                ["staging"] = MakeDeployment("staging", "1.2.0"),
            });

        versionRepo.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(MakeContractVersion("1.2.0"));

        var sut = new GetContractEnvironmentVersionDriftFeature.Handler(deploymentRepo, versionRepo);
        var result = await sut.Handle(new GetContractEnvironmentVersionDriftFeature.Query(ApiAssetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDrift.Should().BeTrue();
        result.Value.DriftStatus.Should().Be("CriticalDrift");
        result.Value.DistinctVersionCount.Should().Be(2);
        result.Value.LaggingEnvironments.Should().Contain("production");
    }

    // ── Cenário: apenas ambientes low-impact com drift (Drift, não CriticalDrift) ──

    [Fact]
    public async Task Handle_Returns_Drift_When_OnlyNonProductionEnvironmentsDiverge()
    {
        var deploymentRepo = Substitute.For<IContractDeploymentRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();

        deploymentRepo.GetLatestSuccessfulByEnvironmentAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, ContractDeployment>(StringComparer.OrdinalIgnoreCase)
            {
                ["dev"] = MakeDeployment("dev", "1.0.0"),
                ["qa"] = MakeDeployment("qa", "1.1.0"),
            });

        versionRepo.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(MakeContractVersion("1.1.0"));

        var sut = new GetContractEnvironmentVersionDriftFeature.Handler(deploymentRepo, versionRepo);
        var result = await sut.Handle(new GetContractEnvironmentVersionDriftFeature.Query(ApiAssetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDrift.Should().BeTrue();
        // dev and qa are non-high-impact — not CriticalDrift
        result.Value.DriftStatus.Should().Be("Drift");
    }

    // ── Cenário: ambientes atrás da versão canónica mas em sync entre si ──

    [Fact]
    public async Task Handle_Returns_BehindLatest_When_EnvironmentsInSyncButBehindLatestPublished()
    {
        var deploymentRepo = Substitute.For<IContractDeploymentRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();

        deploymentRepo.GetLatestSuccessfulByEnvironmentAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, ContractDeployment>(StringComparer.OrdinalIgnoreCase)
            {
                ["production"] = MakeDeployment("production", "1.0.0"),
                ["staging"] = MakeDeployment("staging", "1.0.0"),
            });

        versionRepo.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(MakeContractVersion("1.1.0")); // published version is ahead

        var sut = new GetContractEnvironmentVersionDriftFeature.Handler(deploymentRepo, versionRepo);
        var result = await sut.Handle(new GetContractEnvironmentVersionDriftFeature.Query(ApiAssetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDrift.Should().BeFalse();
        result.Value.DriftStatus.Should().Be("BehindLatest");
        result.Value.LaggingEnvironments.Should().Contain("production");
        result.Value.LaggingEnvironments.Should().Contain("staging");
    }

    // ── Cenário: sem deployments registados ──────────────────────────────

    [Fact]
    public async Task Handle_Returns_Failure_When_NoDeploymentsExist()
    {
        var deploymentRepo = Substitute.For<IContractDeploymentRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();

        deploymentRepo.GetLatestSuccessfulByEnvironmentAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, ContractDeployment>(StringComparer.OrdinalIgnoreCase));

        var sut = new GetContractEnvironmentVersionDriftFeature.Handler(deploymentRepo, versionRepo);
        var result = await sut.Handle(new GetContractEnvironmentVersionDriftFeature.Query(ApiAssetId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── Cenário: LatestPublishedSemVer é null quando não há versão publicada ──

    [Fact]
    public async Task Handle_Returns_NullLatestPublished_When_NoContractVersionExists()
    {
        var deploymentRepo = Substitute.For<IContractDeploymentRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();

        deploymentRepo.GetLatestSuccessfulByEnvironmentAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, ContractDeployment>(StringComparer.OrdinalIgnoreCase)
            {
                ["production"] = MakeDeployment("production", "1.0.0"),
            });

        versionRepo.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var sut = new GetContractEnvironmentVersionDriftFeature.Handler(deploymentRepo, versionRepo);
        var result = await sut.Handle(new GetContractEnvironmentVersionDriftFeature.Query(ApiAssetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LatestPublishedSemVer.Should().BeNull();
        result.Value.LaggingEnvironments.Should().BeEmpty();
    }

    // ── Cenário: staging marcado como IsHighImpact ────────────────────────

    [Fact]
    public async Task Handle_Marks_Staging_And_Production_As_HighImpact()
    {
        var deploymentRepo = Substitute.For<IContractDeploymentRepository>();
        var versionRepo = Substitute.For<IContractVersionRepository>();

        deploymentRepo.GetLatestSuccessfulByEnvironmentAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, ContractDeployment>(StringComparer.OrdinalIgnoreCase)
            {
                ["production"] = MakeDeployment("production", "1.2.0"),
                ["staging"] = MakeDeployment("staging", "1.2.0"),
                ["dev"] = MakeDeployment("dev", "1.2.0"),
            });

        versionRepo.GetLatestByApiAssetAsync(ApiAssetId, Arg.Any<CancellationToken>())
            .Returns(MakeContractVersion("1.2.0"));

        var sut = new GetContractEnvironmentVersionDriftFeature.Handler(deploymentRepo, versionRepo);
        var result = await sut.Handle(new GetContractEnvironmentVersionDriftFeature.Query(ApiAssetId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var productionState = result.Value.EnvironmentStates.First(e => e.Environment == "production");
        var stagingState = result.Value.EnvironmentStates.First(e => e.Environment == "staging");
        var devState = result.Value.EnvironmentStates.First(e => e.Environment == "dev");

        productionState.IsHighImpact.Should().BeTrue();
        stagingState.IsHighImpact.Should().BeTrue();
        devState.IsHighImpact.Should().BeFalse();
    }
}

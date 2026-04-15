using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Contracts.Services;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace NexTraceOne.Catalog.Tests.Contracts.Infrastructure.Services;

public sealed class ContractsModuleServiceTests
{
    [Fact]
    public async Task GetLatestChangeLevelAsync_Should_ReturnNull_When_NoContractVersion()
    {
        var apiAssetId = Guid.NewGuid();
        var versionRepository = Substitute.For<IContractVersionRepository>();
        versionRepository.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        await using var context = CreateDbContext();
        var sut = new ContractsModuleService(versionRepository, context);

        var result = await sut.GetLatestChangeLevelAsync(apiAssetId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task HasContractVersionAsync_Should_ReturnTrue_When_LatestVersionExists()
    {
        var apiAssetId = Guid.NewGuid();
        var version = CreateContractVersion(apiAssetId);
        var versionRepository = Substitute.For<IContractVersionRepository>();
        versionRepository.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(version);

        await using var context = CreateDbContext();
        var sut = new ContractsModuleService(versionRepository, context);

        var result = await sut.HasContractVersionAsync(apiAssetId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetLatestOverallScoreAsync_Should_ReturnLatestScore_When_ScorecardsExist()
    {
        var apiAssetId = Guid.NewGuid();
        var version = CreateContractVersion(apiAssetId);
        var versionRepository = Substitute.For<IContractVersionRepository>();
        versionRepository.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(version);

        await using var context = CreateDbContext();
        context.ContractScorecards.Add(CreateScorecard(version.Id, 0.45m, new DateTimeOffset(2026, 03, 27, 10, 0, 0, TimeSpan.Zero)));
        context.ContractScorecards.Add(CreateScorecard(version.Id, 0.82m, new DateTimeOffset(2026, 03, 28, 10, 0, 0, TimeSpan.Zero)));
        await context.SaveChangesAsync();

        var sut = new ContractsModuleService(versionRepository, context);

        var result = await sut.GetLatestOverallScoreAsync(apiAssetId, CancellationToken.None);

        result.Should().Be(0.82m);
    }

    [Fact]
    public async Task RequiresWorkflowApprovalAsync_Should_ReturnTrue_When_LatestChangeIsBreaking()
    {
        var apiAssetId = Guid.NewGuid();
        var version = CreateContractVersion(apiAssetId);
        var versionRepository = Substitute.For<IContractVersionRepository>();
        versionRepository.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(version);

        await using var context = CreateDbContext();
        context.ContractDiffs.Add(CreateDiff(version.Id, apiAssetId, ChangeLevel.Breaking, new DateTimeOffset(2026, 03, 28, 11, 0, 0, TimeSpan.Zero)));
        await context.SaveChangesAsync();

        var sut = new ContractsModuleService(versionRepository, context);

        var result = await sut.RequiresWorkflowApprovalAsync(apiAssetId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RequiresWorkflowApprovalAsync_Should_ReturnTrue_When_LifecycleStateIsInReview_AndNotBreaking()
    {
        var apiAssetId = Guid.NewGuid();
        var version = CreateContractVersion(apiAssetId, ContractLifecycleState.InReview);
        var versionRepository = Substitute.For<IContractVersionRepository>();
        versionRepository.GetLatestByApiAssetAsync(apiAssetId, Arg.Any<CancellationToken>())
            .Returns(version);

        await using var context = CreateDbContext();
        context.ContractDiffs.Add(CreateDiff(version.Id, apiAssetId, ChangeLevel.NonBreaking, new DateTimeOffset(2026, 03, 28, 11, 0, 0, TimeSpan.Zero)));
        await context.SaveChangesAsync();

        var sut = new ContractsModuleService(versionRepository, context);

        var result = await sut.RequiresWorkflowApprovalAsync(apiAssetId, CancellationToken.None);

        result.Should().BeTrue();
    }

    private static ContractsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractsDbContext>()
            .UseInMemoryDatabase($"contracts-module-service-tests-{Guid.NewGuid():N}")
            .Options;

        return new ContractsDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

    private static ContractVersion CreateContractVersion(Guid apiAssetId, ContractLifecycleState lifecycleState = ContractLifecycleState.Approved)
    {
        var importResult = ContractVersion.Import(
            apiAssetId,
            semVer: "1.0.0",
            specContent: "{\"openapi\":\"3.0.0\"}",
            format: "json",
            importedFrom: "tests",
            protocol: ContractProtocol.OpenApi);

        var version = importResult.Value;
        if (lifecycleState != ContractLifecycleState.Draft)
        {
            _ = version.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow);
            if (lifecycleState == ContractLifecycleState.Approved)
            {
                _ = version.TransitionTo(ContractLifecycleState.Approved, DateTimeOffset.UtcNow);
            }
        }

        return version;
    }

    private static ContractDiff CreateDiff(
        ContractVersionId versionId,
        Guid apiAssetId,
        ChangeLevel changeLevel,
        DateTimeOffset computedAt)
    {
        return ContractDiff.Create(
            contractVersionId: versionId,
            baseVersionId: versionId,
            targetVersionId: versionId,
            apiAssetId: apiAssetId,
            changeLevel: changeLevel,
            breakingChanges: [],
            nonBreakingChanges: [],
            additiveChanges: [],
            suggestedSemVer: "1.0.1",
            computedAt: computedAt,
            protocol: ContractProtocol.OpenApi,
            confidence: 1m);
    }

    private static ContractScorecard CreateScorecard(
        ContractVersionId versionId,
        decimal overallApproximate,
        DateTimeOffset computedAt)
    {
        // Overall = quality*0.30 + completeness*0.25 + compatibility*0.25 + (1-risk)*0.20
        // Set all to produce a predictable OverallScore close to desired value.
        var quality = overallApproximate;
        var completeness = overallApproximate;
        var compatibility = overallApproximate;
        var risk = 1m - overallApproximate;

        return ContractScorecard.Create(
            contractVersionId: versionId,
            protocol: ContractProtocol.OpenApi,
            qualityScore: quality,
            completenessScore: completeness,
            compatibilityScore: compatibility,
            riskScore: risk,
            operationCount: 5,
            schemaCount: 3,
            hasSecurityDefinitions: true,
            hasExamples: true,
            hasDescriptions: true,
            qualityJustification: "q",
            completenessJustification: "c",
            compatibilityJustification: "k",
            riskJustification: "r",
            computedAt: computedAt);
    }

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        public string Slug => "tests";
        public string Name => "Tests";
        public bool IsActive => true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id => "catalog-tests-user";
        public string Name => "Catalog Tests";
        public string Email => "catalog.tests@nextraceone.local";
        public string? Persona { get; } = null;
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        private static readonly DateTimeOffset FixedNow = new(2026, 03, 28, 12, 00, 00, TimeSpan.Zero);

        public DateTimeOffset UtcNow => FixedNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}

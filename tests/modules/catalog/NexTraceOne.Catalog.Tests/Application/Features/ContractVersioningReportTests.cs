using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractVersioningReport;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave O.1 — GetContractVersioningReport.
/// Cobre distribuição de ciclo de vida, distribuição de protocolo,
/// rácio de contratos obsoletos, lista de contratos deprecados/sunset e casos de borda.
/// </summary>
public sealed class ContractVersioningReportTests
{
    private const int DefaultPageSize = 200;

    private static (IContractVersionRepository repo, ContractSummaryData summary) MakeRepo(
        IReadOnlyList<ContractVersion> items,
        int distinctContracts,
        int deprecatedCount)
    {
        var summary = new ContractSummaryData(
            TotalVersions: items.Count,
            DistinctContracts: distinctContracts,
            DraftCount: items.Count(v => v.LifecycleState == ContractLifecycleState.Draft),
            InReviewCount: items.Count(v => v.LifecycleState == ContractLifecycleState.InReview),
            ApprovedCount: items.Count(v => v.LifecycleState == ContractLifecycleState.Approved),
            LockedCount: items.Count(v => v.LifecycleState == ContractLifecycleState.Locked),
            DeprecatedCount: deprecatedCount,
            ByProtocol: []);

        var repo = Substitute.For<IContractVersionRepository>();
        repo.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(summary);
        repo.SearchAsync(null, null, null, null, 1, DefaultPageSize, Arg.Any<CancellationToken>())
            .Returns((items, items.Count));

        return (repo, summary);
    }

    private static ContractVersion MakeVersion(
        ContractLifecycleState state,
        ContractProtocol protocol = ContractProtocol.OpenApi)
    {
        // Import creates a Draft version: Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired
        var importResult = ContractVersion.Import(
            apiAssetId: Guid.NewGuid(),
            semVer: "1.0.0",
            specContent: "{\"openapi\":\"3.0.0\"}",
            format: "json",
            importedFrom: "test",
            protocol: protocol);

        importResult.IsSuccess.Should().BeTrue("MakeVersion import should succeed");
        var version = importResult.Value;

        if (state == ContractLifecycleState.Draft)
            return version;

        // Draft → InReview
        version.TransitionTo(ContractLifecycleState.InReview, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        if (state == ContractLifecycleState.InReview)
            return version;

        // InReview → Approved
        version.TransitionTo(ContractLifecycleState.Approved, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        if (state == ContractLifecycleState.Approved)
            return version;

        // Approved → Locked
        version.TransitionTo(ContractLifecycleState.Locked, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        if (state == ContractLifecycleState.Locked)
            return version;

        // Locked → Deprecated
        version.TransitionTo(ContractLifecycleState.Deprecated, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        if (state == ContractLifecycleState.Deprecated)
            return version;

        // Deprecated → Sunset
        version.TransitionTo(ContractLifecycleState.Sunset, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        if (state == ContractLifecycleState.Sunset)
            return version;

        // Sunset → Retired
        version.TransitionTo(ContractLifecycleState.Retired, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        return version;
    }

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_IsSuccess_When_NoVersions()
    {
        var (repo, _) = MakeRepo([], 0, 0);
        var handler = new GetContractVersioningReport.Handler(repo);
        var result = await handler.Handle(new GetContractVersioningReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalVersions.Should().Be(0);
        result.Value.DistinctContracts.Should().Be(0);
        result.Value.DeprecatedRatio.Should().Be(0m);
        result.Value.ActiveRatio.Should().Be(0m);
        result.Value.ByLifecycleState.Should().BeEmpty();
        result.Value.ByProtocol.Should().BeEmpty();
        result.Value.TopDeprecatedContracts.Should().BeEmpty();
    }

    // ── Lifecycle distribution ─────────────────────────────────────────────

    [Fact]
    public async Task LifecycleDistribution_Correct_When_MixedStates()
    {
        var items = new List<ContractVersion>
        {
            MakeVersion(ContractLifecycleState.Draft),
            MakeVersion(ContractLifecycleState.Approved),
            MakeVersion(ContractLifecycleState.Approved),
            MakeVersion(ContractLifecycleState.Locked),
            MakeVersion(ContractLifecycleState.Deprecated),
        };
        var (repo, _) = MakeRepo(items, 5, 1);
        var handler = new GetContractVersioningReport.Handler(repo);
        var result = await handler.Handle(new GetContractVersioningReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var states = result.Value.ByLifecycleState.Select(e => e.State).ToList();
        states.Should().Contain("Approved");
        states.Should().Contain("Draft");
        states.Should().Contain("Locked");
        states.Should().Contain("Deprecated");
    }

    [Fact]
    public async Task LifecycleDistribution_Percent_Sums_To_100_Or_Less()
    {
        var items = new List<ContractVersion>
        {
            MakeVersion(ContractLifecycleState.Draft),
            MakeVersion(ContractLifecycleState.Approved),
            MakeVersion(ContractLifecycleState.Deprecated),
        };
        var (repo, _) = MakeRepo(items, 3, 1);
        var handler = new GetContractVersioningReport.Handler(repo);
        var result = await handler.Handle(new GetContractVersioningReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var totalPct = result.Value.ByLifecycleState.Sum(e => e.Percent);
        // Allow rounding error (e.g. 99.9 or 100.1)
        totalPct.Should().BeApproximately(100m, 1m);
    }

    // ── Protocol distribution ─────────────────────────────────────────────

    [Fact]
    public async Task ProtocolDistribution_Correct_When_MultiProtocol()
    {
        var items = new List<ContractVersion>
        {
            MakeVersion(ContractLifecycleState.Approved, ContractProtocol.OpenApi),
            MakeVersion(ContractLifecycleState.Approved, ContractProtocol.OpenApi),
            MakeVersion(ContractLifecycleState.Approved, ContractProtocol.AsyncApi),
            MakeVersion(ContractLifecycleState.Approved, ContractProtocol.Protobuf),
        };
        var (repo, _) = MakeRepo(items, 4, 0);
        var handler = new GetContractVersioningReport.Handler(repo);
        var result = await handler.Handle(new GetContractVersioningReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var openApi = result.Value.ByProtocol.FirstOrDefault(p => p.Protocol == "OpenApi");
        openApi.Should().NotBeNull();
        openApi!.Count.Should().Be(2);
        openApi.Percent.Should().Be(50m);
    }

    // ── Deprecated/Sunset counts ──────────────────────────────────────────

    [Fact]
    public async Task DeprecatedRatio_Calculated_Correctly()
    {
        var items = new List<ContractVersion>
        {
            MakeVersion(ContractLifecycleState.Approved),
            MakeVersion(ContractLifecycleState.Deprecated),
            MakeVersion(ContractLifecycleState.Sunset),
            MakeVersion(ContractLifecycleState.Retired),
        };
        // deprecated=1, sunset=1, retired=1 → obsolete=3/4=75%
        var (repo, _) = MakeRepo(items, 4, 1);
        var handler = new GetContractVersioningReport.Handler(repo);
        var result = await handler.Handle(new GetContractVersioningReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SunsetCount.Should().Be(1);
        result.Value.RetiredCount.Should().Be(1);
        result.Value.DeprecatedRatio.Should().Be(75m);
    }

    [Fact]
    public async Task ActiveRatio_Only_Approved_And_Locked()
    {
        var items = new List<ContractVersion>
        {
            MakeVersion(ContractLifecycleState.Approved),
            MakeVersion(ContractLifecycleState.Locked),
            MakeVersion(ContractLifecycleState.Draft),
            MakeVersion(ContractLifecycleState.Draft),
        };
        // active = 2/4 = 50%
        var (repo, _) = MakeRepo(items, 4, 0);
        var handler = new GetContractVersioningReport.Handler(repo);
        var result = await handler.Handle(new GetContractVersioningReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveRatio.Should().Be(50m);
    }

    // ── TopDeprecatedContracts ────────────────────────────────────────────

    [Fact]
    public async Task TopDeprecatedContracts_Limited_By_Count()
    {
        var items = Enumerable.Range(0, 15)
            .Select(_ => MakeVersion(ContractLifecycleState.Deprecated))
            .ToList();
        var (repo, _) = MakeRepo(items, 15, 15);
        var handler = new GetContractVersioningReport.Handler(repo);
        var result = await handler.Handle(
            new GetContractVersioningReport.Query(TopDeprecatedCount: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopDeprecatedContracts.Count.Should().Be(5);
    }

    [Fact]
    public async Task TopDeprecatedContracts_Includes_Sunset_And_Retired()
    {
        var items = new List<ContractVersion>
        {
            MakeVersion(ContractLifecycleState.Deprecated),
            MakeVersion(ContractLifecycleState.Sunset),
            MakeVersion(ContractLifecycleState.Retired),
            MakeVersion(ContractLifecycleState.Approved), // not obsolete
        };
        var (repo, _) = MakeRepo(items, 4, 1);
        var handler = new GetContractVersioningReport.Handler(repo);
        var result = await handler.Handle(new GetContractVersioningReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopDeprecatedContracts.Count.Should().Be(3);
        result.Value.TopDeprecatedContracts
            .Select(c => c.LifecycleState)
            .Should().NotContain("Approved");
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_TopDeprecatedCount_Zero()
    {
        var validator = new GetContractVersioningReport.Validator();
        var result = validator.Validate(new GetContractVersioningReport.Query(TopDeprecatedCount: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_PageSize_Below_Min()
    {
        var validator = new GetContractVersioningReport.Validator();
        var result = validator.Validate(new GetContractVersioningReport.Query(PageSize: 5));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetContractVersioningReport.Validator();
        var result = validator.Validate(new GetContractVersioningReport.Query(TopDeprecatedCount: 10, PageSize: 200));
        result.IsValid.Should().BeTrue();
    }
}

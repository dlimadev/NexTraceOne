using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractConsumerImpactReport;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave Q.2 — GetContractConsumerImpactReport.
/// Cobre: relatório vazio, contratos deprecated sem consumidores, contratos sunset com consumidores,
/// distribuição de lifecycle state, top contratos por consumidores, distribuição por domínio,
/// IncludeRetired flag, totais de serviços e domínios distintos.
/// </summary>
public sealed class ContractConsumerImpactReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantApiAssetId1 = Guid.NewGuid();
    private static readonly Guid TenantApiAssetId2 = Guid.NewGuid();
    private static readonly Guid TenantApiAssetId3 = Guid.NewGuid();

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ContractVersion MakeContractVersion(Guid apiAssetId, ContractLifecycleState state, ContractProtocol protocol = ContractProtocol.OpenApi)
    {
        var importResult = ContractVersion.Import(apiAssetId, "1.0.0", "{\"openapi\":\"3.0.0\"}", "json", "upload", protocol);
        var version = importResult.Value;
        // Lock first to allow lifecycle transitions
        version.Lock("admin", FixedNow.AddDays(-10));
        if (state == ContractLifecycleState.Deprecated)
            version.Deprecate("Old version", FixedNow.AddDays(-5), FixedNow.AddDays(30));
        else if (state == ContractLifecycleState.Sunset)
        {
            version.Deprecate("Old version", FixedNow.AddDays(-5), FixedNow.AddDays(1));
            version.TransitionTo(ContractLifecycleState.Sunset, FixedNow.AddDays(-2));
        }
        return version;
    }

    private static ConsumerExpectation MakeConsumer(Guid apiAssetId, string serviceName, string domain, bool active = true)
    {
        var c = ConsumerExpectation.Create(apiAssetId, serviceName, domain, "{}", null, FixedNow.AddDays(-7));
        if (!active) c.Deactivate();
        return c;
    }

    private static GetContractConsumerImpactReport.Handler CreateHandler(
        IReadOnlyDictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>> versionsByState,
        IReadOnlyDictionary<Guid, IReadOnlyList<ConsumerExpectation>> consumersByApiAsset)
    {
        var versionRepo = Substitute.For<IContractVersionRepository>();
        var consumerRepo = Substitute.For<IConsumerExpectationRepository>();

        versionRepo.SearchAsync(
                Arg.Any<ContractProtocol?>(),
                Arg.Any<ContractLifecycleState?>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var state = callInfo.ArgAt<ContractLifecycleState?>(1);
                if (state is null || !versionsByState.TryGetValue(state.Value, out var list))
                    return ((IReadOnlyList<ContractVersion>)[], 0);
                return (list, list.Count);
            });

        consumerRepo.ListByApiAssetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                return consumersByApiAsset.TryGetValue(id, out var list)
                    ? list
                    : (IReadOnlyList<ConsumerExpectation>)[];
            });

        return new GetContractConsumerImpactReport.Handler(versionRepo, consumerRepo, CreateClock());
    }

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_AtRisk_Contracts()
    {
        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>(),
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>());

        var result = await handler.Handle(new GetContractConsumerImpactReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAtRiskContracts.Should().Be(0);
        result.Value.TotalAffectedExpectations.Should().Be(0);
        result.Value.DistinctConsumerServices.Should().Be(0);
        result.Value.TopContractsByConsumerCount.Should().BeEmpty();
        result.Value.TopDomainsByExposure.Should().BeEmpty();
    }

    // ── Deprecated with no consumers ──────────────────────────────────────

    [Fact]
    public async Task Deprecated_Contract_With_No_Consumers_Counted()
    {
        var version = MakeContractVersion(TenantApiAssetId1, ContractLifecycleState.Deprecated);
        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>
            {
                [ContractLifecycleState.Deprecated] = [version]
            },
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>());

        var result = await handler.Handle(new GetContractConsumerImpactReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAtRiskContracts.Should().Be(1);
        result.Value.TotalAffectedExpectations.Should().Be(0);
        result.Value.StateDistribution.DeprecatedCount.Should().Be(1);
        result.Value.StateDistribution.SunsetCount.Should().Be(0);
    }

    // ── Deprecated with consumers ─────────────────────────────────────────

    [Fact]
    public async Task Deprecated_Contract_With_Consumers_Reports_Correct_Counts()
    {
        var version = MakeContractVersion(TenantApiAssetId1, ContractLifecycleState.Deprecated);
        var consumers = new List<ConsumerExpectation>
        {
            MakeConsumer(TenantApiAssetId1, "svc-a", "payments"),
            MakeConsumer(TenantApiAssetId1, "svc-b", "orders"),
        };

        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>
            {
                [ContractLifecycleState.Deprecated] = [version]
            },
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>
            {
                [TenantApiAssetId1] = consumers
            });

        var result = await handler.Handle(new GetContractConsumerImpactReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAffectedExpectations.Should().Be(2);
        result.Value.DistinctConsumerServices.Should().Be(2);
        result.Value.DistinctConsumerDomains.Should().Be(2);
    }

    // ── Inactive consumers excluded ───────────────────────────────────────

    [Fact]
    public async Task Inactive_Consumers_Excluded_From_Counts()
    {
        var version = MakeContractVersion(TenantApiAssetId1, ContractLifecycleState.Deprecated);
        var consumers = new List<ConsumerExpectation>
        {
            MakeConsumer(TenantApiAssetId1, "svc-active", "core", active: true),
            MakeConsumer(TenantApiAssetId1, "svc-inactive", "legacy", active: false),
        };

        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>
            {
                [ContractLifecycleState.Deprecated] = [version]
            },
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>
            {
                [TenantApiAssetId1] = consumers
            });

        var result = await handler.Handle(new GetContractConsumerImpactReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAffectedExpectations.Should().Be(1);
        result.Value.DistinctConsumerServices.Should().Be(1);
    }

    // ── Both Deprecated and Sunset ────────────────────────────────────────

    [Fact]
    public async Task Both_Deprecated_And_Sunset_Contracts_Included()
    {
        var deprecatedVersion = MakeContractVersion(TenantApiAssetId1, ContractLifecycleState.Deprecated);
        var sunsetVersion = MakeContractVersion(TenantApiAssetId2, ContractLifecycleState.Sunset);

        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>
            {
                [ContractLifecycleState.Deprecated] = [deprecatedVersion],
                [ContractLifecycleState.Sunset] = [sunsetVersion],
            },
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>());

        var result = await handler.Handle(new GetContractConsumerImpactReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAtRiskContracts.Should().Be(2);
        result.Value.StateDistribution.DeprecatedCount.Should().Be(1);
        result.Value.StateDistribution.SunsetCount.Should().Be(1);
        result.Value.StateDistribution.RetiredCount.Should().Be(0);
    }

    // ── IncludeRetired ────────────────────────────────────────────────────

    [Fact]
    public async Task IncludeRetired_False_Excludes_Retired_Contracts()
    {
        var retiredVersion = MakeContractVersion(TenantApiAssetId3, ContractLifecycleState.Deprecated);

        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>
            {
                [ContractLifecycleState.Deprecated] = [],
                [ContractLifecycleState.Retired] = [retiredVersion],
            },
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>());

        var result = await handler.Handle(new GetContractConsumerImpactReport.Query(IncludeRetired: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAtRiskContracts.Should().Be(0);
    }

    // ── TopContractsByConsumerCount ───────────────────────────────────────

    [Fact]
    public async Task TopContracts_Ordered_By_Consumer_Count_Descending()
    {
        var v1 = MakeContractVersion(TenantApiAssetId1, ContractLifecycleState.Deprecated);
        var v2 = MakeContractVersion(TenantApiAssetId2, ContractLifecycleState.Deprecated);

        var c1 = new List<ConsumerExpectation> { MakeConsumer(TenantApiAssetId1, "a", "d1") };
        var c2 = new List<ConsumerExpectation>
        {
            MakeConsumer(TenantApiAssetId2, "b", "d1"),
            MakeConsumer(TenantApiAssetId2, "c", "d2"),
            MakeConsumer(TenantApiAssetId2, "e", "d3"),
        };

        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>
            {
                [ContractLifecycleState.Deprecated] = [v1, v2]
            },
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>
            {
                [TenantApiAssetId1] = c1,
                [TenantApiAssetId2] = c2,
            });

        var result = await handler.Handle(new GetContractConsumerImpactReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopContractsByConsumerCount.First().ApiAssetId.Should().Be(TenantApiAssetId2);
        result.Value.TopContractsByConsumerCount.First().ActiveConsumerCount.Should().Be(3);
    }

    // ── Domain distribution ───────────────────────────────────────────────

    [Fact]
    public async Task TopDomains_Aggregated_Correctly()
    {
        var v1 = MakeContractVersion(TenantApiAssetId1, ContractLifecycleState.Deprecated);
        var consumers = new List<ConsumerExpectation>
        {
            MakeConsumer(TenantApiAssetId1, "svc-a", "payments"),
            MakeConsumer(TenantApiAssetId1, "svc-b", "payments"),
            MakeConsumer(TenantApiAssetId1, "svc-c", "logistics"),
        };

        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>
            {
                [ContractLifecycleState.Deprecated] = [v1]
            },
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>
            {
                [TenantApiAssetId1] = consumers
            });

        var result = await handler.Handle(new GetContractConsumerImpactReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopDomainsByExposure.Should().HaveCountGreaterThan(0);
        result.Value.TopDomainsByExposure.Any(d => d.Domain == "payments").Should().BeTrue();
    }

    // ── MaxTopContracts cap ───────────────────────────────────────────────

    [Fact]
    public async Task TopContracts_Capped_By_MaxTopContracts()
    {
        var versions = Enumerable.Range(1, 15)
            .Select(i => MakeContractVersion(Guid.NewGuid(), ContractLifecycleState.Deprecated))
            .ToList();

        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>
            {
                [ContractLifecycleState.Deprecated] = versions
            },
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>());

        var result = await handler.Handle(
            new GetContractConsumerImpactReport.Query(MaxTopContracts: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopContractsByConsumerCount.Count.Should().BeLessThanOrEqualTo(5);
    }

    // ── GeneratedAt ───────────────────────────────────────────────────────

    [Fact]
    public async Task Report_GeneratedAt_Matches_Clock()
    {
        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>(),
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>());

        var result = await handler.Handle(new GetContractConsumerImpactReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_MaxTopContracts_Zero()
    {
        var validator = new GetContractConsumerImpactReport.Validator();
        var r = validator.Validate(new GetContractConsumerImpactReport.Query(MaxTopContracts: 0));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_PageSize_Too_Small()
    {
        var validator = new GetContractConsumerImpactReport.Validator();
        var r = validator.Validate(new GetContractConsumerImpactReport.Query(PageSize: 5));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetContractConsumerImpactReport.Validator();
        var r = validator.Validate(new GetContractConsumerImpactReport.Query(MaxTopContracts: 10, MaxTopDomains: 20, PageSize: 500));
        r.IsValid.Should().BeTrue();
    }

    // ── Distinct services across contracts ────────────────────────────────

    [Fact]
    public async Task DistinctConsumerServices_Counts_Unique_Services_Across_All_Contracts()
    {
        var v1 = MakeContractVersion(TenantApiAssetId1, ContractLifecycleState.Deprecated);
        var v2 = MakeContractVersion(TenantApiAssetId2, ContractLifecycleState.Deprecated);

        // Same service "svc-shared" consumes both contracts
        var consumers1 = new List<ConsumerExpectation> { MakeConsumer(TenantApiAssetId1, "svc-shared", "core") };
        var consumers2 = new List<ConsumerExpectation>
        {
            MakeConsumer(TenantApiAssetId2, "svc-shared", "core"),
            MakeConsumer(TenantApiAssetId2, "svc-unique", "payments"),
        };

        var handler = CreateHandler(
            new Dictionary<ContractLifecycleState, IReadOnlyList<ContractVersion>>
            {
                [ContractLifecycleState.Deprecated] = [v1, v2]
            },
            new Dictionary<Guid, IReadOnlyList<ConsumerExpectation>>
            {
                [TenantApiAssetId1] = consumers1,
                [TenantApiAssetId2] = consumers2,
            });

        var result = await handler.Handle(new GetContractConsumerImpactReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DistinctConsumerServices.Should().Be(2); // svc-shared + svc-unique
        result.Value.TotalAffectedExpectations.Should().Be(3); // 1 + 2
    }
}

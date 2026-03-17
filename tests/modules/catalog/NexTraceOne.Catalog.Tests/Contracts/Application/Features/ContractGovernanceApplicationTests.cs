using FluentAssertions;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;

using NSubstitute;

using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using ListContractsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContracts.ListContracts;
using GetContractsSummaryFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractsSummary.GetContractsSummary;
using ListContractsByServiceFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContractsByService.ListContractsByService;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes dos novos handlers de Contract Governance da Fase 4.2 — listagem,
/// resumo e relação service→contracts.
/// </summary>
public sealed class ContractGovernanceApplicationTests
{
    // ── ListContracts ──────────────────────────────────────────────────

    [Fact]
    public async Task ListContracts_Should_ReturnPaginatedResult()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var sut = new ListContractsFeature.Handler(repository);

        var contract = ContractVersion.Import(
            Guid.NewGuid(), "1.0.0", "{}", "json", "upload", ContractProtocol.OpenApi).Value;

        repository.ListLatestPerApiAssetAsync(null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<ContractVersion> { contract }, 1));

        var result = await sut.Handle(
            new ListContractsFeature.Query(null, null, null, 1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].Protocol.Should().Be("OpenApi");
        result.Value.Items[0].LifecycleState.Should().Be("Draft");
    }

    [Fact]
    public async Task ListContracts_Should_PassFilters_To_Repository()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var sut = new ListContractsFeature.Handler(repository);

        repository.ListLatestPerApiAssetAsync(
            ContractProtocol.AsyncApi, ContractLifecycleState.Approved, "test", 2, 10,
            Arg.Any<CancellationToken>())
            .Returns((new List<ContractVersion>(), 0));

        var result = await sut.Handle(
            new ListContractsFeature.Query(ContractProtocol.AsyncApi, ContractLifecycleState.Approved, "test", 2, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        await repository.Received(1).ListLatestPerApiAssetAsync(
            ContractProtocol.AsyncApi, ContractLifecycleState.Approved, "test", 2, 10,
            Arg.Any<CancellationToken>());
    }

    // ── GetContractsSummary ──────────────────────────────────────────

    [Fact]
    public async Task GetContractsSummary_Should_ReturnAggregatedCounts()
    {
        var repository = Substitute.For<IContractVersionRepository>();
        var sut = new GetContractsSummaryFeature.Handler(repository);

        var summaryData = new ContractSummaryData(
            TotalVersions: 10,
            DistinctContracts: 5,
            DraftCount: 2,
            InReviewCount: 1,
            ApprovedCount: 3,
            LockedCount: 2,
            DeprecatedCount: 2,
            ByProtocol: new List<ProtocolCount>
            {
                new("OpenApi", 7),
                new("AsyncApi", 3)
            });

        repository.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(summaryData);

        var result = await sut.Handle(
            new GetContractsSummaryFeature.Query(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalVersions.Should().Be(10);
        result.Value.DistinctContracts.Should().Be(5);
        result.Value.DraftCount.Should().Be(2);
        result.Value.ByProtocol.Should().HaveCount(2);
        result.Value.ByProtocol[0].Protocol.Should().Be("OpenApi");
    }

    // ── ListContractsByService ───────────────────────────────────────

    [Fact]
    public async Task ListContractsByService_Should_ReturnContracts_WhenServiceHasApis()
    {
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var sut = new ListContractsByServiceFeature.Handler(apiRepo, contractRepo);

        var service = ServiceAsset.Create("test-service", "Finance", "Team A");
        var api = ApiAsset.Register("Payments API", "/api/payments", "1.0.0", "Public", service);

        apiRepo.ListByServiceIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { api });

        var contract = ContractVersion.Import(
            api.Id.Value, "1.0.0", "{}", "json", "upload", ContractProtocol.OpenApi).Value;

        contractRepo.ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { contract });

        var result = await sut.Handle(
            new ListContractsByServiceFeature.Query(service.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contracts.Should().HaveCount(1);
        result.Value.Contracts[0].ApiName.Should().Be("Payments API");
        result.Value.Contracts[0].Protocol.Should().Be("OpenApi");
    }

    [Fact]
    public async Task ListContractsByService_Should_ReturnEmpty_WhenServiceHasNoApis()
    {
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var sut = new ListContractsByServiceFeature.Handler(apiRepo, contractRepo);

        apiRepo.ListByServiceIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());

        var result = await sut.Handle(
            new ListContractsByServiceFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contracts.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}

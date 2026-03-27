using FluentAssertions;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;

using NSubstitute;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetServiceSourceOfTruth;
using NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetContractSourceOfTruth;
using NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetServiceCoverage;
using NexTraceOne.Catalog.Application.SourceOfTruth.Features.GlobalSearch;
using NexTraceOne.Catalog.Application.SourceOfTruth.Features.SearchSourceOfTruth;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Entities;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;
using NexTraceOne.Knowledge.Contracts;

namespace NexTraceOne.Catalog.Tests.SourceOfTruth.Application.Features;

/// <summary>
/// Testes dos handlers de Source of Truth da Fase 4.3 — visões consolidadas,
/// cobertura e pesquisa unificada.
/// </summary>
public sealed class SourceOfTruthApplicationTests
{
    // ── GetServiceSourceOfTruth ──────────────────────────────────────

    [Fact]
    public async Task GetServiceSourceOfTruth_Should_ReturnConsolidatedView()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var refRepo = Substitute.For<ILinkedReferenceRepository>();

        var service = ServiceAsset.Create("payments-api", "Finance", "Team Alpha");
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var api = ApiAsset.Register("Payments API", "/api/payments", "1.0", "Public", service);
        apiRepo.ListByServiceIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { api });

        var contract = ContractVersion.Import(
            api.Id.Value, "1.0.0", "{}", "json", "upload", ContractProtocol.OpenApi).Value;
        contractRepo.ListByApiAssetIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContractVersion> { contract });

        var docRef = LinkedReference.Create(
            service.Id.Value, LinkedAssetType.Service, LinkedReferenceType.Documentation, "API Docs");
        refRepo.ListByAssetAsync(Arg.Any<Guid>(), LinkedAssetType.Service, Arg.Any<CancellationToken>())
            .Returns(new List<LinkedReference> { docRef });

        var sut = new GetServiceSourceOfTruth.Handler(serviceRepo, apiRepo, contractRepo, refRepo);

        var result = await sut.Handle(
            new GetServiceSourceOfTruth.Query(service.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("payments-api");
        result.Value.Apis.Should().HaveCount(1);
        result.Value.Contracts.Should().HaveCount(1);
        result.Value.References.Should().HaveCount(1);
        result.Value.Coverage.HasOwner.Should().BeTrue();
        result.Value.Coverage.HasContracts.Should().BeTrue();
        result.Value.Coverage.HasDocumentation.Should().BeTrue();
    }

    [Fact]
    public async Task GetServiceSourceOfTruth_Should_ReturnError_WhenServiceNotFound()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var refRepo = Substitute.For<ILinkedReferenceRepository>();

        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var sut = new GetServiceSourceOfTruth.Handler(serviceRepo, apiRepo, contractRepo, refRepo);

        var result = await sut.Handle(
            new GetServiceSourceOfTruth.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── GetContractSourceOfTruth ─────────────────────────────────────

    [Fact]
    public async Task GetContractSourceOfTruth_Should_ReturnConsolidatedView()
    {
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var refRepo = Substitute.For<ILinkedReferenceRepository>();

        var contract = ContractVersion.Import(
            Guid.NewGuid(), "2.0.0", "{}", "json", "upload", ContractProtocol.AsyncApi).Value;
        contractRepo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(contract);

        refRepo.ListByAssetAsync(Arg.Any<Guid>(), LinkedAssetType.Contract, Arg.Any<CancellationToken>())
            .Returns(new List<LinkedReference>());

        var sut = new GetContractSourceOfTruth.Handler(contractRepo, refRepo);

        var result = await sut.Handle(
            new GetContractSourceOfTruth.Query(contract.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SemVer.Should().Be("2.0.0");
        result.Value.Protocol.Should().Be("AsyncApi");
    }

    [Fact]
    public async Task GetContractSourceOfTruth_Should_ReturnError_WhenNotFound()
    {
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var refRepo = Substitute.For<ILinkedReferenceRepository>();

        contractRepo.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((ContractVersion?)null);

        var sut = new GetContractSourceOfTruth.Handler(contractRepo, refRepo);

        var result = await sut.Handle(
            new GetContractSourceOfTruth.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── GetServiceCoverage ───────────────────────────────────────────

    [Fact]
    public async Task GetServiceCoverage_Should_ReturnCoverageScore()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var refRepo = Substitute.For<ILinkedReferenceRepository>();

        var service = ServiceAsset.Create("test-svc", "Domain", "Team");
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var api = ApiAsset.Register("Test API", "/api/test", "1.0", "Internal", service);
        apiRepo.ListByServiceIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { api });

        var docRef = LinkedReference.Create(
            service.Id.Value, LinkedAssetType.Service, LinkedReferenceType.Documentation, "Docs");
        var runbookRef = LinkedReference.Create(
            service.Id.Value, LinkedAssetType.Service, LinkedReferenceType.Runbook, "Runbook");
        refRepo.ListByAssetAsync(Arg.Any<Guid>(), LinkedAssetType.Service, Arg.Any<CancellationToken>())
            .Returns(new List<LinkedReference> { docRef, runbookRef });

        var sut = new GetServiceCoverage.Handler(serviceRepo, apiRepo, refRepo);

        var result = await sut.Handle(
            new GetServiceCoverage.Query(service.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasOwner.Should().BeTrue();
        result.Value.HasDocumentation.Should().BeTrue();
        result.Value.HasRunbook.Should().BeTrue();
        result.Value.HasDependenciesMapped.Should().BeTrue();
        result.Value.CoverageScore.Should().BeGreaterThan(0);
        result.Value.MetIndicators.Should().BeGreaterThan(0);
    }

    // ── SearchSourceOfTruth ──────────────────────────────────────────

    [Fact]
    public async Task SearchSourceOfTruth_Should_ReturnResults()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var refRepo = Substitute.For<ILinkedReferenceRepository>();

        var service = ServiceAsset.Create("payments", "Finance", "Team A");
        serviceRepo.SearchAsync("pay", Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { service });

        contractRepo.SearchAsync(null, null, null, "pay", 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<ContractVersion>(), 0));

        refRepo.SearchAsync("pay", null, Arg.Any<CancellationToken>())
            .Returns(new List<LinkedReference>());

        var sut = new SearchSourceOfTruth.Handler(serviceRepo, contractRepo, refRepo);

        var result = await sut.Handle(
            new SearchSourceOfTruth.Query("pay"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().HaveCount(1);
        result.Value.TotalResults.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchSourceOfTruth_WithScope_Should_LimitSearch()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var refRepo = Substitute.For<ILinkedReferenceRepository>();

        serviceRepo.SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());

        var sut = new SearchSourceOfTruth.Handler(serviceRepo, contractRepo, refRepo);

        var result = await sut.Handle(
            new SearchSourceOfTruth.Query("test", "services"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await serviceRepo.Received(1).SearchAsync("test", Arg.Any<CancellationToken>());
        await contractRepo.DidNotReceive().SearchAsync(
            Arg.Any<ContractProtocol?>(), Arg.Any<ContractLifecycleState?>(),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    // ── GlobalSearch (P10.2) ─────────────────────────────────────────

    [Fact]
    public async Task GlobalSearch_Should_IncludeKnowledgeResults_WhenProviderIsAvailable()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var refRepo = Substitute.For<ILinkedReferenceRepository>();
        var knowledgeProvider = Substitute.For<IKnowledgeSearchProvider>();

        serviceRepo.SearchAsync("runbook", Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());
        contractRepo.SearchAsync(null, null, null, "runbook", 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<ContractVersion>(), 0));
        refRepo.SearchAsync("runbook", null, Arg.Any<CancellationToken>())
            .Returns(new List<LinkedReference>());
        knowledgeProvider.SearchAsync("runbook", "all", 10, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeSearchResultItem>
            {
                new(Guid.NewGuid(), "knowledge", "API Runbook", "Payments", "Published", "/knowledge/documents/1", 0.9),
                new(Guid.NewGuid(), "note", "Night incident note", "Service · Warning", "Warning", "/knowledge/notes/1", 0.7)
            });

        var sut = new GlobalSearch.Handler(serviceRepo, contractRepo, refRepo, knowledgeProvider);

        var result = await sut.Handle(new GlobalSearch.Query("runbook", "all", null, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().Contain(i => i.EntityType == "knowledge");
        result.Value.Items.Should().Contain(i => i.EntityType == "note");
        result.Value.FacetCounts["knowledge"].Should().Be(1);
        result.Value.FacetCounts["notes"].Should().Be(1);
    }

    [Fact]
    public async Task GlobalSearch_Should_ReturnZeroKnowledgeFacets_WhenProviderIsMissing()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var contractRepo = Substitute.For<IContractVersionRepository>();
        var refRepo = Substitute.For<ILinkedReferenceRepository>();

        serviceRepo.SearchAsync("test", Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());
        contractRepo.SearchAsync(null, null, null, "test", 1, 5, Arg.Any<CancellationToken>())
            .Returns((new List<ContractVersion>(), 0));
        refRepo.SearchAsync("test", null, Arg.Any<CancellationToken>())
            .Returns(new List<LinkedReference>());

        var sut = new GlobalSearch.Handler(serviceRepo, contractRepo, refRepo);

        var result = await sut.Handle(new GlobalSearch.Query("test", "all", null, 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FacetCounts["knowledge"].Should().Be(0);
        result.Value.FacetCounts["notes"].Should().Be(0);
    }
}

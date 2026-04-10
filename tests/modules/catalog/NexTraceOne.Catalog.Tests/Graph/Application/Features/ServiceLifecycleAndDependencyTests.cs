using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using DecommissionAssetFeature = NexTraceOne.Catalog.Application.Graph.Features.DecommissionAsset.DecommissionAsset;
using AddServiceLinkFeature = NexTraceOne.Catalog.Application.Graph.Features.AddServiceLink.AddServiceLink;
using ComputeServiceMaturityFeature = NexTraceOne.Catalog.Application.Graph.Features.ComputeServiceMaturity.ComputeServiceMaturity;
using DetectCircularDependenciesFeature = NexTraceOne.Catalog.Application.Graph.Features.DetectCircularDependencies.DetectCircularDependencies;
using GetOwnershipAuditFeature = NexTraceOne.Catalog.Application.Graph.Features.GetOwnershipAudit.GetOwnershipAudit;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes dos handlers de ciclo de vida de serviço, links, maturidade,
/// dependências circulares e auditoria de ownership no módulo Graph.
/// Cobre DecommissionAsset, AddServiceLink, ComputeServiceMaturity,
/// DetectCircularDependencies e GetOwnershipAudit.
/// </summary>
public sealed class ServiceLifecycleAndDependencyTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 06, 01, 10, 0, 0, TimeSpan.Zero);

    // ── DecommissionAsset ─────────────────────────────────────────────

    [Fact]
    public async Task DecommissionAsset_Should_ReturnSuccess_When_AssetExists()
    {
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new DecommissionAssetFeature.Handler(apiAssetRepository, unitOfWork);

        var service = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var apiAsset = ApiAsset.Register("payments-api", "/api/payments", "1.0", "Public", service);

        apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>())
            .Returns(apiAsset);

        var result = await sut.Handle(
            new DecommissionAssetFeature.Command(apiAsset.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DecommissionAsset_Should_ReturnNotFound_When_AssetDoesNotExist()
    {
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new DecommissionAssetFeature.Handler(apiAssetRepository, unitOfWork);

        apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ApiAsset?)null);

        var result = await sut.Handle(
            new DecommissionAssetFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.ApiAsset.NotFound");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DecommissionAsset_Validator_Should_Fail_When_ApiAssetIdIsEmpty()
    {
        var validator = new DecommissionAssetFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new DecommissionAssetFeature.Command(Guid.Empty));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "ApiAssetId");
    }

    // ── AddServiceLink ────────────────────────────────────────────────

    [Fact]
    public async Task AddServiceLink_Should_ReturnResponse_When_ServiceExists()
    {
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var serviceLinkRepository = Substitute.For<IServiceLinkRepository>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var sut = new AddServiceLinkFeature.Handler(serviceAssetRepository, serviceLinkRepository, unitOfWork);

        var service = ServiceAsset.Create("payments-service", "Finance", "Payments Team");

        serviceAssetRepository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var result = await sut.Handle(
            new AddServiceLinkFeature.Command(
                service.Id.Value, "Repository", "GitHub Repo",
                "https://github.com/org/payments-service", "Main repo", "github", 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceAssetId.Should().Be(service.Id.Value);
        result.Value.Title.Should().Be("GitHub Repo");
        result.Value.Url.Should().Be("https://github.com/org/payments-service");
        result.Value.Category.Should().Be("Repository");
        serviceLinkRepository.Received(1).Add(Arg.Any<ServiceLink>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddServiceLink_Should_ReturnNotFound_When_ServiceDoesNotExist()
    {
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var serviceLinkRepository = Substitute.For<IServiceLinkRepository>();
        var unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
        var sut = new AddServiceLinkFeature.Handler(serviceAssetRepository, serviceLinkRepository, unitOfWork);

        serviceAssetRepository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new AddServiceLinkFeature.Command(
                Guid.NewGuid(), "Repository", "Repo", "https://github.com/repo"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.ServiceAsset.NotFoundById");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddServiceLink_Validator_Should_Fail_When_TitleIsEmpty()
    {
        var validator = new AddServiceLinkFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new AddServiceLinkFeature.Command(
                Guid.NewGuid(), "Repository", "", "https://github.com/repo"));

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    // ── ComputeServiceMaturity ────────────────────────────────────────

    [Fact]
    public async Task ComputeServiceMaturity_Should_ReturnResponse_When_ServiceExists()
    {
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var serviceLinkRepository = Substitute.For<IServiceLinkRepository>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var contractVersionRepository = Substitute.For<IContractVersionRepository>();
        var sut = new ComputeServiceMaturityFeature.Handler(
            serviceAssetRepository, serviceLinkRepository, apiAssetRepository, contractVersionRepository);

        var service = ServiceAsset.Create("payments-service", "Finance", "Payments Team");

        serviceAssetRepository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);
        serviceLinkRepository.ListByServiceAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServiceLink>());
        apiAssetRepository.ListByServiceIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());

        var result = await sut.Handle(
            new ComputeServiceMaturityFeature.Query(service.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be(service.Id.Value);
        result.Value.ServiceName.Should().Be("payments-service");
        result.Value.Dimensions.Should().NotBeEmpty();
        result.Value.OverallScore.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ComputeServiceMaturity_Should_ReturnNotFound_When_ServiceDoesNotExist()
    {
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var serviceLinkRepository = Substitute.For<IServiceLinkRepository>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var contractVersionRepository = Substitute.For<IContractVersionRepository>();
        var sut = new ComputeServiceMaturityFeature.Handler(
            serviceAssetRepository, serviceLinkRepository, apiAssetRepository, contractVersionRepository);

        serviceAssetRepository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new ComputeServiceMaturityFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.ServiceAsset.NotFoundById");
    }

    // ── DetectCircularDependencies ────────────────────────────────────

    [Fact]
    public async Task DetectCircularDependencies_Should_ReturnNoCycles_When_GraphIsAcyclic()
    {
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var sut = new DetectCircularDependenciesFeature.Handler(apiAssetRepository);

        apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());

        var result = await sut.Handle(
            new DetectCircularDependenciesFeature.Query(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CircularDependenciesFound.Should().BeFalse();
        result.Value.Cycles.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectCircularDependencies_Should_ReturnNoCycles_When_SingleServiceWithNoConsumers()
    {
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var sut = new DetectCircularDependenciesFeature.Handler(apiAssetRepository);

        var service = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var api = ApiAsset.Register("payments-api", "/api/payments", "1.0", "Public", service);

        apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset> { api });

        var result = await sut.Handle(
            new DetectCircularDependenciesFeature.Query(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CircularDependenciesFound.Should().BeFalse();
        result.Value.TotalServicesAnalyzed.Should().Be(1);
    }

    [Fact]
    public async Task DetectCircularDependencies_Validator_Should_Pass_When_NoServiceNameProvided()
    {
        var validator = new DetectCircularDependenciesFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new DetectCircularDependenciesFeature.Query());

        validationResult.IsValid.Should().BeTrue();
    }

    // ── GetOwnershipAudit ─────────────────────────────────────────────

    [Fact]
    public async Task GetOwnershipAudit_Should_ReturnFindings_When_ServicesHaveGaps()
    {
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var serviceLinkRepository = Substitute.For<IServiceLinkRepository>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var contractVersionRepository = Substitute.For<IContractVersionRepository>();
        var sut = new GetOwnershipAuditFeature.Handler(
            serviceAssetRepository, serviceLinkRepository, apiAssetRepository, contractVersionRepository);

        // Serviço com equipa definida mas sem TechnicalOwner — deve gerar findings
        var service = ServiceAsset.Create("incomplete-service", "Finance", "Team Alpha");

        serviceAssetRepository.ListFilteredAsync(
                null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { service });
        serviceLinkRepository.ListByServiceAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServiceLink>());
        apiAssetRepository.ListByServiceIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());

        var result = await sut.Handle(
            new GetOwnershipAuditFeature.Query(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Findings.Should().NotBeEmpty();
        result.Value.Summary.ServicesWithIssues.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetOwnershipAudit_Should_ReturnEmpty_When_NoServicesExist()
    {
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var serviceLinkRepository = Substitute.For<IServiceLinkRepository>();
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var contractVersionRepository = Substitute.For<IContractVersionRepository>();
        var sut = new GetOwnershipAuditFeature.Handler(
            serviceAssetRepository, serviceLinkRepository, apiAssetRepository, contractVersionRepository);

        serviceAssetRepository.ListFilteredAsync(
                null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());

        var result = await sut.Handle(
            new GetOwnershipAuditFeature.Query(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Findings.Should().BeEmpty();
        result.Value.Summary.TotalServicesAudited.Should().Be(0);
        result.Value.Summary.HealthyServices.Should().Be(0);
    }

    [Fact]
    public async Task GetOwnershipAudit_Validator_Should_Pass_When_NoFilters()
    {
        var validator = new GetOwnershipAuditFeature.Validator();

        var validationResult = await validator.ValidateAsync(
            new GetOwnershipAuditFeature.Query());

        validationResult.IsValid.Should().BeTrue();
    }
}

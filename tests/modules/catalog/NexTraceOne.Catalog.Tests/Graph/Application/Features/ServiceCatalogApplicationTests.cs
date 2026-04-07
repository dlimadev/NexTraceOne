using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using GetServiceDetailFeature = NexTraceOne.Catalog.Application.Graph.Features.GetServiceDetail.GetServiceDetail;
using GetServicesSummaryFeature = NexTraceOne.Catalog.Application.Graph.Features.GetServicesSummary.GetServicesSummary;
using ListServicesFeature = NexTraceOne.Catalog.Application.Graph.Features.ListServices.ListServices;
using RegisterServiceAssetFeature = NexTraceOne.Catalog.Application.Graph.Features.RegisterServiceAsset.RegisterServiceAsset;
using SearchServicesFeature = NexTraceOne.Catalog.Application.Graph.Features.SearchServices.SearchServices;
using UpdateServiceAssetFeature = NexTraceOne.Catalog.Application.Graph.Features.UpdateServiceAsset.UpdateServiceAsset;
using UpdateServiceOwnershipFeature = NexTraceOne.Catalog.Application.Graph.Features.UpdateServiceOwnership.UpdateServiceOwnership;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes dos novos handlers de Service Catalog da Fase 4.1 — identidade,
/// ownership, classificação e funcionalidades de catálogo.
/// </summary>
public sealed class ServiceCatalogApplicationTests
{
    // ── ListServices ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListServices_Should_ReturnAllServices_When_NoFilters()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var sut = new ListServicesFeature.Handler(repository);

        var services = new List<ServiceAsset>
        {
            ServiceAsset.Create("svc-a", "Finance", "Team Alpha"),
            ServiceAsset.Create("svc-b", "Sales", "Team Beta")
        };
        repository.ListFilteredAsync(null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(services);

        var result = await sut.Handle(
            new ListServicesFeature.Query(null, null, null, null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListServices_Should_PassFilters_To_Repository()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var sut = new ListServicesFeature.Handler(repository);

        repository.ListFilteredAsync("Team Alpha", "Finance", ServiceType.RestApi, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset>());

        var result = await sut.Handle(
            new ListServicesFeature.Query("Team Alpha", "Finance", ServiceType.RestApi, null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).ListFilteredAsync("Team Alpha", "Finance", ServiceType.RestApi, null, null, null, null, Arg.Any<CancellationToken>());
    }

    // ── GetServiceDetail ──────────────────────────────────────────────────

    [Fact]
    public async Task GetServiceDetail_Should_ReturnDetail_When_ServiceExists()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var sut = new GetServiceDetailFeature.Handler(serviceRepo, apiRepo);

        var service = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);
        apiRepo.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ApiAsset>());

        var result = await sut.Handle(
            new GetServiceDetailFeature.Query(service.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("payments-service");
        result.Value.Domain.Should().Be("Finance");
        result.Value.TeamName.Should().Be("Payments Team");
        result.Value.ApiCount.Should().Be(0);
    }

    [Fact]
    public async Task GetServiceDetail_Should_ReturnNotFound_When_ServiceDoesNotExist()
    {
        var serviceRepo = Substitute.For<IServiceAssetRepository>();
        var apiRepo = Substitute.For<IApiAssetRepository>();
        var sut = new GetServiceDetailFeature.Handler(serviceRepo, apiRepo);

        serviceRepo.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new GetServiceDetailFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.ServiceAsset.NotFoundById");
    }

    // ── UpdateServiceAsset ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateServiceAsset_Should_UpdateDetails_When_ServiceExists()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new UpdateServiceAssetFeature.Handler(repository, unitOfWork);

        var service = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        repository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var result = await sut.Handle(
            new UpdateServiceAssetFeature.Command(
                service.Id.Value,
                "Payments Service",
                "Handles all payment processing",
                ServiceType.RestApi,
                "Core Platform",
                Criticality.Critical,
                LifecycleStatus.Active,
                ExposureType.Internal,
                "https://docs.example.com/payments",
                "https://github.com/example/payments"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("Payments Service");
        result.Value.Criticality.Should().Be("Critical");
        result.Value.LifecycleStatus.Should().Be("Active");
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateServiceAsset_Should_ReturnNotFound_When_ServiceDoesNotExist()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new UpdateServiceAssetFeature.Handler(repository, unitOfWork);

        repository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new UpdateServiceAssetFeature.Command(
                Guid.NewGuid(), "Name", "Desc", ServiceType.RestApi, "",
                Criticality.Medium, LifecycleStatus.Active, ExposureType.Internal, "", ""),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.ServiceAsset.NotFoundById");
    }

    // ── UpdateServiceOwnership ────────────────────────────────────────────

    [Fact]
    public async Task UpdateServiceOwnership_Should_UpdateOwnership_When_ServiceExists()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new UpdateServiceOwnershipFeature.Handler(repository, unitOfWork);

        var service = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        repository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns(service);

        var result = await sut.Handle(
            new UpdateServiceOwnershipFeature.Command(
                service.Id.Value, "New Team", "john.doe", "jane.product"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamName.Should().Be("New Team");
        result.Value.TechnicalOwner.Should().Be("john.doe");
        result.Value.BusinessOwner.Should().Be("jane.product");
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateServiceOwnership_Should_ReturnNotFound_When_ServiceDoesNotExist()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new UpdateServiceOwnershipFeature.Handler(repository, unitOfWork);

        repository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new UpdateServiceOwnershipFeature.Command(Guid.NewGuid(), "Team", "", ""),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.ServiceAsset.NotFoundById");
    }

    // ── SearchServices ────────────────────────────────────────────────────

    [Fact]
    public async Task SearchServices_Should_ReturnMatches_When_TermMatches()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var sut = new SearchServicesFeature.Handler(repository);

        var services = new List<ServiceAsset>
        {
            ServiceAsset.Create("payments-service", "Finance", "Payments Team")
        };
        repository.SearchAsync("payments", Arg.Any<CancellationToken>())
            .Returns(services);

        var result = await sut.Handle(
            new SearchServicesFeature.Query("payments"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("payments-service");
    }

    // ── GetServicesSummary ────────────────────────────────────────────────

    [Fact]
    public async Task GetServicesSummary_Should_ReturnAggregatedCounts()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var sut = new GetServicesSummaryFeature.Handler(repository);

        var svc1 = ServiceAsset.Create("svc-a", "Finance", "Team Alpha");
        var svc2 = ServiceAsset.Create("svc-b", "Finance", "Team Alpha");
        svc1.UpdateDetails("SvcA", "", ServiceType.RestApi, "", Criticality.Critical, LifecycleStatus.Active, ExposureType.Internal, "", "");
        svc2.UpdateDetails("SvcB", "", ServiceType.BackgroundService, "", Criticality.Medium, LifecycleStatus.Active, ExposureType.Internal, "", "");

        repository.ListFilteredAsync(null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ServiceAsset> { svc1, svc2 });

        var result = await sut.Handle(
            new GetServicesSummaryFeature.Query(null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.CriticalCount.Should().Be(1);
        result.Value.ActiveCount.Should().Be(2);
        result.Value.ByServiceType.Should().HaveCount(2);
    }

    // ── ServiceAsset Domain Tests ─────────────────────────────────────────

    [Fact]
    public void ServiceAsset_Create_Should_SetDefaults()
    {
        var service = ServiceAsset.Create("test-service", "TestDomain", "TestTeam");

        service.Name.Should().Be("test-service");
        service.DisplayName.Should().Be("test-service");
        service.Domain.Should().Be("TestDomain");
        service.TeamName.Should().Be("TestTeam");
        service.ServiceType.Should().Be(ServiceType.RestApi);
        service.Criticality.Should().Be(Criticality.Medium);
        service.LifecycleStatus.Should().Be(LifecycleStatus.Active);
        service.ExposureType.Should().Be(ExposureType.Internal);
        service.Description.Should().BeEmpty();
        service.TechnicalOwner.Should().BeEmpty();
        service.BusinessOwner.Should().BeEmpty();
    }

    [Fact]
    public void ServiceAsset_UpdateDetails_Should_ChangeProperties()
    {
        var service = ServiceAsset.Create("test-service", "TestDomain", "TestTeam");

        service.UpdateDetails(
            "Test Service Display",
            "A test service description",
            ServiceType.KafkaProducer,
            "Core Platform",
            Criticality.High,
            LifecycleStatus.Deprecating,
            ExposureType.External,
            "https://docs.example.com",
            "https://github.com/example");

        service.DisplayName.Should().Be("Test Service Display");
        service.Description.Should().Be("A test service description");
        service.ServiceType.Should().Be(ServiceType.KafkaProducer);
        service.SystemArea.Should().Be("Core Platform");
        service.Criticality.Should().Be(Criticality.High);
        service.LifecycleStatus.Should().Be(LifecycleStatus.Deprecating);
        service.ExposureType.Should().Be(ExposureType.External);
        service.DocumentationUrl.Should().Be("https://docs.example.com");
        service.RepositoryUrl.Should().Be("https://github.com/example");
    }

    [Fact]
    public void ServiceAsset_UpdateOwnership_Should_ChangeOwnerFields()
    {
        var service = ServiceAsset.Create("test-service", "TestDomain", "TestTeam");

        service.UpdateOwnership("New Team", "tech.owner", "biz.owner");

        service.TeamName.Should().Be("New Team");
        service.TechnicalOwner.Should().Be("tech.owner");
        service.BusinessOwner.Should().Be("biz.owner");
    }

    [Fact]
    public void ServiceAsset_UpdateOwnership_Should_Reject_EmptyTeamName()
    {
        var service = ServiceAsset.Create("test-service", "TestDomain", "TestTeam");

        var act = () => service.UpdateOwnership("", "tech.owner", "biz.owner");

        act.Should().Throw<ArgumentException>();
    }

    // ── RegisterServiceAsset ──────────────────────────────────────────────

    [Fact]
    public async Task RegisterServiceAsset_Should_CreateService_WithMinimalFields()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var configService = Substitute.For<IConfigurationResolutionService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new RegisterServiceAssetFeature.Handler(repository, configService, unitOfWork);

        repository.GetByNameAsync("new-service", Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new RegisterServiceAssetFeature.Command("new-service", "Finance", "Team Alpha"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("new-service");
        result.Value.Domain.Should().Be("Finance");
        result.Value.TeamName.Should().Be("Team Alpha");
        result.Value.ServiceType.Should().Be("RestApi");
        result.Value.Criticality.Should().Be("Medium");
        result.Value.ExposureType.Should().Be("Internal");
        result.Value.Description.Should().BeEmpty();
        result.Value.TechnicalOwner.Should().BeEmpty();
        result.Value.BusinessOwner.Should().BeEmpty();
        result.Value.DocumentationUrl.Should().BeEmpty();
        result.Value.RepositoryUrl.Should().BeEmpty();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterServiceAsset_Should_CreateService_WithAllFields()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var configService = Substitute.For<IConfigurationResolutionService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new RegisterServiceAssetFeature.Handler(repository, configService, unitOfWork);

        repository.GetByNameAsync("full-service", Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new RegisterServiceAssetFeature.Command(
                "full-service",
                "Finance",
                "Team Alpha",
                Description: "A full service",
                ServiceType: "KafkaProducer",
                Criticality: "Critical",
                ExposureType: "External",
                TechnicalOwner: "john.doe",
                BusinessOwner: "jane.product",
                DocumentationUrl: "https://docs.example.com",
                RepositoryUrl: "https://github.com/example/svc"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("full-service");
        result.Value.Domain.Should().Be("Finance");
        result.Value.TeamName.Should().Be("Team Alpha");
        result.Value.Description.Should().Be("A full service");
        result.Value.ServiceType.Should().Be("KafkaProducer");
        result.Value.Criticality.Should().Be("Critical");
        result.Value.ExposureType.Should().Be("External");
        result.Value.TechnicalOwner.Should().Be("john.doe");
        result.Value.BusinessOwner.Should().Be("jane.product");
        result.Value.DocumentationUrl.Should().Be("https://docs.example.com");
        result.Value.RepositoryUrl.Should().Be("https://github.com/example/svc");
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterServiceAsset_Should_ReturnError_When_ServiceAlreadyExists()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var configService = Substitute.For<IConfigurationResolutionService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new RegisterServiceAssetFeature.Handler(repository, configService, unitOfWork);

        var existing = ServiceAsset.Create("existing-service", "Finance", "Team Alpha");
        repository.GetByNameAsync("existing-service", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await sut.Handle(
            new RegisterServiceAssetFeature.Command("existing-service", "Finance", "Team Alpha"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CatalogGraph.ServiceAsset.AlreadyExists");
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterServiceAsset_Should_ParseEnumFieldsSafely()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var configService = Substitute.For<IConfigurationResolutionService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new RegisterServiceAssetFeature.Handler(repository, configService, unitOfWork);

        repository.GetByNameAsync("safe-service", Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new RegisterServiceAssetFeature.Command(
                "safe-service",
                "Finance",
                "Team Alpha",
                ServiceType: "InvalidEnumValue",
                Criticality: "NotAValidCriticality",
                ExposureType: "Unknown"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceType.Should().Be("RestApi");
        result.Value.Criticality.Should().Be("Low");
        result.Value.ExposureType.Should().Be("Internal");
    }

    [Fact]
    public async Task RegisterServiceAsset_Should_ApplyDetailsAndOwnership_When_OptionalFieldsProvided()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var configService = Substitute.For<IConfigurationResolutionService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new RegisterServiceAssetFeature.Handler(repository, configService, unitOfWork);

        repository.GetByNameAsync("detail-service", Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new RegisterServiceAssetFeature.Command(
                "detail-service",
                "Sales",
                "Team Beta",
                Description: "Handles sales",
                ServiceType: "Framework",
                TechnicalOwner: "tech.lead"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Handles sales");
        result.Value.ServiceType.Should().Be("Framework");
        result.Value.TechnicalOwner.Should().Be("tech.lead");
        result.Value.BusinessOwner.Should().BeEmpty();
        result.Value.Criticality.Should().Be("Low");
        result.Value.ExposureType.Should().Be("Internal");
        repository.Received(1).Add(Arg.Any<ServiceAsset>());
    }
}

using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using RegisterApiAssetFeature = NexTraceOne.EngineeringGraph.Application.Features.RegisterApiAsset.RegisterApiAsset;
using RegisterServiceAssetFeature = NexTraceOne.EngineeringGraph.Application.Features.RegisterServiceAsset.RegisterServiceAsset;
using MapConsumerRelationshipFeature = NexTraceOne.EngineeringGraph.Application.Features.MapConsumerRelationship.MapConsumerRelationship;
using GetAssetGraphFeature = NexTraceOne.EngineeringGraph.Application.Features.GetAssetGraph.GetAssetGraph;

namespace NexTraceOne.EngineeringGraph.Tests.Application.Features;

/// <summary>
/// Testes de handlers da camada Application do módulo EngineeringGraph.
/// </summary>
public sealed class EngineeringGraphApplicationTests
{
    // ── RegisterServiceAsset ──────────────────────────────────────────────

    [Fact]
    public async Task RegisterServiceAsset_Should_ReturnResponse_When_ServiceIsNew()
    {
        var repository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new RegisterServiceAssetFeature.Handler(repository, unitOfWork);

        repository.GetByNameAsync("payments-service", Arg.Any<CancellationToken>()).Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new RegisterServiceAssetFeature.Command("payments-service", "Finance", "Payments Team"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("payments-service");
        result.Value.Domain.Should().Be("Finance");
        repository.Received(1).Add(Arg.Any<ServiceAsset>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterServiceAsset_Should_ReturnConflict_When_ServiceAlreadyExists()
    {
        var existing = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var repository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new RegisterServiceAssetFeature.Handler(repository, unitOfWork);

        repository.GetByNameAsync("payments-service", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await sut.Handle(
            new RegisterServiceAssetFeature.Command("payments-service", "Finance", "Payments Team"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EngineeringGraph.ServiceAsset.AlreadyExists");
        repository.DidNotReceive().Add(Arg.Any<ServiceAsset>());
    }

    // ── RegisterApiAsset ──────────────────────────────────────────────────

    [Fact]
    public async Task RegisterApiAsset_Should_ReturnResponse_When_InputIsValid()
    {
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new RegisterApiAssetFeature.Handler(apiAssetRepository, serviceAssetRepository, unitOfWork);

        serviceAssetRepository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(ownerService);
        apiAssetRepository.GetByNameAndOwnerAsync(Arg.Any<string>(), Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns((ApiAsset?)null);

        var result = await sut.Handle(
            new RegisterApiAssetFeature.Command("Payments API", "/api/payments", "1.0.0", "Internal", ownerService.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Payments API");
        result.Value.OwnerServiceName.Should().Be("payments-service");
        apiAssetRepository.Received(1).Add(Arg.Any<ApiAsset>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterApiAsset_Should_ReturnNotFound_When_OwnerServiceDoesNotExist()
    {
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new RegisterApiAssetFeature.Handler(apiAssetRepository, serviceAssetRepository, unitOfWork);

        serviceAssetRepository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns((ServiceAsset?)null);

        var result = await sut.Handle(
            new RegisterApiAssetFeature.Command("Payments API", "/api/payments", "1.0.0", "Internal", Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EngineeringGraph.ServiceAsset.NotFound");
        apiAssetRepository.DidNotReceive().Add(Arg.Any<ApiAsset>());
    }

    [Fact]
    public async Task RegisterApiAsset_Should_ReturnConflict_When_ApiAlreadyExists()
    {
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var existing = ApiAsset.Register("Payments API", "/api/payments", "1.0.0", "Internal", ownerService);
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new RegisterApiAssetFeature.Handler(apiAssetRepository, serviceAssetRepository, unitOfWork);

        serviceAssetRepository.GetByIdAsync(Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(ownerService);
        apiAssetRepository.GetByNameAndOwnerAsync(Arg.Any<string>(), Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>()).Returns(existing);

        var result = await sut.Handle(
            new RegisterApiAssetFeature.Command("Payments API", "/api/payments", "1.0.0", "Internal", ownerService.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EngineeringGraph.ApiAsset.AlreadyExists");
    }

    // ── MapConsumerRelationship ───────────────────────────────────────────

    [Fact]
    public async Task MapConsumerRelationship_Should_ReturnResponse_When_ApiAssetExists()
    {
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var apiAsset = ApiAsset.Register("Payments API", "/api/payments", "1.0.0", "Internal", ownerService);
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var sut = new MapConsumerRelationshipFeature.Handler(apiAssetRepository, dateTimeProvider, unitOfWork);

        apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>()).Returns(apiAsset);
        dateTimeProvider.UtcNow.Returns(now);

        var result = await sut.Handle(
            new MapConsumerRelationshipFeature.Command(
                apiAsset.Id.Value,
                "billing-service",
                "Service",
                "Production",
                "CatalogImport",
                "catalog/import.csv",
                0.85m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConsumerName.Should().Be("billing-service");
        result.Value.SourceType.Should().Be("CatalogImport");
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MapConsumerRelationship_Should_ReturnNotFound_When_ApiAssetDoesNotExist()
    {
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sut = new MapConsumerRelationshipFeature.Handler(apiAssetRepository, dateTimeProvider, unitOfWork);

        apiAssetRepository.GetByIdAsync(Arg.Any<ApiAssetId>(), Arg.Any<CancellationToken>()).Returns((ApiAsset?)null);

        var result = await sut.Handle(
            new MapConsumerRelationshipFeature.Command(
                Guid.NewGuid(),
                "billing-service",
                "Service",
                "Production",
                "CatalogImport",
                "catalog/import.csv",
                0.85m),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EngineeringGraph.ApiAsset.NotFound");
    }

    // ── GetAssetGraph ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAssetGraph_Should_ReturnGraphResponse_With_ServicesAndApis()
    {
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        var apiAsset = ApiAsset.Register("Payments API", "/api/payments", "1.0.0", "Internal", ownerService);
        var apiAssetRepository = Substitute.For<IApiAssetRepository>();
        var serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
        var sut = new GetAssetGraphFeature.Handler(apiAssetRepository, serviceAssetRepository);

        apiAssetRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<ApiAsset> { apiAsset });
        serviceAssetRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<ServiceAsset> { ownerService });

        var result = await sut.Handle(new GetAssetGraphFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().ContainSingle(s => s.Name == "payments-service");
        result.Value.Apis.Should().ContainSingle(a => a.Name == "Payments API");
    }
}

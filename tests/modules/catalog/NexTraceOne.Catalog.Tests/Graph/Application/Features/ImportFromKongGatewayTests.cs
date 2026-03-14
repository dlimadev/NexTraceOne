using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using ImportFromKongFeature = NexTraceOne.EngineeringGraph.Application.Features.ImportFromKongGateway.ImportFromKongGateway;

namespace NexTraceOne.EngineeringGraph.Tests.Application.Features;

/// <summary>
/// Testes do handler ImportFromKongGateway para importação de serviços e rotas do Kong.
/// Valida criação de serviços e APIs, idempotência e tratamento de falhas por item.
/// </summary>
public sealed class ImportFromKongGatewayTests
{
    private readonly IServiceAssetRepository _serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
    private readonly IApiAssetRepository _apiAssetRepository = Substitute.For<IApiAssetRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DateTimeOffset _now = new(2026, 03, 14, 10, 0, 0, TimeSpan.Zero);

    private ImportFromKongFeature.Handler CreateSut()
    {
        _dateTimeProvider.UtcNow.Returns(_now);
        return new ImportFromKongFeature.Handler(
            _serviceAssetRepository, _apiAssetRepository, _dateTimeProvider, _unitOfWork);
    }

    [Fact]
    public async Task ImportFromKong_Should_CreateServiceAndApi_When_NewServiceWithRoutes()
    {
        // Arrange — serviço não existe ainda no grafo
        _serviceAssetRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);
        _apiAssetRepository.GetByNameAndOwnerAsync(Arg.Any<string>(), Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ApiAsset?)null);

        var sut = CreateSut();
        var command = new ImportFromKongFeature.Command(
            [new ImportFromKongFeature.KongServiceItem(
                "kong-svc-001", "payments-service", "payments.internal", 8080, "http",
                "Finance", "Payments Team",
                [new ImportFromKongFeature.KongRouteItem(
                    "kong-route-001", "Payments API", "/api/v1/payments", "1.0.0", "Internal")])],
            "kong-prod-01",
            "import-001");

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServicesCreated.Should().Be(1);
        result.Value.ApisCreated.Should().Be(1);
        result.Value.ApisSkipped.Should().Be(0);
        result.Value.Failed.Should().Be(0);
        result.Value.TotalServicesProcessed.Should().Be(1);
        result.Value.CorrelationId.Should().Be("import-001");
        _serviceAssetRepository.Received(1).Add(Arg.Any<ServiceAsset>());
        _apiAssetRepository.Received(1).Add(Arg.Any<ApiAsset>());
    }

    [Fact]
    public async Task ImportFromKong_Should_SkipApi_When_ApiAlreadyExists()
    {
        // Arrange — serviço existe, API já registada
        var existingService = ServiceAsset.Create("payments-service", "Finance", "Payments Team");
        _serviceAssetRepository.GetByNameAsync("payments-service", Arg.Any<CancellationToken>())
            .Returns(existingService);

        var existingApi = ApiAsset.Register("Payments API", "/api/v1/payments", "1.0.0", "Internal", existingService);
        _apiAssetRepository.GetByNameAndOwnerAsync("Payments API", existingService.Id, Arg.Any<CancellationToken>())
            .Returns(existingApi);

        var sut = CreateSut();
        var command = new ImportFromKongFeature.Command(
            [new ImportFromKongFeature.KongServiceItem(
                "kong-svc-001", "payments-service", "payments.internal", 8080, "http",
                "Finance", "Payments Team",
                [new ImportFromKongFeature.KongRouteItem(
                    "kong-route-001", "Payments API", "/api/v1/payments", "1.0.0", "Internal")])],
            "kong-prod-01",
            null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServicesCreated.Should().Be(0);
        result.Value.ApisCreated.Should().Be(0);
        result.Value.ApisSkipped.Should().Be(1);
        result.Value.Results.Should().ContainSingle(r =>
            r.Outcome == ImportFromKongFeature.ImportOutcome.Skipped);
    }

    [Fact]
    public async Task ImportFromKong_Should_CreateMultipleApis_When_ServiceHasMultipleRoutes()
    {
        // Arrange — serviço novo com 3 rotas
        _serviceAssetRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);
        _apiAssetRepository.GetByNameAndOwnerAsync(Arg.Any<string>(), Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ApiAsset?)null);

        var sut = CreateSut();
        var command = new ImportFromKongFeature.Command(
            [new ImportFromKongFeature.KongServiceItem(
                "kong-svc-001", "billing-service", "billing.internal", 8080, "http",
                "Finance", "Billing Team",
                [
                    new ImportFromKongFeature.KongRouteItem("r1", "Invoices API", "/api/v1/invoices", "1.0.0", "Internal"),
                    new ImportFromKongFeature.KongRouteItem("r2", "Payments API", "/api/v1/payments", "1.0.0", "Internal"),
                    new ImportFromKongFeature.KongRouteItem("r3", "Refunds API", "/api/v1/refunds", "1.0.0", "Internal"),
                ])],
            "kong-prod-01",
            null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServicesCreated.Should().Be(1);
        result.Value.ApisCreated.Should().Be(3);
        result.Value.Failed.Should().Be(0);
    }

    [Fact]
    public async Task ImportFromKong_Validator_Should_Reject_EmptyServices()
    {
        var validator = new ImportFromKongFeature.Validator();
        var command = new ImportFromKongFeature.Command([], "kong-01", null);

        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ImportFromKong_Validator_Should_Reject_TooManyServices()
    {
        var validator = new ImportFromKongFeature.Validator();
        var services = Enumerable.Range(1, 51)
            .Select(i => new ImportFromKongFeature.KongServiceItem(
                $"svc-{i}", $"service-{i}", "host", 8080, "http", "Domain", "Team",
                [new ImportFromKongFeature.KongRouteItem($"r-{i}", $"API-{i}", "/api", "1.0.0", "Internal")]))
            .ToList();
        var command = new ImportFromKongFeature.Command(services, "kong-01", null);

        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ImportFromKong_Validator_Should_Accept_ValidCommand()
    {
        var validator = new ImportFromKongFeature.Validator();
        var command = new ImportFromKongFeature.Command(
            [new ImportFromKongFeature.KongServiceItem(
                "svc-001", "payments-service", "payments.internal", 8080, "http",
                "Finance", "Payments Team",
                [new ImportFromKongFeature.KongRouteItem("r-001", "Payments API", "/api/v1/payments", "1.0.0", "Internal")])],
            "kong-prod-01",
            "correlation-001");

        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeTrue();
    }
}

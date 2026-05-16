using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using ImportFromBackstageFeature = NexTraceOne.Catalog.Application.Graph.Features.ImportFromBackstage.ImportFromBackstage;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes do handler ImportFromBackstage para importação de entidades do catálogo Backstage.io.
/// Valida criação de serviços e APIs, idempotência e criação implícita de serviços proprietários.
/// </summary>
public sealed class ImportFromBackstageTests
{
    private readonly IServiceAssetRepository _serviceAssetRepository = Substitute.For<IServiceAssetRepository>();
    private readonly IApiAssetRepository _apiAssetRepository = Substitute.For<IApiAssetRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICatalogGraphUnitOfWork _unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();
    private readonly DateTimeOffset _now = new(2026, 03, 14, 10, 0, 0, TimeSpan.Zero);

    private ImportFromBackstageFeature.Handler CreateSut()
    {
        _dateTimeProvider.UtcNow.Returns(_now);
        return new ImportFromBackstageFeature.Handler(
            _serviceAssetRepository, _apiAssetRepository, _dateTimeProvider, _unitOfWork, Substitute.For<ICurrentTenant>());
    }

    [Fact]
    public async Task ImportFromBackstage_Should_CreateService_When_ComponentEntityIsNew()
    {
        // Arrange — serviço não existe no grafo
        _serviceAssetRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        var sut = CreateSut();
        var command = new ImportFromBackstageFeature.Command(
            [new ImportFromBackstageFeature.BackstageEntityItem(
                "Component", "payments-service", "default", "production",
                "team-payments", "Finance", "Payment processing service", null)],
            "backstage.internal.company.com",
            "bs-import-001");

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServicesCreated.Should().Be(1);
        result.Value.ApisCreated.Should().Be(0);
        result.Value.Skipped.Should().Be(0);
        result.Value.Failed.Should().Be(0);
        result.Value.CorrelationId.Should().Be("bs-import-001");
        _serviceAssetRepository.Received(1).Add(Arg.Any<ServiceAsset>());
    }

    [Fact]
    public async Task ImportFromBackstage_Should_SkipService_When_ComponentAlreadyExists()
    {
        // Arrange — serviço já existe
        var existingService = ServiceAsset.Create("payments-service", "Finance", "team-payments", Guid.NewGuid());
        _serviceAssetRepository.GetByNameAsync("payments-service", Arg.Any<CancellationToken>())
            .Returns(existingService);

        var sut = CreateSut();
        var command = new ImportFromBackstageFeature.Command(
            [new ImportFromBackstageFeature.BackstageEntityItem(
                "Component", "payments-service", "default", "production",
                "team-payments", "Finance", "Payment processing service", null)],
            "backstage.internal.company.com",
            null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServicesCreated.Should().Be(0);
        result.Value.Skipped.Should().Be(1);
        result.Value.Results.Should().ContainSingle(r =>
            r.Outcome == ImportFromBackstageFeature.ImportOutcome.Skipped);
    }

    [Fact]
    public async Task ImportFromBackstage_Should_CreateApi_When_ApiEntityHasValidSpec()
    {
        // Arrange — serviço proprietário existe
        var ownerService = ServiceAsset.Create("payments-service", "Finance", "team-payments", Guid.NewGuid());
        _serviceAssetRepository.GetByNameAsync("payments-service", Arg.Any<CancellationToken>())
            .Returns(ownerService);
        _apiAssetRepository.GetByNameAndOwnerAsync(Arg.Any<string>(), Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ApiAsset?)null);

        var sut = CreateSut();
        var command = new ImportFromBackstageFeature.Command(
            [new ImportFromBackstageFeature.BackstageEntityItem(
                "API", "payments-api", "default", "production",
                "team-payments", "Finance", "Payments REST API",
                new ImportFromBackstageFeature.BackstageApiSpec(
                    "/api/v1/payments", "1.0.0", "Internal", "payments-service"))],
            "backstage.internal.company.com",
            null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApisCreated.Should().Be(1);
        result.Value.Failed.Should().Be(0);
        _apiAssetRepository.Received(1).Add(Arg.Any<ApiAsset>());
    }

    [Fact]
    public async Task ImportFromBackstage_Should_CreateImplicitService_When_OwnerNotFound()
    {
        // Arrange — nenhum serviço existe, API referencia serviço inexistente
        _serviceAssetRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);
        _apiAssetRepository.GetByNameAndOwnerAsync(Arg.Any<string>(), Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ApiAsset?)null);

        var sut = CreateSut();
        var command = new ImportFromBackstageFeature.Command(
            [new ImportFromBackstageFeature.BackstageEntityItem(
                "API", "billing-api", "default", "production",
                "team-billing", "Finance", "Billing API",
                new ImportFromBackstageFeature.BackstageApiSpec(
                    "/api/v1/billing", "1.0.0", "Internal", "billing-service"))],
            "backstage.internal.company.com",
            null);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — serviço criado implicitamente + API criada
        result.IsSuccess.Should().BeTrue();
        result.Value.ServicesCreated.Should().Be(1, because: "billing-service criado implicitamente");
        result.Value.ApisCreated.Should().Be(1);
    }

    [Fact]
    public async Task ImportFromBackstage_Should_ProcessMixedBatch_With_ComponentsAndApis()
    {
        // Arrange — serviço novo, API nova
        _serviceAssetRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);
        _apiAssetRepository.GetByNameAndOwnerAsync(Arg.Any<string>(), Arg.Any<ServiceAssetId>(), Arg.Any<CancellationToken>())
            .Returns((ApiAsset?)null);

        var sut = CreateSut();
        var command = new ImportFromBackstageFeature.Command(
            [
                new ImportFromBackstageFeature.BackstageEntityItem(
                    "Component", "orders-service", "default", "production",
                    "team-orders", "Commerce", "Order processing", null),
                new ImportFromBackstageFeature.BackstageEntityItem(
                    "API", "orders-api", "default", "production",
                    "team-orders", "Commerce", "Orders REST API",
                    new ImportFromBackstageFeature.BackstageApiSpec(
                        "/api/v1/orders", "2.0.0", "Public", "orders-service")),
            ],
            "backstage.internal.company.com",
            "batch-001");

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEntitiesProcessed.Should().Be(2);
        result.Value.CorrelationId.Should().Be("batch-001");
    }

    [Fact]
    public async Task ImportFromBackstage_Validator_Should_Reject_EmptyEntities()
    {
        var validator = new ImportFromBackstageFeature.Validator();
        var command = new ImportFromBackstageFeature.Command([], "backstage.test.com", null);

        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ImportFromBackstage_Validator_Should_Reject_UnsupportedKind()
    {
        var validator = new ImportFromBackstageFeature.Validator();
        var command = new ImportFromBackstageFeature.Command(
            [new ImportFromBackstageFeature.BackstageEntityItem(
                "Unknown", "test", "default", "production",
                "team", "Domain", null, null)],
            "backstage.test.com",
            null);

        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ImportFromBackstage_Validator_Should_Reject_ApiWithoutSpec()
    {
        var validator = new ImportFromBackstageFeature.Validator();
        var command = new ImportFromBackstageFeature.Command(
            [new ImportFromBackstageFeature.BackstageEntityItem(
                "API", "test-api", "default", "production",
                "team", "Domain", null, null)],
            "backstage.test.com",
            null);

        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ImportFromBackstage_Validator_Should_Accept_ValidMixedBatch()
    {
        var validator = new ImportFromBackstageFeature.Validator();
        var command = new ImportFromBackstageFeature.Command(
            [
                new ImportFromBackstageFeature.BackstageEntityItem(
                    "Component", "svc-1", "default", "production",
                    "team", "Domain", null, null),
                new ImportFromBackstageFeature.BackstageEntityItem(
                    "API", "api-1", "default", "production",
                    "team", "Domain", null,
                    new ImportFromBackstageFeature.BackstageApiSpec(
                        "/api/v1/test", "1.0.0", "Internal", "svc-1")),
            ],
            "backstage.test.com",
            "correlation-001");

        var validationResult = await validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeTrue();
    }
}

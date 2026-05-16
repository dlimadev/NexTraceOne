using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using RegisterServiceAssetFeature = NexTraceOne.Catalog.Application.Graph.Features.RegisterServiceAsset.RegisterServiceAsset;

namespace NexTraceOne.Catalog.Tests.Graph.Application.Features;

/// <summary>
/// Testes de enriquecimento completo do RegisterServiceAsset — cobertura dos 11 campos,
/// UpdateDetails, UpdateOwnership, validação e cenários de erro.
/// </summary>
public sealed class RegisterServiceAssetEnrichmentTests
{
    private readonly IServiceAssetRepository _repository;
    private readonly IConfigurationResolutionService _configService;
    private readonly ICatalogGraphUnitOfWork _unitOfWork;
    private readonly RegisterServiceAssetFeature.Handler _sut;

    public RegisterServiceAssetEnrichmentTests()
    {
        _repository = Substitute.For<IServiceAssetRepository>();
        _configService = Substitute.For<IConfigurationResolutionService>();
        _unitOfWork = Substitute.For<ICatalogGraphUnitOfWork>();

        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceAsset?)null);

        _configService.ResolveEffectiveValueAsync(
                Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((NexTraceOne.Configuration.Contracts.DTOs.EffectiveConfigurationDto?)null);

        _sut = new RegisterServiceAssetFeature.Handler(_repository, _configService, _unitOfWork, Substitute.For<ICurrentTenant>());
    }

    [Fact]
    public async Task Should_Return_AllFields_When_FullyEnriched()
    {
        // Arrange
        var command = new RegisterServiceAssetFeature.Command(
            Name: "payment-service",
            Domain: "Payments",
            TeamName: "payments-team",
            Description: "Handles payment processing",
            ServiceType: "RestApi",
            Criticality: "High",
            ExposureType: "External",
            TechnicalOwner: "john.smith@company.com",
            BusinessOwner: "Jane Doe",
            DocumentationUrl: "https://docs.company.com/payments",
            RepositoryUrl: "https://github.com/org/payment-service");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.Name.Should().Be("payment-service");
        response.Domain.Should().Be("Payments");
        response.TeamName.Should().Be("payments-team");
        response.Description.Should().Be("Handles payment processing");
        response.ServiceType.Should().Be("RestApi");
        response.Criticality.Should().Be("High");
        response.ExposureType.Should().Be("External");
        response.TechnicalOwner.Should().Be("john.smith@company.com");
        response.BusinessOwner.Should().Be("Jane Doe");
        response.DocumentationUrl.Should().Be("https://docs.company.com/payments");
        response.RepositoryUrl.Should().Be("https://github.com/org/payment-service");
    }

    [Fact]
    public async Task Should_CreateService_WithOnlyRequiredFields()
    {
        // Arrange
        var command = new RegisterServiceAssetFeature.Command("order-service", "Commerce", "orders-team");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.Name.Should().Be("order-service");
        response.Domain.Should().Be("Commerce");
        response.TeamName.Should().Be("orders-team");
        response.ServiceType.Should().Be("RestApi"); // entity default
        response.Criticality.Should().Be("Medium"); // entity default
        response.ExposureType.Should().Be("Internal"); // entity default
    }

    [Fact]
    public async Task Should_CallUpdateDetails_When_OptionalDetailsProvided()
    {
        // Arrange
        var command = new RegisterServiceAssetFeature.Command(
            "svc-1", "Domain1", "team-1",
            Description: "A description",
            ServiceType: "KafkaProducer",
            Criticality: "Critical",
            ExposureType: "Partner",
            DocumentationUrl: "https://docs.example.com",
            RepositoryUrl: "https://github.com/org/repo");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("A description");
        result.Value.ServiceType.Should().Be("KafkaProducer");
        result.Value.Criticality.Should().Be("Critical");
        result.Value.ExposureType.Should().Be("Partner");
        result.Value.DocumentationUrl.Should().Be("https://docs.example.com");
        result.Value.RepositoryUrl.Should().Be("https://github.com/org/repo");
    }

    [Fact]
    public async Task Should_CallUpdateOwnership_When_OwnersProvided()
    {
        // Arrange
        var command = new RegisterServiceAssetFeature.Command(
            "svc-2", "Domain2", "team-2",
            TechnicalOwner: "tech@company.com",
            BusinessOwner: "Business Manager");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TechnicalOwner.Should().Be("tech@company.com");
        result.Value.BusinessOwner.Should().Be("Business Manager");
    }

    [Fact]
    public async Task Should_HandleFrameworkServiceType()
    {
        // Arrange
        var command = new RegisterServiceAssetFeature.Command(
            "my-sdk", "Platform", "platform-team",
            ServiceType: "Framework");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceType.Should().Be("Framework");
    }

    [Fact]
    public async Task Should_HandleInvalidServiceType_WithDefault()
    {
        // Arrange — invalid enum value should fall back to default
        var command = new RegisterServiceAssetFeature.Command(
            "svc-invalid", "Domain3", "team-3",
            ServiceType: "InvalidType");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceType.Should().Be("RestApi"); // default for ServiceType enum
    }

    [Fact]
    public async Task Should_Fail_When_ServiceAlreadyExists()
    {
        // Arrange
        var existing = ServiceAsset.Create("existing-svc", "Domain", "team", Guid.NewGuid());
        _repository.GetByNameAsync("existing-svc", Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new RegisterServiceAssetFeature.Command("existing-svc", "Domain", "team");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Should_CommitUnitOfWork_OnSuccess()
    {
        // Arrange
        var command = new RegisterServiceAssetFeature.Command("svc-commit", "Domain", "team");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_AddToRepository_OnSuccess()
    {
        // Arrange
        var command = new RegisterServiceAssetFeature.Command("svc-add", "Domain", "team");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repository.Received(1).Add(Arg.Any<ServiceAsset>());
    }
}

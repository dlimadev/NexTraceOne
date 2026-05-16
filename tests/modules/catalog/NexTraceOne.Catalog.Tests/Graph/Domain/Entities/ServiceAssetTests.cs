using FluentAssertions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Tests.Graph.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade ServiceAsset que representa
/// um serviço proprietário de ativos de API no grafo de engenharia.
/// Cenários cobertos: criação válida e geração de Id não-vazio.
/// </summary>
public sealed class ServiceAssetTests
{
    [Fact]
    public void Create_Should_ReturnServiceAsset_When_DataIsValid()
    {
        // Act
        var service = ServiceAsset.Create("orders-service", "Commerce", "Orders Team", Guid.NewGuid());

        // Assert
        service.Name.Should().Be("orders-service");
        service.Domain.Should().Be("Commerce");
        service.TeamName.Should().Be("Orders Team");
    }

    [Fact]
    public void Create_Should_GenerateNonEmptyId_When_ServiceIsCreated()
    {
        // Act
        var service = ServiceAsset.Create("billing-service", "Finance", "Billing Team", Guid.NewGuid());

        // Assert
        service.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void ServiceType_Enum_Should_Have_Framework_With_Value_20()
    {
        // Assert
        ((int)ServiceType.Framework).Should().Be(20);
        Enum.IsDefined(typeof(ServiceType), 20).Should().BeTrue();
    }

    [Fact]
    public void Create_And_UpdateDetails_With_Framework_Type_Should_Succeed()
    {
        // Arrange
        var service = ServiceAsset.Create("shared-framework", "Platform", "Core Team", Guid.NewGuid());

        // Act
        service.UpdateDetails(
            "Shared Framework",
            "Internal framework for cross-cutting concerns",
            ServiceType.Framework,
            "Platform Core",
            Criticality.High,
            LifecycleStatus.Active,
            ExposureType.Internal,
            "https://docs.example.com/framework",
            "https://github.com/example/framework");

        // Assert
        service.ServiceType.Should().Be(ServiceType.Framework);
        service.DisplayName.Should().Be("Shared Framework");
        service.Description.Should().Be("Internal framework for cross-cutting concerns");
        service.Criticality.Should().Be(Criticality.High);
        service.ExposureType.Should().Be(ExposureType.Internal);
        service.DocumentationUrl.Should().Be("https://docs.example.com/framework");
        service.RepositoryUrl.Should().Be("https://github.com/example/framework");
    }
}

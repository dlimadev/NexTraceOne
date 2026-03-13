using FluentAssertions;
using NexTraceOne.EngineeringGraph.Domain.Entities;

namespace NexTraceOne.EngineeringGraph.Tests.Domain.Entities;

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
        var service = ServiceAsset.Create("orders-service", "Commerce", "Orders Team");

        // Assert
        service.Name.Should().Be("orders-service");
        service.Domain.Should().Be("Commerce");
        service.TeamName.Should().Be("Orders Team");
    }

    [Fact]
    public void Create_Should_GenerateNonEmptyId_When_ServiceIsCreated()
    {
        // Act
        var service = ServiceAsset.Create("billing-service", "Finance", "Billing Team");

        // Assert
        service.Id.Value.Should().NotBeEmpty();
    }
}

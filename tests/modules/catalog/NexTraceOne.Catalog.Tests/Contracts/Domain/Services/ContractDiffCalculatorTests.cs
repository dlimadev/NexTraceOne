using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.Contracts.Domain.Enums;
using NexTraceOne.Contracts.Domain.Services;

namespace NexTraceOne.Contracts.Tests.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="ContractDiffCalculator"/>.
/// Valida que o orquestrador multi-protocolo delega corretamente ao calculador
/// específico de cada protocolo e retorna resultado vazio para protocolos não suportados.
/// </summary>
public sealed class ContractDiffCalculatorTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.0.0",
          "paths": {
            "/users": {
              "get": {}
            }
          }
        }
        """;

    private const string SwaggerSpec = """
        {
          "swagger": "2.0",
          "paths": {
            "/users": {
              "get": {}
            }
          }
        }
        """;

    private const string AsyncApiSpec = """
        {
          "asyncapi": "2.6.0",
          "channels": {
            "user/signedup": {
              "publish": {}
            }
          }
        }
        """;

    private const string WsdlSpec = """
        <?xml version="1.0"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/">
          <portType name="UserService">
            <operation name="GetUser"/>
          </portType>
        </definitions>
        """;

    [Fact]
    public void ComputeDiff_Should_DelegateToOpenApi_When_ProtocolIsOpenApi()
    {
        // Arrange — remove path na target
        var emptySpec = """{ "paths": {} }""";

        // Act
        var result = ContractDiffCalculator.ComputeDiff(OpenApiSpec, emptySpec, ContractProtocol.OpenApi);

        // Assert — path removido detectado via OpenApiDiffCalculator
        result.BreakingChanges.Should().ContainSingle(c => c.ChangeType == "PathRemoved");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_DelegateToSwagger_When_ProtocolIsSwagger()
    {
        // Arrange
        var emptySpec = """{ "paths": {} }""";

        // Act
        var result = ContractDiffCalculator.ComputeDiff(SwaggerSpec, emptySpec, ContractProtocol.Swagger);

        // Assert
        result.BreakingChanges.Should().ContainSingle(c => c.ChangeType == "PathRemoved");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_DelegateToAsyncApi_When_ProtocolIsAsyncApi()
    {
        // Arrange
        var emptySpec = """{ "channels": {} }""";

        // Act
        var result = ContractDiffCalculator.ComputeDiff(AsyncApiSpec, emptySpec, ContractProtocol.AsyncApi);

        // Assert
        result.BreakingChanges.Should().ContainSingle(c => c.ChangeType == "ChannelRemoved");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_DelegateToWsdl_When_ProtocolIsWsdl()
    {
        // Arrange
        var emptySpec = """
            <?xml version="1.0"?>
            <definitions xmlns="http://schemas.xmlsoap.org/wsdl/">
            </definitions>
            """;

        // Act
        var result = ContractDiffCalculator.ComputeDiff(WsdlSpec, emptySpec, ContractProtocol.Wsdl);

        // Assert
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "PortTypeRemoved");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_ReturnEmptyResult_When_ProtocolIsProtobuf()
    {
        // Act
        var result = ContractDiffCalculator.ComputeDiff("proto content", "proto content", ContractProtocol.Protobuf);

        // Assert
        result.BreakingChanges.Should().BeEmpty();
        result.AdditiveChanges.Should().BeEmpty();
        result.NonBreakingChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
    }

    [Fact]
    public void ComputeDiff_Should_ReturnEmptyResult_When_ProtocolIsGraphQl()
    {
        // Act
        var result = ContractDiffCalculator.ComputeDiff("graphql content", "graphql content", ContractProtocol.GraphQl);

        // Assert
        result.BreakingChanges.Should().BeEmpty();
        result.AdditiveChanges.Should().BeEmpty();
        result.NonBreakingChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_SpecsIdenticalForAllProtocols()
    {
        // Act & Assert — todos os protocolos devem retornar NonBreaking quando specs são iguais
        ContractDiffCalculator.ComputeDiff(OpenApiSpec, OpenApiSpec, ContractProtocol.OpenApi)
            .ChangeLevel.Should().Be(ChangeLevel.NonBreaking);

        ContractDiffCalculator.ComputeDiff(SwaggerSpec, SwaggerSpec, ContractProtocol.Swagger)
            .ChangeLevel.Should().Be(ChangeLevel.NonBreaking);

        ContractDiffCalculator.ComputeDiff(AsyncApiSpec, AsyncApiSpec, ContractProtocol.AsyncApi)
            .ChangeLevel.Should().Be(ChangeLevel.NonBreaking);

        ContractDiffCalculator.ComputeDiff(WsdlSpec, WsdlSpec, ContractProtocol.Wsdl)
            .ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
    }
}

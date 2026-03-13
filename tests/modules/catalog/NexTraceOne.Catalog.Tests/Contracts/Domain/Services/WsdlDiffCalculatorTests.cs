using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.Contracts.Domain.Services;

namespace NexTraceOne.Contracts.Tests.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="WsdlDiffCalculator"/>.
/// Valida a detecção de mudanças breaking, aditivas e non-breaking entre
/// especificações WSDL, incluindo portTypes, operações e message parts.
/// </summary>
public sealed class WsdlDiffCalculatorTests
{
    private const string BaseSpec = """
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                     xmlns:tns="http://example.com/users"
                     targetNamespace="http://example.com/users">
          <message name="GetUserRequest">
            <part name="userId" type="xsd:string"/>
          </message>
          <message name="GetUserResponse">
            <part name="result" type="tns:User"/>
          </message>
          <message name="CreateUserRequest">
            <part name="userData" type="tns:UserInput"/>
          </message>
          <message name="CreateUserResponse">
            <part name="result" type="tns:User"/>
          </message>
          <portType name="UserService">
            <operation name="GetUser">
              <input message="tns:GetUserRequest"/>
              <output message="tns:GetUserResponse"/>
            </operation>
            <operation name="CreateUser">
              <input message="tns:CreateUserRequest"/>
              <output message="tns:CreateUserResponse"/>
            </operation>
          </portType>
        </definitions>
        """;

    [Fact]
    public void ComputeDiff_Should_DetectRemovedPortType_When_PortTypeMissingInTarget()
    {
        // Arrange — spec alvo sem nenhum portType
        var targetSpec = """
            <?xml version="1.0"?>
            <definitions xmlns="http://schemas.xmlsoap.org/wsdl/">
              <types/>
            </definitions>
            """;

        // Act
        var result = WsdlDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "PortTypeRemoved" && c.Path == "UserService");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_DetectAddedPortType_When_PortTypeMissingInBase()
    {
        // Arrange — spec alvo adiciona novo portType
        var targetSpec = """
            <?xml version="1.0" encoding="UTF-8"?>
            <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                         xmlns:tns="http://example.com/users"
                         targetNamespace="http://example.com/users">
              <message name="GetUserRequest">
                <part name="userId" type="xsd:string"/>
              </message>
              <message name="GetUserResponse">
                <part name="result" type="tns:User"/>
              </message>
              <message name="CreateUserRequest">
                <part name="userData" type="tns:UserInput"/>
              </message>
              <message name="CreateUserResponse">
                <part name="result" type="tns:User"/>
              </message>
              <message name="PingRequest"/>
              <portType name="UserService">
                <operation name="GetUser">
                  <input message="tns:GetUserRequest"/>
                  <output message="tns:GetUserResponse"/>
                </operation>
                <operation name="CreateUser">
                  <input message="tns:CreateUserRequest"/>
                  <output message="tns:CreateUserResponse"/>
                </operation>
              </portType>
              <portType name="HealthService">
                <operation name="Ping">
                  <input message="PingRequest"/>
                </operation>
              </portType>
            </definitions>
            """;

        // Act
        var result = WsdlDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.AdditiveChanges.Should().ContainSingle(c => c.ChangeType == "PortTypeAdded" && c.Path == "HealthService");
        result.BreakingChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedOperation_When_OperationMissingInTarget()
    {
        // Arrange — remove CreateUser
        var targetSpec = """
            <?xml version="1.0" encoding="UTF-8"?>
            <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                         xmlns:tns="http://example.com/users"
                         targetNamespace="http://example.com/users">
              <message name="GetUserRequest">
                <part name="userId" type="xsd:string"/>
              </message>
              <message name="GetUserResponse">
                <part name="result" type="tns:User"/>
              </message>
              <portType name="UserService">
                <operation name="GetUser">
                  <input message="tns:GetUserRequest"/>
                  <output message="tns:GetUserResponse"/>
                </operation>
              </portType>
            </definitions>
            """;

        // Act
        var result = WsdlDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "OperationRemoved" && c.Method == "CreateUser");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_SpecsAreIdentical()
    {
        // Act
        var result = WsdlDiffCalculator.ComputeDiff(BaseSpec, BaseSpec);

        // Assert
        result.BreakingChanges.Should().BeEmpty();
        result.AdditiveChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
    }

    [Fact]
    public void ComputeDiff_Should_HandleMalformedXml_Gracefully()
    {
        // Act
        var result = WsdlDiffCalculator.ComputeDiff("<broken xml>>", BaseSpec);

        // Assert
        result.Should().NotBeNull();
        result.AdditiveChanges.Should().NotBeEmpty();
    }
}

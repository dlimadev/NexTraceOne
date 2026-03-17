using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="WsdlSpecParser"/>.
/// Valida a extração de portTypes, operações e message parts de especificações WSDL,
/// garantindo que o parsing XML está correto e resiliente a XML malformado.
/// </summary>
public sealed class WsdlSpecParserTests
{
    private const string ValidWsdl = """
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                     xmlns:tns="http://example.com/users"
                     targetNamespace="http://example.com/users">
          <message name="GetUserRequest">
            <part name="userId" type="xsd:string"/>
            <part name="includeDetails" type="xsd:boolean"/>
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

    private const string PlainWsdl = """
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions>
          <message name="PingRequest">
            <part name="input" type="xsd:string"/>
          </message>
          <portType name="HealthService">
            <operation name="Ping">
              <input message="PingRequest"/>
            </operation>
          </portType>
        </definitions>
        """;

    #region ExtractOperations

    [Fact]
    public void ExtractOperations_Should_ReturnAllPortTypes_When_ValidWsdl()
    {
        // Act
        var operations = WsdlSpecParser.ExtractOperations(ValidWsdl);

        // Assert
        operations.Should().ContainSingle();
        operations.Should().ContainKey("UserService");
    }

    [Fact]
    public void ExtractOperations_Should_ReturnAllOperations_When_MultipleOperationsDefined()
    {
        // Act
        var operations = WsdlSpecParser.ExtractOperations(ValidWsdl);

        // Assert
        operations["UserService"].Should().HaveCount(2);
        operations["UserService"].Should().Contain("GetUser").And.Contain("CreateUser");
    }

    [Fact]
    public void ExtractOperations_Should_HandlePlainXml_When_NoWsdlNamespace()
    {
        // Act — WSDL sem namespace WSDL (plain XML)
        var operations = WsdlSpecParser.ExtractOperations(PlainWsdl);

        // Assert
        operations.Should().ContainKey("HealthService");
        operations["HealthService"].Should().ContainSingle().Which.Should().Be("Ping");
    }

    [Fact]
    public void ExtractOperations_Should_ReturnEmptyDictionary_When_XmlMalformed()
    {
        // Act
        var operations = WsdlSpecParser.ExtractOperations("<not valid xml>>>");

        // Assert
        operations.Should().BeEmpty();
    }

    [Fact]
    public void ExtractOperations_Should_ReturnEmptyDictionary_When_NoPortTypes()
    {
        // Arrange
        var emptyWsdl = """
            <?xml version="1.0"?>
            <definitions xmlns="http://schemas.xmlsoap.org/wsdl/">
              <types/>
            </definitions>
            """;

        // Act
        var operations = WsdlSpecParser.ExtractOperations(emptyWsdl);

        // Assert
        operations.Should().BeEmpty();
    }

    #endregion

    #region ExtractMessageParts

    [Fact]
    public void ExtractMessageParts_Should_ReturnParts_When_ValidOperation()
    {
        // Act
        var parts = WsdlSpecParser.ExtractMessageParts(ValidWsdl, "GetUser");

        // Assert
        parts.Should().HaveCount(2);
        parts.Should().ContainKey("userId").WhoseValue.Should().BeTrue();
        parts.Should().ContainKey("includeDetails").WhoseValue.Should().BeTrue();
    }

    [Fact]
    public void ExtractMessageParts_Should_ReturnEmpty_When_OperationNotFound()
    {
        // Act
        var parts = WsdlSpecParser.ExtractMessageParts(ValidWsdl, "NonExistentOp");

        // Assert
        parts.Should().BeEmpty();
    }

    [Fact]
    public void ExtractMessageParts_Should_ReturnEmpty_When_XmlMalformed()
    {
        // Act
        var parts = WsdlSpecParser.ExtractMessageParts("<broken xml", "GetUser");

        // Assert
        parts.Should().BeEmpty();
    }

    [Fact]
    public void ExtractMessageParts_Should_HandlePlainXml_When_NoWsdlNamespace()
    {
        // Act — WSDL sem namespace
        var parts = WsdlSpecParser.ExtractMessageParts(PlainWsdl, "Ping");

        // Assert
        parts.Should().ContainSingle();
        parts.Should().ContainKey("input").WhoseValue.Should().BeTrue();
    }

    #endregion
}

using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="WsdlMetadataExtractor"/>.
/// Valida a extração correta de metadados SOAP/WSDL: nome do serviço, targetNamespace,
/// versão SOAP (1.1/1.2), endpoint, portType, binding e mapa de operações.
/// </summary>
public sealed class WsdlMetadataExtractorTests
{
    private const string ValidWsdl11 = """
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                     xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/"
                     xmlns:tns="http://example.com/users"
                     name="UserService"
                     targetNamespace="http://example.com/users">
          <portType name="UserServicePort">
            <operation name="GetUser"/>
            <operation name="CreateUser"/>
          </portType>
          <binding name="UserServiceBinding" type="tns:UserServicePort">
            <soap:binding style="document" transport="http://schemas.xmlsoap.org/soap/http"/>
          </binding>
          <service name="UserService">
            <port name="UserServicePort" binding="tns:UserServiceBinding">
              <soap:address location="http://example.com/users/service"/>
            </port>
          </service>
        </definitions>
        """;

    private const string ValidWsdl12 = """
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                     xmlns:soap12="http://schemas.xmlsoap.org/wsdl/soap12/"
                     xmlns:tns="http://example.com/orders"
                     name="OrderService"
                     targetNamespace="http://example.com/orders">
          <portType name="OrderServicePort">
            <operation name="PlaceOrder"/>
          </portType>
          <binding name="OrderServiceBinding" type="tns:OrderServicePort">
            <soap12:binding style="document" transport="http://schemas.xmlsoap.org/soap/http"/>
          </binding>
          <service name="OrderService">
            <port name="OrderServicePort" binding="tns:OrderServiceBinding">
              <soap12:address location="http://example.com/orders/service"/>
            </port>
          </service>
        </definitions>
        """;

    [Fact]
    public void Extract_Should_Return_ServiceName_From_NameAttribute()
    {
        var result = WsdlMetadataExtractor.Extract(ValidWsdl11);

        result.ServiceName.Should().Be("UserService");
    }

    [Fact]
    public void Extract_Should_Return_TargetNamespace()
    {
        var result = WsdlMetadataExtractor.Extract(ValidWsdl11);

        result.TargetNamespace.Should().Be("http://example.com/users");
    }

    [Fact]
    public void Extract_Should_Detect_Soap11()
    {
        var result = WsdlMetadataExtractor.Extract(ValidWsdl11);

        result.SoapVersion.Should().Be("1.1");
    }

    [Fact]
    public void Extract_Should_Detect_Soap12()
    {
        var result = WsdlMetadataExtractor.Extract(ValidWsdl12);

        result.SoapVersion.Should().Be("1.2");
    }

    [Fact]
    public void Extract_Should_Extract_EndpointUrl_Soap11()
    {
        var result = WsdlMetadataExtractor.Extract(ValidWsdl11);

        result.EndpointUrl.Should().Be("http://example.com/users/service");
    }

    [Fact]
    public void Extract_Should_Extract_EndpointUrl_Soap12()
    {
        var result = WsdlMetadataExtractor.Extract(ValidWsdl12);

        result.EndpointUrl.Should().Be("http://example.com/orders/service");
    }

    [Fact]
    public void Extract_Should_Extract_PortTypeName()
    {
        var result = WsdlMetadataExtractor.Extract(ValidWsdl11);

        result.PortTypeName.Should().Be("UserServicePort");
    }

    [Fact]
    public void Extract_Should_Extract_BindingName()
    {
        var result = WsdlMetadataExtractor.Extract(ValidWsdl11);

        result.BindingName.Should().Be("UserServiceBinding");
    }

    [Fact]
    public void Extract_Should_Serialize_Operations_Json()
    {
        var result = WsdlMetadataExtractor.Extract(ValidWsdl11);

        result.ExtractedOperationsJson.Should().Contain("GetUser");
        result.ExtractedOperationsJson.Should().Contain("CreateUser");
    }

    [Fact]
    public void Extract_Should_Return_Default_For_Malformed_Xml()
    {
        const string malformed = "this is not xml";

        var result = WsdlMetadataExtractor.Extract(malformed, "FallbackService");

        result.ServiceName.Should().Be("FallbackService");
        result.SoapVersion.Should().Be("1.1");
        result.ExtractedOperationsJson.Should().Be("{}");
    }

    [Fact]
    public void Extract_Should_Return_Default_For_Empty_Content()
    {
        var result = WsdlMetadataExtractor.Extract(string.Empty, "MyService");

        result.ServiceName.Should().Be("MyService");
        result.TargetNamespace.Should().Be("http://example.com/service");
        result.SoapVersion.Should().Be("1.1");
    }

    [Fact]
    public void Extract_Should_Use_FallbackServiceName_When_WSDL_Has_No_Name()
    {
        const string wsdlWithoutName = """
            <?xml version="1.0"?>
            <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                         targetNamespace="http://example.com/anon">
            </definitions>
            """;

        var result = WsdlMetadataExtractor.Extract(wsdlWithoutName, "AnonymousService");

        result.ServiceName.Should().Be("AnonymousService");
    }
}

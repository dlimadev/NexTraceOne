using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para a entidade <see cref="SoapContractDetail"/>.
/// Valida criação, métodos de domínio e invariantes da entidade SOAP-específica.
/// </summary>
public sealed class SoapContractDetailTests
{
    private static readonly ContractVersionId ValidVersionId = ContractVersionId.From(Guid.NewGuid());

    [Fact]
    public void Create_Should_Return_Valid_SoapContractDetail()
    {
        var result = SoapContractDetail.Create(
            ValidVersionId,
            serviceName: "UserService",
            targetNamespace: "http://example.com/users",
            soapVersion: "1.1",
            extractedOperationsJson: "{}");

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("UserService");
        result.Value.TargetNamespace.Should().Be("http://example.com/users");
        result.Value.SoapVersion.Should().Be("1.1");
        result.Value.ExtractedOperationsJson.Should().Be("{}");
    }

    [Fact]
    public void Create_Should_Accept_Soap12()
    {
        var result = SoapContractDetail.Create(
            ValidVersionId,
            serviceName: "OrderService",
            targetNamespace: "http://example.com/orders",
            soapVersion: "1.2",
            extractedOperationsJson: "{}");

        result.IsSuccess.Should().BeTrue();
        result.Value.SoapVersion.Should().Be("1.2");
    }

    [Fact]
    public void Create_Should_Fail_For_Invalid_SoapVersion()
    {
        var result = SoapContractDetail.Create(
            ValidVersionId,
            serviceName: "TestService",
            targetNamespace: "http://example.com/test",
            soapVersion: "3.0",
            extractedOperationsJson: "{}");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Soap.InvalidSoapVersion");
    }

    [Fact]
    public void Create_Should_Set_OptionalFields_When_Provided()
    {
        var result = SoapContractDetail.Create(
            ValidVersionId,
            serviceName: "UserService",
            targetNamespace: "http://example.com/users",
            soapVersion: "1.1",
            extractedOperationsJson: """{"UserPortType":["GetUser","CreateUser"]}""",
            endpointUrl: "http://example.com/users/service",
            wsdlSourceUrl: "http://example.com/users.wsdl",
            portTypeName: "UserPortType",
            bindingName: "UserBinding");

        result.IsSuccess.Should().BeTrue();
        result.Value.EndpointUrl.Should().Be("http://example.com/users/service");
        result.Value.WsdlSourceUrl.Should().Be("http://example.com/users.wsdl");
        result.Value.PortTypeName.Should().Be("UserPortType");
        result.Value.BindingName.Should().Be("UserBinding");
    }

    [Fact]
    public void UpdateEndpoint_Should_Update_EndpointUrl()
    {
        var detail = SoapContractDetail.Create(
            ValidVersionId,
            serviceName: "TestService",
            targetNamespace: "http://example.com",
            soapVersion: "1.1",
            extractedOperationsJson: "{}").Value;

        detail.UpdateEndpoint("http://production.example.com/service");

        detail.EndpointUrl.Should().Be("http://production.example.com/service");
    }

    [Fact]
    public void UpdateFromParsing_Should_Update_AllFields()
    {
        var detail = SoapContractDetail.Create(
            ValidVersionId,
            serviceName: "OldService",
            targetNamespace: "http://old.example.com",
            soapVersion: "1.1",
            extractedOperationsJson: "{}").Value;

        detail.UpdateFromParsing(
            "NewService",
            "http://new.example.com",
            "1.2",
            """{"NewPort":["Op1"]}""",
            "NewPort",
            "NewBinding");

        detail.ServiceName.Should().Be("NewService");
        detail.TargetNamespace.Should().Be("http://new.example.com");
        detail.SoapVersion.Should().Be("1.2");
        detail.ExtractedOperationsJson.Should().Be("""{"NewPort":["Op1"]}""");
        detail.PortTypeName.Should().Be("NewPort");
        detail.BindingName.Should().Be("NewBinding");
    }

    [Fact]
    public void SoapContractDetailId_New_Should_Return_UniqueId()
    {
        var id1 = SoapContractDetailId.New();
        var id2 = SoapContractDetailId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void SoapContractDetailId_From_Should_Roundtrip()
    {
        var guid = Guid.NewGuid();
        var id = SoapContractDetailId.From(guid);

        id.Value.Should().Be(guid);
    }
}

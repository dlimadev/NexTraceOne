using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para a entidade <see cref="SoapDraftMetadata"/>.
/// Valida criação e atualização dos metadados SOAP específicos de drafts de contrato.
/// </summary>
public sealed class SoapDraftMetadataTests
{
    private static readonly ContractDraftId ValidDraftId = ContractDraftId.From(Guid.NewGuid());

    [Fact]
    public void Create_Should_Return_Valid_SoapDraftMetadata()
    {
        var metadata = SoapDraftMetadata.Create(
            ValidDraftId,
            serviceName: "PaymentService",
            targetNamespace: "http://example.com/payments");

        metadata.ServiceName.Should().Be("PaymentService");
        metadata.TargetNamespace.Should().Be("http://example.com/payments");
        metadata.SoapVersion.Should().Be("1.1");
        metadata.OperationsJson.Should().Be("{}");
    }

    [Fact]
    public void Create_Should_Accept_Soap12_Version()
    {
        var metadata = SoapDraftMetadata.Create(
            ValidDraftId,
            serviceName: "OrderService",
            targetNamespace: "http://example.com/orders",
            soapVersion: "1.2");

        metadata.SoapVersion.Should().Be("1.2");
    }

    [Fact]
    public void Create_Should_Default_To_11_For_Invalid_Version()
    {
        var metadata = SoapDraftMetadata.Create(
            ValidDraftId,
            serviceName: "TestService",
            targetNamespace: "http://example.com/test",
            soapVersion: "3.0");

        metadata.SoapVersion.Should().Be("1.1");
    }

    [Fact]
    public void Create_Should_Set_Optional_Fields_When_Provided()
    {
        var metadata = SoapDraftMetadata.Create(
            ValidDraftId,
            serviceName: "TestService",
            targetNamespace: "http://example.com/test",
            soapVersion: "1.1",
            endpointUrl: "http://example.com/service",
            portTypeName: "TestPort",
            bindingName: "TestBinding",
            operationsJson: """{"TestPort":["Op1","Op2"]}""");

        metadata.EndpointUrl.Should().Be("http://example.com/service");
        metadata.PortTypeName.Should().Be("TestPort");
        metadata.BindingName.Should().Be("TestBinding");
        metadata.OperationsJson.Should().Be("""{"TestPort":["Op1","Op2"]}""");
    }

    [Fact]
    public void Update_Should_Change_All_Fields()
    {
        var metadata = SoapDraftMetadata.Create(
            ValidDraftId,
            serviceName: "OldService",
            targetNamespace: "http://old.example.com");

        metadata.Update(
            serviceName: "NewService",
            targetNamespace: "http://new.example.com",
            soapVersion: "1.2",
            operationsJson: """{"NewPort":["GetData"]}""",
            endpointUrl: "http://new.example.com/ws",
            portTypeName: "NewPort",
            bindingName: "NewBinding");

        metadata.ServiceName.Should().Be("NewService");
        metadata.TargetNamespace.Should().Be("http://new.example.com");
        metadata.SoapVersion.Should().Be("1.2");
        metadata.OperationsJson.Should().Be("""{"NewPort":["GetData"]}""");
        metadata.EndpointUrl.Should().Be("http://new.example.com/ws");
        metadata.PortTypeName.Should().Be("NewPort");
        metadata.BindingName.Should().Be("NewBinding");
    }

    [Fact]
    public void SoapDraftMetadataId_New_Should_Return_UniqueId()
    {
        var id1 = SoapDraftMetadataId.New();
        var id2 = SoapDraftMetadataId.New();

        id1.Should().NotBe(id2);
    }
}

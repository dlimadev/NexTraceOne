using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

using GetSoapContractDetailFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetSoapContractDetail.GetSoapContractDetail;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para <see cref="GetSoapContractDetailFeature"/>.
/// Valida que a query retorna os detalhes SOAP corretamente ou erro quando não encontrado.
/// </summary>
public sealed class GetSoapContractDetailTests
{
    private static readonly ContractVersionId ValidVersionId = ContractVersionId.From(Guid.NewGuid());

    private static ISoapContractDetailRepository CreateRepository() =>
        Substitute.For<ISoapContractDetailRepository>();

    private static SoapContractDetail CreateDetail(ContractVersionId versionId) =>
        SoapContractDetail.Create(
            versionId,
            serviceName: "UserService",
            targetNamespace: "http://example.com/users",
            soapVersion: "1.1",
            extractedOperationsJson: """{"UserPort":["GetUser","CreateUser"]}""",
            endpointUrl: "http://example.com/users/service",
            wsdlSourceUrl: "http://example.com/users.wsdl",
            portTypeName: "UserPort",
            bindingName: "UserBinding").Value;

    [Fact]
    public async Task Handle_Should_Return_SoapDetail_When_Found()
    {
        var repository = CreateRepository();
        var detail = CreateDetail(ValidVersionId);
        repository.GetByContractVersionIdAsync(Arg.Is<ContractVersionId>(id => id == ValidVersionId), Arg.Any<CancellationToken>())
            .Returns(detail);

        var sut = new GetSoapContractDetailFeature.Handler(repository);
        var query = new GetSoapContractDetailFeature.Query(ValidVersionId.Value);

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("UserService");
        result.Value.TargetNamespace.Should().Be("http://example.com/users");
        result.Value.SoapVersion.Should().Be("1.1");
        result.Value.EndpointUrl.Should().Be("http://example.com/users/service");
        result.Value.PortTypeName.Should().Be("UserPort");
        result.Value.BindingName.Should().Be("UserBinding");
        result.Value.ExtractedOperationsJson.Should().Contain("GetUser");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_Error_When_Detail_Missing()
    {
        var repository = CreateRepository();
        repository.GetByContractVersionIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns((SoapContractDetail?)null);

        var sut = new GetSoapContractDetailFeature.Handler(repository);
        var query = new GetSoapContractDetailFeature.Query(Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contracts.Soap.DetailNotFound");
    }

    [Fact]
    public async Task Handle_Should_Return_Correct_ContractVersionId_In_Response()
    {
        var versionId = ContractVersionId.From(Guid.NewGuid());
        var repository = CreateRepository();
        var detail = CreateDetail(versionId);
        repository.GetByContractVersionIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>())
            .Returns(detail);

        var sut = new GetSoapContractDetailFeature.Handler(repository);
        var query = new GetSoapContractDetailFeature.Query(versionId.Value);

        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContractVersionId.Should().Be(versionId.Value);
    }

    [Fact]
    public void Validator_Should_Fail_For_Empty_ContractVersionId()
    {
        var validator = new GetSoapContractDetailFeature.Validator();
        var query = new GetSoapContractDetailFeature.Query(Guid.Empty);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Pass_For_Valid_ContractVersionId()
    {
        var validator = new GetSoapContractDetailFeature.Validator();
        var query = new GetSoapContractDetailFeature.Query(Guid.NewGuid());

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using ValidateContractIntegrityFeature = NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractIntegrity.ValidateContractIntegrity;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

public sealed class ValidateContractIntegrityProtocolTests
{
    private const string OpenApiSpec = """{"openapi":"3.0.0","info":{"title":"Test","version":"1.0.0"},"paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private const string SwaggerSpec = """{"swagger":"2.0","info":{"title":"Test","version":"1.0.0"},"basePath":"/api","paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""";

    private const string AsyncApiSpec = """{"asyncapi":"2.0.0","info":{"title":"User Events","version":"1.0.0"},"channels":{"user/created":{"publish":{"message":{"payload":{"type":"object","properties":{"userId":{"type":"string"}}}}}}}}""";

    private const string WsdlSpec =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                     xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/"
                     name="TestService">
          <portType name="TestPortType">
            <operation name="GetUser">
              <input message="tns:GetUserRequest"/>
              <output message="tns:GetUserResponse"/>
            </operation>
          </portType>
        </definitions>
        """;

    private const string ProtobufSpec = """syntax = "proto3"; message User { string id = 1; }""";

    // ── OpenAPI ─────────────────────────────────────────────────

    [Fact]
    public async Task ValidateContractIntegrity_OpenApi_ReturnsPathAndEndpointCounts()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", OpenApiSpec, "json", "upload", ContractProtocol.OpenApi).Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        var result = await sut.Handle(
            new ValidateContractIntegrityFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.PathCount.Should().BeGreaterThan(0);
        result.Value.EndpointCount.Should().BeGreaterThan(0);
        result.Value.SchemaVersion.Should().NotBeNullOrWhiteSpace();
        result.Value.ValidationError.Should().BeNull();
    }

    // ── Swagger ─────────────────────────────────────────────────

    [Fact]
    public async Task ValidateContractIntegrity_Swagger_ReturnsValidResult()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", SwaggerSpec, "json", "upload", ContractProtocol.Swagger).Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        var result = await sut.Handle(
            new ValidateContractIntegrityFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.PathCount.Should().BeGreaterThan(0);
        result.Value.EndpointCount.Should().BeGreaterThan(0);
        result.Value.ValidationError.Should().BeNull();
    }

    // ── AsyncAPI ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateContractIntegrity_AsyncApi_ReturnsValidResult()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", AsyncApiSpec, "json", "upload", ContractProtocol.AsyncApi).Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        var result = await sut.Handle(
            new ValidateContractIntegrityFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.PathCount.Should().BeGreaterThan(0);
        result.Value.EndpointCount.Should().BeGreaterThan(0);
        result.Value.ValidationError.Should().BeNull();
    }

    // ── WSDL ────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateContractIntegrity_Wsdl_ReturnsValidResult()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", WsdlSpec, "xml", "upload", ContractProtocol.Wsdl).Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        var result = await sut.Handle(
            new ValidateContractIntegrityFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.PathCount.Should().BeGreaterThan(0);
        result.Value.EndpointCount.Should().BeGreaterThan(0);
        result.Value.ValidationError.Should().BeNull();
    }

    // ── Protobuf (stub) ─────────────────────────────────────────

    [Fact]
    public async Task ValidateContractIntegrity_Protobuf_ReturnsStubResult()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ProtobufSpec, "json", "upload", ContractProtocol.Protobuf).Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        var result = await sut.Handle(
            new ValidateContractIntegrityFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.PathCount.Should().Be(0);
        result.Value.EndpointCount.Should().Be(0);
        result.Value.ValidationError.Should().BeNull();
    }

    // ── Conteúdo inválido (degradação graciosa) ────────────────

    [Fact]
    public async Task ValidateContractIntegrity_InvalidContent_ReturnsValidWithZeroCounts()
    {
        // Parsers são resilientes: JSON malformado retorna schema vazio (IsValid=true, 0 counts)
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", "{{not valid json at all!!", "json", "upload", ContractProtocol.OpenApi).Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        var result = await sut.Handle(
            new ValidateContractIntegrityFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.PathCount.Should().Be(0);
        result.Value.EndpointCount.Should().Be(0);
    }
}

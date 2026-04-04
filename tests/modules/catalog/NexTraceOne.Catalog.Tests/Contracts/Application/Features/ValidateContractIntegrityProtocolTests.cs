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

    private const string ProtobufSpec =
        """
        syntax = "proto3";
        
        package user.v1;
        
        message User {
          string id = 1;
          string name = 2;
        }
        
        message GetUserRequest {
          string id = 1;
        }
        
        service UserService {
          rpc GetUser (GetUserRequest) returns (User);
          rpc ListUsers (GetUserRequest) returns (stream User);
        }
        """;

    private const string GraphQlSpec =
        """
        type User {
          id: ID!
          name: String!
          email: String
        }
        
        input CreateUserInput {
          name: String!
          email: String
        }
        
        enum UserRole {
          ADMIN
          USER
        }
        
        type Query {
          user(id: ID!): User
          users: [User!]!
        }
        
        type Mutation {
          createUser(input: CreateUserInput!): User
        }
        """;

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

    // ── Protobuf ───────────────────────────────────────────────

    [Fact]
    public async Task ValidateContractIntegrity_Protobuf_ReturnsMessageAndRpcCounts()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", ProtobufSpec, "proto", "upload", ContractProtocol.Protobuf).Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        var result = await sut.Handle(
            new ValidateContractIntegrityFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.PathCount.Should().Be(2, "there are 2 message definitions");
        result.Value.EndpointCount.Should().Be(2, "there are 2 rpc definitions");
        result.Value.SchemaVersion.Should().Be("proto3");
        result.Value.ValidationError.Should().BeNull();
    }

    // ── GraphQL ─────────────────────────────────────────────────

    [Fact]
    public async Task ValidateContractIntegrity_GraphQl_ReturnsTypeAndFieldCounts()
    {
        var version = ContractVersion.Import(Guid.NewGuid(), "1.0.0", GraphQlSpec, "graphql", "upload", ContractProtocol.GraphQl).Value;
        var repository = Substitute.For<IContractVersionRepository>();
        repository.GetByIdAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);
        var sut = new ValidateContractIntegrityFeature.Handler(repository);

        var result = await sut.Handle(
            new ValidateContractIntegrityFeature.Query(version.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.PathCount.Should().BeGreaterThanOrEqualTo(4, "type User, input CreateUserInput, enum UserRole, type Query, type Mutation");
        result.Value.EndpointCount.Should().BeGreaterThanOrEqualTo(2, "Query has user+users, Mutation has createUser");
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

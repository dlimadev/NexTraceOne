using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="CanonicalModelBuilder"/>.
/// Valida a construção do modelo canônico a partir de especificações
/// OpenAPI, Swagger, AsyncAPI e WSDL.
/// </summary>
public sealed class CanonicalModelBuilderTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.1.0",
          "info": { "title": "Users API", "version": "1.2.0", "description": "User management API" },
          "servers": [{ "url": "https://api.example.com" }],
          "tags": [{ "name": "users" }],
          "paths": {
            "/users": {
              "get": {
                "operationId": "listUsers",
                "summary": "List all users",
                "tags": ["users"],
                "parameters": [
                  { "name": "page", "required": false, "schema": { "type": "integer" } },
                  { "name": "limit", "required": true, "schema": { "type": "integer" } }
                ]
              },
              "post": {
                "operationId": "createUser",
                "description": "Create a new user",
                "deprecated": true
              }
            }
          },
          "components": {
            "schemas": {
              "User": { "type": "object", "description": "User entity" },
              "Error": { "type": "object" }
            },
            "securitySchemes": {
              "bearerAuth": { "type": "http", "scheme": "bearer" }
            }
          }
        }
        """;

    private const string SwaggerSpec = """
        {
          "swagger": "2.0",
          "info": { "title": "Legacy API", "version": "1.0.0" },
          "paths": {
            "/items": {
              "get": { "operationId": "listItems", "summary": "List items" }
            }
          },
          "definitions": {
            "Item": { "type": "object" }
          },
          "securityDefinitions": {
            "apiKey": { "type": "apiKey" }
          },
          "tags": [{ "name": "items" }]
        }
        """;

    private const string AsyncApiSpec = """
        {
          "asyncapi": "2.6.0",
          "info": { "title": "User Events", "version": "1.0.0", "description": "User event streaming" },
          "servers": {
            "production": { "url": "kafka://broker:9092" }
          },
          "channels": {
            "user/signedup": {
              "publish": { "operationId": "userSignedUp", "description": "User signed up event" },
              "subscribe": { "operationId": "onUserSignedUp" }
            }
          },
          "components": {
            "schemas": {
              "UserSignedUpEvent": { "type": "object" }
            }
          },
          "tags": [{ "name": "users" }]
        }
        """;

    private const string WsdlSpec = """
        <?xml version="1.0"?>
        <definitions name="UserService"
            xmlns="http://schemas.xmlsoap.org/wsdl/"
            xmlns:xsd="http://www.w3.org/2001/XMLSchema">
          <types>
            <xsd:schema>
              <xsd:element name="GetUserRequest" type="xsd:string"/>
              <xsd:element name="GetUserResponse" type="xsd:string"/>
            </xsd:schema>
          </types>
          <portType name="UserPortType">
            <operation name="GetUser">
              <documentation>Retrieves user by ID</documentation>
              <input message="GetUserRequest"/>
              <output message="GetUserResponse"/>
            </operation>
            <operation name="UpdateUser">
              <input message="UpdateUserRequest"/>
            </operation>
          </portType>
        </definitions>
        """;

    [Fact]
    public void Build_Should_ExtractOpenApiMetadata_When_ValidSpec()
    {
        var model = CanonicalModelBuilder.Build(OpenApiSpec, ContractProtocol.OpenApi);

        model.Protocol.Should().Be(ContractProtocol.OpenApi);
        model.Title.Should().Be("Users API");
        model.SpecVersion.Should().Be("1.2.0");
        model.Description.Should().Be("User management API");
        model.Servers.Should().Contain("https://api.example.com");
        model.Tags.Should().Contain("users");
        model.HasSecurityDefinitions.Should().BeTrue();
    }

    [Fact]
    public void Build_Should_ExtractOpenApiOperations_When_ValidSpec()
    {
        var model = CanonicalModelBuilder.Build(OpenApiSpec, ContractProtocol.OpenApi);

        model.Operations.Should().HaveCount(2);
        model.OperationCount.Should().Be(2);

        var getUsers = model.Operations.First(o => o.OperationId == "listUsers");
        getUsers.Method.Should().Be("GET");
        getUsers.Path.Should().Be("/users");
        getUsers.InputParameters.Should().HaveCount(2);
        getUsers.IsDeprecated.Should().BeFalse();

        var createUser = model.Operations.First(o => o.OperationId == "createUser");
        createUser.Method.Should().Be("POST");
        createUser.IsDeprecated.Should().BeTrue();
    }

    [Fact]
    public void Build_Should_ExtractOpenApiSchemas_When_ValidSpec()
    {
        var model = CanonicalModelBuilder.Build(OpenApiSpec, ContractProtocol.OpenApi);

        model.GlobalSchemas.Should().HaveCount(2);
        model.SchemaCount.Should().Be(2);
        model.GlobalSchemas.Should().Contain(s => s.Name == "User" && s.Description == "User entity");
        model.GlobalSchemas.Should().Contain(s => s.Name == "Error");
    }

    [Fact]
    public void Build_Should_ExtractSwaggerData_When_ValidSpec()
    {
        var model = CanonicalModelBuilder.Build(SwaggerSpec, ContractProtocol.Swagger);

        model.Protocol.Should().Be(ContractProtocol.Swagger);
        model.Title.Should().Be("Legacy API");
        model.SpecVersion.Should().Be("1.0.0");
        model.Operations.Should().HaveCount(1);
        model.GlobalSchemas.Should().HaveCount(1);
        model.HasSecurityDefinitions.Should().BeTrue();
        model.SecuritySchemes.Should().Contain("apiKey");
        model.Tags.Should().Contain("items");
    }

    [Fact]
    public void Build_Should_ExtractAsyncApiData_When_ValidSpec()
    {
        var model = CanonicalModelBuilder.Build(AsyncApiSpec, ContractProtocol.AsyncApi);

        model.Protocol.Should().Be(ContractProtocol.AsyncApi);
        model.Title.Should().Be("User Events");
        model.SpecVersion.Should().Be("2.6.0");
        model.Description.Should().Be("User event streaming");
        model.Operations.Should().HaveCount(2);
        model.Servers.Should().Contain("kafka://broker:9092");
        model.GlobalSchemas.Should().HaveCount(1);
    }

    [Fact]
    public void Build_Should_ExtractWsdlData_When_ValidSpec()
    {
        var model = CanonicalModelBuilder.Build(WsdlSpec, ContractProtocol.Wsdl);

        model.Protocol.Should().Be(ContractProtocol.Wsdl);
        model.Title.Should().Be("UserService");
        model.Operations.Should().HaveCount(2);
        model.Operations.Should().Contain(o => o.Name == "GetUser");
        model.Operations.Should().Contain(o => o.Name == "UpdateUser");
    }

    [Fact]
    public void Build_Should_ReturnEmptyModel_When_MalformedJson()
    {
        var model = CanonicalModelBuilder.Build("{ invalid json }", ContractProtocol.OpenApi);

        model.Protocol.Should().Be(ContractProtocol.OpenApi);
        model.Title.Should().Be("Unknown");
        model.OperationCount.Should().Be(0);
    }

    [Fact]
    public void Build_Should_ReturnEmptyModel_When_UnsupportedProtocol()
    {
        var model = CanonicalModelBuilder.Build("{}", ContractProtocol.Protobuf);

        model.Protocol.Should().Be(ContractProtocol.Protobuf);
        model.OperationCount.Should().Be(0);
    }

    [Fact]
    public void Build_Should_DetectHasDescriptions_When_OperationsHaveDescriptions()
    {
        var model = CanonicalModelBuilder.Build(OpenApiSpec, ContractProtocol.OpenApi);
        model.HasDescriptions.Should().BeTrue();
    }

    [Fact]
    public void Build_Should_ExtractOperationParameters_When_OpenApiSpecHasParams()
    {
        var model = CanonicalModelBuilder.Build(OpenApiSpec, ContractProtocol.OpenApi);
        var listUsers = model.Operations.First(o => o.OperationId == "listUsers");

        listUsers.InputParameters.Should().HaveCount(2);
        listUsers.InputParameters.Should().Contain(p => p.Name == "page" && !p.IsRequired);
        listUsers.InputParameters.Should().Contain(p => p.Name == "limit" && p.IsRequired);
    }

    [Fact]
    public void Build_Should_ExtractWsdlSchemaElements_When_TypesDefined()
    {
        var model = CanonicalModelBuilder.Build(WsdlSpec, ContractProtocol.Wsdl);
        model.GlobalSchemas.Should().HaveCountGreaterThanOrEqualTo(2);
        model.GlobalSchemas.Should().Contain(s => s.Name == "GetUserRequest");
    }
}

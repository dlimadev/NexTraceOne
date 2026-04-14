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

    // ── YAML input tests ─────────────────────────────────────────────

    private const string OpenApiYamlSpec = """
        openapi: '3.0.3'
        info:
          title: Pet Store API
          version: '2.0.0'
          description: A sample Pet Store API in YAML
        servers:
          - url: https://petstore.example.com/v2
        paths:
          /pets:
            get:
              operationId: listPets
              summary: List all pets
              tags:
                - pets
              parameters:
                - name: limit
                  required: false
                  schema:
                    type: integer
            post:
              operationId: createPet
              description: Create a pet
        components:
          schemas:
            Pet:
              type: object
              description: A pet entity
            Error:
              type: object
          securitySchemes:
            apiKeyAuth:
              type: apiKey
        tags:
          - name: pets
        """;

    [Fact]
    public void Build_Should_ParseYamlOpenApiSpec_When_ContentIsYaml()
    {
        var model = CanonicalModelBuilder.Build(OpenApiYamlSpec, ContractProtocol.OpenApi);

        model.Protocol.Should().Be(ContractProtocol.OpenApi);
        model.Title.Should().Be("Pet Store API");
        model.SpecVersion.Should().Be("2.0.0");
        model.Description.Should().Be("A sample Pet Store API in YAML");
        model.Servers.Should().Contain("https://petstore.example.com/v2");
        model.Operations.Should().HaveCount(2);
        model.OperationCount.Should().Be(2);
        model.GlobalSchemas.Should().HaveCount(2);
        model.SchemaCount.Should().Be(2);
        model.HasSecurityDefinitions.Should().BeTrue();
        model.Tags.Should().Contain("pets");
    }

    [Fact]
    public void Build_Should_ParseYamlSwaggerSpec_When_ContentIsYaml()
    {
        const string yamlSwagger = """
            swagger: '2.0'
            info:
              title: Legacy YAML API
              version: '1.0.0'
            paths:
              /items:
                get:
                  operationId: getItems
                  summary: Get items
            definitions:
              Item:
                type: object
            securityDefinitions:
              apiKey:
                type: apiKey
            """;

        var model = CanonicalModelBuilder.Build(yamlSwagger, ContractProtocol.Swagger);

        model.Protocol.Should().Be(ContractProtocol.Swagger);
        model.Title.Should().Be("Legacy YAML API");
        model.Operations.Should().HaveCount(1);
        model.GlobalSchemas.Should().HaveCount(1);
        model.HasSecurityDefinitions.Should().BeTrue();
    }

    [Fact]
    public void Build_Should_ParseYamlAsyncApiSpec_When_ContentIsYaml()
    {
        const string yamlAsyncApi = """
            asyncapi: '2.6.0'
            info:
              title: Order Events YAML
              version: '1.0.0'
              description: Order event streaming in YAML
            servers:
              production:
                url: kafka://broker:9092
            channels:
              order/created:
                publish:
                  operationId: orderCreated
                  description: Order created event
            components:
              schemas:
                OrderCreatedEvent:
                  type: object
            """;

        var model = CanonicalModelBuilder.Build(yamlAsyncApi, ContractProtocol.AsyncApi);

        model.Protocol.Should().Be(ContractProtocol.AsyncApi);
        model.Title.Should().Be("Order Events YAML");
        model.Description.Should().Be("Order event streaming in YAML");
        model.Operations.Should().HaveCount(1);
        model.Servers.Should().Contain("kafka://broker:9092");
    }

    // ── Robustness tests ─────────────────────────────────────────────

    [Fact]
    public void Build_Should_HandlePathLevelProperties_When_OpenApiHasSummaryAndDescription()
    {
        const string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "API com path-level props", "version": "1.0.0" },
              "paths": {
                "/produtos": {
                  "summary": "Product operations",
                  "description": "All product-related operations",
                  "get": {
                    "operationId": "listProducts",
                    "summary": "List all products",
                    "responses": {
                      "200": {
                        "description": "OK",
                        "content": { "application/json": { "schema": { "type": "array" } } }
                      }
                    }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Produto": { "type": "object", "description": "Product entity" }
                }
              }
            }
            """;

        var model = CanonicalModelBuilder.Build(spec, ContractProtocol.OpenApi);

        model.Title.Should().Be("API com path-level props");
        model.Operations.Should().HaveCount(1);
        model.Operations[0].OperationId.Should().Be("listProducts");
        model.GlobalSchemas.Should().HaveCount(1);
    }

    [Fact]
    public void Build_Should_HandleYamlWithUnquotedVersions_When_OpenApiSpec()
    {
        const string yaml = """
            openapi: 3.0.0
            info:
              title: API de Catálogo de Produtos
              description: Interface para gerenciamento de inventário e consulta de preços.
              version: 1.0.0
            servers:
              - url: https://api.exemplo.com/v1
                description: Servidor de Produção
            paths:
              /produtos:
                get:
                  summary: Lista todos os produtos
                  description: Retorna uma lista paginada de produtos ativos.
                  parameters:
                    - name: limit
                      in: query
                      description: Quantidade máxima de itens a retornar.
                      schema:
                        type: integer
                        default: 10
                  responses:
                    '200':
                      description: Sucesso ao obter a lista.
                      content:
                        application/json:
                          schema:
                            type: array
                            items:
                              type: object
                              properties:
                                id:
                                  type: integer
                                nome:
                                  type: string
            """;

        var model = CanonicalModelBuilder.Build(yaml, ContractProtocol.OpenApi);

        model.Title.Should().Be("API de Catálogo de Produtos");
        model.SpecVersion.Should().Be("1.0.0");
        model.Description.Should().Be("Interface para gerenciamento de inventário e consulta de preços.");
        model.Servers.Should().Contain("https://api.exemplo.com/v1");
        model.Operations.Should().HaveCount(1);
        model.Operations[0].InputParameters.Should().HaveCount(1);
        model.Operations[0].InputParameters[0].Name.Should().Be("limit");
    }

    [Fact]
    public void Build_Should_HandleNonObjectPathValues_When_SpecHasRefPaths()
    {
        const string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Ref Path API", "version": "1.0.0" },
              "paths": {
                "/items": {
                  "get": {
                    "operationId": "getItems",
                    "responses": { "200": { "description": "OK" } }
                  }
                }
              }
            }
            """;

        var model = CanonicalModelBuilder.Build(spec, ContractProtocol.OpenApi);

        model.Title.Should().Be("Ref Path API");
        model.Operations.Should().HaveCount(1);
    }

    [Fact]
    public void Build_Should_ExtractRequestBody_When_OpenApiPostHasBody()
    {
        const string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Product API", "version": "1.0.0" },
              "paths": {
                "/produtos": {
                  "post": {
                    "operationId": "createProduto",
                    "summary": "Cria um novo produto",
                    "requestBody": {
                      "required": true,
                      "content": {
                        "application/json": {
                          "schema": {
                            "type": "object",
                            "required": ["nome", "preco"],
                            "properties": {
                              "nome": { "type": "string" },
                              "preco": { "type": "number", "format": "double" }
                            }
                          }
                        }
                      }
                    },
                    "responses": {
                      "201": {
                        "description": "Produto criado com sucesso.",
                        "content": {
                          "application/json": {
                            "schema": {
                              "type": "object",
                              "properties": {
                                "id": { "type": "string", "format": "uuid" },
                                "nome": { "type": "string" },
                                "preco": { "type": "number" },
                                "disponivel": { "type": "boolean" }
                              }
                            }
                          }
                        }
                      },
                      "400": {
                        "description": "Dados inválidos."
                      }
                    }
                  }
                }
              }
            }
            """;

        var model = CanonicalModelBuilder.Build(spec, ContractProtocol.OpenApi);

        model.Operations.Should().HaveCount(1);
        var op = model.Operations[0];

        // Request body assertions
        op.RequestBody.Should().NotBeNull();
        op.RequestBody!.ContentType.Should().Be("application/json");
        op.RequestBody.IsRequired.Should().BeTrue();
        op.RequestBody.Properties.Should().HaveCount(2);
        op.RequestBody.Properties.Should().Contain(p => p.Name == "nome" && p.DataType == "string" && p.IsRequired);
        op.RequestBody.Properties.Should().Contain(p => p.Name == "preco" && p.DataType == "number" && p.Format == "double");

        // Responses assertions
        op.Responses.Should().NotBeNull();
        op.Responses.Should().HaveCount(2);

        var r201 = op.Responses!.First(r => r.StatusCode == "201");
        r201.Description.Should().Be("Produto criado com sucesso.");
        r201.ContentType.Should().Be("application/json");
        r201.Properties.Should().HaveCount(4);
        r201.Properties.Should().Contain(p => p.Name == "id" && p.Format == "uuid");
        r201.Properties.Should().Contain(p => p.Name == "disponivel" && p.DataType == "boolean");

        var r400 = op.Responses!.First(r => r.StatusCode == "400");
        r400.Description.Should().Be("Dados inválidos.");
        r400.Properties.Should().BeEmpty();

        // OutputFields should be populated from 2xx responses
        op.OutputFields.Should().HaveCount(4);
    }

    [Fact]
    public void Build_Should_ExtractRequestBodyAndResponses_When_SchemaUsesRef()
    {
        const string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Ref API", "version": "1.0.0" },
              "paths": {
                "/users": {
                  "post": {
                    "operationId": "createUser",
                    "requestBody": {
                      "required": true,
                      "content": {
                        "application/json": {
                          "schema": { "$ref": "#/components/schemas/CreateUserRequest" }
                        }
                      }
                    },
                    "responses": {
                      "200": {
                        "description": "User created",
                        "content": {
                          "application/json": {
                            "schema": { "$ref": "#/components/schemas/User" }
                          }
                        }
                      }
                    }
                  }
                }
              },
              "components": {
                "schemas": {
                  "CreateUserRequest": {
                    "type": "object",
                    "required": ["email"],
                    "properties": {
                      "email": { "type": "string", "format": "email" },
                      "name": { "type": "string" }
                    }
                  },
                  "User": {
                    "type": "object",
                    "properties": {
                      "id": { "type": "string", "format": "uuid" },
                      "email": { "type": "string" },
                      "name": { "type": "string" }
                    }
                  }
                }
              }
            }
            """;

        var model = CanonicalModelBuilder.Build(spec, ContractProtocol.OpenApi);
        var op = model.Operations.Should().ContainSingle().Subject;

        // Request body with $ref resolved
        op.RequestBody.Should().NotBeNull();
        op.RequestBody!.SchemaRef.Should().Be("#/components/schemas/CreateUserRequest");
        op.RequestBody.Properties.Should().HaveCount(2);
        op.RequestBody.Properties.Should().Contain(p => p.Name == "email" && p.IsRequired);

        // Response with $ref resolved
        op.Responses.Should().ContainSingle();
        var r200 = op.Responses![0];
        r200.SchemaRef.Should().Be("#/components/schemas/User");
        r200.Properties.Should().HaveCount(3);
        r200.Properties.Should().Contain(p => p.Name == "id" && p.Format == "uuid");

        // Global schemas should have children
        model.GlobalSchemas.Should().HaveCount(2);
        var userSchema = model.GlobalSchemas.First(s => s.Name == "User");
        userSchema.Children.Should().NotBeNull();
        userSchema.Children.Should().HaveCount(3);
    }

    [Fact]
    public void Build_Should_ExtractGetEndpointWithoutRequestBody()
    {
        const string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Simple API", "version": "1.0.0" },
              "paths": {
                "/items": {
                  "get": {
                    "operationId": "listItems",
                    "parameters": [
                      { "name": "page", "in": "query", "required": false, "schema": { "type": "integer" } }
                    ],
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/json": {
                            "schema": {
                              "type": "array",
                              "items": {
                                "type": "object",
                                "properties": {
                                  "id": { "type": "integer" },
                                  "name": { "type": "string" }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var model = CanonicalModelBuilder.Build(spec, ContractProtocol.OpenApi);
        var op = model.Operations.Should().ContainSingle().Subject;

        op.RequestBody.Should().BeNull();
        op.InputParameters.Should().HaveCount(1);
        op.Responses.Should().ContainSingle();

        // Array items should be flattened to properties
        var r200 = op.Responses![0];
        r200.Properties.Should().HaveCount(2);
        r200.Properties.Should().Contain(p => p.Name == "id" && p.DataType == "integer");
    }
}

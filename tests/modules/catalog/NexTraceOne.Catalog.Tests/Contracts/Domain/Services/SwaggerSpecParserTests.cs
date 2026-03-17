using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="SwaggerSpecParser"/>.
/// Valida a extração de paths, métodos HTTP e parâmetros de especificações Swagger 2.0,
/// garantindo que o parsing JSON está correto e resiliente a specs malformadas.
/// </summary>
public sealed class SwaggerSpecParserTests
{
    private const string ValidSpec = """
        {
          "swagger": "2.0",
          "host": "api.example.com",
          "basePath": "/v1",
          "consumes": ["application/json"],
          "produces": ["application/json"],
          "paths": {
            "/users": {
              "get": { "parameters": [{ "name": "page", "in": "query", "required": false }, { "name": "limit", "in": "query", "required": false }] },
              "post": { "parameters": [{ "name": "body", "in": "body", "required": true }] }
            },
            "/orders": {
              "get": { "parameters": [] },
              "delete": { "parameters": [{ "name": "id", "in": "query", "required": true }] }
            },
            "/health": {
              "head": {}
            }
          }
        }
        """;

    #region ExtractPathsAndMethods

    [Fact]
    public void ExtractPathsAndMethods_Should_ReturnAllPaths_When_ValidSpec()
    {
        // Act
        var paths = SwaggerSpecParser.ExtractPathsAndMethods(ValidSpec);

        // Assert
        paths.Should().HaveCount(3);
        paths.Should().ContainKey("/users");
        paths.Should().ContainKey("/orders");
        paths.Should().ContainKey("/health");
    }

    [Fact]
    public void ExtractPathsAndMethods_Should_ReturnCorrectMethods_When_MultipleMethodsDefined()
    {
        // Act
        var paths = SwaggerSpecParser.ExtractPathsAndMethods(ValidSpec);

        // Assert
        paths["/users"].Should().HaveCount(2);
        paths["/users"].Should().Contain("GET").And.Contain("POST");
        paths["/orders"].Should().Contain("GET").And.Contain("DELETE");
        paths["/health"].Should().ContainSingle().Which.Should().Be("HEAD");
    }

    [Fact]
    public void ExtractPathsAndMethods_Should_ReturnEmptyDictionary_When_JsonMalformed()
    {
        // Act
        var paths = SwaggerSpecParser.ExtractPathsAndMethods("{ not valid }}}");

        // Assert
        paths.Should().BeEmpty();
    }

    [Fact]
    public void ExtractPathsAndMethods_Should_ReturnEmptyDictionary_When_NoPathsProperty()
    {
        // Arrange
        var specWithoutPaths = """{ "swagger": "2.0", "info": { "title": "Test" } }""";

        // Act
        var paths = SwaggerSpecParser.ExtractPathsAndMethods(specWithoutPaths);

        // Assert
        paths.Should().BeEmpty();
    }

    [Fact]
    public void ExtractPathsAndMethods_Should_FilterNonHttpMethods_When_SpecContainsExtensions()
    {
        // Arrange — spec Swagger 2.0 com extensão x-custom
        var specWithExtension = """
            {
              "swagger": "2.0",
              "paths": {
                "/api": {
                  "get": {},
                  "x-custom": { "value": true },
                  "parameters": []
                }
              }
            }
            """;

        // Act
        var paths = SwaggerSpecParser.ExtractPathsAndMethods(specWithExtension);

        // Assert — apenas GET deve ser extraído
        paths["/api"].Should().ContainSingle().Which.Should().Be("GET");
    }

    #endregion

    #region ExtractParameters

    [Fact]
    public void ExtractParameters_Should_ReturnParameters_When_ValidEndpoint()
    {
        // Act
        var parameters = SwaggerSpecParser.ExtractParameters(ValidSpec, "/users", "get");

        // Assert
        parameters.Should().HaveCount(2);
        parameters.Should().ContainKey("page").WhoseValue.Should().BeFalse();
        parameters.Should().ContainKey("limit").WhoseValue.Should().BeFalse();
    }

    [Fact]
    public void ExtractParameters_Should_MarkRequired_When_ParameterIsRequired()
    {
        // Act
        var parameters = SwaggerSpecParser.ExtractParameters(ValidSpec, "/users", "post");

        // Assert
        parameters.Should().ContainSingle();
        parameters.Should().ContainKey("body").WhoseValue.Should().BeTrue();
    }

    [Fact]
    public void ExtractParameters_Should_ReturnEmpty_When_PathNotFound()
    {
        // Act
        var parameters = SwaggerSpecParser.ExtractParameters(ValidSpec, "/nonexistent", "get");

        // Assert
        parameters.Should().BeEmpty();
    }

    [Fact]
    public void ExtractParameters_Should_ReturnEmpty_When_JsonMalformed()
    {
        // Act
        var parameters = SwaggerSpecParser.ExtractParameters("{ invalid }", "/users", "get");

        // Assert
        parameters.Should().BeEmpty();
    }

    #endregion
}

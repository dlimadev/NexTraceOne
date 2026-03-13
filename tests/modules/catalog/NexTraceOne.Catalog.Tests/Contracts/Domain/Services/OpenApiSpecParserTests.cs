using NexTraceOne.Contracts.Domain.Services;

namespace NexTraceOne.Contracts.Tests.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="OpenApiSpecParser"/>.
/// Valida a extração de paths, métodos HTTP e parâmetros de especificações OpenAPI,
/// garantindo que o parsing JSON está correto e resiliente a specs malformadas.
/// </summary>
public sealed class OpenApiSpecParserTests
{
    private const string ValidSpec = """
        {
          "paths": {
            "/users": {
              "get": { "parameters": [{ "name": "page", "required": false }, { "name": "limit", "required": false }] },
              "post": { "parameters": [{ "name": "body", "required": true }] }
            },
            "/orders": {
              "get": { "parameters": [] },
              "delete": { "parameters": [{ "name": "id", "required": true }] }
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
        var paths = OpenApiSpecParser.ExtractPathsAndMethods(ValidSpec);

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
        var paths = OpenApiSpecParser.ExtractPathsAndMethods(ValidSpec);

        // Assert
        paths["/users"].Should().HaveCount(2);
        paths["/users"].Should().Contain("GET").And.Contain("POST");
        paths["/orders"].Should().Contain("GET").And.Contain("DELETE");
        paths["/health"].Should().ContainSingle().Which.Should().Be("HEAD");
    }

    [Fact]
    public void ExtractPathsAndMethods_Should_FilterNonHttpMethods_When_SpecContainsExtensions()
    {
        // Arrange — spec com extensão x-custom que não é método HTTP
        var specWithExtension = """
            {
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
        var paths = OpenApiSpecParser.ExtractPathsAndMethods(specWithExtension);

        // Assert — apenas GET deve ser extraído, x-custom e parameters devem ser ignorados
        paths["/api"].Should().ContainSingle().Which.Should().Be("GET");
    }

    [Fact]
    public void ExtractPathsAndMethods_Should_ReturnEmptyDictionary_When_JsonMalformed()
    {
        // Act
        var paths = OpenApiSpecParser.ExtractPathsAndMethods("{ not valid }}}");

        // Assert
        paths.Should().BeEmpty();
    }

    [Fact]
    public void ExtractPathsAndMethods_Should_ReturnEmptyDictionary_When_NoPathsProperty()
    {
        // Arrange
        var specWithoutPaths = """{ "info": { "title": "Test" } }""";

        // Act
        var paths = OpenApiSpecParser.ExtractPathsAndMethods(specWithoutPaths);

        // Assert
        paths.Should().BeEmpty();
    }

    [Fact]
    public void ExtractPathsAndMethods_Should_BeCaseInsensitive_ForMethods()
    {
        // Arrange — métodos em lowercase (padrão OpenAPI)
        var spec = """
            {
              "paths": {
                "/items": {
                  "get": {},
                  "post": {},
                  "put": {},
                  "delete": {},
                  "patch": {},
                  "options": {}
                }
              }
            }
            """;

        // Act
        var paths = OpenApiSpecParser.ExtractPathsAndMethods(spec);

        // Assert — todos os métodos devem ser normalizados para uppercase
        paths["/items"].Should().HaveCount(6);
        paths["/items"].Should().Contain("GET")
            .And.Contain("POST")
            .And.Contain("PUT")
            .And.Contain("DELETE")
            .And.Contain("PATCH")
            .And.Contain("OPTIONS");
    }

    #endregion

    #region ExtractParameters

    [Fact]
    public void ExtractParameters_Should_ReturnParameters_When_ValidEndpoint()
    {
        // Act
        var parameters = OpenApiSpecParser.ExtractParameters(ValidSpec, "/users", "get");

        // Assert
        parameters.Should().HaveCount(2);
        parameters.Should().ContainKey("page").WhoseValue.Should().BeFalse();
        parameters.Should().ContainKey("limit").WhoseValue.Should().BeFalse();
    }

    [Fact]
    public void ExtractParameters_Should_MarkRequired_When_ParameterIsRequired()
    {
        // Act
        var parameters = OpenApiSpecParser.ExtractParameters(ValidSpec, "/users", "post");

        // Assert
        parameters.Should().ContainSingle();
        parameters.Should().ContainKey("body").WhoseValue.Should().BeTrue();
    }

    [Fact]
    public void ExtractParameters_Should_ReturnEmpty_When_PathNotFound()
    {
        // Act
        var parameters = OpenApiSpecParser.ExtractParameters(ValidSpec, "/nonexistent", "get");

        // Assert
        parameters.Should().BeEmpty();
    }

    [Fact]
    public void ExtractParameters_Should_ReturnEmpty_When_MethodNotFound()
    {
        // Act
        var parameters = OpenApiSpecParser.ExtractParameters(ValidSpec, "/users", "delete");

        // Assert
        parameters.Should().BeEmpty();
    }

    [Fact]
    public void ExtractParameters_Should_ReturnEmpty_When_NoParametersProperty()
    {
        // Act — /health HEAD não tem propriedade parameters
        var parameters = OpenApiSpecParser.ExtractParameters(ValidSpec, "/health", "head");

        // Assert
        parameters.Should().BeEmpty();
    }

    [Fact]
    public void ExtractParameters_Should_ReturnEmpty_When_JsonMalformed()
    {
        // Act
        var parameters = OpenApiSpecParser.ExtractParameters("{ invalid }", "/users", "get");

        // Assert
        parameters.Should().BeEmpty();
    }

    [Fact]
    public void ExtractParameters_Should_IgnoreParametersWithoutName()
    {
        // Arrange — parâmetro sem propriedade name
        var spec = """
            {
              "paths": {
                "/test": {
                  "get": {
                    "parameters": [
                      { "required": true },
                      { "name": "valid", "required": false }
                    ]
                  }
                }
              }
            }
            """;

        // Act
        var parameters = OpenApiSpecParser.ExtractParameters(spec, "/test", "get");

        // Assert — apenas o parâmetro com nome deve ser retornado
        parameters.Should().ContainSingle();
        parameters.Should().ContainKey("valid");
    }

    #endregion
}

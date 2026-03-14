using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Contracts.Domain.Services;

namespace NexTraceOne.Contracts.Tests.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="OpenApiDiffCalculator"/>.
/// Valida a detecção de mudanças breaking, aditivas e non-breaking entre
/// especificações OpenAPI, incluindo paths, métodos e parâmetros.
/// </summary>
public sealed class OpenApiDiffCalculatorTests
{
    private const string BaseSpec = """
        {
          "paths": {
            "/users": {
              "get": { "parameters": [{ "name": "page", "required": false }] },
              "post": { "parameters": [{ "name": "body", "required": true }] }
            },
            "/orders": {
              "get": { "parameters": [] }
            }
          }
        }
        """;

    private const string EmptyPathsSpec = """
        {
          "paths": {}
        }
        """;

    [Fact]
    public void ComputeDiff_Should_DetectRemovedPath_When_PathMissingInTarget()
    {
        // Arrange — spec alvo não contém "/orders"
        var targetSpec = """
            {
              "paths": {
                "/users": {
                  "get": { "parameters": [{ "name": "page", "required": false }] },
                  "post": { "parameters": [{ "name": "body", "required": true }] }
                }
              }
            }
            """;

        // Act
        var result = OpenApiDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.BreakingChanges.Should().ContainSingle(c => c.ChangeType == "PathRemoved" && c.Path == "/orders");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_DetectAddedPath_When_PathMissingInBase()
    {
        // Arrange — spec alvo inclui "/products" que não existe na base
        var targetSpec = """
            {
              "paths": {
                "/users": {
                  "get": { "parameters": [{ "name": "page", "required": false }] },
                  "post": { "parameters": [{ "name": "body", "required": true }] }
                },
                "/orders": {
                  "get": { "parameters": [] }
                },
                "/products": {
                  "get": { "parameters": [] }
                }
              }
            }
            """;

        // Act
        var result = OpenApiDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.AdditiveChanges.Should().ContainSingle(c => c.ChangeType == "PathAdded" && c.Path == "/products");
        result.BreakingChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedMethod_When_MethodMissingInTarget()
    {
        // Arrange — spec alvo remove POST de "/users"
        var targetSpec = """
            {
              "paths": {
                "/users": {
                  "get": { "parameters": [{ "name": "page", "required": false }] }
                },
                "/orders": {
                  "get": { "parameters": [] }
                }
              }
            }
            """;

        // Act
        var result = OpenApiDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.BreakingChanges.Should().ContainSingle(c => c.ChangeType == "MethodRemoved" && c.Method == "POST");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_DetectAddedMethod_When_MethodMissingInBase()
    {
        // Arrange — spec alvo adiciona DELETE em "/users"
        var targetSpec = """
            {
              "paths": {
                "/users": {
                  "get": { "parameters": [{ "name": "page", "required": false }] },
                  "post": { "parameters": [{ "name": "body", "required": true }] },
                  "delete": { "parameters": [] }
                },
                "/orders": {
                  "get": { "parameters": [] }
                }
              }
            }
            """;

        // Act
        var result = OpenApiDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.AdditiveChanges.Should().ContainSingle(c => c.ChangeType == "MethodAdded" && c.Method == "DELETE");
        result.BreakingChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_SpecsAreIdentical()
    {
        // Act
        var result = OpenApiDiffCalculator.ComputeDiff(BaseSpec, BaseSpec);

        // Assert
        result.BreakingChanges.Should().BeEmpty();
        result.AdditiveChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
    }

    [Fact]
    public void ComputeDiff_Should_ReturnBreaking_When_RequiredParameterAdded()
    {
        // Arrange — spec alvo adiciona parâmetro obrigatório "filter" em GET /users
        var targetSpec = """
            {
              "paths": {
                "/users": {
                  "get": { "parameters": [
                    { "name": "page", "required": false },
                    { "name": "filter", "required": true }
                  ]},
                  "post": { "parameters": [{ "name": "body", "required": true }] }
                },
                "/orders": {
                  "get": { "parameters": [] }
                }
              }
            }
            """;

        // Act
        var result = OpenApiDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "ParameterRequired" && c.Description.Contains("filter"));
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_HandleInvalidJson_When_SpecIsMalformed()
    {
        // Arrange — JSON inválido como spec base e alvo
        var malformedSpec = "{ not valid json }}}";

        // Act
        var result = OpenApiDiffCalculator.ComputeDiff(malformedSpec, BaseSpec);

        // Assert — spec malformada resulta em dicionário vazio, todos os paths da target são aditivos
        result.Should().NotBeNull();
        result.AdditiveChanges.Should().NotBeEmpty();
    }

    [Fact]
    public void ExtractPathsAndMethods_Should_ReturnPaths_When_ValidOpenApiSpec()
    {
        // Act — parsing delegado ao OpenApiSpecParser após refatoração SRP
        var paths = OpenApiSpecParser.ExtractPathsAndMethods(BaseSpec);

        // Assert
        paths.Should().ContainKey("/users");
        paths.Should().ContainKey("/orders");
        paths["/users"].Should().Contain("GET").And.Contain("POST");
        paths["/orders"].Should().ContainSingle().Which.Should().Be("GET");
    }
}

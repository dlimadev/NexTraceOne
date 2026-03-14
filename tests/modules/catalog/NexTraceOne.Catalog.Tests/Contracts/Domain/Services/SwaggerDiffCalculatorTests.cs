using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Contracts.Domain.Services;

namespace NexTraceOne.Contracts.Tests.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="SwaggerDiffCalculator"/>.
/// Valida a detecção de mudanças breaking, aditivas e non-breaking entre
/// especificações Swagger 2.0, incluindo paths, métodos e parâmetros.
/// </summary>
public sealed class SwaggerDiffCalculatorTests
{
    private const string BaseSpec = """
        {
          "swagger": "2.0",
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

    [Fact]
    public void ComputeDiff_Should_DetectRemovedPath_When_PathMissingInTarget()
    {
        // Arrange
        var targetSpec = """
            {
              "swagger": "2.0",
              "paths": {
                "/users": {
                  "get": { "parameters": [{ "name": "page", "required": false }] },
                  "post": { "parameters": [{ "name": "body", "required": true }] }
                }
              }
            }
            """;

        // Act
        var result = SwaggerDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.BreakingChanges.Should().ContainSingle(c => c.ChangeType == "PathRemoved" && c.Path == "/orders");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_DetectAddedPath_When_PathMissingInBase()
    {
        // Arrange
        var targetSpec = """
            {
              "swagger": "2.0",
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
        var result = SwaggerDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.AdditiveChanges.Should().ContainSingle(c => c.ChangeType == "PathAdded" && c.Path == "/products");
        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_SpecsAreIdentical()
    {
        // Act
        var result = SwaggerDiffCalculator.ComputeDiff(BaseSpec, BaseSpec);

        // Assert
        result.BreakingChanges.Should().BeEmpty();
        result.AdditiveChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedMethod_When_MethodMissingInTarget()
    {
        // Arrange — remove POST de /users
        var targetSpec = """
            {
              "swagger": "2.0",
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
        var result = SwaggerDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.BreakingChanges.Should().ContainSingle(c => c.ChangeType == "MethodRemoved" && c.Method == "POST");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_HandleMalformedJson_Gracefully()
    {
        // Act
        var result = SwaggerDiffCalculator.ComputeDiff("{ invalid }", BaseSpec);

        // Assert — spec malformada resulta em dicionário vazio, todos os paths da target são aditivos
        result.Should().NotBeNull();
        result.AdditiveChanges.Should().NotBeEmpty();
    }
}

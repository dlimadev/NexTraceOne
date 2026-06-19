using System;
using System.Linq;
using FluentAssertions;
using NexTrace.Sdk.Clients;

namespace NexTrace.Sdk.Tests;

public class OpenApiSpecFilterTests
{
    [Fact]
    public void Apply_WithNoFilters_ReturnsNull()
    {
        var result = OpenApiSpecFilter.Apply("{}", null);
        result.Should().BeNull();
    }

    [Fact]
    public void Apply_WithEmptyFilterSet_ReturnsNull()
    {
        var result = OpenApiSpecFilter.Apply("{}", Array.Empty<string>());
        result.Should().BeNull();
    }

    [Fact]
    public void Apply_KeepsMatchingPathAndRemovesOthers()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "T", "version": "1.0" },
              "paths": {
                "/api/v1/payments": {
                  "get": {
                    "operationId": "listPayments",
                    "responses": { "200": { "description": "OK" } }
                  }
                },
                "/api/v1/admin": {
                  "get": {
                    "operationId": "adminHealth",
                    "responses": { "200": { "description": "OK" } }
                  }
                }
              }
            }
            """;

        var result = OpenApiSpecFilter.Apply(spec, ["api/v1/payments"]);

        result.Should().NotBeNull();
        result.Should().Contain("/api/v1/payments");
        result.Should().NotContain("/api/v1/admin");
        result.Should().MatchRegex("\"openapi\"\\s*:\\s*\"3\\.0\\.");
    }

    [Fact]
    public void Apply_KeepsMatchingOperationId()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "T", "version": "1.0" },
              "paths": {
                "/api/v1/mixed": {
                  "get": {
                    "operationId": "keepMe",
                    "responses": { "200": { "description": "OK" } }
                  },
                  "post": {
                    "operationId": "removeMe",
                    "responses": { "200": { "description": "OK" } }
                  }
                }
              }
            }
            """;

        var result = OpenApiSpecFilter.Apply(spec, ["keepMe"]);

        result.Should().NotBeNull();
        result.Should().Contain("keepMe");
        result.Should().NotContain("removeMe");
    }

    [Fact]
    public void Apply_PreservesSwaggerV2Version()
    {
        var spec = """
            {
              "swagger": "2.0",
              "info": { "title": "T", "version": "1.0" },
              "paths": {
                "/api/v1/payments": {
                  "get": {
                    "operationId": "listPayments",
                    "responses": { "200": { "description": "OK" } }
                  }
                },
                "/api/v1/admin": {
                  "get": {
                    "operationId": "adminHealth",
                    "responses": { "200": { "description": "OK" } }
                  }
                }
              },
              "definitions": {
                "Payment": { "type": "object" }
              }
            }
            """;

        var result = OpenApiSpecFilter.Apply(spec, ["listPayments"]);

        result.Should().NotBeNull();
        result.Should().Contain("\"swagger\": \"2.0\"");
        result.Should().Contain("/api/v1/payments");
        result.Should().NotContain("/api/v1/admin");
    }

    [Fact]
    public void Apply_RemovesOrphanedSchemas()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "T", "version": "1.0" },
              "paths": {
                "/api/v1/payments": {
                  "get": {
                    "operationId": "listPayments",
                    "responses": {
                      "200": {
                        "description": "OK",
                        "content": {
                          "application/json": {
                            "schema": { "$ref": "#/components/schemas/Payment" }
                          }
                        }
                      }
                    }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Payment": { "type": "object" },
                  "Orphan": { "type": "object" }
                }
              }
            }
            """;

        var result = OpenApiSpecFilter.Apply(spec, ["listPayments"]);

        result.Should().NotBeNull();
        result.Should().Contain("\"Payment\"");
        result.Should().NotContain("\"Orphan\"");
    }

    [Fact]
    public void Apply_WhenNoPathsRemain_ReturnsNull()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "T", "version": "1.0" },
              "paths": {
                "/api/v1/admin": {
                  "get": {
                    "operationId": "adminHealth",
                    "responses": { "200": { "description": "OK" } }
                  }
                }
              }
            }
            """;

        var result = OpenApiSpecFilter.Apply(spec, ["unknown"]);

        result.Should().BeNull();
    }
}

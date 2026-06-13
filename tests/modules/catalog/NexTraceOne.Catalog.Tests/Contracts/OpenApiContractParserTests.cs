using System.Linq;

using NexTraceOne.Catalog.Application.Contracts.Generation;
using NexTraceOne.Catalog.Infrastructure.Contracts.Generation;

namespace NexTraceOne.Catalog.Tests.Contracts;

/// <summary>
/// Testes para AQ.4 — OpenApiContractParser. Garante que JSON e YAML equivalentes
/// produzem o mesmo modelo neutro (cobertura do caminho YAML via YamlDotNet).
/// </summary>
public sealed class OpenApiContractParserTests
{
    private const string JsonSpec = """
    {
      "openapi": "3.0.0",
      "info": { "title": "Payments API" },
      "paths": {
        "/payments": {
          "post": {
            "operationId": "createPayment",
            "tags": ["Payments"],
            "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/CreatePaymentRequest" } } } },
            "responses": { "201": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Payment" } } } } }
          }
        }
      },
      "components": {
        "schemas": {
          "CreatePaymentRequest": {
            "type": "object",
            "required": ["amount"],
            "properties": { "amount": { "type": "number" }, "currency": { "type": "string" } }
          },
          "Payment": { "type": "object", "properties": { "id": { "type": "string", "format": "uuid" } } }
        }
      }
    }
    """;

    private const string YamlSpec = """
    openapi: 3.0.0
    info:
      title: Payments API
    paths:
      /payments:
        post:
          operationId: createPayment
          tags:
            - Payments
          requestBody:
            content:
              application/json:
                schema:
                  $ref: '#/components/schemas/CreatePaymentRequest'
          responses:
            '201':
              content:
                application/json:
                  schema:
                    $ref: '#/components/schemas/Payment'
    components:
      schemas:
        CreatePaymentRequest:
          type: object
          required:
            - amount
          properties:
            amount:
              type: number
            currency:
              type: string
        Payment:
          type: object
          properties:
            id:
              type: string
              format: uuid
    """;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Parse_JsonAndYaml_ProduceEquivalentModel(bool useYaml)
    {
        var parser = new OpenApiContractParser();

        var result = parser.Parse(useYaml ? YamlSpec : JsonSpec);

        result.IsSuccess.Should().BeTrue();
        var model = result.Value;
        model.Title.Should().Be("Payments API");

        var request = model.Schemas.Single(s => s.Name == "CreatePaymentRequest");
        request.Properties.Single(p => p.Name == "amount").Should()
            .BeEquivalentTo(new { NeutralType = "number", Required = true });
        request.Properties.Single(p => p.Name == "currency").Required.Should().BeFalse();

        var payment = model.Schemas.Single(s => s.Name == "Payment");
        payment.Properties.Single(p => p.Name == "id").NeutralType.Should().Be("uuid");

        var op = model.Operations.Single();
        op.Method.Should().Be("POST");
        op.Path.Should().Be("/payments");
        op.OperationId.Should().Be("createPayment");
        op.Tag.Should().Be("Payments");
        op.RequestSchemaName.Should().Be("CreatePaymentRequest");
        op.ResponseSchemaName.Should().Be("Payment");
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsValidationError()
    {
        var parser = new OpenApiContractParser();

        var result = parser.Parse("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Contract.Empty");
    }
}

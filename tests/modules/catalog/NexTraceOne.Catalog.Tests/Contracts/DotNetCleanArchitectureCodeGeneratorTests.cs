using System.Linq;

using NexTraceOne.Catalog.Application.Contracts.Generation;

namespace NexTraceOne.Catalog.Tests.Contracts;

/// <summary>
/// Testes para AQ.4 — gerador determinístico OpenAPI→código (.NET Clean Architecture).
/// Exercita a lógica pura de geração com um modelo construído à mão (sem parser/biblioteca).
/// </summary>
public sealed class DotNetCleanArchitectureCodeGeneratorTests
{
    private static readonly CodeGenerationOptions Options = new("payment-api");

    [Fact]
    public void Generate_Schema_ProducesRecordDtoWithMappedTypes()
    {
        var model = new OpenApiContractModel(
            Title: "Payments API",
            Schemas: new[]
            {
                new SchemaModel("CreatePaymentRequest", new[]
                {
                    new PropertyModel("amount", "number", Required: true),
                    new PropertyModel("currency", "string", Required: true),
                    new PropertyModel("note", "string", Required: false),
                    new PropertyModel("created_at", "date-time", Required: true)
                })
            },
            Operations: Array.Empty<OperationModel>());

        var files = DotNetCleanArchitectureCodeGenerator.Generate(model, Options);

        var dto = files.Single(f => f.Path == "src/payment-api.Contracts/CreatePaymentRequest.cs");
        dto.Content.Should().Contain("namespace PaymentApi.Contracts;");
        dto.Content.Should().Contain("public sealed record CreatePaymentRequest");
        dto.Content.Should().Contain("public double Amount { get; init; }");
        dto.Content.Should().Contain("public string Currency { get; init; }");
        dto.Content.Should().Contain("public string? Note { get; init; }");
        dto.Content.Should().Contain("public DateTimeOffset CreatedAt { get; init; }");
    }

    [Fact]
    public void Generate_SnakeCaseProperty_AddsJsonPropertyNameAttribute()
    {
        var model = new OpenApiContractModel(
            "API",
            new[] { new SchemaModel("Customer", new[] { new PropertyModel("first_name", "string", true) }) },
            Array.Empty<OperationModel>());

        var files = DotNetCleanArchitectureCodeGenerator.Generate(model, Options);

        var dto = files.Single(f => f.Path.EndsWith("Customer.cs"));
        dto.Content.Should().Contain("[JsonPropertyName(\"first_name\")]");
        dto.Content.Should().Contain("public string FirstName { get; init; }");
    }

    [Fact]
    public void Generate_ArrayAndRefTypes_AreMappedToClrCollectionsAndTypes()
    {
        var model = new OpenApiContractModel(
            "API",
            new[]
            {
                new SchemaModel("Order", new[]
                {
                    new PropertyModel("items", "array:ref:OrderItem", Required: true),
                    new PropertyModel("customer", "ref:Customer", Required: false)
                })
            },
            Array.Empty<OperationModel>());

        var files = DotNetCleanArchitectureCodeGenerator.Generate(model, Options);

        var dto = files.Single(f => f.Path.EndsWith("Order.cs"));
        dto.Content.Should().Contain("public IReadOnlyList<OrderItem> Items { get; init; }");
        dto.Content.Should().Contain("public Customer? Customer { get; init; }");
    }

    [Fact]
    public void Generate_Operations_ProduceEndpointsGroupedByTag()
    {
        var model = new OpenApiContractModel(
            "Payments API",
            Array.Empty<SchemaModel>(),
            new[]
            {
                new OperationModel("POST", "/payments", "createPayment", "Payments",
                    RequestSchemaName: "CreatePaymentRequest", ResponseSchemaName: "Payment", Summary: "Create a payment"),
                new OperationModel("GET", "/payments/{id}", "getPayment", "Payments",
                    RequestSchemaName: null, ResponseSchemaName: "Payment", Summary: null)
            });

        var files = DotNetCleanArchitectureCodeGenerator.Generate(model, Options);

        var endpoints = files.Single(f => f.Path == "src/payment-api.Api/Endpoints/PaymentsEndpoints.cs");
        endpoints.Content.Should().Contain("public static class PaymentsEndpoints");
        endpoints.Content.Should().Contain("public static void MapPaymentsEndpoints(IEndpointRouteBuilder app)");
        endpoints.Content.Should().Contain("app.MapPost(\"/payments\", (CreatePaymentRequest request) =>");
        endpoints.Content.Should().Contain(".WithName(\"CreatePayment\");");
        endpoints.Content.Should().Contain("app.MapGet(\"/payments/{id}\", () =>");
        endpoints.Content.Should().Contain(".WithName(\"GetPayment\");");
    }

    [Theory]
    [InlineData("string", true, "string")]
    [InlineData("string", false, "string?")]
    [InlineData("integer", true, "int")]
    [InlineData("long", true, "long")]
    [InlineData("number", true, "double")]
    [InlineData("boolean", false, "bool?")]
    [InlineData("uuid", true, "Guid")]
    [InlineData("date-time", true, "DateTimeOffset")]
    [InlineData("array:string", true, "IReadOnlyList<string>")]
    [InlineData("ref:Customer", true, "Customer")]
    public void MapToClrType_MapsNeutralTypesCorrectly(string neutral, bool required, string expected)
    {
        DotNetCleanArchitectureCodeGenerator.MapToClrType(neutral, required).Should().Be(expected);
    }
}

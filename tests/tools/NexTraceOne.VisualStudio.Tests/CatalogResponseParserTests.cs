using System.Linq;
using FluentAssertions;
using NexTraceOne.VisualStudio.ToolWindows;
using Xunit;

namespace NexTraceOne.VisualStudio.Tests;

public class CatalogResponseParserTests
{
    [Fact]
    public void Parse_EmptyJson_ReturnsEmptyList()
    {
        var result = CatalogResponseParser.Parse(string.Empty);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_RawArray_ReturnsServices()
    {
        var json = """
            [
              { "name": "payments-api", "teamName": "Payments", "domain": "Finance" },
              { "name": "orders-api", "teamName": "Checkout", "type": "RESTAPI" }
            ]
            """;

        var result = CatalogResponseParser.Parse(json);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("payments-api");
        result[0].TeamName.Should().Be("Payments");
        result[0].Domain.Should().Be("Finance");
        result[1].Name.Should().Be("orders-api");
        result[1].Type.Should().Be("RESTAPI");
    }

    [Fact]
    public void Parse_WrappedItems_ReturnsServices()
    {
        var json = """
            {
              "items": [
                { "name": "inventory-api", "status": "Active" }
              ],
              "total": 1
            }
            """;

        var result = CatalogResponseParser.Parse(json);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("inventory-api");
        result[0].Status.Should().Be("Active");
    }

    [Fact]
    public void Parse_MissingName_FallsBackToUnknown()
    {
        var json = "[{ \"type\": \"service\" }]";

        var result = CatalogResponseParser.Parse(json);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("(unknown)");
    }
}

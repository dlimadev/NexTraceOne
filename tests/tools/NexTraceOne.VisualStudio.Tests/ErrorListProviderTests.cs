using System.Linq;
using FluentAssertions;
using NexTraceOne.VisualStudio.Providers;
using Xunit;

namespace NexTraceOne.VisualStudio.Tests;

public class ErrorListProviderTests
{
    [Fact]
    public void ParseIssues_RawArray_ReturnsIssues()
    {
        var json = """
            [
              {
                "serviceName": "payments-api",
                "message": "Missing owner",
                "severity": "error",
                "filePath": "src/foo.cs",
                "line": 10,
                "column": 5
              }
            ]
            """;

        var result = NexErrorListProvider.ParseIssues(json);

        result.Should().HaveCount(1);
        result[0].ServiceName.Should().Be("payments-api");
        result[0].Message.Should().Be("Missing owner");
        result[0].Severity.Should().Be("error");
        result[0].FilePath.Should().Be("src/foo.cs");
        result[0].Line.Should().Be(10);
        result[0].Column.Should().Be(5);
    }

    [Fact]
    public void ParseIssues_WrappedItems_ReturnsIssues()
    {
        var json = """
            {
              "items": [
                { "service": "orders-api", "description": "No contract", "severity": "warning" }
              ]
            }
            """;

        var result = NexErrorListProvider.ParseIssues(json);

        result.Should().HaveCount(1);
        result[0].ServiceName.Should().Be("orders-api");
        result[0].Message.Should().Be("No contract");
        result[0].Severity.Should().Be("warning");
    }

    [Fact]
    public void ParseIssues_InvalidJson_ReturnsEmptyList()
    {
        var result = NexErrorListProvider.ParseIssues("not json");
        result.Should().BeEmpty();
    }
}

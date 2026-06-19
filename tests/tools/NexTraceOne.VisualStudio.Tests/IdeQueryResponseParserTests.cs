using FluentAssertions;
using NexTraceOne.VisualStudio.ToolWindows;
using Xunit;

namespace NexTraceOne.VisualStudio.Tests;

public class IdeQueryResponseParserTests
{
    [Theory]
    [InlineData("{ \"content\": \"hello\" }", "hello")]
    [InlineData("{ \"output\": \"world\" }", "world")]
    [InlineData("{ \"message\": \"hi\" }", "hi")]
    [InlineData("{ \"response\": \"ok\" }", "ok")]
    [InlineData("{ \"result\": \"done\" }", "done")]
    public void ExtractMessage_ReturnsKnownField(string body, string expected)
    {
        var result = IdeQueryResponseParser.ExtractMessage(body);
        result.Should().Be(expected);
    }

    [Fact]
    public void ExtractMessage_WhenNoKnownField_ReturnsRawBody()
    {
        var body = "{ \"unknown\": \"value\" }";

        var result = IdeQueryResponseParser.ExtractMessage(body);

        result.Should().Be(body);
    }

    [Fact]
    public void ExtractMessage_WhenBodyIsInvalidJson_ReturnsRawBody()
    {
        var body = "not json";

        var result = IdeQueryResponseParser.ExtractMessage(body);

        result.Should().Be(body);
    }

    [Fact]
    public void ExtractMessage_WhenBodyIsEmpty_ReturnsEmptyBody()
    {
        var result = IdeQueryResponseParser.ExtractMessage(string.Empty);
        result.Should().BeEmpty();
    }
}

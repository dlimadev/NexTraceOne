using NexTraceOne.AIKnowledge.Application.Runtime.Utils;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Utils;

public sealed class LlmJsonParserTests
{
    private sealed record TestPayload(string Name, int Value);

    [Fact]
    public void TryParse_PureJson_ReturnsParsedObject()
    {
        var json = """{"name":"test","value":42}""";

        var ok = LlmJsonParser.TryParse<TestPayload>(json, out var result);

        ok.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Name.Should().Be("test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void TryParse_MarkdownCodeBlock_ReturnsParsedObject()
    {
        var markdown = """
            Here is the JSON:
            ```json
            {
              "name": "from-markdown",
              "value": 99
            }
            ```
            """;

        var ok = LlmJsonParser.TryParse<TestPayload>(markdown, out var result);

        ok.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Name.Should().Be("from-markdown");
        result.Value.Should().Be(99);
    }

    [Fact]
    public void TryParse_GenericCodeBlock_ReturnsParsedObject()
    {
        var markdown = """
            ```
            {"name":"generic","value":7}
            ```
            """;

        var ok = LlmJsonParser.TryParse<TestPayload>(markdown, out var result);

        ok.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Name.Should().Be("generic");
    }

    [Fact]
    public void TryParse_JsonSurroundedByText_ReturnsParsedObject()
    {
        var text = "Some explanation before {\"name\":\"embedded\",\"value\":123} and after";

        var ok = LlmJsonParser.TryParse<TestPayload>(text, out var result);

        ok.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Name.Should().Be("embedded");
        result.Value.Should().Be(123);
    }

    [Fact]
    public void TryParse_InvalidJson_ReturnsFalse()
    {
        var ok = LlmJsonParser.TryParse<TestPayload>("not json at all", out var result);

        ok.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryParse_NullOrEmpty_ReturnsFalse()
    {
        LlmJsonParser.TryParse<TestPayload>(null, out _).Should().BeFalse();
        LlmJsonParser.TryParse<TestPayload>("", out _).Should().BeFalse();
        LlmJsonParser.TryParse<TestPayload>("   ", out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_ArrayRoot_ReturnsParsedList()
    {
        var json = """[{"name":"a","value":1},{"name":"b","value":2}]""";

        var ok = LlmJsonParser.TryParse<List<TestPayload>>(json, out var result);

        ok.Should().BeTrue();
        result.Should().HaveCount(2);
        result![0].Name.Should().Be("a");
        result[1].Name.Should().Be("b");
    }

    [Fact]
    public void TryParse_CaseInsensitiveProperties_Works()
    {
        var json = """{"NAME":"CaseTest","VALUE":5}""";

        var ok = LlmJsonParser.TryParse<TestPayload>(json, out var result);

        ok.Should().BeTrue();
        result!.Name.Should().Be("CaseTest");
        result.Value.Should().Be(5);
    }
}

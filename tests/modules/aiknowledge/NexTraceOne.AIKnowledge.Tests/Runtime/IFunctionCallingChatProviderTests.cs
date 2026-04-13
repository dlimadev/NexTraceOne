using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime;

/// <summary>
/// Testes unitários para IFunctionCallingChatProvider.
/// Valida o contrato da interface, a criação de FunctionCallingResult
/// e a estrutura de FunctionDefinition e NativeToolCall.
/// </summary>
public sealed class IFunctionCallingChatProviderTests
{
    // ── FunctionDefinition ──────────────────────────────────────────────

    [Fact]
    public void FunctionDefinition_Created_WithRequiredFields()
    {
        var schema = new { type = "object", properties = new { query = new { type = "string" } } };
        var fd = new FunctionDefinition("search_services", "Searches services", schema);

        fd.Name.Should().Be("search_services");
        fd.Description.Should().Be("Searches services");
        fd.Parameters.Should().BeSameAs(schema);
    }

    [Fact]
    public void FunctionDefinition_NullParameters_IsAllowed()
    {
        var fd = new FunctionDefinition("tool_name", "A tool");
        fd.Parameters.Should().BeNull();
    }

    // ── NativeToolCall ──────────────────────────────────────────────────

    [Fact]
    public void NativeToolCall_Properties_SetCorrectly()
    {
        var tc = new NativeToolCall("call_001", "list_services", """{"env":"production"}""");

        tc.Id.Should().Be("call_001");
        tc.FunctionName.Should().Be("list_services");
        tc.ArgumentsJson.Should().Be("""{"env":"production"}""");
    }

    // ── FunctionCallingResult ───────────────────────────────────────────

    [Fact]
    public void FunctionCallingResult_HasToolCalls_WhenToolCallsPresent()
    {
        var calls = new List<NativeToolCall>
        {
            new("id1", "tool_a", "{}"),
            new("id2", "tool_b", "{}")
        };

        var result = new FunctionCallingResult(
            true, null, calls, "gpt-4o", "openai",
            100, 0, TimeSpan.FromMilliseconds(200));

        result.HasToolCalls.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(2);
        result.Content.Should().BeNull();
    }

    [Fact]
    public void FunctionCallingResult_HasToolCalls_False_WhenEmpty()
    {
        var result = new FunctionCallingResult(
            true, "Hello!", [], "claude-3-5-sonnet-20241022", "anthropic",
            50, 20, TimeSpan.FromMilliseconds(100));

        result.HasToolCalls.Should().BeFalse();
        result.Content.Should().Be("Hello!");
    }

    [Fact]
    public void FunctionCallingResult_Failure_HasErrorMessage()
    {
        var result = new FunctionCallingResult(
            false, null, [], "gpt-4o", "openai",
            0, 0, TimeSpan.Zero, ErrorMessage: "Timeout");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Timeout");
    }

    // ── IFunctionCallingChatProvider interface ──────────────────────────

    [Fact]
    public async Task Provider_CompleteWithToolsAsync_Returns_NativeToolCalls()
    {
        var provider = Substitute.For<IFunctionCallingChatProvider>();
        var functions = new List<FunctionDefinition>
        {
            new("list_services", "Lists all services in the catalog")
        };
        var request = new ChatCompletionRequest("gpt-4o", [], SystemPrompt: null);
        var expectedResult = new FunctionCallingResult(
            true, null,
            new List<NativeToolCall> { new("call_abc", "list_services", "{}") },
            "gpt-4o", "openai", 80, 0, TimeSpan.FromMilliseconds(350));

        provider.CompleteWithToolsAsync(request, functions, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var actual = await provider.CompleteWithToolsAsync(request, functions, CancellationToken.None);

        actual.HasToolCalls.Should().BeTrue();
        actual.ToolCalls[0].FunctionName.Should().Be("list_services");
        actual.ToolCalls[0].Id.Should().Be("call_abc");
    }
}

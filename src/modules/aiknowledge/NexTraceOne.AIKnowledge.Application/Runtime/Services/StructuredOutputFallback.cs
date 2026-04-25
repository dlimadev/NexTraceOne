using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Estratégia de fallback para providers sem native tool calling.
/// Injeta instruções de formato JSON no system prompt e parseia a resposta do modelo.
/// Mais fiável do que o padrão textual [TOOL_CALL:] para modelos pequenos.
/// </summary>
public sealed class StructuredOutputFallback
{
    private readonly ILogger<StructuredOutputFallback> _logger;

    public StructuredOutputFallback(ILogger<StructuredOutputFallback> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executa o provider com um system prompt que instrui o modelo a retornar JSON estruturado.
    /// Parseia o JSON e mapeia para FunctionCallingResult.
    /// </summary>
    public async Task<FunctionCallingResult> CompleteWithStructuredJsonAsync(
        IChatCompletionProvider provider,
        ChatCompletionRequest request,
        IReadOnlyList<FunctionDefinition> functions,
        CancellationToken ct = default)
    {
        var toolPromptAddendum = BuildToolInstructions(functions);
        var existingSystem = request.SystemPrompt ?? string.Empty;
        var combinedSystem = string.IsNullOrWhiteSpace(existingSystem)
            ? toolPromptAddendum
            : $"{existingSystem}\n\n{toolPromptAddendum}";

        var modifiedRequest = new ChatCompletionRequest(
            request.ModelId,
            request.Messages,
            request.Temperature,
            request.MaxTokens,
            combinedSystem);

        var result = await provider.CompleteAsync(modifiedRequest, ct);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Content))
        {
            return new FunctionCallingResult(
                false, null, [], result.ModelId, result.ProviderId,
                result.PromptTokens, result.CompletionTokens, result.Duration,
                ErrorMessage: result.ErrorMessage ?? "No content in response");
        }

        return ParseStructuredResponse(result);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string BuildToolInstructions(IReadOnlyList<FunctionDefinition> functions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Tool Calling Instructions");
        sb.AppendLine("You MUST respond with a single JSON object. No markdown, no extra text.");
        sb.AppendLine();
        sb.AppendLine("To call a tool:");
        sb.AppendLine("""{"action":"call_tool","tool_name":"<name>","arguments":{...}}""");
        sb.AppendLine();
        sb.AppendLine("To give a final answer:");
        sb.AppendLine("""{"action":"respond","response":"<your answer here>"}""");
        sb.AppendLine();
        sb.AppendLine("Available tools:");

        foreach (var fn in functions)
        {
            sb.AppendLine($"- **{fn.Name}**: {fn.Description}");
        }

        return sb.ToString().TrimEnd();
    }

    private FunctionCallingResult ParseStructuredResponse(ChatCompletionResult result)
    {
        var content = result.Content!.Trim();

        // Strip markdown code fences if model wraps in ```json ... ```
        if (content.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = content.IndexOf('\n');
            var lastFence = content.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
                content = content[(firstNewline + 1)..lastFence].Trim();
        }

        try
        {
            var node = JsonNode.Parse(content);
            if (node is null)
                return TextOnlyResult(result);

            var action = node["action"]?.GetValue<string>() ?? "respond";

            if (string.Equals(action, "call_tool", StringComparison.OrdinalIgnoreCase))
            {
                var toolName = node["tool_name"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(toolName))
                    return TextOnlyResult(result);

                var argsNode = node["arguments"];
                var argsJson = argsNode?.ToJsonString() ?? "{}";

                var toolCall = new NativeToolCall(
                    Id: Guid.NewGuid().ToString("N"),
                    FunctionName: toolName,
                    ArgumentsJson: argsJson);

                return new FunctionCallingResult(
                    true,
                    Content: null,
                    ToolCalls: [toolCall],
                    result.ModelId,
                    result.ProviderId,
                    result.PromptTokens,
                    result.CompletionTokens,
                    result.Duration,
                    FinishReason: "tool_use");
            }

            // action == "respond" (or anything else)
            var responseText = node["response"]?.GetValue<string>() ?? content;
            return new FunctionCallingResult(
                true,
                Content: responseText,
                ToolCalls: [],
                result.ModelId,
                result.ProviderId,
                result.PromptTokens,
                result.CompletionTokens,
                result.Duration,
                FinishReason: "stop");
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "StructuredOutputFallback: model did not return valid JSON — treating as plain text");
            return TextOnlyResult(result);
        }
    }

    private static FunctionCallingResult TextOnlyResult(ChatCompletionResult result)
        => new(
            true,
            result.Content,
            [],
            result.ModelId,
            result.ProviderId,
            result.PromptTokens,
            result.CompletionTokens,
            result.Duration,
            FinishReason: "stop");
}

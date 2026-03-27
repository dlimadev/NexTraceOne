using System.Diagnostics;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Executor centralizado de tools. Resolve a tool pelo nome,
/// executa-a e retorna o resultado com duração e rastreabilidade.
/// </summary>
public sealed class AgentToolExecutor : IToolExecutor
{
    private readonly IReadOnlyDictionary<string, IAgentTool> _tools;
    private readonly ILogger<AgentToolExecutor> _logger;

    public AgentToolExecutor(
        IEnumerable<IAgentTool> tools,
        ILogger<AgentToolExecutor> logger)
    {
        _tools = tools.ToDictionary(t => t.Definition.Name, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(
        ToolCallRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(request.ToolName, out var tool))
        {
            _logger.LogWarning("Tool '{ToolName}' not found in registry", request.ToolName);
            return new ToolExecutionResult(
                false, request.ToolName, string.Empty, 0,
                $"Tool '{request.ToolName}' is not registered.");
        }

        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation(
                "Executing tool '{ToolName}' with arguments: {Args}",
                request.ToolName, request.ArgumentsJson);

            var result = await tool.ExecuteAsync(request.ArgumentsJson, cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "Tool '{ToolName}' completed in {DurationMs}ms (Success={Success})",
                request.ToolName, sw.ElapsedMilliseconds, result.Success);

            return result with { DurationMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Tool '{ToolName}' execution failed", request.ToolName);
            return new ToolExecutionResult(
                false, request.ToolName, string.Empty,
                sw.ElapsedMilliseconds, $"Tool execution failed: {ex.Message}");
        }
    }
}

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Contrato para uma tool individual de agent.
/// Cada tool implementa esta interface e é registada via DI.
/// </summary>
public interface IAgentTool
{
    /// <summary>Definição da tool (nome, descrição, parâmetros).</summary>
    ToolDefinition Definition { get; }

    /// <summary>Executa a tool com os argumentos fornecidos em JSON.</summary>
    Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default);
}

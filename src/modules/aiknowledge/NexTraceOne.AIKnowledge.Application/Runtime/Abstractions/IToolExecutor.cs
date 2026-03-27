namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Executor de tools em runtime. Recebe um pedido de execução,
/// valida, executa a tool real e retorna o resultado.
/// </summary>
public interface IToolExecutor
{
    /// <summary>Executa uma tool pelo nome com os argumentos fornecidos.</summary>
    Task<ToolExecutionResult> ExecuteAsync(
        ToolCallRequest request,
        CancellationToken cancellationToken = default);
}

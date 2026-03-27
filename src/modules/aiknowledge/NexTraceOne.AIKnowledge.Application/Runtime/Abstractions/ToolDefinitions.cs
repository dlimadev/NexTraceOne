namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Definição de uma tool disponível para execução por agents de IA.
/// Cada tool tem nome único, descrição, parâmetros e categoria funcional.
/// </summary>
public sealed record ToolDefinition(
    /// <summary>Nome único da tool (ex: "list_services", "get_service_health").</summary>
    string Name,
    /// <summary>Descrição da funcionalidade da tool para o agente.</summary>
    string Description,
    /// <summary>Categoria funcional (ex: "service_catalog", "change_intelligence").</summary>
    string Category,
    /// <summary>Parâmetros aceites pela tool (JSON schema simplificado).</summary>
    IReadOnlyList<ToolParameterDefinition> Parameters);

/// <summary>Definição de um parâmetro de tool.</summary>
public sealed record ToolParameterDefinition(
    string Name,
    string Description,
    string Type,
    bool Required = false);

/// <summary>Pedido de execução de tool pelo agent runtime.</summary>
public sealed record ToolCallRequest(
    /// <summary>Nome da tool a executar.</summary>
    string ToolName,
    /// <summary>Argumentos passados pelo modelo (JSON).</summary>
    string ArgumentsJson);

/// <summary>Resultado da execução de uma tool.</summary>
public sealed record ToolExecutionResult(
    /// <summary>Indica se a execução foi bem sucedida.</summary>
    bool Success,
    /// <summary>Nome da tool executada.</summary>
    string ToolName,
    /// <summary>Resultado da execução (JSON ou texto).</summary>
    string Output,
    /// <summary>Duração em milissegundos.</summary>
    long DurationMs,
    /// <summary>Mensagem de erro, quando aplicável.</summary>
    string? ErrorMessage = null);

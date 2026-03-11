using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.Workflow.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo Workflow.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: {Módulo}.{Entidade}.{Descrição}
/// </summary>
public static class WorkflowErrors
{
    // TODO: Definir erros específicos do módulo
    // Exemplo: public static Error NotFound(string id) => Error.NotFound("Workflow.NotFound", $"...");
}

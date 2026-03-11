using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.RuntimeIntelligence.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo RuntimeIntelligence.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: {Módulo}.{Entidade}.{Descrição}
/// </summary>
public static class RuntimeIntelligenceErrors
{
    // TODO: Definir erros específicos do módulo
    // Exemplo: public static Error NotFound(string id) => Error.NotFound("RuntimeIntelligence.NotFound", $"...");
}

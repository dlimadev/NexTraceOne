using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.Identity.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo Identity.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: {Módulo}.{Entidade}.{Descrição}
/// </summary>
public static class IdentityErrors
{
    // TODO: Definir erros específicos do módulo
    // Exemplo: public static Error NotFound(string id) => Error.NotFound("Identity.NotFound", $"...");
}

using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.Audit.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo Audit.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: {Módulo}.{Entidade}.{Descrição}
/// </summary>
public static class AuditErrors
{
    // TODO: Definir erros específicos do módulo
    // Exemplo: public static Error NotFound(string id) => Error.NotFound("Audit.NotFound", $"...");
}

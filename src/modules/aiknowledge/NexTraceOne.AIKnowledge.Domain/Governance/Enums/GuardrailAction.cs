namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Acção automática executada quando um guardrail é activado.
/// </summary>
public enum GuardrailAction
{
    /// <summary>Bloqueia o request/response e devolve mensagem de erro ao utilizador.</summary>
    Block,

    /// <summary>Sanitiza o conteúdo removendo ou substituindo partes problemáticas.</summary>
    Sanitize,

    /// <summary>Alerta o utilizador mas permite continuar o fluxo.</summary>
    Warn,

    /// <summary>Regista o evento em auditoria sem interromper o fluxo.</summary>
    Log
}

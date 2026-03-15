namespace NexTraceOne.AiGovernance.Domain.Enums;

/// <summary>
/// Caminho de execução selecionado pela estratégia de roteamento de IA.
/// Determina se a resposta é servida internamente ou escalada para IA externa.
/// </summary>
public enum AIRoutingPath
{
    /// <summary>Execução interna — modelo local, sem saída de dados.</summary>
    InternalOnly,

    /// <summary>Execução interna preferencial com fallback externo permitido.</summary>
    InternalPreferred,

    /// <summary>Escalonamento controlado para IA externa — requer política e auditoria.</summary>
    ExternalEscalation,

    /// <summary>Execução bloqueada por política — nenhum modelo disponível.</summary>
    Blocked
}

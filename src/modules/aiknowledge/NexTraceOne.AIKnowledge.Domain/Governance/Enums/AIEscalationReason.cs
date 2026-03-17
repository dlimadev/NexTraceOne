namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Razão para escalonamento de execução para IA externa.
/// Registada na decisão de roteamento para auditoria e explicabilidade.
/// </summary>
public enum AIEscalationReason
{
    /// <summary>Sem escalonamento — execução permaneceu interna.</summary>
    None,

    /// <summary>Capacidade insuficiente do modelo interno para o caso de uso.</summary>
    InsufficientInternalCapability,

    /// <summary>Complexidade do caso de uso requer modelo mais avançado.</summary>
    ComplexityRequiresAdvancedModel,

    /// <summary>Solicitação explícita do utilizador com modelo preferencial externo.</summary>
    UserRequestedExternalModel,

    /// <summary>Fallback automático após tentativa interna sem sucesso.</summary>
    InternalFallback
}

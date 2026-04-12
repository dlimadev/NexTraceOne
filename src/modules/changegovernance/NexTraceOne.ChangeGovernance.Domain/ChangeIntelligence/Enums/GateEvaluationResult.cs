namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Resultado da avaliação de um gate de promoção contra uma mudança.
/// </summary>
public enum GateEvaluationResult
{
    /// <summary>Todos os critérios do gate foram cumpridos.</summary>
    Passed = 0,

    /// <summary>Pelo menos um critério obrigatório falhou.</summary>
    Failed = 1,

    /// <summary>Critérios não-bloqueantes falharam — atenção recomendada.</summary>
    Warning = 2
}

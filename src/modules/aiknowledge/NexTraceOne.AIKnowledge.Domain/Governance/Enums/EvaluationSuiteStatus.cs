namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>Estado de ciclo de vida de uma suite de avaliação de IA.</summary>
public enum EvaluationSuiteStatus
{
    /// <summary>Suite em construção — ainda não executável.</summary>
    Draft = 0,

    /// <summary>Suite activa e executável.</summary>
    Active = 1,

    /// <summary>Suite arquivada — histórico preservado, sem novas execuções.</summary>
    Archived = 2
}

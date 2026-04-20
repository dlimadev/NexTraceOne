namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>Estado de execução de uma avaliação de IA.</summary>
public enum EvaluationRunStatus
{
    /// <summary>Execução agendada, aguarda início.</summary>
    Pending = 0,

    /// <summary>Execução em progresso.</summary>
    Running = 1,

    /// <summary>Execução concluída com sucesso.</summary>
    Completed = 2,

    /// <summary>Execução terminada com falha.</summary>
    Failed = 3
}

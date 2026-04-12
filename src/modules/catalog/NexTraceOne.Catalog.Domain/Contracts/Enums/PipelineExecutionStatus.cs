namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Estado de execução de um pipeline de geração de código a partir de contrato.
/// </summary>
public enum PipelineExecutionStatus
{
    /// <summary>Execução pendente de início.</summary>
    Pending = 0,

    /// <summary>Execução em progresso.</summary>
    Running = 1,

    /// <summary>Todos os estágios concluídos com sucesso.</summary>
    Completed = 2,

    /// <summary>Execução falhou antes de concluir todos os estágios.</summary>
    Failed = 3,

    /// <summary>Alguns estágios concluíram mas pelo menos um falhou.</summary>
    PartiallyCompleted = 4
}

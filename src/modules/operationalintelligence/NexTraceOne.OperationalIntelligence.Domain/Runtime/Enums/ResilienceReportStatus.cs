namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

/// <summary>Estado do ciclo de vida de um ResilienceReport.</summary>
public enum ResilienceReportStatus
{
    /// <summary>Relatório gerado automaticamente após experimento.</summary>
    Generated = 0,

    /// <summary>Relatório revisado por utilizador responsável.</summary>
    Reviewed = 1,

    /// <summary>Relatório arquivado — não mais ativo.</summary>
    Archived = 2
}

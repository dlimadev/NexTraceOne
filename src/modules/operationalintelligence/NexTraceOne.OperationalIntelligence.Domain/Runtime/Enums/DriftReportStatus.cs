namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

/// <summary>
/// Estado do relatório de drift entre ambientes.
/// Ciclo de vida: Generated → (Reviewed | Stale).
/// </summary>
public enum DriftReportStatus
{
    /// <summary>Relatório gerado, aguardando revisão.</summary>
    Generated = 0,

    /// <summary>Relatório revisado pelo utilizador responsável.</summary>
    Reviewed = 1,

    /// <summary>Relatório substituído por análise mais recente.</summary>
    Stale = 2
}

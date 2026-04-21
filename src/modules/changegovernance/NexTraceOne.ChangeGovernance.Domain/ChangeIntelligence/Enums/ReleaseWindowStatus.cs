namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Estado de ciclo de vida de uma entrada no Release Calendar.
/// </summary>
public enum ReleaseWindowStatus
{
    /// <summary>Janela activa — aplica-se a mudanças no período configurado.</summary>
    Active = 0,

    /// <summary>Janela encerrada manualmente antes do fim previsto.</summary>
    Closed = 1,

    /// <summary>Janela cancelada antes de entrar em vigor.</summary>
    Cancelled = 2
}

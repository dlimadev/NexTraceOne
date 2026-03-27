namespace NexTraceOne.ProductAnalytics.Domain.Enums;

/// <summary>
/// Estado de uma jornada de produto.
/// Usado para medir completion rate e identificar pontos de abandono.
/// </summary>
public enum JourneyStatus
{
    /// <summary>Jornada iniciada pelo utilizador.</summary>
    Started = 0,

    /// <summary>Jornada em progresso — pelo menos um step concluído.</summary>
    InProgress = 1,

    /// <summary>Jornada concluída com sucesso.</summary>
    Completed = 2,

    /// <summary>Jornada abandonada antes da conclusão.</summary>
    Abandoned = 3
}

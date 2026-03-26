namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Estado de uma jornada de produto.
/// Usado para medir completion rate e identificar pontos de abandono.
/// COMPATIBILIDADE TRANSITÓRIA (P2.4): Enum pertence semanticamente ao módulo Product Analytics.
/// Permanece em Governance.Domain.Enums enquanto GetJourneys handler residir em Governance.Application.
/// Extração para ProductAnalytics.Domain.Enums prevista em fase futura.
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

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Tipo de janela de release no Release Calendar.
/// Define se a janela permite, restringe ou proíbe mudanças.
/// </summary>
public enum ReleaseWindowType
{
    /// <summary>Janela de deployment planeada — mudanças são permitidas e encorajadas.</summary>
    Scheduled = 0,

    /// <summary>Período de congelamento — nenhuma mudança em produção permitida.</summary>
    Freeze = 1,

    /// <summary>Janela reservada para hotfixes críticos aprovados explicitamente.</summary>
    HotfixAllowed = 2,

    /// <summary>Janela de manutenção planeada — sistema em estado controlado.</summary>
    Maintenance = 3
}

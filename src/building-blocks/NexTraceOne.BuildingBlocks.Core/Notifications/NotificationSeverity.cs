namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Severidade da notificação, usada para priorização, filtragem
/// e decisão de fallback entre canais.
/// </summary>
public enum NotificationSeverity
{
    /// <summary>Informação genérica sem urgência.</summary>
    Info = 0,

    /// <summary>Aviso que requer atenção eventual.</summary>
    Warning = 1,

    /// <summary>Alerta importante que exige ação em prazo definido.</summary>
    Alert = 2,

    /// <summary>Situação crítica que exige ação imediata.</summary>
    Critical = 3,

    /// <summary>Emergência operacional (break glass, falha de segurança).</summary>
    Emergency = 4
}

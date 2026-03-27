namespace NexTraceOne.Knowledge.Domain.Enums;

/// <summary>
/// Severidade ou importância de uma OperationalNote.
/// </summary>
public enum NoteSeverity
{
    /// <summary>Informação geral.</summary>
    Info,

    /// <summary>Aviso — requer atenção.</summary>
    Warning,

    /// <summary>Crítico — requer ação imediata.</summary>
    Critical
}

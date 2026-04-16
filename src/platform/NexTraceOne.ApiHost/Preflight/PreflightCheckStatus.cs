namespace NexTraceOne.ApiHost.Preflight;

/// <summary>
/// Estado individual de um preflight check.
/// Ok — check passou sem problemas.
/// Warning — check passou com ressalvas (não bloqueante).
/// Error — check falhou (bloqueante se for obrigatório).
/// </summary>
public enum PreflightCheckStatus
{
    Ok,
    Warning,
    Error,
}

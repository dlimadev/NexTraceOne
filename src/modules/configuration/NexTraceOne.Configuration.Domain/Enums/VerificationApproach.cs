namespace NexTraceOne.Configuration.Domain.Enums;

/// <summary>
/// Abordagem de verificação de contratos.
/// </summary>
public enum VerificationApproach
{
    /// <summary>Verificação passiva sem bloqueio.</summary>
    Passive = 0,

    /// <summary>Verificação ativa com notificação.</summary>
    Active = 1,

    /// <summary>Verificação estrita com bloqueio.</summary>
    Strict = 2
}

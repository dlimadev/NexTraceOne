namespace NexTraceOne.Configuration.Domain.Enums;

/// <summary>
/// Modo de verificação de contratos configurável por política.
/// </summary>
public enum VerificationMode
{
    /// <summary>Verificação desativada.</summary>
    Disabled = 0,

    /// <summary>Verificação baseada em ficheiro de especificação.</summary>
    SpecFile = 1,

    /// <summary>Extração automática da especificação.</summary>
    AutoExtract = 2,

    /// <summary>Modo híbrido combinando ficheiro e extração automática.</summary>
    Hybrid = 3
}

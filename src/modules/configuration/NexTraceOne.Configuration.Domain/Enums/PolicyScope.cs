namespace NexTraceOne.Configuration.Domain.Enums;

/// <summary>
/// Âmbito de aplicação de uma política de compliance.
/// </summary>
public enum PolicyScope
{
    /// <summary>Política aplicada ao nível da organização.</summary>
    Organization = 0,

    /// <summary>Política aplicada ao nível da equipa.</summary>
    Team = 1,

    /// <summary>Política aplicada ao nível do ambiente.</summary>
    Environment = 2,

    /// <summary>Política aplicada ao nível do serviço.</summary>
    Service = 3
}

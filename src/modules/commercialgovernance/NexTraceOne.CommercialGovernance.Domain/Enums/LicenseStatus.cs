namespace NexTraceOne.Licensing.Domain.Enums;

/// <summary>
/// Status operacional da licença que reflete seu estado atual no ciclo de vida.
/// Complementa IsActive com semântica mais rica para o frontend e auditoria.
/// </summary>
public enum LicenseStatus
{
    /// <summary>Licença ativa e operacional dentro da validade.</summary>
    Active = 0,

    /// <summary>Licença expirada mas dentro do grace period.</summary>
    GracePeriod = 1,

    /// <summary>Licença expirada sem grace period ou após grace period.</summary>
    Expired = 2,

    /// <summary>Licença suspensa administrativamente.</summary>
    Suspended = 3,

    /// <summary>Licença revogada permanentemente.</summary>
    Revoked = 4,

    /// <summary>Licença pendente de ativação inicial.</summary>
    PendingActivation = 5
}

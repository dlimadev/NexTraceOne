namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Estado de validade da licença do tenant.</summary>
public enum TenantLicenseStatus
{
    /// <summary>Licença ativa e válida.</summary>
    Active = 0,

    /// <summary>Período de trial ativo.</summary>
    Trial = 1,

    /// <summary>Licença suspensa por falta de pagamento ou violação.</summary>
    Suspended = 2,

    /// <summary>Licença expirada (past ValidUntil).</summary>
    Expired = 3,
}

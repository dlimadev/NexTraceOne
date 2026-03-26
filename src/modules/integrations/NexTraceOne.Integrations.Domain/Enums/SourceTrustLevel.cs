namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Nível de confiança atribuído a uma fonte de ingestão.
/// </summary>
public enum SourceTrustLevel
{
    /// <summary>Fonte não verificada, dados tratados com cautela.</summary>
    Unverified = 0,

    /// <summary>Fonte básica, com verificação mínima.</summary>
    Basic = 1,

    /// <summary>Fonte verificada, com histórico de dados consistentes.</summary>
    Verified = 2,

    /// <summary>Fonte confiável, com longo histórico de qualidade.</summary>
    Trusted = 3,

    /// <summary>Fonte oficial, considerada source of truth.</summary>
    Official = 4
}

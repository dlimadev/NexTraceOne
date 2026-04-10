namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Origem de uma entrada de changelog de contrato.
/// Indica como a entrada foi criada no sistema.
/// </summary>
public enum ChangelogSource
{
    /// <summary>Entrada criada manualmente pelo utilizador.</summary>
    Manual = 0,

    /// <summary>Entrada gerada automaticamente por verificação.</summary>
    Verification = 1,

    /// <summary>Entrada gerada durante promoção entre ambientes.</summary>
    Promotion = 2,

    /// <summary>Entrada gerada durante importação de contrato.</summary>
    Import = 3
}

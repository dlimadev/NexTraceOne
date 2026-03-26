namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Estado de freshness (atualidade) dos dados de uma fonte.
/// </summary>
public enum FreshnessStatus
{
    /// <summary>Freshness desconhecida (sem dados recebidos).</summary>
    Unknown = 0,

    /// <summary>Dados atualizados, dentro do intervalo esperado.</summary>
    Fresh = 1,

    /// <summary>Dados ligeiramente desatualizados.</summary>
    Stale = 2,

    /// <summary>Dados significativamente desatualizados.</summary>
    Outdated = 3,

    /// <summary>Dados muito antigos ou fonte inativa.</summary>
    Expired = 4
}

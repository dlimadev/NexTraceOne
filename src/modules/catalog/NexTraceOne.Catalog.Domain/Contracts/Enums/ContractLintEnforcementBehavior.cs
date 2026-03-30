using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Comportamento do linting face a violações detectadas num contrato.
/// Controla se as violações são apenas informativas ou bloqueantes no workflow de governança.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContractLintEnforcementBehavior
{
    /// <summary>Apenas informativo — mostra resultados sem bloquear.</summary>
    AdvisoryOnly = 0,

    /// <summary>Apenas avisos — destaca problemas sem bloquear acções.</summary>
    WarningOnly = 1,

    /// <summary>Bloqueia publicação quando há violações de erro.</summary>
    BlockingOnPublish = 2,

    /// <summary>Bloqueia submissão para revisão quando há violações de erro.</summary>
    BlockingOnReview = 3,

    /// <summary>Silencioso — regras executam mas resultados não são mostrados quando desabilitado.</summary>
    Silent = 4
}

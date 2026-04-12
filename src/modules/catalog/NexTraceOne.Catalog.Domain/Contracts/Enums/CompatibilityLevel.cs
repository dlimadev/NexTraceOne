using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Nível de compatibilidade entre duas versões de um contrato.
/// Classificação produzida pelo Schema Evolution Advisor para orientar
/// decisões de promoção, migração e comunicação com consumidores.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompatibilityLevel
{
    /// <summary>Totalmente compatível — nenhuma alteração de impacto nos consumidores.</summary>
    FullyCompatible = 0,

    /// <summary>Compatível para trás — consumidores existentes continuam a funcionar sem alteração.</summary>
    BackwardCompatible = 1,

    /// <summary>Compatível para a frente — produtores existentes continuam a funcionar sem alteração.</summary>
    ForwardCompatible = 2,

    /// <summary>Breaking change — requer intervenção dos consumidores para adaptação.</summary>
    BreakingChange = 3
}

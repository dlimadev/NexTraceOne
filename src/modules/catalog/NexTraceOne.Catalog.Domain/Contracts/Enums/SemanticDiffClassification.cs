using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Classificação do resultado de um diff semântico assistido por IA entre duas versões de contrato.
/// Utilizada para orientar decisões de promoção, comunicação com consumidores e mitigação de impacto.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SemanticDiffClassification
{
    /// <summary>Breaking change — alteração incompatível que requer intervenção dos consumidores.</summary>
    Breaking = 0,

    /// <summary>Non-breaking — alteração compatível que não afeta consumidores existentes.</summary>
    NonBreaking = 1,

    /// <summary>Enhancement — melhoria funcional sem impacto negativo nos consumidores.</summary>
    Enhancement = 2
}

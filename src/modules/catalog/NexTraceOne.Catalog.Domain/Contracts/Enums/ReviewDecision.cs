using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Decisão de revisão de um draft de contrato.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewDecision
{
    /// <summary>Aprovado pelo revisor.</summary>
    Approved = 0,

    /// <summary>Rejeitado pelo revisor, necessita ajustes.</summary>
    Rejected = 1
}

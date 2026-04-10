using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>Estado da publicação no marketplace interno de contratos.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MarketplaceListingStatus
{
    /// <summary>Rascunho — listagem ainda não visível no marketplace.</summary>
    Draft = 0,

    /// <summary>Publicada — listagem visível e disponível para consumo.</summary>
    Published = 1,

    /// <summary>Arquivada — listagem removida da vista principal mas preservada para histórico.</summary>
    Archived = 2
}

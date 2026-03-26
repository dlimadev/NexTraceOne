using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Portal.Enums;

/// <summary>
/// Estado de publicação de um contrato no Developer Portal / Publication Center.
/// Controla o fluxo: um contrato aprovado pode ser explicitamente publicado, retirado ou deprecado no portal.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContractPublicationStatus
{
    /// <summary>Rascunho de publicação — submetido para publicação mas ainda não visível no portal.</summary>
    PendingPublication = 0,

    /// <summary>Publicado — visível no Developer Portal para consumidores autorizados.</summary>
    Published = 1,

    /// <summary>Retirado — removido do portal sem deprecação formal; pode ser republicado.</summary>
    Withdrawn = 2,

    /// <summary>Deprecado — marcado como a caminho da remoção; ainda visível mas com aviso de descontinuação.</summary>
    Deprecated = 3
}

/// <summary>
/// Escopo de visibilidade de um contrato publicado no Developer Portal.
/// Controla quem pode ver o contrato publicado no catálogo.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PublicationVisibility
{
    /// <summary>Visível apenas para membros internos da organização.</summary>
    Internal = 0,

    /// <summary>Visível para consumidores externos autorizados.</summary>
    External = 1,

    /// <summary>Visível apenas para equipas explicitamente autorizadas.</summary>
    RestrictedToTeams = 2
}

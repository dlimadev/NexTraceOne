using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Estado de uma entidade Canonical no módulo de contratos.
/// Governa a progressão desde rascunho até aposentadoria,
/// garantindo que entidades publicadas sejam estáveis para reutilização.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalEntityState
{
    /// <summary>Rascunho — em definição, não disponível para reutilização.</summary>
    Draft = 0,

    /// <summary>Publicado — disponível para reutilização por contratos.</summary>
    Published = 1,

    /// <summary>Depreciado — ainda funcional mas com recomendação de migração.</summary>
    Deprecated = 2,

    /// <summary>Aposentado — removido de uso activo, apenas consulta histórica.</summary>
    Retired = 3
}

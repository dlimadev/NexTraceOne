namespace NexTraceOne.Catalog.API.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa um contrato publicado no catálogo.
/// Expõe protocolo, versão semântica, estado de ciclo de vida e ownership.
/// Persona: Engineer, Tech Lead, Architect.
/// </summary>
public sealed class ContractSummaryType
{
    /// <summary>Identificador da versão do contrato.</summary>
    public Guid VersionId { get; init; }

    /// <summary>Identificador do asset de API ao qual o contrato pertence.</summary>
    public Guid ApiAssetId { get; init; }

    /// <summary>Identificador do serviço dono do contrato.</summary>
    public Guid ServiceId { get; init; }

    /// <summary>Nome da API.</summary>
    public string ApiName { get; init; } = string.Empty;

    /// <summary>Padrão de rota da API (ex: /api/v1/users).</summary>
    public string ApiRoutePattern { get; init; } = string.Empty;

    /// <summary>Versão semântica (ex: 1.2.3).</summary>
    public string SemVer { get; init; } = string.Empty;

    /// <summary>Protocolo do contrato (REST, SOAP, Event, gRPC, etc.).</summary>
    public string Protocol { get; init; } = string.Empty;

    /// <summary>Estado do ciclo de vida (Draft, Active, Deprecated, Retired).</summary>
    public string LifecycleState { get; init; } = string.Empty;

    /// <summary>Indica se o contrato está bloqueado para edição.</summary>
    public bool IsLocked { get; init; }

    /// <summary>Data de criação do contrato.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}

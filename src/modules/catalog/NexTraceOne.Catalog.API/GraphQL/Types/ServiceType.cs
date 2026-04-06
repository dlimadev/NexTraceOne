namespace NexTraceOne.Catalog.API.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa um serviço no catálogo do NexTraceOne.
/// Expõe ownership, metadados de classificação e estado do ciclo de vida.
/// Persona: Engineer, Tech Lead, Architect, Executive.
/// </summary>
public sealed class ServiceType
{
    /// <summary>Identificador único do serviço.</summary>
    public Guid ServiceId { get; init; }

    /// <summary>Nome técnico do serviço.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Nome de exibição legível para humanos.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Descrição funcional do serviço.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Classificação técnica (API, Worker, Gateway, etc.).</summary>
    public string ServiceKind { get; init; } = string.Empty;

    /// <summary>Domínio de negócio ao qual o serviço pertence.</summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>Área de sistema técnico do serviço.</summary>
    public string SystemArea { get; init; } = string.Empty;

    /// <summary>Equipa responsável pelo serviço.</summary>
    public string TeamName { get; init; } = string.Empty;

    /// <summary>Technical owner (utilizador ou grupo).</summary>
    public string TechnicalOwner { get; init; } = string.Empty;

    /// <summary>Criticidade operacional (Low, Medium, High, Critical).</summary>
    public string Criticality { get; init; } = string.Empty;

    /// <summary>Estado do ciclo de vida (Active, Deprecated, Decommissioned).</summary>
    public string LifecycleStatus { get; init; } = string.Empty;

    /// <summary>Tipo de exposição (Internal, External, Partner).</summary>
    public string ExposureType { get; init; } = string.Empty;
}

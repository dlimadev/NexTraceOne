using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Entidade central do catálogo de serviços — representa um serviço com identidade,
/// ownership, classificação e contexto operacional.
/// É o ponto de entrada para governança de contratos, confiança em mudanças e
/// rastreabilidade operacional no NexTraceOne.
/// </summary>
public sealed class ServiceAsset : Entity<ServiceAssetId>
{
    private ServiceAsset() { }

    // ── Identidade ────────────────────────────────────────────────────

    /// <summary>Nome único do serviço (identificador técnico).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação do serviço (legível para humanos).</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição do serviço e a sua finalidade.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Tipo técnico do serviço (REST API, Kafka, Background, etc.).</summary>
    public ServiceType ServiceType { get; private set; } = ServiceType.RestApi;

    /// <summary>Domínio de negócio ao qual o serviço pertence.</summary>
    public string Domain { get; private set; } = string.Empty;

    /// <summary>Sistema ou área de plataforma a que o serviço pertence.</summary>
    public string SystemArea { get; private set; } = string.Empty;

    // ── Ownership ─────────────────────────────────────────────────────

    /// <summary>Equipa responsável pelo serviço.</summary>
    public string TeamName { get; private set; } = string.Empty;

    /// <summary>Owner técnico do serviço (pessoa ou conta).</summary>
    public string TechnicalOwner { get; private set; } = string.Empty;

    /// <summary>Owner de negócio do serviço (quando aplicável).</summary>
    public string BusinessOwner { get; private set; } = string.Empty;

    // ── Classificação ─────────────────────────────────────────────────

    /// <summary>Nível de criticidade do serviço para o negócio.</summary>
    public Criticality Criticality { get; private set; } = Criticality.Medium;

    /// <summary>Estado do ciclo de vida do serviço.</summary>
    public LifecycleStatus LifecycleStatus { get; private set; } = LifecycleStatus.Active;

    /// <summary>Tipo de exposição do serviço (interno, externo, partner).</summary>
    public ExposureType ExposureType { get; private set; } = ExposureType.Internal;

    // ── Governança ────────────────────────────────────────────────────

    /// <summary>URL ou referência para a documentação do serviço.</summary>
    public string DocumentationUrl { get; private set; } = string.Empty;

    /// <summary>URL ou referência para o repositório de código.</summary>
    public string RepositoryUrl { get; private set; } = string.Empty;

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Factory method ────────────────────────────────────────────────

    /// <summary>Cria um novo serviço no catálogo com os campos obrigatórios.</summary>
    public static ServiceAsset Create(string name, string domain, string teamName)
        => new()
        {
            Id = ServiceAssetId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            DisplayName = name,
            Domain = Guard.Against.NullOrWhiteSpace(domain),
            TeamName = Guard.Against.NullOrWhiteSpace(teamName)
        };

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza a identidade e classificação do serviço.</summary>
    public void UpdateDetails(
        string displayName,
        string description,
        ServiceType serviceType,
        string systemArea,
        Criticality criticality,
        LifecycleStatus lifecycleStatus,
        ExposureType exposureType,
        string documentationUrl,
        string repositoryUrl)
    {
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        ServiceType = serviceType;
        SystemArea = systemArea ?? string.Empty;
        Criticality = criticality;
        LifecycleStatus = lifecycleStatus;
        ExposureType = exposureType;
        DocumentationUrl = documentationUrl ?? string.Empty;
        RepositoryUrl = repositoryUrl ?? string.Empty;
    }

    /// <summary>Atualiza o ownership do serviço.</summary>
    public void UpdateOwnership(
        string teamName,
        string technicalOwner,
        string businessOwner)
    {
        TeamName = Guard.Against.NullOrWhiteSpace(teamName);
        TechnicalOwner = technicalOwner ?? string.Empty;
        BusinessOwner = businessOwner ?? string.Empty;
    }

    /// <summary>Atualiza o estado do ciclo de vida do serviço.</summary>
    public void UpdateLifecycleStatus(LifecycleStatus status)
    {
        LifecycleStatus = status;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────

    /// <summary>
    /// Transiciona o ciclo de vida do serviço, validando transições permitidas.
    /// Transições válidas: Planning→Development→Staging→Active→Deprecating→Deprecated→Retired.
    /// </summary>
    /// <param name="target">Estado de ciclo de vida pretendido.</param>
    /// <returns>Result indicando sucesso ou erro de transição inválida.</returns>
    public Result<LifecycleStatus> TransitionTo(LifecycleStatus target)
    {
        if (target == LifecycleStatus)
            return LifecycleStatus;

        var allowed = LifecycleStatus switch
        {
            LifecycleStatus.Planning => target == LifecycleStatus.Development,
            LifecycleStatus.PendingApproval => target is LifecycleStatus.Planning or LifecycleStatus.Active or LifecycleStatus.Development,
            LifecycleStatus.Development => target == LifecycleStatus.Staging,
            LifecycleStatus.Staging => target is LifecycleStatus.Active or LifecycleStatus.Development,
            LifecycleStatus.Active => target == LifecycleStatus.Deprecating,
            LifecycleStatus.Deprecating => target is LifecycleStatus.Deprecated or LifecycleStatus.Active,
            LifecycleStatus.Deprecated => target == LifecycleStatus.Retired,
            LifecycleStatus.Retired => false,
            _ => false
        };

        if (!allowed)
            return CatalogGraphErrors.InvalidLifecycleTransition(
                Name, LifecycleStatus.ToString(), target.ToString());

        LifecycleStatus = target;
        return LifecycleStatus;
    }
}

/// <summary>Identificador fortemente tipado de ServiceAsset.</summary>
public sealed record ServiceAssetId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ServiceAssetId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ServiceAssetId From(Guid id) => new(id);
}

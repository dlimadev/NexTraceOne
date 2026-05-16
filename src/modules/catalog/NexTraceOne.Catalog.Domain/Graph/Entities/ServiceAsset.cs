using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Attributes;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;
using NpgsqlTypes;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Entidade central do catálogo de serviços — representa um serviço com identidade,
/// ownership, classificação e contexto operacional.
/// É o ponto de entrada para governança de contratos, confiança em mudanças e
/// rastreabilidade operacional no NexTraceOne.
/// </summary>
public sealed class ServiceAsset : AuditableEntity<ServiceAssetId>
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

    /// <summary>Identificador do tenant ao qual o serviço pertence.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Owner técnico do serviço (pessoa ou conta).</summary>
    [EncryptedField]
    public string TechnicalOwner { get; private set; } = string.Empty;

    /// <summary>Owner de negócio do serviço (quando aplicável).</summary>
    [EncryptedField]
    public string BusinessOwner { get; private set; } = string.Empty;

    // ── Classificação ─────────────────────────────────────────────────

    /// <summary>Nível de criticidade do serviço para o negócio.</summary>
    public Criticality Criticality { get; private set; } = Criticality.Medium;

    /// <summary>Estado do ciclo de vida do serviço.</summary>
    public LifecycleStatus LifecycleStatus { get; private set; } = LifecycleStatus.Active;

    /// <summary>Tipo de exposição do serviço (interno, externo, partner).</summary>
    public ExposureType ExposureType { get; private set; } = ExposureType.Internal;

    /// <summary>Vetor de busca full-text (PostgreSQL tsvector) para pesquisa eficiente.</summary>
    public NpgsqlTsVector SearchVector { get; private set; } = null!;

    // ── Governança ────────────────────────────────────────────────────

    /// <summary>URL ou referência para a documentação do serviço.</summary>
    public string DocumentationUrl { get; private set; } = string.Empty;

    /// <summary>URL ou referência para o repositório de código.</summary>
    public string RepositoryUrl { get; private set; } = string.Empty;

    // ── Metadados estendidos ──────────────────────────────────────────

    /// <summary>Subdomínio dentro do Domain de negócio.</summary>
    public string? SubDomain { get; private set; }

    /// <summary>Capacidade de negócio que o serviço representa.</summary>
    public string? Capability { get; private set; }

    /// <summary>URL do repositório Git do serviço.</summary>
    public string GitRepository { get; private set; } = string.Empty;

    /// <summary>URL do pipeline de CI/CD do serviço.</summary>
    public string CiPipelineUrl { get; private set; } = string.Empty;

    /// <summary>Plataforma de infraestrutura onde o serviço corre (Kubernetes, IIS, ECS, VM, Mainframe).</summary>
    public string InfrastructureProvider { get; private set; } = string.Empty;

    /// <summary>Plataforma de hosting (Azure, AWS, GCP, On-Prem, Hybrid).</summary>
    public string HostingPlatform { get; private set; } = string.Empty;

    /// <summary>Linguagem principal do runtime (C#, Java, Python, COBOL).</summary>
    public string RuntimeLanguage { get; private set; } = string.Empty;

    /// <summary>Versão do runtime (ex: .NET 10, Java 21).</summary>
    public string RuntimeVersion { get; private set; } = string.Empty;

    /// <summary>Objetivo de SLO do serviço (ex: 99.9%).</summary>
    public string SloTarget { get; private set; } = string.Empty;

    /// <summary>Classificação de dados tratados pelo serviço (Public, Internal, Confidential, Restricted).</summary>
    public string DataClassification { get; private set; } = string.Empty;

    /// <summary>Âmbito regulatório aplicável (PCI-DSS, LGPD, GDPR, None).</summary>
    public string RegulatoryScope { get; private set; } = string.Empty;

    /// <summary>Frequência de mudança do serviço (High, Medium, Low, Stable).</summary>
    public string ChangeFrequency { get; private set; } = string.Empty;

    /// <summary>PM/PO responsável pelo serviço.</summary>
    public string ProductOwner { get; private set; } = string.Empty;

    /// <summary>Canal de contacto da equipa (Slack, lista de e-mail).</summary>
    [EncryptedField]
    public string ContactChannel { get; private set; } = string.Empty;

    /// <summary>Referência à rotação de on-call do serviço.</summary>
    public string OnCallRotationId { get; private set; } = string.Empty;

    /// <summary>Tier operacional do serviço — define requisitos mínimos de SLO, gates e maturidade.</summary>
    public ServiceTierType Tier { get; private set; } = ServiceTierType.Standard;

    /// <summary>
    /// Data/hora UTC em que o ownership foi revisto pela última vez.
    /// Utilizado para detecção de drift de ownership quando o valor supera
    /// o threshold configurável <c>catalog.ownershipDrift.threshold.days</c>.
    /// </summary>
    public DateTimeOffset? LastOwnershipReviewAt { get; private set; }

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Factory method ────────────────────────────────────────────────

    /// <summary>Cria um novo serviço no catálogo com os campos obrigatórios.</summary>
    public static ServiceAsset Create(string name, string domain, string teamName, Guid tenantId)
        => new()
        {
            Id = ServiceAssetId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            DisplayName = name,
            Domain = Guard.Against.NullOrWhiteSpace(domain),
            TeamName = Guard.Against.NullOrWhiteSpace(teamName),
            TenantId = tenantId
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

    /// <summary>Atualiza os metadados estendidos de infraestrutura, runtime e governança do serviço.</summary>
    public void UpdateExtendedMetadata(
        string? subDomain,
        string? capability,
        string? gitRepository,
        string? ciPipelineUrl,
        string? infrastructureProvider,
        string? hostingPlatform,
        string? runtimeLanguage,
        string? runtimeVersion,
        string? sloTarget,
        string? dataClassification,
        string? regulatoryScope,
        string? changeFrequency,
        string? productOwner,
        string? contactChannel,
        string? onCallRotationId)
    {
        SubDomain = subDomain;
        Capability = capability;
        GitRepository = gitRepository ?? string.Empty;
        CiPipelineUrl = ciPipelineUrl ?? string.Empty;
        InfrastructureProvider = infrastructureProvider ?? string.Empty;
        HostingPlatform = hostingPlatform ?? string.Empty;
        RuntimeLanguage = runtimeLanguage ?? string.Empty;
        RuntimeVersion = runtimeVersion ?? string.Empty;
        SloTarget = sloTarget ?? string.Empty;
        DataClassification = dataClassification ?? string.Empty;
        RegulatoryScope = regulatoryScope ?? string.Empty;
        ChangeFrequency = changeFrequency ?? string.Empty;
        ProductOwner = productOwner ?? string.Empty;
        ContactChannel = contactChannel ?? string.Empty;
        OnCallRotationId = onCallRotationId ?? string.Empty;
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

    /// <summary>Define o tier operacional do serviço.</summary>
    public void SetTier(ServiceTierType tier) => Tier = tier;

    /// <summary>
    /// Regista o momento em que o ownership foi revisto.
    /// Deve ser chamado quando um utilizador confirma/actualiza dados de ownership
    /// (equipa, owner técnico, owner de negócio, on-call, canal de contacto).
    /// </summary>
    public void RecordOwnershipReview(DateTimeOffset reviewedAt) =>
        LastOwnershipReviewAt = reviewedAt;

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

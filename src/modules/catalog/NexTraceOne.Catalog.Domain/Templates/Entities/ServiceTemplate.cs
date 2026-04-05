using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Templates.Enums;

namespace NexTraceOne.Catalog.Domain.Templates.Entities;

/// <summary>
/// Identificador fortemente tipado de ServiceTemplate.
/// </summary>
public sealed record ServiceTemplateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria um novo ServiceTemplateId.</summary>
    public static ServiceTemplateId New() => new(Guid.NewGuid());
}

/// <summary>
/// Entidade ServiceTemplate — representa um template governado para criação de novos serviços.
///
/// Um ServiceTemplate define:
///   - O padrão de contrato e stack tecnológica (lingua, tipo de serviço)
///   - A estrutura de pastas e ficheiros a gerar (scaffolding manifest)
///   - Políticas de governança aplicadas automaticamente ao serviço criado
///   - Contratos pré-definidos (OpenAPI spec, AsyncAPI, etc.)
///   - Ownership padrão (equipa e domínio)
///
/// Valor:
///   Developers criam serviços conformes com governança, contratos e ownership
///   desde o primeiro commit — eliminando a configuração manual e a deriva de padrões.
///
/// Pilar: Service Governance + Source of Truth (Phase 3.1 do roadmap).
/// </summary>
public sealed class ServiceTemplate : AuditableEntity<ServiceTemplateId>
{
    private ServiceTemplate() { }

    // ── Identidade ────────────────────────────────────────────────────

    /// <summary>Nome único do template (slug para uso em CLI/API).</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Nome legível do template (exibido na UI).</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição do propósito do template.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Versão do template (semver: 1.0.0).</summary>
    public string Version { get; private set; } = string.Empty;

    // ── Classificação ─────────────────────────────────────────────────

    /// <summary>Tipo de serviço gerado (RestApi, EventDriven, BackgroundWorker, etc.).</summary>
    public TemplateServiceType ServiceType { get; private set; } = TemplateServiceType.RestApi;

    /// <summary>Linguagem/stack tecnológica alvo.</summary>
    public TemplateLanguage Language { get; private set; } = TemplateLanguage.DotNet;

    /// <summary>Tags para pesquisa e filtragem do template.</summary>
    public IReadOnlyList<string> Tags { get; private set; } = Array.Empty<string>();

    // ── Governança ────────────────────────────────────────────────────

    /// <summary>Domínio de negócio padrão para serviços criados com este template.</summary>
    public string DefaultDomain { get; private set; } = string.Empty;

    /// <summary>Equipa padrão responsável pelos serviços criados com este template.</summary>
    public string DefaultTeam { get; private set; } = string.Empty;

    /// <summary>IDs das políticas de governança aplicadas automaticamente.</summary>
    public IReadOnlyList<Guid> GovernancePolicyIds { get; private set; } = Array.Empty<Guid>();

    /// <summary>Contrato base incluído no template (OpenAPI spec, AsyncAPI, WSDL, etc.).</summary>
    public string? BaseContractSpec { get; private set; }

    // ── Scaffolding ───────────────────────────────────────────────────

    /// <summary>Manifesto de ficheiros a gerar (lista de paths relativos e conteúdo template).</summary>
    public string? ScaffoldingManifestJson { get; private set; }

    /// <summary>URL do repositório Git onde o template base está armazenado.</summary>
    public string? RepositoryTemplateUrl { get; private set; }

    /// <summary>Branch do repositório template a usar.</summary>
    public string? RepositoryTemplateBranch { get; private set; }

    // ── Estado ────────────────────────────────────────────────────────

    /// <summary>Indica se o template está ativo e disponível para scaffolding.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Número de vezes que este template foi usado para scaffolding.</summary>
    public int UsageCount { get; private set; }

    /// <summary>Tenant ao qual este template pertence (null = template global/plataforma).</summary>
    public Guid? TenantId { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────

    /// <summary>
    /// Cria um novo ServiceTemplate com todos os campos obrigatórios.
    /// </summary>
    public static ServiceTemplate Create(
        string slug,
        string displayName,
        string description,
        string version,
        TemplateServiceType serviceType,
        TemplateLanguage language,
        string defaultDomain,
        string defaultTeam,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<Guid>? governancePolicyIds = null,
        string? baseContractSpec = null,
        string? scaffoldingManifestJson = null,
        string? repositoryTemplateUrl = null,
        string? repositoryTemplateBranch = null,
        Guid? tenantId = null)
    {
        Guard.Against.NullOrWhiteSpace(slug);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NullOrWhiteSpace(version);
        Guard.Against.NullOrWhiteSpace(defaultDomain);
        Guard.Against.NullOrWhiteSpace(defaultTeam);

        return new ServiceTemplate
        {
            Id = ServiceTemplateId.New(),
            Slug = slug.ToLowerInvariant().Trim(),
            DisplayName = displayName.Trim(),
            Description = description.Trim(),
            Version = version.Trim(),
            ServiceType = serviceType,
            Language = language,
            DefaultDomain = defaultDomain.Trim(),
            DefaultTeam = defaultTeam.Trim(),
            Tags = tags ?? Array.Empty<string>(),
            GovernancePolicyIds = governancePolicyIds ?? Array.Empty<Guid>(),
            BaseContractSpec = baseContractSpec,
            ScaffoldingManifestJson = scaffoldingManifestJson,
            RepositoryTemplateUrl = repositoryTemplateUrl,
            RepositoryTemplateBranch = repositoryTemplateBranch,
            TenantId = tenantId,
            IsActive = true,
            UsageCount = 0
        };
    }

    /// <summary>Desativa o template (não pode mais ser usado para scaffolding).</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reativa o template.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Incrementa o contador de uso ao fazer scaffolding.</summary>
    public void IncrementUsage() => UsageCount++;

    /// <summary>Actualiza o manifesto de scaffolding.</summary>
    public void UpdateScaffoldingManifest(string? manifestJson)
        => ScaffoldingManifestJson = manifestJson;

    /// <summary>Atualiza a URL do repositório template.</summary>
    public void UpdateRepositoryTemplate(string? url, string? branch)
    {
        RepositoryTemplateUrl = url;
        RepositoryTemplateBranch = branch;
    }

    /// <summary>
    /// Atualiza os metadados editáveis do template (display name, descrição, versão, tags,
    /// políticas de governança, contrato base, domínio e equipa padrão).
    /// O slug e o tipo de serviço/linguagem não são alteráveis após criação.
    /// </summary>
    public void Update(
        string displayName,
        string description,
        string version,
        string defaultDomain,
        string defaultTeam,
        IReadOnlyList<string>? tags,
        IReadOnlyList<Guid>? governancePolicyIds,
        string? baseContractSpec,
        string? scaffoldingManifestJson,
        string? repositoryTemplateUrl,
        string? repositoryTemplateBranch)
    {
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NullOrWhiteSpace(version);
        Guard.Against.NullOrWhiteSpace(defaultDomain);
        Guard.Against.NullOrWhiteSpace(defaultTeam);

        DisplayName = displayName.Trim();
        Description = description.Trim();
        Version = version.Trim();
        DefaultDomain = defaultDomain.Trim();
        DefaultTeam = defaultTeam.Trim();
        Tags = tags ?? Array.Empty<string>();
        GovernancePolicyIds = governancePolicyIds ?? Array.Empty<Guid>();
        BaseContractSpec = baseContractSpec;
        ScaffoldingManifestJson = scaffoldingManifestJson;
        RepositoryTemplateUrl = repositoryTemplateUrl;
        RepositoryTemplateBranch = repositoryTemplateBranch;
    }
}

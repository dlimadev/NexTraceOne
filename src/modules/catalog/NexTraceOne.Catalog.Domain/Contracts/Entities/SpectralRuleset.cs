using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa um ruleset Spectral cadastrado na plataforma.
/// Permite que organizações definam, versão e apliquem regras de linting
/// customizadas aos seus contratos, com controlo de escopo e enforcement.
/// </summary>
public sealed class SpectralRuleset : AuditableEntity<SpectralRulesetId>
{
    private SpectralRuleset() { }

    /// <summary>Nome legível do ruleset.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do propósito e cobertura do ruleset.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Versão do ruleset (semver-like ou incremental).</summary>
    public string Version { get; private set; } = "1.0.0";

    /// <summary>Conteúdo do ruleset (JSON/YAML Spectral).</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Origem do ruleset: plataforma, organização, equipa, importado.</summary>
    public SpectralRulesetOrigin Origin { get; private set; }

    /// <summary>Modo de execução padrão: realtime, on save, before publish, etc.</summary>
    public SpectralExecutionMode DefaultExecutionMode { get; private set; }

    /// <summary>Comportamento face a violações: advisory, blocking, etc.</summary>
    public SpectralEnforcementBehavior EnforcementBehavior { get; private set; }

    /// <summary>Organização/tenant owner do ruleset. Null para rulesets da plataforma.</summary>
    public string? OrganizationId { get; private set; }

    /// <summary>Owner do ruleset (pessoa ou equipa).</summary>
    public string? Owner { get; private set; }

    /// <summary>Domínio de aplicação (ex: "payments", "identity").</summary>
    public string? Domain { get; private set; }

    /// <summary>Tipo de serviço aplicável (ex: "RestApi", "EventApi"). Null = todos.</summary>
    public string? ApplicableServiceType { get; private set; }

    /// <summary>Protocolos aplicáveis (ex: "OpenApi,AsyncApi"). Null = todos.</summary>
    public string? ApplicableProtocols { get; private set; }

    /// <summary>Indica se o ruleset está ativo e deve ser aplicado.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Indica se este é o ruleset default da organização.</summary>
    public bool IsDefault { get; private set; }

    /// <summary>URL de origem, quando importado de fonte externa.</summary>
    public string? SourceUrl { get; private set; }

    /// <summary>Cria novo ruleset Spectral.</summary>
    public static SpectralRuleset Create(
        string name,
        string description,
        string content,
        SpectralRulesetOrigin origin,
        SpectralExecutionMode defaultExecutionMode,
        SpectralEnforcementBehavior enforcementBehavior,
        string? organizationId = null,
        string? owner = null,
        string? domain = null,
        string? applicableServiceType = null,
        string? applicableProtocols = null,
        string? sourceUrl = null,
        bool isDefault = false)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(content);

        return new SpectralRuleset
        {
            Id = SpectralRulesetId.New(),
            Name = name,
            Description = description ?? string.Empty,
            Version = "1.0.0",
            Content = content,
            Origin = origin,
            DefaultExecutionMode = defaultExecutionMode,
            EnforcementBehavior = enforcementBehavior,
            OrganizationId = organizationId,
            Owner = owner,
            Domain = domain,
            ApplicableServiceType = applicableServiceType,
            ApplicableProtocols = applicableProtocols,
            IsActive = true,
            IsDefault = isDefault,
            SourceUrl = sourceUrl
        };
    }

    /// <summary>Atualiza o conteúdo do ruleset e incrementa a versão.</summary>
    public void UpdateContent(string content, string newVersion)
    {
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NullOrWhiteSpace(newVersion);
        Content = content;
        Version = newVersion;
    }

    /// <summary>Atualiza a metadata do ruleset.</summary>
    public void UpdateMetadata(
        string name,
        string description,
        string? owner,
        string? domain,
        string? applicableServiceType,
        string? applicableProtocols)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Name = name;
        Description = description ?? string.Empty;
        Owner = owner;
        Domain = domain;
        ApplicableServiceType = applicableServiceType;
        ApplicableProtocols = applicableProtocols;
    }

    /// <summary>Atualiza a configuração de execução e enforcement.</summary>
    public void UpdateConfiguration(
        SpectralExecutionMode executionMode,
        SpectralEnforcementBehavior enforcementBehavior)
    {
        DefaultExecutionMode = executionMode;
        EnforcementBehavior = enforcementBehavior;
    }

    /// <summary>Ativa o ruleset para aplicação.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Desativa o ruleset.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Define como ruleset default da organização.</summary>
    public void SetAsDefault() => IsDefault = true;

    /// <summary>Remove a flag de default.</summary>
    public void UnsetDefault() => IsDefault = false;
}

/// <summary>Identificador fortemente tipado de SpectralRuleset.</summary>
public sealed record SpectralRulesetId(Guid Value) : TypedIdBase(Value)
{
    public static SpectralRulesetId New() => new(Guid.NewGuid());
    public static SpectralRulesetId From(Guid value) => new(value);
}

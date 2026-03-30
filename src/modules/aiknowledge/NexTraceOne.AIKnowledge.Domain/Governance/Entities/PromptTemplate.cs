using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Template de prompt versionado e categorizado para operações assistidas por IA.
/// Suporta variáveis de substituição (ex: {{serviceName}}, {{environment}}) e
/// versionamento para controlo de evolução e rollback.
///
/// Categorias incluem: system, user, agent, analysis, troubleshooting, governance.
/// Cada template pode ser associado a um agente específico ou ser genérico.
///
/// O template é auditável e imutável após publicação — alterações criam nova versão.
/// </summary>
public sealed class PromptTemplate : AuditableEntity<PromptTemplateId>
{
    private PromptTemplate() { }

    /// <summary>Nome técnico único do template (ex: "incident-root-cause-analysis").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de exibição na interface (ex: "Incident Root Cause Analysis").</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição funcional do template.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Categoria funcional (ex: "system", "user", "agent", "analysis").</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Conteúdo do template com placeholders (ex: "Analyze service {{serviceName}}").</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Lista de variáveis esperadas (CSV, ex: "serviceName,environment,timeRange").</summary>
    public string Variables { get; private set; } = string.Empty;

    /// <summary>Versão numérica do template (incrementada a cada revisão).</summary>
    public int Version { get; private set; }

    /// <summary>Indica se este template é a versão ativa/publicada.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Indica se é um template oficial da plataforma (não editável pelo tenant).</summary>
    public bool IsOfficial { get; private set; }

    /// <summary>Identificador do agente associado (null para templates genéricos).</summary>
    public Guid? AgentId { get; private set; }

    /// <summary>Personas alvo (CSV, ex: "Engineer,Tech Lead,Architect").</summary>
    public string TargetPersonas { get; private set; } = string.Empty;

    /// <summary>Dica de escopo contextual (ex: "serviceId", "incidentId").</summary>
    public string? ScopeHint { get; private set; }

    /// <summary>Nível de relevância (ex: "high", "medium", "low").</summary>
    public string Relevance { get; private set; } = string.Empty;

    /// <summary>Modelo preferido para este template (null para usar padrão).</summary>
    public Guid? PreferredModelId { get; private set; }

    /// <summary>Temperatura recomendada para inferência (null para usar padrão do modelo).</summary>
    public decimal? RecommendedTemperature { get; private set; }

    /// <summary>Número máximo de tokens de saída recomendado.</summary>
    public int? MaxOutputTokens { get; private set; }

    /// <summary>
    /// Cria um novo template de prompt com validação completa.
    /// </summary>
    public static PromptTemplate Create(
        string name,
        string displayName,
        string description,
        string category,
        string content,
        string variables,
        int version,
        bool isActive,
        bool isOfficial,
        Guid? agentId,
        string targetPersonas,
        string? scopeHint,
        string relevance,
        Guid? preferredModelId = null,
        decimal? recommendedTemperature = null,
        int? maxOutputTokens = null)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(category);
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NegativeOrZero(version);

        return new PromptTemplate
        {
            Id = PromptTemplateId.New(),
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Category = category.Trim(),
            Content = content,
            Variables = variables?.Trim() ?? string.Empty,
            Version = version,
            IsActive = isActive,
            IsOfficial = isOfficial,
            AgentId = agentId,
            TargetPersonas = targetPersonas?.Trim() ?? string.Empty,
            ScopeHint = scopeHint?.Trim(),
            Relevance = relevance?.Trim() ?? "medium",
            PreferredModelId = preferredModelId,
            RecommendedTemperature = recommendedTemperature,
            MaxOutputTokens = maxOutputTokens
        };
    }

    /// <summary>
    /// Desativa este template (marca como inativo).
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Ativa este template.
    /// </summary>
    public void Activate() => IsActive = true;
}

/// <summary>Identificador fortemente tipado de PromptTemplate.</summary>
public sealed record PromptTemplateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PromptTemplateId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PromptTemplateId From(Guid id) => new(id);
}

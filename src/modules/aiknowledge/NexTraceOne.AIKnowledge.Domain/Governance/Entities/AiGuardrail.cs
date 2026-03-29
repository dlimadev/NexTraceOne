using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Guardrail de IA para proteção de input/output.
/// Define regras de validação de conteúdo com padrões, severidade e ações automáticas.
///
/// Guardrails podem detetar PII, injeção de prompt, dados sensíveis, linguagem ofensiva,
/// e aplicar ações como bloquear, sanitizar, alertar ou registar em auditoria.
///
/// Cada guardrail pode ser aplicado a nível global, por tenant, por agente ou por modelo.
/// </summary>
public sealed class AiGuardrail : AuditableEntity<AiGuardrailId>
{
    private AiGuardrail() { }

    /// <summary>Nome técnico único do guardrail (ex: "pii-detection").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de exibição na interface.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição funcional do guardrail.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Categoria funcional (ex: "security", "privacy", "compliance", "quality").</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Tipo de guarda: "input", "output", ou "both".</summary>
    public string GuardType { get; private set; } = string.Empty;

    /// <summary>Padrão de deteção (regex ou expressão de classificação).</summary>
    public string Pattern { get; private set; } = string.Empty;

    /// <summary>Tipo de padrão: "regex", "keyword", "classifier", "semantic".</summary>
    public string PatternType { get; private set; } = string.Empty;

    /// <summary>Severidade: "critical", "high", "medium", "low", "info".</summary>
    public string Severity { get; private set; } = string.Empty;

    /// <summary>Ação quando o guardrail é ativado: "block", "sanitize", "warn", "log".</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Mensagem apresentada ao utilizador quando o guardrail é ativado.</summary>
    public string? UserMessage { get; private set; }

    /// <summary>Indica se o guardrail está ativo.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Indica se é um guardrail oficial da plataforma.</summary>
    public bool IsOfficial { get; private set; }

    /// <summary>Identificador do agente associado (null para guardrails globais).</summary>
    public Guid? AgentId { get; private set; }

    /// <summary>Identificador do modelo associado (null para todos os modelos).</summary>
    public Guid? ModelId { get; private set; }

    /// <summary>Prioridade de execução (menor = executa primeiro).</summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Cria um novo guardrail com validação completa.
    /// </summary>
    public static AiGuardrail Create(
        string name,
        string displayName,
        string description,
        string category,
        string guardType,
        string pattern,
        string patternType,
        string severity,
        string action,
        string? userMessage,
        bool isActive,
        bool isOfficial,
        Guid? agentId,
        Guid? modelId,
        int priority)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(category);
        Guard.Against.NullOrWhiteSpace(guardType);
        Guard.Against.NullOrWhiteSpace(pattern);
        Guard.Against.NullOrWhiteSpace(patternType);
        Guard.Against.NullOrWhiteSpace(severity);
        Guard.Against.NullOrWhiteSpace(action);
        Guard.Against.Negative(priority);

        return new AiGuardrail
        {
            Id = AiGuardrailId.New(),
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Category = category.Trim(),
            GuardType = guardType.Trim(),
            Pattern = pattern,
            PatternType = patternType.Trim(),
            Severity = severity.Trim(),
            Action = action.Trim(),
            UserMessage = userMessage?.Trim(),
            IsActive = isActive,
            IsOfficial = isOfficial,
            AgentId = agentId,
            ModelId = modelId,
            Priority = priority
        };
    }

    /// <summary>Desativa este guardrail.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Ativa este guardrail.</summary>
    public void Activate() => IsActive = true;
}

/// <summary>Identificador fortemente tipado de AiGuardrail.</summary>
public sealed record AiGuardrailId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiGuardrailId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiGuardrailId From(Guid id) => new(id);
}

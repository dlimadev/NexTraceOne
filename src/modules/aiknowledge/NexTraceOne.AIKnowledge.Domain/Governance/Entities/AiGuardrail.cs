using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
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

    /// <summary>Categoria funcional do guardrail.</summary>
    public GuardrailCategory Category { get; private set; }

    /// <summary>Tipo de guarda — fase do pipeline em que actua.</summary>
    public GuardrailType GuardType { get; private set; }

    /// <summary>Padrão de deteção (regex ou expressão de classificação).</summary>
    public string Pattern { get; private set; } = string.Empty;

    /// <summary>Tipo de padrão de deteção.</summary>
    public GuardrailPatternType PatternType { get; private set; }

    /// <summary>Severidade de activação.</summary>
    public GuardrailSeverity Severity { get; private set; }

    /// <summary>Acção automática quando o guardrail é activado.</summary>
    public GuardrailAction Action { get; private set; }

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
        GuardrailCategory category,
        GuardrailType guardType,
        string pattern,
        GuardrailPatternType patternType,
        GuardrailSeverity severity,
        GuardrailAction action,
        string? userMessage,
        bool isActive,
        bool isOfficial,
        Guid? agentId,
        Guid? modelId,
        int priority)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(displayName);
        Guard.Against.NullOrWhiteSpace(pattern);
        Guard.Against.Negative(priority);

        return new AiGuardrail
        {
            Id = AiGuardrailId.New(),
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Category = category,
            GuardType = guardType,
            Pattern = pattern,
            PatternType = patternType,
            Severity = severity,
            Action = action,
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

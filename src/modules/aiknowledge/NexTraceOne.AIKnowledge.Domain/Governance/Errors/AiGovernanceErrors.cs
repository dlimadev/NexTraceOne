using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo AiGovernance com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: AiGovernance.{Entidade}.{Descrição}
/// </summary>
public static class AiGovernanceErrors
{
    /// <summary>Modelo de IA não encontrado pelo identificador informado.</summary>
    public static Error ModelNotFound(string modelId)
        => Error.NotFound(
            "AiGovernance.Model.NotFound",
            "AI model '{0}' was not found.",
            modelId);

    /// <summary>Política de acesso de IA não encontrada pelo identificador informado.</summary>
    public static Error PolicyNotFound(string policyId)
        => Error.NotFound(
            "AiGovernance.Policy.NotFound",
            "AI access policy '{0}' was not found.",
            policyId);

    /// <summary>Budget de IA não encontrado pelo identificador informado.</summary>
    public static Error BudgetNotFound(string budgetId)
        => Error.NotFound(
            "AiGovernance.Budget.NotFound",
            "AI budget '{0}' was not found.",
            budgetId);

    /// <summary>Quota de tokens ou requisições excedida para o escopo informado.</summary>
    public static Error QuotaExceeded(string scope, string scopeValue)
        => Error.Business(
            "AiGovernance.Budget.QuotaExceeded",
            "Token quota exceeded for {0} '{1}'.",
            scope, scopeValue);

    /// <summary>Modelo de IA bloqueado por política de governança.</summary>
    public static Error ModelBlocked(string modelName)
        => Error.Forbidden(
            "AiGovernance.Model.Blocked",
            "AI model '{0}' is blocked by policy.",
            modelName);

    /// <summary>Acesso a IA externa não permitido para o utilizador informado.</summary>
    public static Error ExternalAINotAllowed(string userId)
        => Error.Forbidden(
            "AiGovernance.Policy.ExternalAINotAllowed",
            "External AI access is not allowed for user '{0}'.",
            userId);

    /// <summary>Modelo de IA está inativo e não pode ser utilizado.</summary>
    public static Error ModelInactive(string modelName)
        => Error.Business(
            "AiGovernance.Model.Inactive",
            "AI model '{0}' is not active.",
            modelName);

    /// <summary>Entrada de auditoria de uso de IA não encontrada.</summary>
    public static Error UsageEntryNotFound(string entryId)
        => Error.NotFound(
            "AiGovernance.UsageEntry.NotFound",
            "AI usage entry '{0}' was not found.",
            entryId);

    /// <summary>Fonte de conhecimento de IA não encontrada pelo identificador informado.</summary>
    public static Error KnowledgeSourceNotFound(string sourceId)
        => Error.NotFound(
            "AiGovernance.KnowledgeSource.NotFound",
            "AI knowledge source '{0}' was not found.",
            sourceId);

    /// <summary>Conversa do assistente de IA não encontrada pelo identificador informado.</summary>
    public static Error ConversationNotFound(string conversationId)
        => Error.NotFound(
            "AiGovernance.Conversation.NotFound",
            "AI assistant conversation '{0}' was not found.",
            conversationId);

    /// <summary>Conversa do assistente de IA não está ativa.</summary>
    public static Error ConversationNotActive(string conversationId)
        => Error.Business(
            "AiGovernance.Conversation.NotActive",
            "AI assistant conversation '{0}' is not active.",
            conversationId);

    /// <summary>Tipo de cliente IDE inválido ou não suportado.</summary>
    public static Error InvalidIdeClientType(string clientType)
        => Error.Validation(
            "AiGovernance.IDE.InvalidClientType",
            "IDE client type '{0}' is not valid. Expected 'VsCode' or 'VisualStudio'.",
            clientType);

    // ── AI Routing & Enrichment ────────────────────────────────────────────

    /// <summary>Estratégia de roteamento de IA não encontrada.</summary>
    public static Error RoutingStrategyNotFound(string strategyId)
        => Error.NotFound(
            "AiGovernance.RoutingStrategy.NotFound",
            "AI routing strategy '{0}' was not found.",
            strategyId);

    /// <summary>Decisão de roteamento de IA não encontrada.</summary>
    public static Error RoutingDecisionNotFound(string decisionId)
        => Error.NotFound(
            "AiGovernance.RoutingDecision.NotFound",
            "AI routing decision '{0}' was not found.",
            decisionId);

    /// <summary>Nenhuma estratégia de roteamento aplicável ao contexto.</summary>
    public static Error NoApplicableRoutingStrategy(string persona, string useCase)
        => Error.Business(
            "AiGovernance.Routing.NoApplicableStrategy",
            "No applicable routing strategy found for persona '{0}' and use case '{1}'.",
            persona, useCase);

    /// <summary>Execução bloqueada por política de roteamento.</summary>
    public static Error RoutingBlocked(string reason)
        => Error.Forbidden(
            "AiGovernance.Routing.Blocked",
            "AI execution blocked by routing policy: {0}.",
            reason);

    /// <summary>Escalonamento externo não permitido pela estratégia ativa.</summary>
    public static Error ExternalEscalationNotAllowed(string strategyName)
        => Error.Forbidden(
            "AiGovernance.Routing.ExternalEscalationNotAllowed",
            "External AI escalation is not allowed by strategy '{0}'.",
            strategyName);

    /// <summary>Plano de execução de IA não encontrado.</summary>
    public static Error ExecutionPlanNotFound(string planId)
        => Error.NotFound(
            "AiGovernance.ExecutionPlan.NotFound",
            "AI execution plan '{0}' was not found.",
            planId);
}

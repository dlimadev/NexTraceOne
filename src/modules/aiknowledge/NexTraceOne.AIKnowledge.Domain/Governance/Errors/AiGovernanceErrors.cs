using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Errors;

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

    /// <summary>O utilizador atual não tem acesso à conversa do assistente de IA solicitada.</summary>
    public static Error ConversationAccessDenied(string conversationId)
        => Error.Forbidden(
            "AiGovernance.Conversation.AccessDenied",
            "AI assistant conversation '{0}' is not accessible for the current user.",
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

    /// <summary>Acesso ao modelo de IA negado por política de autorização.</summary>
    public static Error ModelAccessDenied(string modelId, string reason)
        => Error.Forbidden(
            "AiGovernance.Model.AccessDenied",
            "Access to AI model '{0}' was denied: {1}.",
            modelId, reason);

    /// <summary>Agent de IA não encontrado pelo identificador informado.</summary>
    public static Error AgentNotFound(string agentId)
        => Error.NotFound(
            "AiGovernance.Agent.NotFound",
            "AI agent '{0}' was not found.",
            agentId);

    // ── Agent Runtime ──────────────────────────────────────────────────

    /// <summary>Utilizador não tem autorização para aceder ao agent.</summary>
    public static Error AgentAccessDenied(string agentId)
        => Error.Forbidden(
            "AiGovernance.Agent.AccessDenied",
            "Access to AI agent '{0}' is denied for the current user.",
            agentId);

    /// <summary>Agent não está activo para execução.</summary>
    public static Error AgentNotActive(string agentId)
        => Error.Business(
            "AiGovernance.Agent.NotActive",
            "AI agent '{0}' is not active.",
            agentId);

    /// <summary>Modelo não permitido para o agent.</summary>
    public static Error ModelNotAllowedForAgent(string modelId, string agentName)
        => Error.Forbidden(
            "AiGovernance.Agent.ModelNotAllowed",
            "AI model '{0}' is not allowed for agent '{1}'.",
            modelId, agentName);

    /// <summary>Execução de agent falhou.</summary>
    public static Error AgentExecutionFailed(string executionId, string reason)
        => Error.Business(
            "AiGovernance.AgentExecution.Failed",
            "Agent execution '{0}' failed: {1}.",
            executionId, reason);

    /// <summary>Execução de agent não encontrada.</summary>
    public static Error AgentExecutionNotFound(string executionId)
        => Error.NotFound(
            "AiGovernance.AgentExecution.NotFound",
            "Agent execution '{0}' was not found.",
            executionId);

    /// <summary>Artefacto de agent não encontrado.</summary>
    public static Error ArtifactNotFound(string artifactId)
        => Error.NotFound(
            "AiGovernance.Artifact.NotFound",
            "Agent artifact '{0}' was not found.",
            artifactId);
}

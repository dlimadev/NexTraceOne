using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo ExternalAi com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: ExternalAi.{Entidade}.{Descrição}
/// </summary>
public static class ExternalAiErrors
{
    /// <summary>Feature ainda não implementada para a fase atual do roadmap.</summary>
    public static Error NotImplemented(string reason)
        => Error.Business(
            "ExternalAi.Feature.NotImplemented",
            "{0}",
            reason);

    /// <summary>Consulta de IA não encontrada pelo identificador informado.</summary>
    public static Error ConsultationNotFound(string consultationId)
        => Error.NotFound(
            "ExternalAi.Consultation.NotFound",
            "External AI consultation '{0}' was not found.",
            consultationId);

    /// <summary>Consulta de IA já foi completada — não aceita nova resposta.</summary>
    public static Error ConsultationAlreadyCompleted(string consultationId)
        => Error.Conflict(
            "ExternalAi.Consultation.AlreadyCompleted",
            "External AI consultation '{0}' has already been completed.",
            consultationId);

    /// <summary>Consulta de IA já falhou — não aceita nova transição.</summary>
    public static Error ConsultationAlreadyFailed(string consultationId)
        => Error.Conflict(
            "ExternalAi.Consultation.AlreadyFailed",
            "External AI consultation '{0}' has already failed.",
            consultationId);

    /// <summary>Provedor de IA não encontrado pelo identificador informado.</summary>
    public static Error ProviderNotFound(string providerId)
        => Error.NotFound(
            "ExternalAi.Provider.NotFound",
            "External AI provider '{0}' was not found.",
            providerId);

    /// <summary>Provedor de IA está inativo e não pode processar consultas.</summary>
    public static Error ProviderInactive(string providerId)
        => Error.Business(
            "ExternalAi.Provider.Inactive",
            "External AI provider '{0}' is inactive and cannot process queries.",
            providerId);

    /// <summary>Política de governança de IA não encontrada pelo identificador informado.</summary>
    public static Error PolicyNotFound(string policyId)
        => Error.NotFound(
            "ExternalAi.Policy.NotFound",
            "External AI policy '{0}' was not found.",
            policyId);

    /// <summary>Captura de conhecimento não encontrada pelo identificador informado.</summary>
    public static Error KnowledgeCaptureNotFound(string captureId)
        => Error.NotFound(
            "ExternalAi.KnowledgeCapture.NotFound",
            "Knowledge capture '{0}' was not found.",
            captureId);

    /// <summary>Captura de conhecimento já foi revisada — não aceita nova revisão.</summary>
    public static Error KnowledgeAlreadyReviewed(string captureId)
        => Error.Conflict(
            "ExternalAi.KnowledgeCapture.AlreadyReviewed",
            "Knowledge capture '{0}' has already been reviewed.",
            captureId);

    /// <summary>Captura de conhecimento não está aprovada — reutilização requer aprovação prévia.</summary>
    public static Error KnowledgeNotApproved(string captureId)
        => Error.Business(
            "ExternalAi.KnowledgeCapture.NotApproved",
            "Knowledge capture '{0}' is not approved and cannot be reused.",
            captureId);

    /// <summary>Valor de confiança inválido — deve estar no intervalo [0, 1].</summary>
    public static Error InvalidConfidence(decimal confidence)
        => Error.Validation(
            "ExternalAi.Consultation.InvalidConfidence",
            "Confidence value ({0}) must be between 0 and 1.",
            confidence);

    /// <summary>Contexto não permitido pela política de governança de IA.</summary>
    public static Error ContextNotAllowed(string context, string policyName)
        => Error.Business(
            "ExternalAi.Policy.ContextNotAllowed",
            "Context '{0}' is not allowed by policy '{1}'.",
            context, policyName);
}

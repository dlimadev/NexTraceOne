namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Tipo de aprovação de release — define como o fluxo de aprovação é conduzido.
/// </summary>
public enum ExternalApprovalType
{
    /// <summary>Aprovação manual por utilizador interno da plataforma.</summary>
    Internal = 0,

    /// <summary>Aprovação via webhook outbound — NexTraceOne envia pedido e aguarda callback.</summary>
    ExternalWebhook = 1,

    /// <summary>Aprovação via integração com ServiceNow Change Management.</summary>
    ServiceNow = 2,

    /// <summary>Aprovação automática — não requer intervenção humana (confiança por política).</summary>
    AutoApprove = 3,
}

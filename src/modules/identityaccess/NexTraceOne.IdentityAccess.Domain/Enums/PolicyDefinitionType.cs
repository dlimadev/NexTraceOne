namespace NexTraceOne.IdentityAccess.Domain.Enums;

/// <summary>Tipo de política configurável no Policy Studio.</summary>
public enum PolicyDefinitionType
{
    /// <summary>Gate de promoção entre ambientes.</summary>
    PromotionGate = 0,

    /// <summary>Controlo de acesso contextual.</summary>
    AccessControl = 1,

    /// <summary>Verificação de conformidade.</summary>
    ComplianceCheck = 2,

    /// <summary>Janela de congelamento de mudanças.</summary>
    FreezeWindow = 3,

    /// <summary>Limiar de alerta operacional.</summary>
    AlertThreshold = 4
}

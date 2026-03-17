namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Resultado da avaliação de governança para uma tentativa de uso de IA.
/// Registrado na trilha de auditoria para rastreabilidade completa.
/// </summary>
public enum UsageResult
{
    /// <summary>Uso permitido — requisição processada com sucesso.</summary>
    Allowed,

    /// <summary>Uso bloqueado por política de acesso.</summary>
    Blocked,

    /// <summary>Quota de tokens ou requisições excedida.</summary>
    QuotaExceeded,

    /// <summary>Negado por política de governança.</summary>
    PolicyDenied,

    /// <summary>Modelo solicitado não está disponível.</summary>
    ModelUnavailable
}

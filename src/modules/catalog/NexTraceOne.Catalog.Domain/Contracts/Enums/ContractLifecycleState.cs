using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Estado do ciclo de vida de uma versão de contrato.
/// Controla a progressão desde a criação até a aposentadoria,
/// garantindo que apenas transições válidas sejam executadas
/// e que cada mudança de estado seja auditada.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContractLifecycleState
{
    /// <summary>Rascunho — versão em edição, não visível para consumers.</summary>
    Draft = 0,

    /// <summary>Em revisão — aguardando aprovação por reviewers.</summary>
    InReview = 1,

    /// <summary>Aprovado — versão validada e pronta para promoção.</summary>
    Approved = 2,

    /// <summary>Bloqueado — contrato selado para produção, imutável.</summary>
    Locked = 3,

    /// <summary>Depreciado — versão ainda funcional, mas com aviso de descontinuação.</summary>
    Deprecated = 4,

    /// <summary>Sunset — versão em período de encerramento, com data final definida.</summary>
    Sunset = 5,

    /// <summary>Aposentado — versão retirada de circulação, apenas consulta histórica.</summary>
    Retired = 6
}

namespace NexTraceOne.Licensing.Domain.Enums;

/// <summary>
/// Modelo comercial que define como o licenciamento é monetizado.
/// Separado do tipo de licença para permitir evolução independente
/// dos aspectos comerciais e técnicos.
/// </summary>
public enum CommercialModel
{
    /// <summary>Licença perpétua com pagamento único.</summary>
    Perpetual = 0,

    /// <summary>Assinatura recorrente (mensal/anual).</summary>
    Subscription = 1,

    /// <summary>Baseado em consumo/uso efetivo.</summary>
    UsageBased = 2,

    /// <summary>Período de avaliação sem custo.</summary>
    Trial = 3,

    /// <summary>Uso interno ou parceiro estratégico sem cobrança.</summary>
    Internal = 4
}

using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Modo de execução do linting de contratos no módulo de governança.
/// Define quando as regras são executadas durante o ciclo de vida do contrato.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContractLintExecutionMode
{
    /// <summary>Validação em tempo real durante edição.</summary>
    Realtime = 0,

    /// <summary>Validação executada ao gravar alterações.</summary>
    OnSave = 1,

    /// <summary>Validação executada manualmente sob demanda.</summary>
    OnDemand = 2,

    /// <summary>Validação obrigatória antes de submeter para revisão.</summary>
    BeforeReview = 3,

    /// <summary>Validação obrigatória antes de publicação/lock.</summary>
    BeforePublish = 4
}

using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Origem/fonte de um ruleset de linting de contratos cadastrado no sistema.
/// Permite rastrear a proveniência e prioridade de aplicação das regras de governança.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContractLintRulesetOrigin
{
    /// <summary>Ruleset padrão fornecido pela plataforma NexTraceOne.</summary>
    Platform = 0,

    /// <summary>Ruleset criado pela organização/empresa.</summary>
    Organization = 1,

    /// <summary>Ruleset criado por uma equipa específica.</summary>
    Team = 2,

    /// <summary>Ruleset importado de fonte externa (URL, Git).</summary>
    Imported = 3,

    /// <summary>Ruleset sincronizado de repositório externo.</summary>
    ExternalRepository = 4
}

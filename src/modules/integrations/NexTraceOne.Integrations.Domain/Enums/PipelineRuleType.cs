namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Tipo de acção que uma regra de pipeline aplica ao sinal de telemetria.
/// </summary>
public enum PipelineRuleType
{
    /// <summary>Redacta campos sensíveis por regex (ex: $.body.email → [REDACTED]).</summary>
    Masking = 1,

    /// <summary>Descarta records que satisfaçam a condição (ex: level == "debug" em produção).</summary>
    Filtering = 2,

    /// <summary>Injeta atributos adicionais no record (ex: serviceOwner, tier, contractCount).</summary>
    Enrichment = 3,

    /// <summary>Transforma valores de campos existentes (ex: renomear atributo, normalizar valor).</summary>
    Transform = 4
}

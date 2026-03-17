using System.Text.Json.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Severidade de um issue de validação detectado por Spectral, regras internas ou checks de canonical.
/// Alinha com a saída do Spectral e adiciona o nível Blocked para governança do NexTraceOne.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ValidationSeverity
{
    /// <summary>Informação — boa prática, sem acção obrigatória.</summary>
    Info = 0,

    /// <summary>Dica — sugestão menor de melhoria.</summary>
    Hint = 1,

    /// <summary>Aviso — potencial problema, não bloqueia publicação por defeito.</summary>
    Warning = 2,

    /// <summary>Erro — violação de regra, bloqueia publicação quando configurado.</summary>
    Error = 3,

    /// <summary>Bloqueado — violação crítica que impede qualquer progressão de lifecycle.</summary>
    Blocked = 4
}

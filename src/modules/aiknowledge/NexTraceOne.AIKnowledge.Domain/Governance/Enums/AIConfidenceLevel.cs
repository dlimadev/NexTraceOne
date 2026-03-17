namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Nível de confiança da resposta da IA, baseado na qualidade do contexto disponível.
/// Usado como sinal visual e de governança para o utilizador.
/// </summary>
public enum AIConfidenceLevel
{
    /// <summary>Resposta com grounding completo — contexto rico e fontes confiáveis.</summary>
    High,

    /// <summary>Resposta com contexto parcial — algumas fontes indisponíveis ou incompletas.</summary>
    Medium,

    /// <summary>Resposta com contexto limitado — pouca informação disponível para grounding.</summary>
    Low,

    /// <summary>Confiança não avaliada — metadados insuficientes para determinação.</summary>
    Unknown
}

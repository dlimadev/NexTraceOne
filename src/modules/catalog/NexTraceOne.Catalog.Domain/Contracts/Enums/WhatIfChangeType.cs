namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>Tipo de mudança proposta para análise de impacto what-if.</summary>
public enum WhatIfChangeType
{
    /// <summary>Mudança aditiva — adiciona campos opcionais, novos endpoints. Risco mínimo.</summary>
    Additive = 0,
    /// <summary>Mudança não-breaking — altera comportamento mas preserva contrato. Risco baixo.</summary>
    NonBreaking = 1,
    /// <summary>Mudança breaking — remove/altera campos obrigatórios, muda paths. Risco alto.</summary>
    Breaking = 2,
    /// <summary>Deprecação — contrato marcado para remoção futura. Risco médio-alto.</summary>
    Deprecation = 3,
}

/// <summary>Nível de impacto estimado para um consumidor na simulação what-if.</summary>
public enum WhatIfImpactLevel
{
    /// <summary>Sem impacto detectável.</summary>
    None = 0,
    /// <summary>Impacto baixo — consumidor pode ignorar com segurança.</summary>
    Low = 1,
    /// <summary>Impacto médio — requer verificação.</summary>
    Medium = 2,
    /// <summary>Impacto alto — requer plano de migração.</summary>
    High = 3,
    /// <summary>Impacto crítico — mudança quebra o consumidor.</summary>
    Critical = 4,
}

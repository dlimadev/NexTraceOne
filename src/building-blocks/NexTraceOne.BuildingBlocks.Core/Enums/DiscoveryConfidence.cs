namespace NexTraceOne.BuildingBlocks.Core.Enums;

/// <summary>
/// Modelo de confiança para dependências descobertas entre serviços.
/// Usado pelo módulo EngineeringGraph para classificar a qualidade da descoberta.
/// </summary>
public enum DiscoveryConfidence
{
    /// <summary>Inferido por análise de código ou heurística. Pode ser falso positivo.</summary>
    Inferred = 0,

    /// <summary>Detectado em logs de gateway com baixo volume de tráfego.</summary>
    Low = 1,

    /// <summary>Detectado via análise estática de contratos ou configuração.</summary>
    Medium = 2,

    /// <summary>Confirmado por traces OpenTelemetry com volume significativo.</summary>
    High = 3,

    /// <summary>Confirmado manualmente por um humano ou importado de catálogo oficial.</summary>
    Confirmed = 4
}

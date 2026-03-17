namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Enums;

/// <summary>
/// Direção da tendência de custo de um serviço ao longo de um período.
/// Utilizado pelo CostTrend para classificar se os custos estão subindo,
/// estáveis ou em declínio.
/// </summary>
public enum TrendDirection
{
    /// <summary>Tendência de custo em alta — variação percentual positiva significativa.</summary>
    Rising = 0,

    /// <summary>Custos estáveis — variação percentual dentro da margem de tolerância.</summary>
    Stable = 1,

    /// <summary>Tendência de custo em queda — variação percentual negativa significativa.</summary>
    Declining = 2
}

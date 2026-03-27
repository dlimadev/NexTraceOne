namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Janela de tempo usada no cálculo do burn rate do error budget.
/// Representa o período sobre o qual o consumo do budget é avaliado.
/// </summary>
public enum BurnRateWindow
{
    /// <summary>Janela de 1 hora — deteção rápida de consumo acelerado.</summary>
    OneHour = 0,

    /// <summary>Janela de 6 horas — avaliação de tendência de curto prazo.</summary>
    SixHours = 1,

    /// <summary>Janela de 24 horas — visão diária de consumo do budget.</summary>
    TwentyFourHours = 2,

    /// <summary>Janela de 7 dias — avaliação semanal de sustentabilidade do SLO.</summary>
    SevenDays = 3
}

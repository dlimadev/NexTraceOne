using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Services;

/// <summary>
/// Serviço de domínio responsável pelos cálculos de error budget e burn rate.
/// Centraliza as fórmulas de reliability para garantir que o cálculo seja
/// determinístico, auditável e independente de repositório.
/// </summary>
public interface IErrorBudgetCalculator
{
    /// <summary>
    /// Calcula o error budget total em minutos para um SLO numa janela de medição.
    /// Fórmula: total = (1 − targetPercent/100) × windowDays × 1440
    /// </summary>
    decimal ComputeTotalBudgetMinutes(SloDefinition slo);

    /// <summary>
    /// Calcula o error budget consumido em minutos dado um observed error rate
    /// aplicado sobre a janela de medição completa do SLO.
    /// Fórmula: consumed = observedErrorRate × windowDays × 1440
    /// </summary>
    decimal ComputeConsumedBudgetMinutes(SloDefinition slo, decimal observedErrorRate);

    /// <summary>
    /// Calcula a taxa de erro tolerada derivada do objetivo do SLO.
    /// Fórmula: toleratedErrorRate = (1 − targetPercent/100)
    /// </summary>
    decimal ComputeToleratedErrorRate(SloDefinition slo);

    /// <summary>
    /// Calcula o burn rate para uma janela de tempo específica.
    /// Fórmula: burnRate = observedErrorRate / toleratedErrorRate
    /// Um burn rate de 1.0 significa consumo sustentável (budget dura exactamente a janela).
    /// </summary>
    decimal ComputeBurnRate(SloDefinition slo, decimal observedErrorRate);

    /// <summary>
    /// Converte uma janela de BurnRateWindow em número de horas.
    /// </summary>
    int GetWindowHours(BurnRateWindow window);
}

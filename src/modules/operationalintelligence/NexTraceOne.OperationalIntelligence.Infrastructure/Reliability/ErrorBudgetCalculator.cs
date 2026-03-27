using NexTraceOne.OperationalIntelligence.Application.Reliability.Services;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability;

/// <summary>
/// Implementação do serviço de cálculo de error budget e burn rate.
///
/// Fórmulas adotadas (alinhadas com Google SRE e OpenSLO):
///
/// 1. Error budget total
///    total_minutes = (1 − target_percent/100) × window_days × 1440
///    Exemplo: target=99.9%, window=30d → (0.001) × 43200 = 43.2 min
///
/// 2. Error budget consumido
///    consumed_minutes = observed_error_rate × window_days × 1440
///    O observed_error_rate vem do RuntimeSnapshot mais recente do serviço.
///    Representa o ritmo médio de erros ao longo da janela completa.
///
/// 3. Burn rate
///    burn_rate = observed_error_rate / tolerated_error_rate
///    Limiares: ≥ 14.4 → Violated; ≥ 6 → AtRisk; &lt; 6 → Healthy
///
/// Nota: nesta fase (P6.2) o observed_error_rate é obtido do snapshot mais recente
/// do RuntimeSnapshot — representa uma proxy do estado actual do serviço.
/// Em fases futuras pode ser substituído por uma média ponderada da janela completa
/// via ClickHouse ou repositório analítico.
/// </summary>
internal sealed class ErrorBudgetCalculator : IErrorBudgetCalculator
{
    private const decimal MinutesPerDay = 1440m;

    /// <inheritdoc />
    public decimal ComputeTotalBudgetMinutes(SloDefinition slo)
    {
        var errorBudgetFraction = 1m - (slo.TargetPercent / 100m);
        return Math.Round(errorBudgetFraction * slo.WindowDays * MinutesPerDay, 4);
    }

    /// <inheritdoc />
    public decimal ComputeConsumedBudgetMinutes(SloDefinition slo, decimal observedErrorRate)
    {
        var clampedRate = Math.Clamp(observedErrorRate, 0m, 1m);
        return Math.Round(clampedRate * slo.WindowDays * MinutesPerDay, 4);
    }

    /// <inheritdoc />
    public decimal ComputeToleratedErrorRate(SloDefinition slo)
        => Math.Round(1m - (slo.TargetPercent / 100m), 8);

    /// <inheritdoc />
    public decimal ComputeBurnRate(SloDefinition slo, decimal observedErrorRate)
    {
        var toleratedRate = ComputeToleratedErrorRate(slo);
        if (toleratedRate <= 0m)
            return observedErrorRate > 0m ? 999m : 0m;

        return Math.Round(Math.Clamp(observedErrorRate, 0m, 1m) / toleratedRate, 4);
    }

    /// <inheritdoc />
    public int GetWindowHours(BurnRateWindow window) => window switch
    {
        BurnRateWindow.OneHour         => 1,
        BurnRateWindow.SixHours        => 6,
        BurnRateWindow.TwentyFourHours => 24,
        BurnRateWindow.SevenDays       => 168,
        _                              => 24
    };
}

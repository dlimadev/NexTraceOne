namespace NexTraceOne.Licensing.Contracts.DTOs;

/// <summary>
/// DTO público com o estado de uma quota de uso da licença.
/// Inclui nível de alerta e percentual de consumo para exibição em dashboards.
/// </summary>
public sealed record UsageQuotaDto(
    string MetricCode,
    long CurrentUsage,
    long Limit,
    bool ThresholdReached,
    decimal UsagePercentage,
    string WarningLevel,
    string EnforcementLevel);

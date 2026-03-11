namespace NexTraceOne.Licensing.Contracts.DTOs;

/// <summary>
/// DTO público com o estado de uma quota de uso da licença.
/// </summary>
public sealed record UsageQuotaDto(string MetricCode, long CurrentUsage, long Limit, bool ThresholdReached);

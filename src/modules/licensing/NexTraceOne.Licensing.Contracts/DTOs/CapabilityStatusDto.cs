namespace NexTraceOne.Licensing.Contracts.DTOs;

/// <summary>
/// DTO público com o estado de uma capability licenciada.
/// </summary>
public sealed record CapabilityStatusDto(string Code, string Name, bool IsEnabled);

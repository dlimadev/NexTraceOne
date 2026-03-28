namespace NexTraceOne.AIKnowledge.Contracts.ExternalAI.ServiceInterfaces;

/// <summary>
/// Cross-module summary for registered external AI providers.
/// </summary>
public sealed record ProviderSummaryDto(
    Guid Id,
    string Name,
    string ProviderType,
    string Status,
    IReadOnlyList<string> Capabilities,
    DateTimeOffset LastHealthCheck);

/// <summary>
/// Cross-module health payload for a specific external AI provider.
/// </summary>
public sealed record ProviderHealthDto(
    Guid ProviderId,
    string Status,
    int? Latency,
    DateTimeOffset LastChecked,
    string? ErrorMessage);

/// <summary>
/// Cross-module routing decision for an external AI request.
/// </summary>
public sealed record RoutingDecisionDto(
    Guid SelectedProviderId,
    string ProviderName,
    string Reason,
    Guid? FallbackProviderId);

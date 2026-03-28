using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para ComplianceGap.
/// </summary>
public sealed record ComplianceGapId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entidade persistida de gaps de compliance.
/// </summary>
public sealed class ComplianceGap : Entity<ComplianceGapId>
{
    public string ServiceId { get; private init; } = string.Empty;
    public string ServiceName { get; private init; } = string.Empty;
    public string Team { get; private init; } = string.Empty;
    public string Domain { get; private init; } = string.Empty;
    public string Description { get; private init; } = string.Empty;
    public PolicySeverity Severity { get; private init; }
    public IReadOnlyList<string> ViolatedPolicyIds { get; private init; } = [];
    public int ViolationCount { get; private init; }
    public DateTimeOffset DetectedAt { get; private init; }

    private ComplianceGap() { }

    public static ComplianceGap Create(
        string serviceId,
        string serviceName,
        string team,
        string domain,
        string description,
        PolicySeverity severity,
        IReadOnlyList<string> violatedPolicyIds,
        DateTimeOffset detectedAt)
    {
        Guard.Against.NullOrWhiteSpace(serviceId, nameof(serviceId));
        Guard.Against.StringTooLong(serviceId, 200, nameof(serviceId));
        Guard.Against.NullOrWhiteSpace(serviceName, nameof(serviceName));
        Guard.Against.StringTooLong(serviceName, 300, nameof(serviceName));
        Guard.Against.NullOrWhiteSpace(team, nameof(team));
        Guard.Against.StringTooLong(team, 200, nameof(team));
        Guard.Against.NullOrWhiteSpace(domain, nameof(domain));
        Guard.Against.StringTooLong(domain, 200, nameof(domain));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.StringTooLong(description, 2000, nameof(description));
        Guard.Against.EnumOutOfRange(severity, nameof(severity));
        Guard.Against.Null(violatedPolicyIds, nameof(violatedPolicyIds));

        var policyIds = violatedPolicyIds.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return new ComplianceGap
        {
            Id = new ComplianceGapId(Guid.NewGuid()),
            ServiceId = serviceId.Trim(),
            ServiceName = serviceName.Trim(),
            Team = team.Trim(),
            Domain = domain.Trim(),
            Description = description.Trim(),
            Severity = severity,
            ViolatedPolicyIds = policyIds,
            ViolationCount = policyIds.Count,
            DetectedAt = detectedAt
        };
    }
}

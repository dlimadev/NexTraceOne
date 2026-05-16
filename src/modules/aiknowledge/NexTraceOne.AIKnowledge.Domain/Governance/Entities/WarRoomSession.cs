using System.Text.Json;
using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Sala de Guerra virtual criada automaticamente para incidentes P0/P1.
/// Coordena engenheiros, timeline de eventos e acções de remediação em tempo real.
/// </summary>
public sealed class WarRoomSession : AuditableEntity<WarRoomSessionId>
{
    private WarRoomSession() { }

    public string IncidentId { get; private set; } = string.Empty;
    public string IncidentTitle { get; private set; } = string.Empty;
    public string Severity { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Open";
    public string ServiceAffected { get; private set; } = string.Empty;
    public string CreatedByAgentId { get; private set; } = string.Empty;
    public string ParticipantsJson { get; private set; } = "[]";
    public string TimelineJson { get; private set; } = "[]";
    public string SuggestedActionsJson { get; private set; } = "[]";
    public string PostMortemDraft { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string SkillUsed { get; private set; } = string.Empty;
    public uint RowVersion { get; private set; }

    public IReadOnlyList<string> ParticipantUserIds =>
        JsonSerializer.Deserialize<List<string>>(ParticipantsJson) ?? [];

    public static WarRoomSession Create(
        string incidentId,
        string incidentTitle,
        string severity,
        string serviceAffected,
        string createdByAgentId,
        string skillUsed,
        Guid tenantId,
        DateTimeOffset openedAt)
    {
        Guard.Against.NullOrWhiteSpace(incidentId);
        Guard.Against.NullOrWhiteSpace(incidentTitle);
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));

        return new WarRoomSession
        {
            Id = WarRoomSessionId.New(),
            IncidentId = incidentId,
            IncidentTitle = incidentTitle,
            Severity = severity,
            ServiceAffected = serviceAffected ?? string.Empty,
            CreatedByAgentId = createdByAgentId ?? string.Empty,
            SkillUsed = skillUsed ?? string.Empty,
            TenantId = tenantId,
            OpenedAt = openedAt,
            Status = "Open",
        };
    }

    public void AddParticipant(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        var list = JsonSerializer.Deserialize<List<string>>(ParticipantsJson) ?? [];
        if (!list.Contains(userId))
            list.Add(userId);
        ParticipantsJson = JsonSerializer.Serialize(list);
    }

    public void AppendTimelineEvent(string eventJson)
    {
        Guard.Against.NullOrWhiteSpace(eventJson);
        var list = JsonSerializer.Deserialize<List<string>>(TimelineJson) ?? [];
        list.Add(eventJson);
        TimelineJson = JsonSerializer.Serialize(list);
    }

    public void UpdateSuggestedActions(string actionsJson)
    {
        SuggestedActionsJson = actionsJson ?? "[]";
    }

    public void Resolve(string postMortemDraft, DateTimeOffset resolvedAt)
    {
        Status = "Resolved";
        PostMortemDraft = postMortemDraft ?? string.Empty;
        ResolvedAt = resolvedAt;
    }

    public void Close()
    {
        Status = "Closed";
    }
}

/// <summary>Identificador fortemente tipado de WarRoomSession.</summary>
public sealed record WarRoomSessionId(Guid Value) : TypedIdBase(Value)
{
    public static WarRoomSessionId New() => new(Guid.NewGuid());
    public static WarRoomSessionId From(Guid id) => new(id);
}

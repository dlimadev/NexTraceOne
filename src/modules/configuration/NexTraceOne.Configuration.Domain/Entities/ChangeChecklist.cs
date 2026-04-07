using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Checklist personalizada para mudanças.
/// Admins definem checklists por tipo de mudança, criticidade e ambiente.
/// Quando IsRequired=true, a mudança não pode avançar sem itens completos.
/// </summary>
public sealed class ChangeChecklist : AuditableEntity<ChangeChecklistId>
{
    private const int MaxNameLength = 100;

    private ChangeChecklist() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ChangeType { get; private set; } = string.Empty;
    public string? Environment { get; private set; }
    public bool IsRequired { get; private set; } = true;
    public IReadOnlyList<string> Items { get; private set; } = [];

    public static ChangeChecklist Create(
        string tenantId,
        string name,
        string changeType,
        string? environment,
        bool isRequired,
        IEnumerable<string> items,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxNameLength);
        Guard.Against.NullOrWhiteSpace(changeType);

        var itemList = items?.Where(i => !string.IsNullOrWhiteSpace(i)).ToList() ?? [];

        var checklist = new ChangeChecklist
        {
            Id = new ChangeChecklistId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name,
            ChangeType = changeType,
            Environment = environment,
            IsRequired = isRequired,
            Items = itemList.AsReadOnly(),
        };
        checklist.SetCreated(createdAt, string.Empty);
        checklist.SetUpdated(createdAt, string.Empty);
        return checklist;
    }

    public void UpdateItems(IEnumerable<string> items, bool isRequired, DateTimeOffset updatedAt)
    {
        Items = (items?.Where(i => !string.IsNullOrWhiteSpace(i)).ToList() ?? []).AsReadOnly();
        IsRequired = isRequired;
        SetUpdated(updatedAt, string.Empty);
    }
}

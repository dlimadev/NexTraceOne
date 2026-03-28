using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade EvidencePackage.
/// </summary>
public sealed record EvidencePackageId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Pacote de evidências de governança para auditoria e compliance.
/// </summary>
public sealed class EvidencePackage : Entity<EvidencePackageId>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;
    public EvidencePackageStatus Status { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? SealedAt { get; private set; }
    public uint RowVersion { get; set; }

    private readonly List<EvidenceItem> _items = [];
    public IReadOnlyList<EvidenceItem> Items => _items;

    private EvidencePackage() { }

    public static EvidencePackage Create(
        string name,
        string description,
        string scope,
        string createdBy)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 300, nameof(name));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.StringTooLong(description, 2000, nameof(description));
        Guard.Against.NullOrWhiteSpace(scope, nameof(scope));
        Guard.Against.StringTooLong(scope, 200, nameof(scope));
        Guard.Against.NullOrWhiteSpace(createdBy, nameof(createdBy));
        Guard.Against.StringTooLong(createdBy, 200, nameof(createdBy));

        return new EvidencePackage
        {
            Id = new EvidencePackageId(Guid.NewGuid()),
            Name = name.Trim(),
            Description = description.Trim(),
            Scope = scope.Trim(),
            Status = EvidencePackageStatus.Draft,
            CreatedBy = createdBy.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            SealedAt = null
        };
    }

    public void UpdateMetadata(string name, string description, string scope)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 300, nameof(name));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.StringTooLong(description, 2000, nameof(description));
        Guard.Against.NullOrWhiteSpace(scope, nameof(scope));
        Guard.Against.StringTooLong(scope, 200, nameof(scope));

        Name = name.Trim();
        Description = description.Trim();
        Scope = scope.Trim();
    }

    public void AddItem(EvidenceItem item)
    {
        Guard.Against.Null(item, nameof(item));
        _items.Add(item);
    }

    public void Seal()
    {
        Status = EvidencePackageStatus.Sealed;
        SealedAt = DateTimeOffset.UtcNow;
    }

    public void MarkExported()
    {
        Status = EvidencePackageStatus.Exported;
    }
}

/// <summary>
/// Identificador fortemente tipado para EvidenceItem.
/// </summary>
public sealed record EvidenceItemId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Item de evidência associado a um pacote.
/// </summary>
public sealed class EvidenceItem : Entity<EvidenceItemId>
{
    public EvidencePackageId PackageId { get; private init; } = default!;
    public EvidenceType Type { get; private init; }
    public string Title { get; private init; } = string.Empty;
    public string Description { get; private init; } = string.Empty;
    public string SourceModule { get; private init; } = string.Empty;
    public string ReferenceId { get; private init; } = string.Empty;
    public string RecordedBy { get; private init; } = string.Empty;
    public DateTimeOffset RecordedAt { get; private init; }

    private EvidenceItem() { }

    public static EvidenceItem Create(
        EvidencePackageId packageId,
        EvidenceType type,
        string title,
        string description,
        string sourceModule,
        string referenceId,
        string recordedBy,
        DateTimeOffset recordedAt)
    {
        Guard.Against.Null(packageId, nameof(packageId));
        Guard.Against.EnumOutOfRange(type, nameof(type));
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.StringTooLong(title, 300, nameof(title));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.StringTooLong(description, 2000, nameof(description));
        Guard.Against.NullOrWhiteSpace(sourceModule, nameof(sourceModule));
        Guard.Against.StringTooLong(sourceModule, 100, nameof(sourceModule));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));
        Guard.Against.StringTooLong(referenceId, 200, nameof(referenceId));
        Guard.Against.NullOrWhiteSpace(recordedBy, nameof(recordedBy));
        Guard.Against.StringTooLong(recordedBy, 200, nameof(recordedBy));

        return new EvidenceItem
        {
            Id = new EvidenceItemId(Guid.NewGuid()),
            PackageId = packageId,
            Type = type,
            Title = title.Trim(),
            Description = description.Trim(),
            SourceModule = sourceModule.Trim(),
            ReferenceId = referenceId.Trim(),
            RecordedBy = recordedBy.Trim(),
            RecordedAt = recordedAt
        };
    }
}

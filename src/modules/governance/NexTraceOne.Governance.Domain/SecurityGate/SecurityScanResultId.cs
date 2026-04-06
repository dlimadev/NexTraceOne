using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.SecurityGate;

/// <summary>Identificador fortemente tipado de SecurityScanResult.</summary>
public sealed record SecurityScanResultId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria um novo identificador único.</summary>
    public static SecurityScanResultId New() => new(Guid.NewGuid());
}

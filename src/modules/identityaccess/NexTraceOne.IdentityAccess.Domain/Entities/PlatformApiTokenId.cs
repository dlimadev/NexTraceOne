using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Identificador fortemente tipado de PlatformApiToken.</summary>
public sealed record PlatformApiTokenId(Guid Value) : TypedIdBase(Value)
{
    public static PlatformApiTokenId New() => new(Guid.NewGuid());
    public static PlatformApiTokenId From(Guid value) => new(value);
}

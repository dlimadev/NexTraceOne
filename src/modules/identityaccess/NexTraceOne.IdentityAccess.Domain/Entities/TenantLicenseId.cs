using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Strongly-typed ID para TenantLicense.</summary>
public sealed record TenantLicenseId(Guid Value) : TypedIdBase(Value)
{
    public static TenantLicenseId New() => new(Guid.NewGuid());
    public static TenantLicenseId From(Guid value) => new(value);
}

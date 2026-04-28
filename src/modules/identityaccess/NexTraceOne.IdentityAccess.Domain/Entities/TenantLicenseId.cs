using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Strongly-typed ID para TenantLicense.</summary>
public sealed record TenantLicenseId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static TenantLicenseId New() => new(Guid.NewGuid());
    public static TenantLicenseId From(Guid value) => new(value);
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("iam_tenant_memberships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => TenantMembershipId.From(value));
        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();
        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();
        builder.Property(x => x.RoleId)
            .HasConversion(id => id.Value, value => RoleId.From(value))
            .IsRequired();
        builder.Property(x => x.JoinedAt).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.UserId, x.TenantId }).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}

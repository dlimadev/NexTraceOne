using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("identity_permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PermissionId.From(value));
        builder.Property(x => x.Code).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C001")), "identity:users:read", "Read users", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C002")), "identity:users:write", "Write users", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C003")), "identity:roles:assign", "Assign roles", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C004")), "identity:sessions:revoke", "Revoke sessions", "Identity"),
            Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C005")), "platform:audit:read", "Read audit logs", "Platform"));
    }
}

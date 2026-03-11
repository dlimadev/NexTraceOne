using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("identity_users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => UserId.From(value));

        builder.Property(x => x.Email)
            .HasConversion(email => email.Value, value => Email.FromDatabase(value))
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.OwnsOne(x => x.FullName, ownedBuilder =>
        {
            ownedBuilder.Property(x => x.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();

            ownedBuilder.Property(x => x.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Property(x => x.PasswordHash)
            .HasConversion(
                hash => hash == null ? null : hash.Value,
                value => string.IsNullOrWhiteSpace(value) ? null : HashedPassword.FromHash(value))
            .HasMaxLength(200);

        builder.Property(x => x.FederationProvider).HasMaxLength(100);
        builder.Property(x => x.ExternalId).HasMaxLength(200);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.FailedLoginAttempts).IsRequired();
        builder.Property(x => x.LastLoginAt);
        builder.Property(x => x.LockoutEnd);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("identity_roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RoleId.From(value));
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsSystem).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C001")), Role.Admin, "Administrative access"),
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C002")), Role.Manager, "Managerial access"),
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C003")), Role.Developer, "Developer access"),
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C004")), Role.Viewer, "Read-only access"),
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C005")), Role.Auditor, "Audit access"));
    }
}

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

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("identity_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SessionId.From(value));
        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();
        builder.Property(x => x.RefreshToken)
            .HasConversion(hash => hash.Value, value => RefreshTokenHash.FromHash(value))
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedByIp).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UserAgent).HasMaxLength(512).IsRequired();
        builder.Property(x => x.RevokedAt);
        builder.HasIndex(x => x.RefreshToken).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("identity_tenant_memberships");
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
        builder.HasIndex(x => new { x.UserId, x.TenantId }).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}

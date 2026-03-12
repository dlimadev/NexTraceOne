using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Configurations;

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

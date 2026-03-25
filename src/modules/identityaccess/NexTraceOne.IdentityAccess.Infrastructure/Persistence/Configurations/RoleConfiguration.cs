using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade Role.
/// Seed data inclui os 7 papéis de sistema do MVP1: PlatformAdmin, TechLead,
/// Developer, Viewer, Auditor, SecurityReview, ApprovalOnly.
/// </summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("iam_roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RoleId.From(value));
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsSystem).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C001")), Role.PlatformAdmin, "Full platform administration access"),
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C002")), Role.TechLead, "Technical leadership with approval and governance"),
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C003")), Role.Developer, "Development access with contract management"),
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C004")), Role.Viewer, "Read-only access across modules"),
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C005")), Role.Auditor, "Audit and compliance access"),
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C006")), Role.SecurityReview, "Security review and session management"),
            Role.CreateSystem(RoleId.From(new Guid("1E91A557-FADE-46DF-B248-0F5F5899C007")), Role.ApprovalOnly, "Restricted to workflow approvals only"));
    }
}

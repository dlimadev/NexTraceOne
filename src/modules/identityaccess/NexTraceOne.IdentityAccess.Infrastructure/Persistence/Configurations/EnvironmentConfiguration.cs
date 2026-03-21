using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.IdentityAccess.Domain.Entities;
using Environment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade Environment.
/// Define tabela, conversões de IDs fortemente tipados, constraints e índices.
/// O par (TenantId, Slug) possui índice único para garantir que cada tenant
/// não tenha ambientes com slugs duplicados.
/// </summary>
internal sealed class EnvironmentConfiguration : IEntityTypeConfiguration<Environment>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Environment> builder)
    {
        builder.ToTable("identity_environments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(e => e.Value, v => EnvironmentId.From(v));

        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Slug })
            .IsUnique()
            .HasDatabaseName("IX_identity_environments_tenant_slug");

        // Phase 1 fields — deferred to migration AddEnvironmentProfileFields (Phase 2).
        // Explicitly ignored to prevent EF Core from attempting convention-based mapping.
        builder.Ignore(x => x.Profile);
        builder.Ignore(x => x.Code);
        builder.Ignore(x => x.Description);
        builder.Ignore(x => x.Criticality);
        builder.Ignore(x => x.Region);
        builder.Ignore(x => x.IsProductionLike);
    }
}

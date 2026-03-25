using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using Environment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade Environment.
/// Define tabela, conversões de IDs fortemente tipados, constraints e índices.
/// O par (TenantId, Slug) possui índice único para garantir que cada tenant
/// não tenha ambientes com slugs duplicados.
/// Prefixo env_ — prepara o módulo para a futura baseline de Environment Management.
/// </summary>
internal sealed class EnvironmentConfiguration : IEntityTypeConfiguration<Environment>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Environment> builder)
    {
        builder.ToTable("env_environments", t =>
        {
            t.HasCheckConstraint(
                "CK_env_environments_profile",
                "\"Profile\" BETWEEN 1 AND 9");

            t.HasCheckConstraint(
                "CK_env_environments_criticality",
                "\"Criticality\" BETWEEN 1 AND 4");

            t.HasCheckConstraint(
                "CK_env_environments_sort_order",
                "\"SortOrder\" >= 0");
        });

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
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200);

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(200);

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Profile & Criticality
        builder.Property(x => x.Profile)
            .HasConversion<int>()
            .HasDefaultValue(EnvironmentProfile.Development)
            .IsRequired();

        builder.Property(x => x.Criticality)
            .HasConversion<int>()
            .HasDefaultValue(EnvironmentCriticality.Low)
            .IsRequired();

        builder.Property(x => x.IsProductionLike)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsPrimaryProduction)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.Description)
            .IsRequired(false);

        builder.Property(x => x.Region)
            .HasMaxLength(100)
            .IsRequired(false);

        // Índice único para slug dentro do tenant
        builder.HasIndex(x => new { x.TenantId, x.Slug })
            .IsUnique()
            .HasDatabaseName("IX_env_environments_tenant_slug");

        // Garante que somente um ambiente ativo com IsPrimaryProduction=true pode existir por tenant.
        builder.HasIndex(x => new { x.TenantId, x.IsPrimaryProduction })
            .IsUnique()
            .HasFilter("\"IsPrimaryProduction\" = true AND \"IsActive\" = true AND \"IsDeleted\" = false")
            .HasDatabaseName("IX_env_environments_tenant_primary_production_unique");

        // Índice filtrado para ambientes ativos não-removidos
        builder.HasIndex(x => new { x.TenantId, x.IsActive })
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_env_environments_tenant_active");

        // Índice por profile para consultas filtradas
        builder.HasIndex(x => new { x.TenantId, x.Profile })
            .HasDatabaseName("IX_env_environments_tenant_profile");

        // Índice por criticality para consultas filtradas
        builder.HasIndex(x => new { x.TenantId, x.Criticality })
            .HasDatabaseName("IX_env_environments_tenant_criticality");

        // Soft-delete filter
        builder.HasIndex(x => x.IsDeleted)
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_env_environments_not_deleted");

        // Query filter para soft-delete
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade FeatureFlagEntry.
/// Define mapeamento de tabela, typed ID, enums, FK para definição, constraints, índices e concorrência otimista.
/// </summary>
internal sealed class FeatureFlagEntryConfiguration : IEntityTypeConfiguration<FeatureFlagEntry>
{
    public void Configure(EntityTypeBuilder<FeatureFlagEntry> builder)
    {
        builder.ToTable("cfg_feature_flag_entries", t =>
        {
            t.HasCheckConstraint(
                "CK_cfg_feature_flag_entries_scope",
                "\"Scope\" IN ('System', 'Tenant', 'Environment', 'Role', 'Team', 'User')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FeatureFlagEntryId(value));

        builder.Property(x => x.DefinitionId)
            .HasConversion(id => id.Value, value => new FeatureFlagDefinitionId(value))
            .IsRequired();

        builder.Property(x => x.Key)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ScopeReferenceId)
            .HasMaxLength(256);

        builder.Property(x => x.IsEnabled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.ChangeReason)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // FK: FeatureFlagEntry → FeatureFlagDefinition
        builder.HasOne<FeatureFlagDefinition>()
            .WithMany()
            .HasForeignKey(x => x.DefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unicidade: apenas uma entrada por flag/âmbito/referência
        builder.HasIndex(x => new { x.Key, x.Scope, x.ScopeReferenceId }).IsUnique();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Key);
        builder.HasIndex(x => x.DefinitionId);
        builder.HasIndex(x => x.IsActive).HasFilter("\"IsActive\" = true");
        builder.HasIndex(x => x.Scope);
    }
}

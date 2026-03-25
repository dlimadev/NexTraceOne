using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ConfigurationEntry.
/// Define mapeamento de tabela, typed ID, enums, FK, constraints, índices e concorrência otimista.
/// </summary>
internal sealed class ConfigurationEntryConfiguration : IEntityTypeConfiguration<ConfigurationEntry>
{
    public void Configure(EntityTypeBuilder<ConfigurationEntry> builder)
    {
        builder.ToTable("cfg_entries", t =>
        {
            t.HasCheckConstraint(
                "CK_cfg_entries_scope",
                "scope IN ('System', 'Tenant', 'Environment', 'Role', 'Team', 'User')");

            t.HasCheckConstraint(
                "CK_cfg_entries_version_positive",
                "version >= 1");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ConfigurationEntryId(value));

        builder.Property(x => x.DefinitionId)
            .HasConversion(id => id.Value, value => new ConfigurationDefinitionId(value))
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

        builder.Property(x => x.Value)
            .HasMaxLength(4000);

        builder.Property(x => x.StructuredValueJson)
            .HasMaxLength(8000);

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

        builder.Property(x => x.EffectiveFrom)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.EffectiveTo)
            .HasColumnType("timestamp with time zone");

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // FK: ConfigurationEntry → ConfigurationDefinition
        builder.HasOne<ConfigurationDefinition>()
            .WithMany()
            .HasForeignKey(x => x.DefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Key);
        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.DefinitionId);
        builder.HasIndex(x => x.IsActive).HasFilter("\"is_active\" = true");
        builder.HasIndex(x => new { x.Key, x.Scope, x.ScopeReferenceId }).IsUnique();
    }
}

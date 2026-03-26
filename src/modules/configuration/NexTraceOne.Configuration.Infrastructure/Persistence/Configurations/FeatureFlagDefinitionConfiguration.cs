using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade FeatureFlagDefinition.
/// Define mapeamento de tabela, typed ID, enums, FK para módulo, índices e concorrência otimista.
/// Esta entidade prepara o terreno para a resolução de feature flags por âmbito (P3.2).
/// </summary>
internal sealed class FeatureFlagDefinitionConfiguration : IEntityTypeConfiguration<FeatureFlagDefinition>
{
    public void Configure(EntityTypeBuilder<FeatureFlagDefinition> builder)
    {
        builder.ToTable("cfg_feature_flag_definitions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FeatureFlagDefinitionId(value));

        builder.Property(x => x.Key)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.DefaultEnabled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.AllowedScopes)
            .HasColumnType("text[]")
            .HasConversion(
                scopes => scopes.Select(s => s.ToString()).ToArray(),
                values => values.Select(v => Enum.Parse<ConfigurationScope>(v)).ToArray());

        builder.Property(x => x.ModuleId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new ConfigurationModuleId(value.Value));

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsEditable)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // FK: FeatureFlagDefinition → ConfigurationModule (opcional)
        builder.HasOne<ConfigurationModule>()
            .WithMany()
            .HasForeignKey(x => x.ModuleId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.ModuleId);
        builder.HasIndex(x => x.IsActive).HasFilter("\"is_active\" = true");
    }
}

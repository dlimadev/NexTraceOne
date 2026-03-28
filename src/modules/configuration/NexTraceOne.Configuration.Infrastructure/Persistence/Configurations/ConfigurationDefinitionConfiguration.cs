using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ConfigurationDefinition.
/// Define mapeamento de tabela, typed ID, enums, constraints, índices e concorrência otimista.
/// </summary>
internal sealed class ConfigurationDefinitionConfiguration : IEntityTypeConfiguration<ConfigurationDefinition>
{
    public void Configure(EntityTypeBuilder<ConfigurationDefinition> builder)
    {
        builder.ToTable("cfg_definitions", t =>
        {
            t.HasCheckConstraint(
                "CK_cfg_definitions_category",
                "\"Category\" IN ('Bootstrap', 'SensitiveOperational', 'Functional')");

            t.HasCheckConstraint(
                "CK_cfg_definitions_value_type",
                "\"ValueType\" IN ('String', 'Integer', 'Decimal', 'Boolean', 'Json', 'StringList')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ConfigurationDefinitionId(value));

        builder.Property(x => x.Key)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.PrimitiveCollection(x => x.AllowedScopes)
            .HasColumnType("text[]")
            .ElementType()
            .HasConversion<string>();

        builder.Property(x => x.DefaultValue)
            .HasMaxLength(4000);

        builder.Property(x => x.ValueType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ValidationRules)
            .HasMaxLength(4000);

        builder.Property(x => x.UiEditorType)
            .HasMaxLength(100);

        builder.Property(x => x.IsDeprecated)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.DeprecatedMessage)
            .HasMaxLength(500);

        builder.Property(x => x.ModuleId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new ConfigurationModuleId(value.Value));

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // FK: ConfigurationDefinition → ConfigurationModule (opcional)
        builder.HasOne<ConfigurationModule>()
            .WithMany()
            .HasForeignKey(x => x.ModuleId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.SortOrder);
        builder.HasIndex(x => x.ModuleId);
    }
}

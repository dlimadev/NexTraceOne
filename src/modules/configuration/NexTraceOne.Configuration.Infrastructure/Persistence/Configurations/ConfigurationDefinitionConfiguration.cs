using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ConfigurationDefinition.
/// Define mapeamento de tabela, typed ID, enums e índices.
/// </summary>
internal sealed class ConfigurationDefinitionConfiguration : IEntityTypeConfiguration<ConfigurationDefinition>
{
    public void Configure(EntityTypeBuilder<ConfigurationDefinition> builder)
    {
        builder.ToTable("cfg_definitions");

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

        builder.Property(x => x.AllowedScopes)
            .HasColumnType("text[]")
            .HasConversion(
                scopes => scopes.Select(s => s.ToString()).ToArray(),
                values => values.Select(v => Enum.Parse<ConfigurationScope>(v)).ToArray());

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

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.Category);
    }
}

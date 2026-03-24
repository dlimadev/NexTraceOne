using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ConfigurationEntry.
/// Define mapeamento de tabela, typed ID, enums e índices.
/// </summary>
internal sealed class ConfigurationEntryConfiguration : IEntityTypeConfiguration<ConfigurationEntry>
{
    public void Configure(EntityTypeBuilder<ConfigurationEntry> builder)
    {
        builder.ToTable("cfg_entries");

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

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Key);
        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => new { x.Key, x.Scope, x.ScopeReferenceId }).IsUnique();
    }
}

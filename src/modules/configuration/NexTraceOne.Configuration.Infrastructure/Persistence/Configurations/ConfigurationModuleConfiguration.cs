using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ConfigurationModule.
/// Define mapeamento de tabela, typed ID, constraints, índices e concorrência otimista.
/// Torna explícita a dimensão "módulo" na hierarquia Instance → Tenant → Environment → Module.
/// </summary>
internal sealed class ConfigurationModuleConfiguration : IEntityTypeConfiguration<ConfigurationModule>
{
    public void Configure(EntityTypeBuilder<ConfigurationModule> builder)
    {
        builder.ToTable("cfg_modules");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ConfigurationModuleId(value));

        builder.Property(x => x.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.SortOrder)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.IsActive)
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

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.IsActive).HasFilter("\"is_active\" = true");
        builder.HasIndex(x => x.SortOrder);
    }
}

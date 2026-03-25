using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade GovernancePack.
/// Define mapeamento de tabela, typed ID, enums, concorrência otimista e índices.
/// </summary>
internal sealed class GovernancePackConfiguration : IEntityTypeConfiguration<GovernancePack>
{
    public void Configure(EntityTypeBuilder<GovernancePack> builder)
    {
        builder.ToTable("gov_packs", t =>
        {
            t.HasCheckConstraint(
                "CK_gov_packs_status",
                "\"Status\" IN ('Draft', 'Published', 'Deprecated', 'Archived')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GovernancePackId(value));

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CurrentVersion)
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Status);
    }
}

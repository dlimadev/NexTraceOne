using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade CostAttribution.
/// Define mapeamento de tabela, typed ID, enums, colunas JSONB, decimais, concorrência otimista e índices.
/// Prefixo gov_ — alinhado com a baseline do módulo Governance.
/// </summary>
internal sealed class CostAttributionConfiguration : IEntityTypeConfiguration<CostAttribution>
{
    public void Configure(EntityTypeBuilder<CostAttribution> builder)
    {
        builder.ToTable("gov_cost_attributions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new CostAttributionId(value));

        builder.Property(x => x.Dimension)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DimensionKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DimensionLabel)
            .HasMaxLength(300);

        builder.Property(x => x.PeriodStart)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.PeriodEnd)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Cost columns — decimal(18,4) for precision
        builder.Property(x => x.TotalCost).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.ComputeCost).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.StorageCost).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.NetworkCost).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.OtherCost).HasColumnType("numeric(18,4)").IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(10)
            .IsRequired();

        // JSONB columns
        builder.Property(x => x.CostBreakdown).HasColumnType("jsonb");
        builder.Property(x => x.DataSources).HasColumnType("jsonb");

        builder.Property(x => x.AttributionMethod)
            .HasMaxLength(100);

        builder.Property(x => x.ComputedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        // Optimistic concurrency via PostgreSQL xmin
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Indexes for common queries
        builder.HasIndex(x => x.Dimension);
        builder.HasIndex(x => x.DimensionKey);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.PeriodStart);
        builder.HasIndex(x => x.PeriodEnd);
        builder.HasIndex(x => x.TotalCost);
        builder.HasIndex(x => new { x.Dimension, x.PeriodStart, x.PeriodEnd });
    }
}

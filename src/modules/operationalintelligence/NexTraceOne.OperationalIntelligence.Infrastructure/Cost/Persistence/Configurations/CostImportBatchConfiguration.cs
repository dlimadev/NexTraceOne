using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="CostImportBatch"/>.</summary>
internal sealed class CostImportBatchConfiguration : IEntityTypeConfiguration<CostImportBatch>
{
    public void Configure(EntityTypeBuilder<CostImportBatch> builder)
    {
        builder.ToTable("oi_cost_import_batches");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CostImportBatchId.From(value));

        builder.Property(x => x.Source).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Period).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(4000);
        builder.Property(x => x.RecordCount).IsRequired();
        builder.Property(x => x.ImportedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.Source, x.Period });
        builder.HasIndex(x => x.Status);
    }
}

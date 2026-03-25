using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="CostRecord"/>.</summary>
internal sealed class CostRecordConfiguration : IEntityTypeConfiguration<CostRecord>
{
    public void Configure(EntityTypeBuilder<CostRecord> builder)
    {
        builder.ToTable("ops_cost_records");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CostRecordId.From(value));

        builder.Property(x => x.BatchId).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Team).HasMaxLength(200);
        builder.Property(x => x.Domain).HasMaxLength(200);
        builder.Property(x => x.Environment).HasMaxLength(100);
        builder.Property(x => x.Period).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TotalCost).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RecordedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.BatchId);
        builder.HasIndex(x => new { x.ServiceId, x.Period });
        builder.HasIndex(x => x.Period);
        builder.HasIndex(x => x.Team);
        builder.HasIndex(x => x.Domain);
    }
}

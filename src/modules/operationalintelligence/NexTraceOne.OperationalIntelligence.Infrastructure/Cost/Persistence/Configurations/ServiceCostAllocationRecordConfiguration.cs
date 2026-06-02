using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="ServiceCostAllocationRecord"/>.</summary>
internal sealed class ServiceCostAllocationRecordConfiguration : IEntityTypeConfiguration<ServiceCostAllocationRecord>
{
    public void Configure(EntityTypeBuilder<ServiceCostAllocationRecord> builder)
    {
        builder.ToTable("ops_service_cost_allocations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ServiceCostAllocationRecordId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TeamId).HasMaxLength(100);
        builder.Property(x => x.DomainName).HasMaxLength(200);
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(100);
        builder.Property(x => x.TagsJson).HasMaxLength(2000);

        builder.Property(x => x.AmountUsd).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.OriginalAmount).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.Category).IsRequired();
        builder.Property(x => x.PeriodStart).IsRequired();
        builder.Property(x => x.PeriodEnd).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ServiceName, x.PeriodStart, x.PeriodEnd });
        builder.HasIndex(x => new { x.TenantId, x.PeriodStart, x.PeriodEnd });
    }
}

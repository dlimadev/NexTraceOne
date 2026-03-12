using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Configurations;

internal sealed class UsageQuotaConfiguration : IEntityTypeConfiguration<UsageQuota>
{
    public void Configure(EntityTypeBuilder<UsageQuota> builder)
    {
        builder.ToTable("licensing_usage_quotas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => UsageQuotaId.From(value));
        builder.Property(x => x.MetricCode).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Limit).IsRequired();
        builder.Property(x => x.CurrentUsage).IsRequired();
        builder.Property(x => x.AlertThresholdPercentage).HasPrecision(5, 4).IsRequired();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Configurations;

internal sealed class RateLimitPolicyConfiguration : IEntityTypeConfiguration<RateLimitPolicy>
{
    public void Configure(EntityTypeBuilder<RateLimitPolicy> builder)
    {
        builder.ToTable("cat_portal_rate_limit_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RateLimitPolicyId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.RequestsPerMinute).IsRequired();
        builder.Property(x => x.RequestsPerHour).IsRequired();
        builder.Property(x => x.RequestsPerDay).IsRequired();
        builder.Property(x => x.BurstLimit).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.ApiAssetId).IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Configurations;

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("cat_portal_api_keys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ApiKeyId.From(value));

        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.ApiAssetId);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.KeyPrefix).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.LastUsedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevokedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevokedByUserId).HasMaxLength(500);
        builder.Property(x => x.RequestCount).HasDefaultValue(0L).IsRequired();

        builder.HasIndex(x => x.KeyHash).IsUnique();
        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.ApiAssetId);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

internal sealed class PlatformApiTokenConfiguration : IEntityTypeConfiguration<PlatformApiToken>
{
    public void Configure(EntityTypeBuilder<PlatformApiToken> builder)
    {
        builder.ToTable("iam_platform_api_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => PlatformApiTokenId.From(v))
            .HasColumnType("uuid");
        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TokenPrefix).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Scope).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevokedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevokedReason).HasMaxLength(500);
        builder.Property(x => x.LastUsedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_iam_platform_tokens_tenant");
        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("uix_iam_platform_tokens_hash");
    }
}

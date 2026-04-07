using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class UserBookmarkConfiguration : IEntityTypeConfiguration<UserBookmark>
{
    public void Configure(EntityTypeBuilder<UserBookmark> builder)
    {
        builder.ToTable("cfg_user_bookmarks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new UserBookmarkId(value));
        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EntityType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.UserId, x.TenantId, x.EntityType, x.EntityId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.TenantId });
        builder.HasIndex(x => x.UserId);
    }
}

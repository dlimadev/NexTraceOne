using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class UserSavedViewConfiguration : IEntityTypeConfiguration<UserSavedView>
{
    public void Configure(EntityTypeBuilder<UserSavedView> builder)
    {
        builder.ToTable("cfg_user_saved_views");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new UserSavedViewId(value));
        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Context).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.FiltersJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsShared).HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.UserId, x.TenantId, x.Context, x.Name }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Context });
        builder.HasIndex(x => x.IsShared).HasFilter("\"IsShared\" = true");
        builder.HasIndex(x => x.UserId);
    }
}

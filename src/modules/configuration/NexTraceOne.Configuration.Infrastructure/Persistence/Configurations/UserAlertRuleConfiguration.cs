using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class UserAlertRuleConfiguration : IEntityTypeConfiguration<UserAlertRule>
{
    public void Configure(EntityTypeBuilder<UserAlertRule> builder)
    {
        builder.ToTable("cfg_user_alert_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new UserAlertRuleId(value));
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Condition).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Channel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.UserId, x.TenantId });
    }
}

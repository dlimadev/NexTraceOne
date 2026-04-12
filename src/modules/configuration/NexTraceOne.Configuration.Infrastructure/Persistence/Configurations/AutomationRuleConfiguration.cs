using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    public void Configure(EntityTypeBuilder<AutomationRule> builder)
    {
        builder.ToTable("cfg_automation_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AutomationRuleId(value));
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Trigger).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ConditionsJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.ActionsJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.RuleCreatedBy).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Trigger });
    }
}

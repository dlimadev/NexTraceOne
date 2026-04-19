using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

public sealed class SelfHealingActionConfiguration : IEntityTypeConfiguration<SelfHealingAction>
{
    public void Configure(EntityTypeBuilder<SelfHealingAction> builder)
    {
        builder.ToTable("aik_self_healing_actions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => SelfHealingActionId.From(value));

        builder.Property(e => e.IncidentId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ServiceName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.ActionType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ActionDescription).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.RiskLevel).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ApprovedBy).HasMaxLength(200);
        builder.Property(e => e.Result).HasMaxLength(2000);
        builder.Property(e => e.AuditTrailJson).HasColumnType("text");

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.IncidentId, e.TenantId });
        builder.HasIndex(e => e.Status);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

public sealed class GuardianAlertConfiguration : IEntityTypeConfiguration<GuardianAlert>
{
    public void Configure(EntityTypeBuilder<GuardianAlert> builder)
    {
        builder.ToTable("aik_guardian_alerts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => GuardianAlertId.From(value));

        builder.Property(e => e.ServiceName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.PatternDetected).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Recommendation).HasMaxLength(2000);
        builder.Property(e => e.Severity).HasMaxLength(50);
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.AcknowledgedBy).HasMaxLength(200);
        builder.Property(e => e.DismissReason).HasMaxLength(1000);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.ServiceName, e.TenantId });

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}

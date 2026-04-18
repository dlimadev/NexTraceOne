using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

internal sealed class FinOpsBudgetApprovalConfiguration : IEntityTypeConfiguration<FinOpsBudgetApproval>
{
    public void Configure(EntityTypeBuilder<FinOpsBudgetApproval> builder)
    {
        builder.ToTable("gov_finops_budget_approvals");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FinOpsBudgetApprovalId(value));

        builder.Property(x => x.ReleaseId).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();

        builder.Property(x => x.ActualCost).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.BaselineCost).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.CostDeltaPct).HasColumnType("numeric(10,4)").IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();

        builder.Property(x => x.RequestedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Justification).HasMaxLength(4000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ResolvedBy).HasMaxLength(500);
        builder.Property(x => x.Comment).HasMaxLength(4000);

        builder.Property(x => x.RequestedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamp with time zone");

        // Optimistic concurrency via PostgreSQL xmin
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Indexes
        builder.HasIndex(x => x.ReleaseId);
        builder.HasIndex(x => x.ServiceName);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequestedAt);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AiTokenUsageLedgerConfiguration : IEntityTypeConfiguration<AiTokenUsageLedger>
{
    public void Configure(EntityTypeBuilder<AiTokenUsageLedger> builder)
    {
        builder.ToTable("aik_token_usage_ledger");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiTokenUsageLedgerId.From(value));

        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ProviderId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ModelId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.PolicyName).HasMaxLength(200);
        builder.Property(x => x.BlockReason).HasMaxLength(1000);
        builder.Property(x => x.RequestId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ExecutionId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Timestamp).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(100).IsRequired();

        // Phase 4: FinOps cost attribution
        builder.Property(x => x.CostPerInputToken).HasColumnType("numeric(18,12)");
        builder.Property(x => x.CostPerOutputToken).HasColumnType("numeric(18,12)");
        builder.Property(x => x.EstimatedCostUsd).HasColumnType("numeric(18,8)");
        builder.Property(x => x.CostCurrency).HasMaxLength(10);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.Status);
    }
}

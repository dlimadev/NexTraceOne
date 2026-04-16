using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

internal sealed class ReleaseApprovalRequestConfiguration : IEntityTypeConfiguration<ReleaseApprovalRequest>
{
    /// <summary>Configura o mapeamento da entidade ReleaseApprovalRequest para a tabela chg_approval_requests.</summary>
    public void Configure(EntityTypeBuilder<ReleaseApprovalRequest> builder)
    {
        builder.ToTable("chg_approval_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ReleaseApprovalRequestId.From(value));

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.ApprovalType).HasColumnType("integer").IsRequired();
        builder.Property(x => x.ExternalSystem).HasMaxLength(200);
        builder.Property(x => x.ExternalRequestId).HasMaxLength(500);
        builder.Property(x => x.CallbackTokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CallbackTokenExpiresAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired()
            .HasDefaultValue(ApprovalRequestStatus.Pending);
        builder.Property(x => x.TargetEnvironment).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OutboundWebhookUrl).HasMaxLength(2000);
        builder.Property(x => x.RequestedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RespondedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RespondedBy).HasMaxLength(500);
        builder.Property(x => x.Comments).HasMaxLength(2000);

        builder.HasIndex(x => x.ReleaseId).HasDatabaseName("ix_chg_approval_requests_release_id");
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_chg_approval_requests_tenant_id");
        // índice para lookup por callback token hash (path de callback inbound)
        builder.HasIndex(x => x.CallbackTokenHash)
            .IsUnique()
            .HasDatabaseName("uix_chg_approval_requests_token_hash");
    }
}

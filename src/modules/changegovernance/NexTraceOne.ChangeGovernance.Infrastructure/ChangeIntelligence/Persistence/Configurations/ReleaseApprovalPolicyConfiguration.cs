using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

internal sealed class ReleaseApprovalPolicyConfiguration : IEntityTypeConfiguration<ReleaseApprovalPolicy>
{
    /// <summary>Configura o mapeamento da entidade ReleaseApprovalPolicy para a tabela chg_release_approval_policies.</summary>
    public void Configure(EntityTypeBuilder<ReleaseApprovalPolicy> builder)
    {
        builder.ToTable("chg_release_approval_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ReleaseApprovalPolicyId.From(value));

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.EnvironmentId).HasMaxLength(200);
        builder.Property(x => x.ServiceId);
        builder.Property(x => x.ServiceTag).HasMaxLength(200);
        builder.Property(x => x.RequiresApproval).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.ApprovalType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ExternalWebhookUrl).HasMaxLength(2000);
        builder.Property(x => x.MinApprovers).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.ApproverGroupsJson).HasMaxLength(4000).IsRequired().HasDefaultValue("[]");
        builder.Property(x => x.BypassRolesJson).HasMaxLength(4000).IsRequired().HasDefaultValue("[]");
        builder.Property(x => x.ExpirationHours).IsRequired().HasDefaultValue(48);
        builder.Property(x => x.RequireEvidencePack).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RequireChecklistCompletion).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.MinRiskScoreForManualApproval);
        builder.Property(x => x.BlockedTimeWindowsJson).HasMaxLength(4000).IsRequired().HasDefaultValue("[]");
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.Priority).IsRequired().HasDefaultValue(100);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedBy).HasMaxLength(500);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_chg_approval_policies_tenant_id");
        builder.HasIndex(x => new { x.TenantId, x.IsActive }).HasDatabaseName("ix_chg_approval_policies_tenant_active");
        builder.HasIndex(x => new { x.EnvironmentId, x.ServiceId, x.TenantId })
            .HasDatabaseName("ix_chg_approval_policies_env_service_tenant");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Configurations;

internal sealed class ComplianceResultConfiguration : IEntityTypeConfiguration<ComplianceResult>
{
    /// <summary>Configura o mapeamento da entidade ComplianceResult para a tabela aud_compliance_results.</summary>
    public void Configure(EntityTypeBuilder<ComplianceResult> builder)
    {
        builder.ToTable("aud_compliance_results", t =>
        {
            t.HasCheckConstraint("CK_aud_compliance_results_outcome",
                "\"Outcome\" IN ('Compliant','NonCompliant','PartiallyCompliant','NotApplicable')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ComplianceResultId.From(value));

        builder.Property(x => x.PolicyId)
            .HasConversion(id => id.Value, value => CompliancePolicyId.From(value))
            .IsRequired();

        builder.Property(x => x.CampaignId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? AuditCampaignId.From(value.Value) : null);

        builder.Property(x => x.ResourceType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ResourceId).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Details).HasColumnType("text");
        builder.Property(x => x.EvaluatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EvaluatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TenantId).IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.PolicyId);
        builder.HasIndex(x => x.CampaignId);
        builder.HasIndex(x => x.Outcome);
        builder.HasIndex(x => x.EvaluatedAt);
    }
}

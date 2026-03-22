using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Configurations;

internal sealed class CompliancePolicyConfiguration : IEntityTypeConfiguration<CompliancePolicy>
{
    /// <summary>Configura o mapeamento da entidade CompliancePolicy para a tabela aud_compliance_policies.</summary>
    public void Configure(EntityTypeBuilder<CompliancePolicy> builder)
    {
        builder.ToTable("aud_compliance_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CompliancePolicyId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text");
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.EvaluationCriteria).HasColumnType("text");
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Severity);
    }
}

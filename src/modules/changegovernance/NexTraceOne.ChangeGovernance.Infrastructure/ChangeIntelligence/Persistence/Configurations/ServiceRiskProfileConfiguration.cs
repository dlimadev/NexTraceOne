using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade ServiceRiskProfile.
/// Tabela: chg_service_risk_profiles
/// Wave F.2 — Risk Center.
/// </summary>
internal sealed class ServiceRiskProfileConfiguration : IEntityTypeConfiguration<ServiceRiskProfile>
{
    /// <summary>Configura o mapeamento da entidade ServiceRiskProfile.</summary>
    public void Configure(EntityTypeBuilder<ServiceRiskProfile> builder)
    {
        builder.ToTable("chg_service_risk_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ServiceRiskProfileId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ServiceAssetId).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.OverallRiskLevel).HasConversion<int>().IsRequired();
        builder.Property(x => x.OverallScore).IsRequired();
        builder.Property(x => x.VulnerabilityScore).IsRequired();
        builder.Property(x => x.ChangeFailureScore).IsRequired();
        builder.Property(x => x.BlastRadiusScore).IsRequired();
        builder.Property(x => x.PolicyViolationScore).IsRequired();
        builder.Property(x => x.ActiveSignalsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.ActiveSignalCount).IsRequired();
        builder.Property(x => x.ComputedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => new { x.TenantId, x.ServiceAssetId, x.ComputedAt })
            .HasDatabaseName("ix_chg_risk_profiles_tenant_service_computed");

        builder.HasIndex(x => new { x.TenantId, x.OverallRiskLevel })
            .HasDatabaseName("ix_chg_risk_profiles_tenant_risk_level");

        builder.HasIndex(x => new { x.TenantId, x.OverallScore })
            .HasDatabaseName("ix_chg_risk_profiles_tenant_score");
    }
}

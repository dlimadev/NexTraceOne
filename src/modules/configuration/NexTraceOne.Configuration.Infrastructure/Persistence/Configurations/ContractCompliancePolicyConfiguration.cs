using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ContractCompliancePolicy.
/// Prefixo cfg_ — alinhado com a baseline do módulo Configuration.
/// </summary>
internal sealed class ContractCompliancePolicyConfiguration : IEntityTypeConfiguration<ContractCompliancePolicy>
{
    public void Configure(EntityTypeBuilder<ContractCompliancePolicy> builder)
    {
        builder.ToTable("cfg_contract_compliance_policies");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ContractCompliancePolicyId(value));

        builder.Property(x => x.TenantId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ScopeId)
            .HasMaxLength(200);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.VerificationMode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.VerificationApproach)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OnBreakingChange)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OnNonBreakingChange)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OnNewEndpoint)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OnRemovedEndpoint)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OnMissingContract)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OnContractNotApproved)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AutoGenerateChangelog)
            .IsRequired();

        builder.Property(x => x.ChangelogFormat)
            .IsRequired();

        builder.Property(x => x.RequireChangelogApproval)
            .IsRequired();

        builder.Property(x => x.EnforceCdct)
            .IsRequired();

        builder.Property(x => x.CdctFailureAction)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EnableRuntimeDriftDetection)
            .IsRequired();

        builder.Property(x => x.DriftDetectionIntervalMinutes)
            .IsRequired();

        builder.Property(x => x.DriftThresholdForAlert)
            .IsRequired();

        builder.Property(x => x.DriftThresholdForIncident)
            .IsRequired();

        builder.Property(x => x.NotifyOnVerificationFailure)
            .IsRequired();

        builder.Property(x => x.NotifyOnBreakingChange)
            .IsRequired();

        builder.Property(x => x.NotifyOnDriftDetected)
            .IsRequired();

        builder.Property(x => x.NotificationChannels)
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        // Indexes
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.TenantId, x.Scope, x.IsActive });
    }
}

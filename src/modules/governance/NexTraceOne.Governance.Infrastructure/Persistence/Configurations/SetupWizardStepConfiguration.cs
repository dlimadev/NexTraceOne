using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade SetupWizardStep.</summary>
internal sealed class SetupWizardStepConfiguration : IEntityTypeConfiguration<SetupWizardStep>
{
    public void Configure(EntityTypeBuilder<SetupWizardStep> builder)
    {
        builder.ToTable("gov_setup_wizard_steps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SetupWizardStepId(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StepId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DataJson).HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(new[] { "TenantId", "StepId" }).IsUnique()
            .HasDatabaseName("ix_gov_setup_wizard_tenant_step");
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_gov_setup_wizard_tenant");
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade PolicyAsCodeDefinition.
/// Persiste definições de política como código (YAML/JSON) com versionamento e gradual enforcement.
/// </summary>
internal sealed class PolicyAsCodeDefinitionConfiguration : IEntityTypeConfiguration<PolicyAsCodeDefinition>
{
    public void Configure(EntityTypeBuilder<PolicyAsCodeDefinition> builder)
    {
        builder.ToTable("gov_policy_as_code");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PolicyAsCodeDefinitionId(value));

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Version)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Format)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.DefinitionContent)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.EnforcementMode)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.SimulatedAffectedServices);
        builder.Property(x => x.SimulatedNonCompliantServices);

        builder.Property(x => x.LastSimulatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RegisteredBy)
            .HasMaxLength(200)
            .IsRequired();

        // Auditoria herdada de AuditableEntity (campos públicos, sem shadow properties)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedBy).HasMaxLength(200);
        builder.Property(x => x.UpdatedBy).HasMaxLength(200);

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.EnforcementMode);
        builder.HasIndex(x => x.TenantId);
    }
}

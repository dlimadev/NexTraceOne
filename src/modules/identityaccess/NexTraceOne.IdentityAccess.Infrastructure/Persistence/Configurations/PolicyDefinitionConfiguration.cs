using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade PolicyDefinition.
/// Tabela: iam_policy_definitions
/// </summary>
internal sealed class PolicyDefinitionConfiguration : IEntityTypeConfiguration<PolicyDefinition>
{
    /// <summary>Configura o mapeamento da entidade PolicyDefinition para o Policy Studio.</summary>
    public void Configure(EntityTypeBuilder<PolicyDefinition> builder)
    {
        builder.ToTable("iam_policy_definitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PolicyDefinitionId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.PolicyType).HasConversion<int>().IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.Version).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.RulesJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.ActionJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.AppliesTo).HasMaxLength(500).IsRequired();
        builder.Property(x => x.EnvironmentFilter).HasMaxLength(200);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(200);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => new { x.TenantId, x.PolicyType })
            .HasDatabaseName("ix_iam_policy_definitions_tenant_type");
        builder.HasIndex(x => x.IsEnabled)
            .HasDatabaseName("ix_iam_policy_definitions_enabled")
            .HasFilter("\"IsEnabled\" = true");
    }
}

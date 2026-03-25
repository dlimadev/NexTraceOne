using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Configurations;

internal sealed class PromotionGateConfiguration : IEntityTypeConfiguration<PromotionGate>
{
    /// <summary>Configura o mapeamento da entidade PromotionGate para a tabela prm_promotion_gates.</summary>
    public void Configure(EntityTypeBuilder<PromotionGate> builder)
    {
        builder.ToTable("chg_promotion_gates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PromotionGateId.From(value));

        builder.Property(x => x.DeploymentEnvironmentId)
            .HasConversion(id => id.Value, value => DeploymentEnvironmentId.From(value))
            .IsRequired();
        builder.Property(x => x.GateName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.GateType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.DeploymentEnvironmentId);
        builder.HasIndex(x => new { x.DeploymentEnvironmentId, x.GateName }).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}

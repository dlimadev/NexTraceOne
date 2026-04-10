using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configura o mapeamento da entidade PromotionGate para a tabela chg_promotion_gates.</summary>
internal sealed class PromotionGateConfiguration : IEntityTypeConfiguration<PromotionGate>
{
    public void Configure(EntityTypeBuilder<PromotionGate> builder)
    {
        builder.ToTable("chg_promotion_gates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PromotionGateId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.EnvironmentFrom).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EnvironmentTo).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Rules).HasColumnType("jsonb");
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.BlockOnFailure).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedBy).HasMaxLength(200);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(200);
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => new { x.EnvironmentFrom, x.EnvironmentTo });
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.TenantId);
    }
}

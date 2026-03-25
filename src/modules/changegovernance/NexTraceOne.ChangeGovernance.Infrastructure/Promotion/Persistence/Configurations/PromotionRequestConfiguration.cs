using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Configurations;

internal sealed class PromotionRequestConfiguration : IEntityTypeConfiguration<PromotionRequest>
{
    /// <summary>Configura o mapeamento da entidade PromotionRequest para a tabela prm_promotion_requests.</summary>
    public void Configure(EntityTypeBuilder<PromotionRequest> builder)
    {
        builder.ToTable("chg_promotion_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PromotionRequestId.From(value));

        builder.Property(x => x.ReleaseId).IsRequired();
        builder.Property(x => x.SourceEnvironmentId)
            .HasConversion(id => id.Value, value => DeploymentEnvironmentId.From(value))
            .IsRequired();
        builder.Property(x => x.TargetEnvironmentId)
            .HasConversion(id => id.Value, value => DeploymentEnvironmentId.From(value))
            .IsRequired();
        builder.Property(x => x.RequestedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.Justification).HasMaxLength(4000);
        builder.Property(x => x.RequestedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.ReleaseId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TargetEnvironmentId);
        builder.HasIndex(x => x.RequestedAt);

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

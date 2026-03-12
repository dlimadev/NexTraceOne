using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Promotion.Domain.Entities;
using NexTraceOne.Promotion.Domain.Enums;

namespace NexTraceOne.Promotion.Infrastructure.Persistence.Configurations;

internal sealed class DeploymentEnvironmentConfiguration : IEntityTypeConfiguration<DeploymentEnvironment>
{
    /// <summary>Configura o mapeamento da entidade DeploymentEnvironment para a tabela prm_deployment_environments.</summary>
    public void Configure(EntityTypeBuilder<DeploymentEnvironment> builder)
    {
        builder.ToTable("prm_deployment_environments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DeploymentEnvironmentId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.RequiresApproval).IsRequired();
        builder.Property(x => x.RequiresEvidencePack).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Order);
    }
}

internal sealed class PromotionRequestConfiguration : IEntityTypeConfiguration<PromotionRequest>
{
    /// <summary>Configura o mapeamento da entidade PromotionRequest para a tabela prm_promotion_requests.</summary>
    public void Configure(EntityTypeBuilder<PromotionRequest> builder)
    {
        builder.ToTable("prm_promotion_requests");
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
    }
}

internal sealed class PromotionGateConfiguration : IEntityTypeConfiguration<PromotionGate>
{
    /// <summary>Configura o mapeamento da entidade PromotionGate para a tabela prm_promotion_gates.</summary>
    public void Configure(EntityTypeBuilder<PromotionGate> builder)
    {
        builder.ToTable("prm_promotion_gates");
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

internal sealed class GateEvaluationConfiguration : IEntityTypeConfiguration<GateEvaluation>
{
    /// <summary>Configura o mapeamento da entidade GateEvaluation para a tabela prm_gate_evaluations.</summary>
    public void Configure(EntityTypeBuilder<GateEvaluation> builder)
    {
        builder.ToTable("prm_gate_evaluations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => GateEvaluationId.From(value));

        builder.Property(x => x.PromotionRequestId)
            .HasConversion(id => id.Value, value => PromotionRequestId.From(value))
            .IsRequired();
        builder.Property(x => x.PromotionGateId)
            .HasConversion(id => id.Value, value => PromotionGateId.From(value))
            .IsRequired();
        builder.Property(x => x.Passed).IsRequired();
        builder.Property(x => x.EvaluatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.EvaluationDetails).HasMaxLength(4000);
        builder.Property(x => x.OverrideJustification).HasMaxLength(4000);
        builder.Property(x => x.EvaluatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.PromotionRequestId);
        builder.HasIndex(x => x.PromotionGateId);
        builder.HasIndex(x => new { x.PromotionRequestId, x.PromotionGateId });
    }
}

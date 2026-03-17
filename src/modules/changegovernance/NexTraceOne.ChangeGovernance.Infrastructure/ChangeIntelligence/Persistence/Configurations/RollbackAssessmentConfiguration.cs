using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade RollbackAssessment.</summary>
internal sealed class RollbackAssessmentConfiguration : IEntityTypeConfiguration<RollbackAssessment>
{
    /// <summary>Configura o mapeamento da entidade RollbackAssessment para a tabela ci_rollback_assessments.</summary>
    public void Configure(EntityTypeBuilder<RollbackAssessment> builder)
    {
        builder.ToTable("ci_rollback_assessments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RollbackAssessmentId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.IsViable).IsRequired();
        builder.Property(x => x.ReadinessScore).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.PreviousVersion).HasMaxLength(50);
        builder.Property(x => x.HasReversibleMigrations).IsRequired();
        builder.Property(x => x.ConsumersAlreadyMigrated).IsRequired();
        builder.Property(x => x.TotalConsumersImpacted).IsRequired();
        builder.Property(x => x.InviabilityReason).HasMaxLength(2000);
        builder.Property(x => x.Recommendation).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AssessedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.ReleaseId).IsUnique();
    }
}

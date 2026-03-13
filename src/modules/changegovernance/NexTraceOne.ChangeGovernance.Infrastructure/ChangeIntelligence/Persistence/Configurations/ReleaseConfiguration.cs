using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    /// <summary>Configura o mapeamento da entidade Release para a tabela ci_releases.</summary>
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("ci_releases");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ReleaseId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PipelineSource).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CommitSha).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChangeLevel).HasColumnType("integer").IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired().HasDefaultValue(DeploymentStatus.Pending);
        builder.Property(x => x.ChangeScore)
            .HasColumnType("numeric(5,4)")
            .HasPrecision(5, 4)
            .IsRequired()
            .HasDefaultValue(0.0m);
        builder.Property(x => x.WorkItemReference).HasMaxLength(500);
        builder.Property(x => x.RolledBackFromReleaseId)
            .HasConversion(
                id => id != null ? (Guid?)id.Value : null,
                value => value.HasValue ? ReleaseId.From(value.Value) : null);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ApiAssetId);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade PostReleaseReview.</summary>
internal sealed class PostReleaseReviewConfiguration : IEntityTypeConfiguration<PostReleaseReview>
{
    /// <summary>Configura o mapeamento da entidade PostReleaseReview para a tabela ci_post_release_reviews.</summary>
    public void Configure(EntityTypeBuilder<PostReleaseReview> builder)
    {
        builder.ToTable("chg_post_release_reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PostReleaseReviewId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.CurrentPhase).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ConfidenceScore).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.IsCompleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.ReleaseId).IsUnique();
    }
}

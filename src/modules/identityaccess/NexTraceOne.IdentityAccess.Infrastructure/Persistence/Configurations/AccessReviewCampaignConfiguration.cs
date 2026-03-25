using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para AccessReviewCampaign.
/// Persiste campanhas de recertificação periódica de acessos para compliance.
/// A navegação para Items é configurada como owned collection via AccessReviewItemConfiguration.
/// </summary>
internal sealed class AccessReviewCampaignConfiguration : IEntityTypeConfiguration<AccessReviewCampaign>
{
    public void Configure(EntityTypeBuilder<AccessReviewCampaign> builder)
    {
        builder.ToTable("iam_access_review_campaigns", t =>
        {
            t.HasCheckConstraint("CK_iam_access_review_campaigns_Status",
                "\"Status\" IN ('Open', 'Completed')");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AccessReviewCampaignId.From(value));

        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.Deadline).IsRequired();
        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.InitiatedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.Status });

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

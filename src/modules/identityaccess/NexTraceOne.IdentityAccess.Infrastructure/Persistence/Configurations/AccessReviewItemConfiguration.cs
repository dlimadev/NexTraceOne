using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para AccessReviewItem.
/// Persiste itens individuais de revisão de acesso associados a uma campanha.
/// Cada item é a combinação de um usuário + role que um reviewer deve confirmar ou revogar.
/// </summary>
internal sealed class AccessReviewItemConfiguration : IEntityTypeConfiguration<AccessReviewItem>
{
    public void Configure(EntityTypeBuilder<AccessReviewItem> builder)
    {
        builder.ToTable("iam_access_review_items");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AccessReviewItemId.From(value));

        builder.Property(x => x.CampaignId)
            .HasConversion(id => id.Value, value => AccessReviewCampaignId.From(value))
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.RoleId)
            .HasConversion(id => id.Value, value => RoleId.From(value))
            .IsRequired();

        builder.Property(x => x.RoleName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ReviewerId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.Decision)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ReviewerComment).HasMaxLength(1000);
        builder.Property(x => x.DecidedAt);

        builder.HasIndex(x => x.CampaignId);
        builder.HasIndex(x => x.ReviewerId);
        builder.HasIndex(x => x.Decision);
    }
}

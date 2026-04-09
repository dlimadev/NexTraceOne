using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade NegotiationComment.
/// Comentários em negociações de contrato suportam revisão colaborativa inline.
/// </summary>
internal sealed class NegotiationCommentConfiguration : IEntityTypeConfiguration<NegotiationComment>
{
    public void Configure(EntityTypeBuilder<NegotiationComment> builder)
    {
        builder.ToTable("cat_negotiation_comments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => NegotiationCommentId.From(value));

        builder.Property(x => x.NegotiationId).IsRequired();

        builder.Property(x => x.AuthorId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.AuthorDisplayName).IsRequired().HasMaxLength(300);

        builder.Property(x => x.Content).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.LineReference).HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.NegotiationId);
        builder.HasIndex(x => x.AuthorId);
    }
}

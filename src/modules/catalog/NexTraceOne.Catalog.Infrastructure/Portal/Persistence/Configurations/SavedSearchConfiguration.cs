using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade SavedSearch.
/// Mapeia pesquisas salvas com índice por utilizador para consultas rápidas.
/// </summary>
internal sealed class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.ToTable("dp_saved_searches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SavedSearchId.From(value));

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SearchQuery).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Filters).HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.LastUsedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.UserId);
    }
}

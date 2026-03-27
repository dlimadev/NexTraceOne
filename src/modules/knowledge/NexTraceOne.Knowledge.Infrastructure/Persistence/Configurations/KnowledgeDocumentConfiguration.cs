using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade KnowledgeDocument.
/// Define mapeamento de tabela, typed ID, enums e índices.
/// Prefixo: knw_
/// </summary>
internal sealed class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.ToTable("knw_documents", t =>
        {
            t.HasCheckConstraint("CK_knw_documents_status",
                "\"Status\" IN ('Draft','Published','Archived','Deprecated')");
            t.HasCheckConstraint("CK_knw_documents_category",
                "\"Category\" IN ('General','Runbook','Troubleshooting','Architecture','Procedure','PostMortem','Reference')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new KnowledgeDocumentId(value));

        builder.Property(x => x.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(600)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasMaxLength(2000);

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Tags)
            .HasColumnType("jsonb")
            .HasConversion(
                tags => System.Text.Json.JsonSerializer.Serialize(tags, (System.Text.Json.JsonSerializerOptions?)null),
                json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .IsRequired();

        builder.Property(x => x.AuthorId)
            .IsRequired();

        builder.Property(x => x.LastEditorId);

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.PublishedAt)
            .HasColumnType("timestamp with time zone");

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AuthorId);
        builder.HasIndex(x => x.CreatedAt);

        // Concorrência otimista (PostgreSQL xmin)
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

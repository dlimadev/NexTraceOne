using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade OperationalNote.
/// Define mapeamento de tabela, typed ID, enums e índices.
/// Prefixo: knw_
/// </summary>
internal sealed class OperationalNoteConfiguration : IEntityTypeConfiguration<OperationalNote>
{
    public void Configure(EntityTypeBuilder<OperationalNote> builder)
    {
        builder.ToTable("knw_operational_notes", t =>
        {
            t.HasCheckConstraint("CK_knw_operational_notes_severity",
                "\"Severity\" IN ('Info','Warning','Critical')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new OperationalNoteId(value));

        builder.Property(x => x.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AuthorId)
            .IsRequired();

        builder.Property(x => x.ContextEntityId);

        builder.Property(x => x.ContextType)
            .HasMaxLength(100);

        builder.Property(x => x.Tags)
            .HasColumnType("jsonb")
            .HasConversion(
                tags => System.Text.Json.JsonSerializer.Serialize(tags, (System.Text.Json.JsonSerializerOptions?)null),
                json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .IsRequired();

        builder.Property(x => x.IsResolved)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ResolvedAt)
            .HasColumnType("timestamp with time zone");

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Severity);
        builder.HasIndex(x => x.AuthorId);
        builder.HasIndex(x => x.ContextEntityId);
        builder.HasIndex(x => x.ContextType);
        builder.HasIndex(x => x.IsResolved);
        builder.HasIndex(x => x.CreatedAt);

        // Concorrência otimista (PostgreSQL xmin)
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

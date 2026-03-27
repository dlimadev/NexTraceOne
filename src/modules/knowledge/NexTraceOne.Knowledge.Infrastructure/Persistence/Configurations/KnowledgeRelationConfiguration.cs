using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade KnowledgeRelation.
/// Define mapeamento de tabela, typed ID, enums e índices.
/// Prefixo: knw_
/// </summary>
internal sealed class KnowledgeRelationConfiguration : IEntityTypeConfiguration<KnowledgeRelation>
{
    public void Configure(EntityTypeBuilder<KnowledgeRelation> builder)
    {
        builder.ToTable("knw_relations", t =>
        {
            t.HasCheckConstraint("CK_knw_relations_target_type",
                "\"TargetType\" IN ('Service','Contract','Change','Incident','KnowledgeDocument','Runbook','Other')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new KnowledgeRelationId(value));

        builder.Property(x => x.SourceEntityId)
            .IsRequired();

        builder.Property(x => x.SourceEntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TargetEntityId)
            .IsRequired();

        builder.Property(x => x.TargetType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedById)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.SourceEntityId);
        builder.HasIndex(x => x.TargetEntityId);
        builder.HasIndex(x => x.TargetType);
        builder.HasIndex(x => new { x.SourceEntityId, x.TargetEntityId }).IsUnique();
    }
}

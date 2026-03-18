using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.Configurations;

internal sealed class KnowledgeCaptureConfiguration : IEntityTypeConfiguration<KnowledgeCapture>
{
    public void Configure(EntityTypeBuilder<KnowledgeCapture> builder)
    {
        builder.ToTable("ext_ai_knowledge_captures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => KnowledgeCaptureId.From(value));

        builder.Property(x => x.ConsultationId)
            .HasConversion(id => id.Value, value => ExternalAiConsultationId.From(value))
            .IsRequired();

        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Content).HasMaxLength(50000).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Tags).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.ReviewedBy).HasMaxLength(500);
        builder.Property(x => x.ReviewedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RejectionReason).HasMaxLength(2000);
        builder.Property(x => x.ReuseCount).IsRequired();
        builder.Property(x => x.CapturedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ConsultationId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.CapturedAt);
    }
}

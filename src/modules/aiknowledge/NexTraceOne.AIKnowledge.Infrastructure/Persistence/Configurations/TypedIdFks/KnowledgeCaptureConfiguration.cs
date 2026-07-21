using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class KnowledgeCaptureConfiguration : IEntityTypeConfiguration<KnowledgeCapture>
{
    public void Configure(EntityTypeBuilder<KnowledgeCapture> builder)
    {
        builder.Property(x => x.ConsultationId)
            .HasConversion(id => id.Value, value => new ExternalAiConsultationId(value));
        builder.HasIndex(x => x.ConsultationId);
    }
}

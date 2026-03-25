using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Configurations;

internal sealed class AiContextConfiguration : IEntityTypeConfiguration<AiContext>
{
    public void Configure(EntityTypeBuilder<AiContext> builder)
    {
        builder.ToTable("aik_contexts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiContextId.From(value));

        builder.Property(x => x.ServiceName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContextType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("text").IsRequired();
        builder.Property(x => x.TokenEstimate).IsRequired();
        builder.Property(x => x.ReleaseId);
        builder.Property(x => x.AssembledAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ServiceName);
        builder.HasIndex(x => x.ContextType);
        builder.HasIndex(x => x.AssembledAt);
    }
}
